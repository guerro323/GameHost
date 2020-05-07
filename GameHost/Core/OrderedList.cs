using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DryIoc;

namespace GameHost.Core
{
    public abstract class UpdateOrderBaseAttribute : Attribute
    {
        public Type[] Types;

        public UpdateOrderBaseAttribute(params Type[] t)
        {
            Types = t;
        }
    }

    public class UpdateAfterAttribute : UpdateOrderBaseAttribute
    {
    }

    public class UpdateBeforeAttribute : UpdateOrderBaseAttribute
    {
    }

    public static class OrderedList
    {
        private static Type[] Get<T>(Type toCheck)
            where T : UpdateOrderBaseAttribute
        {
            var list = new List<Type>(8);
            foreach (var attribute in toCheck.GetAttributes(typeof(T), true))
            {
                var attr = (T)attribute;
                list.AddRange(attr.Types);
            }

            return list.ToArray();
        }

        public static Type[] GetAfter(Type toCheck)
        {
            return Get<UpdateAfterAttribute>(toCheck);
        }

        public static Type[] GetBefore(Type toCheck)
        {
            return Get<UpdateBeforeAttribute>(toCheck);
        }
    }

    public class OrderedList<T> : IEnumerable<T>
    {
        private readonly List<Element> dirtyElements = new List<Element>();

        public           bool    listIsDirty;
        private readonly List<T> orderedElements = new List<T>();

        public ReadOnlyCollection<T> Elements
        {
            get
            {
                if (!listIsDirty)
                    return orderedElements.AsReadOnly();

                orderedElements.Clear();
                foreach (var elem in dirtyElements)
                {
                    orderedElements.Add(elem.Value);
                }

                // the for loop here is done to reinforce the list order.
                for (var i = 0; i != 2; i++)
                {
                    foreach (var elem in dirtyElements.Where(e => e.UpdateBefore != null && e.UpdateBefore.Length != 0))
                    {
                        var index = orderedElements.IndexOf(elem.Value);
                        if (index < 0)
                        {
                            continue;
                        }

                        var min = index;
                        foreach (var condition in elem.UpdateBefore)
                        {
                            var conditionIndex = GetElementIndex(condition);
                            if (conditionIndex < 0 || conditionIndex > min)
                            {
                                continue;
                            }

                            min = conditionIndex;
                        }

                        if (min != index)
                        {
                            orderedElements.RemoveAt(index);
                            orderedElements.Insert(min, elem.Value);
                        }
                    }

                    foreach (var elem in dirtyElements.Where(e => e.UpdateAfter != null && e.UpdateAfter.Length != 0))
                    {
                        var index = orderedElements.IndexOf(elem.Value);
                        if (index < 0)
                        {
                            continue;
                        }

                        var max = index;
                        foreach (var condition in elem.UpdateAfter)
                        {
                            var conditionIndex = GetElementIndex(condition);
                            if (conditionIndex < 0 || conditionIndex < max)
                            {
                                continue;
                            }

                            max = conditionIndex;
                        }

                        if (max != index)
                        {
                            orderedElements.RemoveAt(index);
                            orderedElements.Insert(max, elem.Value);
                        }
                    }
                }

                OnOrderUpdate?.Invoke();

                return orderedElements.AsReadOnly();
            }
        }

        public event Action OnDirty;
        public event Action OnOrderUpdate;

        public void Set(T elem, Type[] updateAfter, Type[] updateBefore)
        {
            for (var i = 0; i != dirtyElements.Count; i++)
            {
                if (!dirtyElements[i].Value.Equals(elem))
                {
                    continue;
                }

                if (dirtyElements[i].UpdateAfter != updateAfter
                    || dirtyElements[i].UpdateBefore != updateBefore)
                {
                    listIsDirty      = true;
                    dirtyElements[i] = new Element {Value = elem, UpdateAfter = updateAfter, UpdateBefore = updateBefore};

                    OnDirty?.Invoke();
                }

                return;
            }

            Add(elem, updateAfter, updateBefore);
        }

        public void Add(T elem, Type[] updateAfter, Type[] updateBefore)
        {
            listIsDirty = true;
            dirtyElements.Add(new Element {Value = elem, UpdateAfter = updateAfter, UpdateBefore = updateBefore});

            OnDirty?.Invoke();
        }

        private int GetElementIndex(Type type)
        {
            for (var i = 0; i != orderedElements.Count; i++)
            {
                if (orderedElements[i].GetType() == type)
                {
                    return i;
                }
            }

            return -1;
        }

        private struct Element
        {
            public T      Value;
            public Type[] UpdateAfter;
            public Type[] UpdateBefore;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Elements.GetEnumerator();
        }
    }
}
