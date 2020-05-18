using DefaultEcs;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public class DefaultEcsImplementation
    {
        public readonly RevolutionWorld RevolutionWorld;
        public readonly World           DefaultEcsWorld;

        public DefaultEcsImplementation(RevolutionWorld revolutionWorld, World defaultEcsWorld)
        {
            this.RevolutionWorld = revolutionWorld;
            this.DefaultEcsWorld = defaultEcsWorld;

            foreach (var entity in DefaultEcsWorld)
                OnEntityCreated(entity);

            DefaultEcsWorld.SubscribeEntityCreated(OnEntityCreated);
            DefaultEcsWorld.SubscribeEntityDisposed(OnEntityDisposed);
        }

        private void OnEntityCreated(in Entity entity)
        {
            entity.Set(RevolutionWorld.CreateIdentEntity(entity));
        }

        private void OnEntityDisposed(in Entity entity)
        {
            RevolutionWorld.RemoveEntity(RevolutionWorld.GetEntityFromIdentifier(entity).Raw);
        }
    }

    public static class DefaultEcsImplementationExtensions
    {
        public static DefaultEcsImplementation ImplementDefaultEcs(this RevolutionWorld origin, World destination)
        {
            return new DefaultEcsImplementation(origin, destination);
        }
    }
}
