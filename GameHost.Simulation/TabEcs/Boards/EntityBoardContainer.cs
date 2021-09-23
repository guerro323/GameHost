using System;
using System.Runtime.InteropServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs.Boards
{
    public class EntityBoardContainer : BoardWithRowCollectionBase
    {
        private (uint[] archetype, uint[] versions, PooledList<uint>[] linkedEntities, PooledList<uint>[] linkParents)
            column;

        private ComponentMetadata[][] p_componentColumn;

        public EntityBoardContainer(GameWorld gameWorld) : base(gameWorld)
        {
            p_componentColumn = Array.Empty<ComponentMetadata[]>();

            column.archetype = Array.Empty<uint>();
            column.versions = Array.Empty<uint>();
            column.linkedEntities = Array.Empty<PooledList<uint>>();
            column.linkParents = Array.Empty<PooledList<uint>>();
        }

        public Span<GameEntityHandle> Alive => MemoryMarshal.Cast<uint, GameEntityHandle>(Rows.UsedRows);
        public Span<EntityArchetype> ArchetypeColumn => MemoryMarshal.Cast<uint, EntityArchetype>(column.archetype);
        public Span<uint> VersionColumn => column.versions;

        private void OnResize()
        {
            GetColumn(Rows.MaxId, ref column.archetype);
            GetColumn(Rows.MaxId, ref column.versions);

            var previousLength = column.linkedEntities.Length;
            GetColumn(Rows.MaxId, ref column.linkedEntities);
            GetColumn(Rows.MaxId, ref column.linkParents);
            for (var i = previousLength; i < column.linkedEntities.Length; i++)
            {
                column.linkedEntities[i] = new PooledList<uint>();
                column.linkParents[i] = new PooledList<uint>();
            }

            for (var i = 0; i < p_componentColumn.Length; i++) GetColumn(Rows.MaxId, ref p_componentColumn[i]);
        }

        public override uint CreateRow()
        {
            var maxId = Rows.MaxId;
            var row = base.CreateRow();

            // Check whether or not the max id has been updated, and if it does, resize our component link columns
            if (Rows.MaxId > maxId)
                OnResize();

            return row;
        }

        public override void CreateRowBulk(Span<uint> rows)
        {
            var maxId = Rows.MaxId;
            base.CreateRowBulk(rows);

            // Check whether or not the max id has been updated, and if it does, resize our component link columns
            if (Rows.MaxId > maxId)
                OnResize();
        }

        public Span<ComponentMetadata> GetComponentColumn(uint type)
        {
            return getComponentColumn(type);
        }

        private ref ComponentMetadata[] getComponentColumn(uint type)
        {
            var length = p_componentColumn.Length;
            if (type >= length)
            {
                CheckForThreadSafety();

                Array.Resize(ref p_componentColumn, ((int) type + 1) * 2);
                for (var i = length; i < p_componentColumn.Length; i++)
                    p_componentColumn[i] = new ComponentMetadata[Rows.MaxId];
            }

            return ref p_componentColumn[type];
        }

        public Span<GameEntityHandle> GetLinkedEntities(uint entity)
        {
            return getLinkedEntitiesColumn(entity);
        }

        public Span<GameEntityHandle> GetLinkedParents(uint entity)
        {
            return getLinkedParentsColumn(entity);
        }

        private Span<GameEntityHandle> getLinkedEntitiesColumn(uint entity)
        {
            return MemoryMarshal.Cast<uint, GameEntityHandle>(column.linkedEntities[entity].Span);
        }

        private Span<GameEntityHandle> getLinkedParentsColumn(uint entity)
        {
            return MemoryMarshal.Cast<uint, GameEntityHandle>(column.linkParents[entity].Span);
        }

        /// <summary>
        ///     Assign a new component to an entity.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="componentType"></param>
        /// <param name="component"></param>
        /// <returns>The previous component id</returns>
        public uint AssignComponentReference(uint row, uint componentType, uint component)
        {
            ref var current = ref GetColumn(row, ref getComponentColumn(componentType));
            var previous = current;

            current = ComponentMetadata.Reference(component);
            return previous.Id;
        }

        /// <summary>
        ///     Assign a shared component from an entity to this entity.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="componentType"></param>
        /// <param name="component"></param>
        /// <returns>The previous component id</returns>
        public uint AssignSharedComponent(uint row, uint componentType, uint linkedRow)
        {
            ref var current = ref GetColumn(row, ref getComponentColumn(componentType));
            var previous = current;

            current = ComponentMetadata.Shared(linkedRow);
            return previous.Id;
        }

        public uint AssignArchetype(uint row, uint archetype)
        {
            ref var current = ref GetColumn(row, ref column.archetype);
            var previous = current;

            current = archetype;
            return previous;
        }

        public bool AddLinked(uint row, uint child)
        {
            var current = GetColumn(row, ref column.linkedEntities);
            if (!current.Contains(child))
            {
                current.Add(child);
                GetColumn(child, ref column.linkParents).Add(row);
                return true;
            }

            return false;
        }

        public bool RemoveLinked(uint row, uint child)
        {
            GetColumn(child, ref column.linkParents).Remove(row);

            var current = GetColumn(row, ref column.linkedEntities);
            return current.Remove(child);
        }

        public override bool DeleteRow(uint row)
        {
            if (!base.DeleteRow(row))
                return false;

            column.linkedEntities[row].Clear();
            column.linkParents[row].Clear();
            column.versions[row]++;
            return true;
        }

        public readonly struct ComponentMetadata
        {
	        /// <summary>
	        ///     The assigned component meta
	        /// </summary>
	        public readonly int Assigned;

	        /// <summary>
	        ///     Is the assignment null?
	        /// </summary>
	        public bool Null => Assigned == 0;

	        /// <summary>
	        ///     Is the assignment valid?
	        /// </summary>
	        public bool Valid => Assigned != 0;

	        /// <summary>
	        ///     Is this a custom component?
	        /// </summary>
	        public bool IsShared => Assigned < 0;

	        /// <summary>
	        ///     The reference to the non custom component
	        /// </summary>
	        public uint Id => IsShared ? 0 : (uint) Assigned;

	        /// <summary>
	        ///     The reference to the entity that share the component
	        /// </summary>
	        public uint Entity => IsShared ? (uint) -Assigned : 0;

            public static ComponentMetadata Reference(uint componentId)
            {
                return new((int) componentId);
            }

            public static ComponentMetadata Shared(uint entity)
            {
                return new((int) -entity);
            }

            private ComponentMetadata(int assigned)
            {
                Assigned = assigned;
            }
        }
    }
}