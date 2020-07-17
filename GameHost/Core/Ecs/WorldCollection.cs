using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefaultEcs;
using DryIoc;
using GameHost.Core.Ecs.Passes;
using GameHost.Injection;

namespace GameHost.Core.Ecs
{
    public class WorldCollection : IDisposable
    {
        public readonly Context Ctx;
        public readonly World   Mgr;

        public IReadOnlyCollection<object> SystemList
        {
            get { return systemList.Elements; }
        }

        public IReadOnlyCollection<IUpdatePass> UpdateLoop
        {
            get { return updatePassRegister.GetObjects(); }
        }

        private OrderedList<object>      systemList;
        private Dictionary<Type, object> systemMap;

        private InitializePassRegister initializePassRegister;
        private UpdatePassRegister     updatePassRegister;

        private List<PassRegisterBase> availablePasses;

        public WorldCollection(Context parentContext, World mgr)
        {
            Mgr = mgr;

            systemList             = new OrderedList<object>();
            systemMap              = new Dictionary<Type, object>(64);
            initializePassRegister = new InitializePassRegister();
            updatePassRegister     = new UpdatePassRegister();

            availablePasses = new List<PassRegisterBase> {initializePassRegister, updatePassRegister};

            Ctx = new Context(parentContext, Rules.Default.With(SelectPropertiesAndFieldsWithImportAttribute));
            Ctx.BindExisting<WorldCollection, WorldCollection>(this);
            Ctx.BindExisting<World, World>(mgr);

            systemList.OnDirty += () =>
            {
                foreach (var register in availablePasses)
                    register.RegisterCollectionAndFilter(systemList);
            };
        }

        public static readonly PropertiesAndFieldsSelector SelectPropertiesAndFieldsWithImportAttribute =
            PropertiesAndFields.All(serviceInfo: GetImportedPropertiesAndFields);

        private static PropertyOrFieldServiceInfo GetImportedPropertiesAndFields(MemberInfo m, Request req)
        {
            var import = (DependencyStrategyAttribute) m.GetAttributes(typeof(DependencyStrategyAttribute)).FirstOrDefault();
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
                    originalList.Add((T) obj);
            }

            isDirty = false;
        }

        public bool TryGet(Type type, out object obj)
        {
            return systemMap.TryGetValue(type, out obj);
        }

        public bool TryGet<T>(out T obj)
        {
            var success = TryGet(typeof(T), out var nonGenObj);
            obj = (T) nonGenObj;
            return success;
        }

        public T GetOrCreate<T>(Func<WorldCollection, T> createFunction) where T : class, IWorldSystem
        {
            return GetOrCreate(createFunction, OrderedList.GetBefore(typeof(T)), OrderedList.GetAfter(typeof(T)));
        }

        public T GetOrCreate<T>(Func<WorldCollection, T> createFunction, Type[] updateBefore, Type[] updateAfter)
            where T : class, IWorldSystem
        {
            if (systemMap.TryGetValue(typeof(T), out var obj))
                return (T) obj;

            obj = createFunction(this);
            new InjectPropertyStrategy(Ctx, true).Inject(obj);
            Add(obj, updateBefore, updateAfter);
            return (T) obj;
        }

        public object GetOrCreate(Type type)
        {
            if (systemMap.TryGetValue(type, out var obj))
                return obj;

            obj = Activator.CreateInstance(type, args: this);
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
            systemList.Set(obj, updateAfter, updateBefore);
            Ctx.Register(obj);
        }

        public void LoopPasses()
        {
            foreach (var register in availablePasses)
                register.Trigger();
        }

        public void Dispose()
        {
            Mgr?.Dispose();
            foreach (var sys in systemList)
            {
                if (sys is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            systemList = null;
        }
    }

    public interface IWorldSystem
    {
        WorldCollection WorldCollection { get; }
    }
}