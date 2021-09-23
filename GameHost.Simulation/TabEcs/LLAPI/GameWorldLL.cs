using System;
using System.Runtime.CompilerServices;
using GameHost.Simulation.TabEcs.Boards;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs.LLAPI
{
    public static class GameWorldLL
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentBoardBase GetComponentBoardBase(ComponentTypeBoardContainer componentTypeBoardContainer,
            ComponentType componentType)
        {
            return componentTypeBoardContainer.ComponentBoardColumns[(int) componentType.Id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CreateComponent(ComponentBoardBase componentBoard)
        {
            return componentBoard.CreateRow();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AssignComponent(ComponentBoardBase componentBoard, ComponentReference componentReference,
            EntityBoardContainer entityBoard, GameEntityHandle entityHandle)
        {
            componentBoard.AddReference(componentReference.Id, entityHandle);

            var previousComponentId =
                entityBoard.AssignComponentReference(entityHandle.Id, componentReference.Type.Id,
                    componentReference.Id);
            if (previousComponentId > 0)
            {
                var refs = componentBoard.RemoveReference(previousComponentId, entityHandle);

                // nobody reference this component anymore, let's remove the row
                if (refs == 0)
                    componentBoard.DeleteRow(previousComponentId);

                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetOwner(ComponentBoardBase componentBoard, ComponentReference componentReference,
            GameEntityHandle entityHandle)
        {
            componentBoard.OwnerColumn[(int) componentReference.Id] = entityHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameEntityHandle GetOwner(ComponentBoardBase componentBoard,
            ComponentReference componentReference)
        {
            return componentBoard.OwnerColumn[(int) componentReference.Id];
        }

        public static Span<GameEntityHandle> GetReferences(ComponentBoardBase componentBoard,
            ComponentReference componentReference)
        {
            return componentBoard.GetReferences(componentReference.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveComponentReference(ComponentBoardBase componentBoard, ComponentType componentType,
            EntityBoardContainer entityBoard, GameEntityHandle entityHandle)
        {
            // todo: we need to have a real method for removing the component metadata on the column.
            var previousComponentId = entityBoard.AssignComponentReference(entityHandle.Id, componentType.Id, 0);
            if (previousComponentId > 0)
            {
                var refs = componentBoard.RemoveReference(previousComponentId, entityHandle);

                // nobody reference this component anymore, let's remove the row
                if (refs == 0)
                    componentBoard.DeleteRow(previousComponentId);
                return true;
            }

            return false;
        }

        public static uint UpdateArchetype(ArchetypeBoardContainer archetypeBoard,
            ComponentTypeBoardContainer componentTypeBoard, EntityBoardContainer entityBoard,
            GameEntityHandle entityHandle)
        {
            var typeSpan = componentTypeBoard.Registered;
            var foundIndex = 0;

            Span<uint> founds = stackalloc uint[typeSpan.Length];

            for (var i = 0; i != typeSpan.Length; i++)
            {
                var metadataSpan = entityBoard.GetComponentColumn(typeSpan[i].Id);
                /*if (metadataSpan.Length <= entityHandle.Id) TODO:: if it bug again, just uncomment this
                    continue;*/

                if (metadataSpan[(int) entityHandle.Id].Valid)
                    founds[foundIndex++] = typeSpan[i].Id;
            }

            if (foundIndex > 128)
                throw new InvalidOperationException("What are you trying to do with " + foundIndex + " components?");


            var archetype = archetypeBoard.GetOrCreateRow(founds.Slice(0, foundIndex), true);
            var currentArchetype = entityBoard.ArchetypeColumn[(int) entityHandle.Id];
            if (currentArchetype.Id != archetype)
            {
                entityBoard.AssignArchetype(entityHandle.Id, archetype);
                if (currentArchetype.Id > 0)
                    archetypeBoard.RemoveEntity(currentArchetype.Id, entityHandle.Id);
                archetypeBoard.AddEntity(archetype, entityHandle.Id);
            }

            return archetype;
        }
    }
}