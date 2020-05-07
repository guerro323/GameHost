using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefaultEcs;
using DryIoc;
using GameHost.Injection;

namespace GameHost.Core.Ecs
{
    public class WorldCollection
    {
        public readonly Context Ctx;
        public readonly World   Mgr;

        public IReadOnlyCollection<IUpdateSystem> UpdateLoop
        {
            get
            {
                return updateLoop;
            }
        }

        private OrderedList<object> systemList;
        private Dictionary<Type, object> systemMap;

        private List<IUpdateSystem> updateLoop;
        private bool isUpdateLoopDirty;
        
        private List<IInitSystem>   leftInitSystem;
        private bool isLeftInitSystemDirty;

        public WorldCollection(Context parentContext, World mgr)
        {
            Mgr = mgr;

            systemList     = new OrderedList<object>();
            systemMap      = new Dictionary<Type, object>(64);
            updateLoop     = new List<IUpdateSystem>(64);
            leftInitSystem = new List<IInitSystem>(64);

            Ctx = new Context(parentContext, Rules.Default.With(SelectPropertiesAndFieldsWithImportAttribute));
            Ctx.Bind<WorldCollection, WorldCollection>(this);
            Ctx.Bind<World, World>(mgr);

            systemList.OnDirty += () =>
            {
                isUpdateLoopDirty     = true;
                isLeftInitSystemDirty = true;
            };
        }
        
        public static readonly PropertiesAndFieldsSelector SelectPropertiesAndFieldsWithImportAttribute =
            PropertiesAndFields.All(serviceInfo: GetImportedPropertiesAndFields);
        
        private static PropertyOrFieldServiceInfo GetImportedPropertiesAndFields(MemberInfo m, Request req)
        {
            var import = (DependencyStrategyAttribute)m.GetAttributes(typeof(DependencyStrategyAttribute)).FirstOrDefault();
            return import == null ? null : PropertyOrFieldServiceInfo.Of(m).WithDetails(ServiceDetails.IfUnresolvedReturnDefaultIfNotRegistered);
        }

        private void RemakeLoop<T>(ref List<T> originalList, ref bool isDirty)
            where T : class
        {
            if (!isDirty)
                return;
            
            var systemToReorder = new List<object>(originalList);
            originalList.Clear();

            // kinda slow?
            foreach (var obj in systemList.Elements)
            {
                if (systemToReorder.Contains(obj))
                    originalList.Add((T)obj);
            }

            isDirty = false;
        }

        public void DoUpdatePass()
        {
            RemakeLoop(ref updateLoop, ref isUpdateLoopDirty);

            foreach (var system in updateLoop)
            {
                //try
               // {
                    if (system.CanUpdate())
                        system.OnUpdate();
               // }
                /*catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }*/
            }
        }

        public T GetOrCreate<T>() where T : class, IWorldSystem, new()
        {
            return GetOrCreate<T>(OrderedList.GetBefore(typeof(T)), OrderedList.GetAfter(typeof(T)));
        }

        public T GetOrCreate<T>(Type[] updateBefore, Type[] updateAfter)
            where T : class, IWorldSystem, new()
        {
            if (systemMap.TryGetValue(typeof(T), out var obj))
                return (T)obj;
            
            obj = new T();
            new InjectPropertyStrategy(Ctx, true).Inject(obj);
            Add(obj, updateBefore, updateAfter);
            return (T) obj;
        }

        public object GetOrCreate(Type type)
        {
            if (systemMap.TryGetValue(type, out var obj))
                return obj;

            obj = Activator.CreateInstance(type);
            new InjectPropertyStrategy(Ctx, true).Inject(obj);
            Add(obj, OrderedList.GetBefore(obj.GetType()), OrderedList.GetAfter(obj.GetType()));
            return obj;
        }

        public void ForceSystemOrder(object obj, Type[] updateBefore, Type[] updateAfter)
        {
            if (!systemMap.ContainsKey(obj.GetType()))
                throw new InvalidOperationException("The system does not exit in world database.");
            systemList.Set(obj, updateBefore, updateAfter);
        }

        public void Add(object obj, Type[] updateBefore, Type[] updateAfter)
        {
            systemMap[obj.GetType()] = obj;
            systemList.Set(obj, updateBefore, updateAfter);
            Ctx.Register(obj);

            var asWorldSystem = (IWorldSystem)obj;
            asWorldSystem.WorldCollection = this;
            
            switch (obj)
            {
                case IInitSystem init:
                    leftInitSystem.Add(init);
                    break;
                case IUpdateSystem update:
                    updateLoop.Add(update);
                    break;
            }
        }

        /// <summary>
        /// Systems based on <see cref="IInitSystem"/> need <see cref="IInitSystem.OnInit"/> to be called.
        /// This function will automatically call the non-initialized systems.
        /// <remarks>
        /// This is mostly useful to setup dependencies between systems.
        /// </remarks>
        /// </summary>
        public void DoInitializePass()
        {
            RemakeLoop(ref leftInitSystem, ref isLeftInitSystemDirty);

            if (leftInitSystem.Count == 0)
                return;
            
            // todo: bad.
            var currentList = new Span<IInitSystem>(leftInitSystem.ToArray());
            foreach (var sys in currentList)
            {
                sys.OnInit();
                if (sys is IUpdateSystem update)
                    updateLoop.Add(update);
            }

            leftInitSystem.Clear();
        }
    }

    public interface IWorldSystem
    {
        WorldCollection WorldCollection { get; set; }
    }

    public interface IUpdateSystem : IWorldSystem
    {
        bool CanUpdate();
        void OnUpdate();
    }

    public interface IInitSystem : IWorldSystem
    {
        void OnInit();
    }
}
