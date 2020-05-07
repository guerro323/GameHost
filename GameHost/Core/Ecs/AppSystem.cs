using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Core.Bindables;
using GameHost.Injection;
using JetBrains.Annotations;

namespace GameHost.Core.Ecs
{
    [AttributeUsage(AttributeTargets.Class)]
    [UsedImplicitly] // would it possible to 'remove' it when DontInject attribute is present?
    public class InjectSystemToWorldAttribute : Attribute
    {
    }

    /// <summary>
    /// Adding this attribute to a system will make it not inject into a world.
    /// It will ignore parents <see cref="InjectSystemToWorldAttribute"/>.
    /// Children that use <see cref="InjectSystemToWorldAttribute"/> will ignore this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("This attribute is not yet implemented.")]
    public class DontInjectSystemToWorldAttribute : Attribute
    {
    }

    /// <summary>
    /// Represent an application system that will automatically be injected in worlds.
    /// </summary>
    [InjectSystemToWorld]
    public abstract class AppSystem : IDisposable, IInitSystem, IUpdateSystem
    {
        public bool Enabled { get; set; } = true;

        public Context         Context { get; private set; }
        public WorldCollection World   { get; private set; }

        protected virtual void OnInit()
        {

        }

        protected virtual void OnUpdate()
        {
        
        }

        public virtual void Dispose()
        {

        }

        // Interfaces implementation
        // .........................

        void IInitSystem.OnInit()
        {
            OnInit();
        }

        void IUpdateSystem.OnUpdate()
        {
            OnUpdate();
        }

        public virtual bool CanUpdate()
        {
            return Enabled;
        }

        WorldCollection IWorldSystem.WorldCollection
        {
            get
            {
                return World;
            }
            set
            {
                World   = value;
                Context = value.Ctx;
            }
        }
    }

    public class ComponentBindable<TValue> : Bindable<TValue>
    {
        public World WorldTarget { get; private set; }

        private IDisposable disposable;

        public ComponentBindable(World world)
        {
            WorldTarget = world;
        }

        public void UseEntitySet(EntitySet set)
        {

        }

        public void BindTo<TComponent>()
        {
            disposable = WorldTarget.SubscribeComponentChanged<TComponent>(OnUpdate);
        }

        public override void Dispose()
        {
            disposable.Dispose();
        }

        private void OnUpdate<TComponent>(in Entity entity, in TComponent oldValue, in TComponent newValue)
        {
        }
    }
}
