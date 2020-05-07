using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GameHost
{
    public interface IRenderPass
    {
        void OnRender();
    }
    
    public class RenderPassManager
    {
        private struct Element
        {
            public IRenderPass Pass;
            public Type[] UpdateAfter;
            public Type[] UpdateBefore;
        }

        private List<Element> dirtyElements = new List<Element>();
        private List<IRenderPass> renderPasses = new List<IRenderPass>();
        
        public bool listIsDirty;
        
        public void Add(IRenderPass pass, Type[] updateAfter, Type[] updateBefore)
        {
            listIsDirty = true;
            dirtyElements.Add(new Element
            {
                Pass = pass,
                UpdateAfter = updateAfter,
                UpdateBefore = updateBefore
            });
        }

        public ReadOnlyCollection<IRenderPass> RenderPasses
        {
            get
            {
                if (listIsDirty)
                {
                    renderPasses.Clear();
                    foreach (var elem in dirtyElements)
                        renderPasses.Add(elem.Pass);

                    for (var i = 0; i != 2; i++)
                    {
                        foreach (var elem in dirtyElements.Where(e => e.UpdateBefore != null && e.UpdateBefore.Length != 0))
                        {
                            var index = renderPasses.IndexOf(elem.Pass);
                            if (index < 0)
                                continue;

                            var min = index;
                            foreach (var condition in elem.UpdateBefore)
                            {
                                var conditionIndex = getPassIndex(condition);
                                if (conditionIndex < 0 || conditionIndex > min)
                                    continue;
                                min = conditionIndex;
                            }

                            if (min != index)
                            {
                                renderPasses.RemoveAt(index);
                                renderPasses.Insert(min, elem.Pass);
                            }
                        }

                        foreach (var elem in dirtyElements.Where(e => e.UpdateAfter != null && e.UpdateAfter.Length != 0))
                        {
                            var index = renderPasses.IndexOf(elem.Pass);
                            if (index < 0)
                                continue;

                            var max = index;
                            foreach (var condition in elem.UpdateAfter)
                            {
                                var conditionIndex = getPassIndex(condition);
                                if (conditionIndex < 0 || conditionIndex < max)
                                    continue;
                                max = conditionIndex;
                            }

                            if (max != index)
                            {
                                renderPasses.RemoveAt(index);
                                renderPasses.Insert(max, elem.Pass);
                            }
                        }
                    }
                }

                return renderPasses.AsReadOnly();
            }
        }

        private int getPassIndex(Type type)
        {
            for (var i = 0; i != renderPasses.Count; i++)
                if (renderPasses[i].GetType() == type)
                    return i;
            return -1;
        }
    }
}
