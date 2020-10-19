using System;
using System.Collections.Generic;
using System.Reflection;
using DefaultEcs;
using GameHost.Core.Ecs.Passes;
using GameHost.Injection;

namespace GameHost.Core.Ecs
{
    public class WorldCollection : IDisposable
    {
        public readonly Context Ctx;
        public readonly World   Mgr;

        public IReadOnlyCollection<IUpdatePass> UpdateLoop
        {
            get { return updatePassRegister.GetObjects(); }
        }

        private InitializePassRegister initializePassRegister;
        private UpdatePassRegister     updatePassRegister;

        public readonly SystemCollection DefaultSystemCollection;

        public WorldCollection(Context parentContext, World mgr)
        {
            Mgr = mgr;

            Ctx = new Context(parentContext);
            Ctx.BindExisting<WorldCollection, WorldCollection>(this);
            Ctx.BindExisting<World, World>(mgr);

            DefaultSystemCollection = new SystemCollection(Ctx, this);
            DefaultSystemCollection.AddPass(initializePassRegister = new InitializePassRegister(), null, null);
            DefaultSystemCollection.AddPass(updatePassRegister     = new UpdatePassRegister(), new[] {typeof(InitializePassRegister)}, null);
        }

        public bool TryGet(Type type, out object obj) => DefaultSystemCollection.TryGet(type, out obj);

        public bool TryGet<T>(out T obj) => DefaultSystemCollection.TryGet(out obj);

        public T GetOrCreate<T>(Func<WorldCollection, T> createFunction) where T : class, IWorldSystem
        {
            return DefaultSystemCollection.GetOrCreate(createFunction);
        }

        public T GetOrCreate<T>(Func<WorldCollection, T> createFunction, Type[] updateBefore, Type[] updateAfter)
            where T : class, IWorldSystem
        {
            return DefaultSystemCollection.GetOrCreate(createFunction, updateBefore, updateAfter);
        }

        public object GetOrCreate(Type type)
        {
            return DefaultSystemCollection.GetOrCreate(type);
        }

        public void ForceSystemOrder(object obj, Type[] updateBefore, Type[] updateAfter)
        {
            DefaultSystemCollection.ForceSystemOrder(obj, updateBefore, updateAfter);
        }

        public void Add(object obj, Type[] updateBefore, Type[] updateAfter)
        {
            DefaultSystemCollection.Add(obj, updateBefore, updateAfter);
        }

        public void Remove(object system)
        {
            DefaultSystemCollection.Unregister(system);
        }

        public void LoopPasses() => DefaultSystemCollection.LoopPasses();

        public void Dispose()
        {
            DefaultSystemCollection.Dispose();
            Mgr?.Dispose();
        }
    }

    public interface IWorldSystem
    {
        WorldCollection WorldCollection { get; }
    }
}