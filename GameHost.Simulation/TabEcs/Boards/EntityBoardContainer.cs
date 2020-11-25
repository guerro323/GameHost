﻿using System;
using System.Collections.Generic;
 using System.Runtime.InteropServices;
 using Collections.Pooled;

 namespace GameHost.Simulation.TabEcs
 {
	 public class EntityBoardContainer : BoardContainer
	 {
		 public readonly struct ComponentMetadata
		 {
			 /// <summary>
			 /// The assigned component meta
			 /// </summary>
			 public readonly int Assigned;

			 /// <summary>
			 /// Is the assignment null?
			 /// </summary>
			 public bool Null => Assigned == 0;

			 /// <summary>
			 /// Is the assignment valid?
			 /// </summary>
			 public bool Valid => Assigned != 0;

			 /// <summary>
			 /// Is this a custom component?
			 /// </summary>
			 public bool IsShared => Assigned < 0;

			 /// <summary>
			 /// The reference to the non custom component
			 /// </summary>
			 public uint Id => IsShared ? 0 : (uint) Assigned;

			 /// <summary>
			 /// The reference to the entity that share the component
			 /// </summary>
			 public uint Entity => IsShared ? (uint) -Assigned : 0;

			 public static ComponentMetadata Reference(uint componentId) => new ComponentMetadata((int) componentId);
			 public static ComponentMetadata Shared(uint    entity)      => new ComponentMetadata((int) -entity);

			 private ComponentMetadata(int assigned)
			 {
				 Assigned = assigned;
			 }
		 }

		 private (uint[] archetype, uint[] versions, PooledList<uint>[] linkedEntities, PooledList<uint>[] linkParents) column;

		 public EntityBoardContainer(int capacity) : base(capacity)
		 {
			 p_componentColumn = new ComponentMetadata[0][];

			 column.archetype      = new uint[0];
			 column.versions       = new uint[0];
			 column.linkedEntities = new PooledList<uint>[0];
			 column.linkParents    = new PooledList<uint>[0];
		 }

		 private void OnResize()
		 {
			 GetColumn(board.MaxId, ref column.archetype);
			 GetColumn(board.MaxId, ref column.versions);

			 var previousLength = column.linkedEntities.Length;
			 GetColumn(board.MaxId, ref column.linkedEntities);
			 GetColumn(board.MaxId, ref column.linkParents);
			 for (var i = previousLength; i < column.linkedEntities.Length; i++)
			 {
				 column.linkedEntities[i] = new PooledList<uint>();
				 column.linkParents[i]    = new PooledList<uint>();
			 }

			 for (var i = 0; i < p_componentColumn.Length; i++)
			 {
				 GetColumn(board.MaxId, ref p_componentColumn[i]);
			 }
		 }

		 public override uint CreateRow()
		 {
			 var maxId = board.MaxId;
			 var row   = base.CreateRow();

			 // Check whether or not the max id has been updated, and if it does, resize our component link columns
			 if (board.MaxId > maxId)
				 OnResize();

			 return row;
		 }

		 public override void CreateRowBulk(Span<uint> rows)
		 {
			 var maxId = board.MaxId;
			 base.CreateRowBulk(rows);

			 // Check whether or not the max id has been updated, and if it does, resize our component link columns
			 if (board.MaxId > maxId)
				 OnResize();
		 }

		 public Span<ComponentMetadata> GetComponentColumn(uint type) => getComponentColumn(type);

		 private ComponentMetadata[][] p_componentColumn;

		 private ref ComponentMetadata[] getComponentColumn(uint type)
		 {
			 var length = p_componentColumn.Length;
			 if (type >= length)
			 {
				 Array.Resize(ref p_componentColumn, ((int) type + 1) * 2);
				 for (var i = length; i < p_componentColumn.Length; i++)
				 {
					 p_componentColumn[i] = new ComponentMetadata[board.MaxId];
				 }
			 }

			 return ref p_componentColumn[type];
		 }

		 public Span<GameEntityHandle> GetLinkedEntities(uint entity) => getLinkedEntitiesColumn(entity);
		 public Span<GameEntityHandle> GetLinkedParents(uint  entity) => getLinkedParentsColumn(entity);

		 private Span<GameEntityHandle> getLinkedEntitiesColumn(uint entity)
		 {
			 return MemoryMarshal.Cast<uint, GameEntityHandle>(column.linkedEntities[entity].Span);
		 }

		 private Span<GameEntityHandle> getLinkedParentsColumn(uint entity)
		 {
			 return MemoryMarshal.Cast<uint, GameEntityHandle>(column.linkParents[entity].Span);
		 }

		 /// <summary>
		 /// Assign a new component to an entity.
		 /// </summary>
		 /// <param name="row"></param>
		 /// <param name="componentType"></param>
		 /// <param name="component"></param>
		 /// <returns>The previous component id</returns>
		 public uint AssignComponentReference(uint row, uint componentType, uint component)
		 {
			 ref var current  = ref GetColumn(row, ref getComponentColumn(componentType));
			 var     previous = current;

			 current = ComponentMetadata.Reference(component);
			 return previous.Id;
		 }

		 /// <summary>
		 /// Assign a shared component from an entity to this entity.
		 /// </summary>
		 /// <param name="row"></param>
		 /// <param name="componentType"></param>
		 /// <param name="component"></param>
		 /// <returns>The previous component id</returns>
		 public uint AssignSharedComponent(uint row, uint componentType, uint linkedRow)
		 {
			 ref var current  = ref GetColumn(row, ref getComponentColumn(componentType));
			 var     previous = current;

			 current = ComponentMetadata.Shared(linkedRow);
			 return previous.Id;
		 }

		 public uint AssignArchetype(uint row, uint archetype)
		 {
			 ref var current  = ref GetColumn(row, ref column.archetype);
			 var     previous = current;

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

		 public Span<GameEntityHandle>      Alive           => MemoryMarshal.Cast<uint, GameEntityHandle>(board.UsedRows);
		 public Span<EntityArchetype> ArchetypeColumn => MemoryMarshal.Cast<uint, EntityArchetype>(column.archetype);
		 public Span<uint>            VersionColumn   => column.versions;
	 }
 }