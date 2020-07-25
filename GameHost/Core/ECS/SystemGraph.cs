using System;
using System.Collections.Generic;
using System.Linq;

namespace GameHost.Core.Ecs
{
    public static class SystemGraph
    {
        public static void ValidateAndFixSystemGraph<T>(Dictionary<Type, OrderedList<T>.Element> dependencyGraph)
        {
            foreach (var systemAndType in dependencyGraph)
            {
                systemAndType.Value.visited                           = false;
                systemAndType.Value.LongestSystemsUpdatingAfterChain  = 0;
                systemAndType.Value.LongestSystemsUpdatingBeforeChain = 0;
            }

            SetLongestDistance(TopoSort(dependencyGraph, true), dependencyGraph, true);
            SetLongestDistance(TopoSort(dependencyGraph, false), dependencyGraph, false);
        }

        static void SetLongestDistance<T>(OrderedList<T>.Element[] deps, Dictionary<Type, OrderedList<T>.Element> dependencyGraph, bool after)
        {
            for (var i = 0; i < deps.Length; i++)
            {
                var adjs = after ? deps[i].UpdateAfter.ToArray() : deps[i].UpdateBefore.ToArray();
                for (var j = 0; j < adjs.Length; j++)
                {
                    var v = dependencyGraph[adjs[j]];
                    if (after)
                    {
                        if (v.LongestSystemsUpdatingAfterChain < deps[i].LongestSystemsUpdatingAfterChain + 1)
                            v.LongestSystemsUpdatingAfterChain = deps[i].LongestSystemsUpdatingAfterChain + 1;
                    }
                    else
                    {
                        if (v.LongestSystemsUpdatingBeforeChain < deps[i].LongestSystemsUpdatingBeforeChain + 1)
                            v.LongestSystemsUpdatingBeforeChain = deps[i].LongestSystemsUpdatingBeforeChain + 1;
                    }
                }
            }
        }

        static void TopoSortRecursive<T>(OrderedList<T>.Element dep, Dictionary<Type, OrderedList<T>.Element> dependencyGraph, Stack<OrderedList<T>.Element> stack, bool after)
        {
            dep.visited = true;
            var adjs = after ? dep.UpdateAfter.ToArray() : dep.UpdateBefore.ToArray();

            for (var i = 0; i < adjs.Length; i++)
            {
                var v = dependencyGraph[adjs[i]];
                if (!v.visited)
                    TopoSortRecursive(v, dependencyGraph, stack, after);
            }

            stack.Push(dep);
        }

        static OrderedList<T>.Element[] TopoSort<T>(Dictionary<Type, OrderedList<T>.Element> dependencyGraph, bool after)
        {
            var stack = new Stack<OrderedList<T>.Element>();

            var visited = new bool[dependencyGraph.Count];
            for (int i = 0; i < dependencyGraph.Count; i++)
                visited[i] = false;

            foreach (var typeAndSystem in dependencyGraph)
            {
                typeAndSystem.Value.visited = false;
            }

            foreach (var typeAndSystem in dependencyGraph)
            {
                if (typeAndSystem.Value.visited == false)
                    TopoSortRecursive(typeAndSystem.Value, dependencyGraph, stack, after);
            }

            return stack.ToArray();
        }
    }
}