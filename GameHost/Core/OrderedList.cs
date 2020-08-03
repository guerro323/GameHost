using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DryIoc;

namespace GameHost.Core
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public abstract class UpdateOrderBaseAttribute : Attribute
    {
        public Type[] Types;

        protected UpdateOrderBaseAttribute(Type[] t)
        {
            Types = t;
        }
    }

    public class UpdateAfterAttribute : UpdateOrderBaseAttribute
    {
        public UpdateAfterAttribute(params Type[] t) : base(t)
        {
        }
    }

    public class UpdateBeforeAttribute : UpdateOrderBaseAttribute
    {
        public UpdateBeforeAttribute(params Type[] t) : base(t)
        {
        }
    }

    public static class OrderedList
    {
        [ThreadStatic]
        private static List<Type> _TemporaryList;
        
        private static Type[] Get<T>(Type toCheck)
            where T : UpdateOrderBaseAttribute
        {
            _TemporaryList ??= new List<Type>(8);
            _TemporaryList.Clear();

            var tab = 0;
            while (toCheck != null)
            {
                foreach (var attribute in toCheck.GetAttributes(typeof(T), true))
                {
                    var attr = (T) attribute;
                    _TemporaryList.AddRange(attr.Types);

                   /* Console.WriteLine(string.Concat(Enumerable.Repeat("\t", tab)) + toCheck);
                    foreach (var t in attr.Types)
                        Console.WriteLine(string.Concat(Enumerable.Repeat("\t", tab)) + "\t" + t);*/
                }
                toCheck = toCheck.BaseType;
                tab++;
            }

            return _TemporaryList.ToArray();
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

        public class Element
        {
            public bool visited;
            public int LongestSystemsUpdatingAfterChain;
            public int LongestSystemsUpdatingBeforeChain;

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
