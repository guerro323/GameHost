using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameHost.Simulation.TabEcs.LLAPI;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs
{
    public partial class GameWorld
    {
        [Conditional("DEBUG")]
        public void ThrowOnInvalidHandle(GameEntityHandle handle)
        {
            if (handle.Id == 0)
                throw new InvalidOperationException("You've passed an invalid handle");
            if (Boards.Entity.ArchetypeColumn[(int) handle.Id].Id == 0)
                throw new InvalidOperationException($"The GameWorld does not contains a handle with id '{handle.Id}'");
        }

        public GameEntityHandle CreateEntity()
        {
            var handle = new GameEntityHandle(Boards.Entity.CreateRow());
            GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, handle);
            return handle;
        }

        public void CreateEntityBulk(Span<GameEntityHandle> entities)
        {
            Boards.Entity.CreateRowBulk(MemoryMarshal.Cast<GameEntityHandle, uint>(entities));
            foreach (var ent in entities)
                GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, ent);
        }

        // Version with GameEntity support
        public void RemoveEntityBulk(ReadOnlySpan<GameEntity> entities)
        {
            // We actually copy the handles since it's possible that it's originally from a list that is automatically resized after an entity is deleted.
            // To make sure there are no stack overflow, we copy a max of 64 entities every iteration (and if there are less than 64, we just use this number)

            const int toOperate = 32;

            Span<GameEntity> bulk = stackalloc GameEntity[toOperate];
            while (entities.Length >= toOperate)
            {
                entities.Slice(0, toOperate).CopyTo(bulk);

                foreach (var entity in bulk)
                    RemoveEntity(entity.Handle);
            }

            if (entities.Length <= 0)
                return;

            entities.CopyTo(bulk);

            foreach (var entity in bulk.Slice(0, entities.Length))
                RemoveEntity(entity.Handle);
        }

        public void RemoveEntityBulk(ReadOnlySpan<GameEntityHandle> handles, bool safe = false)
        {
            if (safe)
            {
                foreach (var handle in handles)
                    RemoveEntity(handle);
                return;
            }

            // We actually copy the handles since it's possible that it's originally from a list that is automatically resized after an entity is deleted.
            // To make sure there are no stack overflow, we copy a max of 64 entities every iteration (and if there are less than 64, we just use this number)

            const int toOperate = 64;

            Span<GameEntityHandle> bulk = stackalloc GameEntityHandle[toOperate];
            while (handles.Length >= toOperate)
            {
                handles.Slice(0, toOperate).CopyTo(bulk);

                foreach (var handle in bulk)
                    RemoveEntity(handle);

                handles = handles.Slice(toOperate);
            }

            if (handles.Length <= 0)
                return;

            handles.CopyTo(bulk);

            foreach (var handle in bulk.Slice(0, handles.Length))
                RemoveEntity(handle);
        }

        public void RemoveEntity(GameEntityHandle entityHandle)
        {
            ThrowOnInvalidHandle(entityHandle);

            foreach (ref readonly var componentType in Boards.ComponentType.Registered)
                RemoveComponent(entityHandle, componentType);

            var children = Boards.Entity.GetLinkedEntities(entityHandle.Id);
            var childrenLength = children.Length;
            for (var ent = 0; ent < childrenLength; ent++)
            {
                var linkedEntity = children[ent];
                if (Contains(linkedEntity))
                {
                    RemoveEntity(linkedEntity);
                    ent--;
                    childrenLength--;
                }
            }

            var parents = Boards.Entity.GetLinkedParents(entityHandle.Id);
            var parentLength = parents.Length;
            for (var ent = 0; ent < parentLength; ent++)
            {
                var parent = parents[ent];
                if (Boards.Entity.RemoveLinked(parent.Id, entityHandle.Id))
                {
                    ent--;
                    parentLength--;
                }
            }

            var archetype = GetArchetype(entityHandle);
            if (archetype.Id > 0)
            {
                Boards.Archetype.RemoveEntity(archetype.Id, entityHandle.Id);
                // Reset archetype of this ID.
                // Since we share the total entity span on clients, the client should know that the entity is deleted via its archetype
                Boards.Entity.ArchetypeColumn[(int) entityHandle.Id] = default;
            }

            Boards.Entity.DeleteRow(entityHandle.Id);
        }

        public EntityArchetype GetArchetype(GameEntityHandle entityHandle)
        {
            return Boards.Entity.ArchetypeColumn[(int) entityHandle.Id];
        }

        /// <summary>
        ///     Whether or not this entity handle is valid in the boards.
        /// </summary>
        public bool Contains(GameEntityHandle entityHandle)
        {
            return entityHandle.Id < Boards.Entity.ArchetypeColumn.Length &&
                   Boards.Entity.ArchetypeColumn[(int) entityHandle.Id].Id > 0;
        }

        /// <summary>
        ///     Whether or not this entity currently exist (handle & version)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Exists(GameEntity entity)
        {
            unchecked
            {
                return Contains(entity.Handle) && Boards.Entity.VersionColumn[(int) entity.Id] == entity.Version;
            }
        }

        /// <summary>
        ///     Get a safe version of the handle (aka with version)
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        /// <remarks>
        ///     It may also be possible that you have an invalid handle, and that you want to check for an updated one.
        ///     For example: oldEntity.Version != gameWorld.Safe(oldEntity.Handle).Version
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameEntity Safe(GameEntityHandle handle)
        {
            ThrowOnInvalidHandle(handle);

            unchecked
            {
                return new GameEntity(handle.Id, Boards.Entity.VersionColumn[(int) handle.Id]);
            }
        }

        /// <summary>
        ///     Set if a child entity should be linked to an owner entity. If the owner get removed, the child will too.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="owner"></param>
        /// <param name="isLinked"></param>
        /// <returns>Return if the linking state has been changed</returns>
        public bool Link(GameEntityHandle child, GameEntityHandle owner, bool isLinked)
        {
            ThrowOnInvalidHandle(child);
            ThrowOnInvalidHandle(owner);

            return isLinked
                ? Boards.Entity.AddLinked(owner.Id, child.Id)
                : Boards.Entity.RemoveLinked(owner.Id, child.Id);
        }

        /// <summary>
        ///     Set if a child entity should be linked to owner entities. If one of the owner get removed, the child will too
        /// </summary>
        /// <param name="child"></param>
        /// <param name="owners"></param>
        /// <param name="isLinked"></param>
        /// <returns>Return if the linking state has been changed on atleast one owner</returns>
        public bool Link(GameEntityHandle child, Span<GameEntityHandle> owners, bool isLinked)
        {
            var b = false;
            foreach (var o in owners)
                b |= Link(child, o, isLinked);

            return b;
        }
    }
}