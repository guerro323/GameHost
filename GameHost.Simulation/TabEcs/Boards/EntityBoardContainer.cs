﻿using System;
using System.Collections.Generic;
 using System.Runtime.InteropServices;

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
			 public static ComponentMetadata Shared(uint entity) => new ComponentMetadata((int) -entity);
			 
			 private ComponentMetadata(int assigned)
			 {
				 Assigned = assigned;
			 }
		 }

		 private class AssignedComponent
		 {
			 public ComponentMetadata[] reference;
		 }

		 private Dictionary<uint, AssignedComponent> assignedComponentMap;

		 public EntityBoardContainer(int capacity) : base(capacity)
		 {
			 assignedComponentMap = new Dictionary<uint, AssignedComponent>();
		 }

		 public override uint CreateRow()
		 {
			 var maxId = board.MaxId;
			 var row   = base.CreateRow();

			 // Check whether or not the max id has been updated, and if it does, resize our component link columns
			 if (board.MaxId > maxId)
			 {
				 foreach (var componentColumn in assignedComponentMap.Values)
				 {
					 GetColumn(board.MaxId, ref componentColumn.reference);
				 }
			 }

			 return row;
		 }

		 public override void CreateRowBulk(Span<uint> rows)
		 {
			 var maxId = board.MaxId;
			 base.CreateRowBulk(rows);

			 // Check whether or not the max id has been updated, and if it does, resize our component link columns
			 if (board.MaxId > maxId)
			 {
				 foreach (var componentColumn in assignedComponentMap.Values)
				 {
					 GetColumn(board.MaxId, ref componentColumn.reference);
				 }
			 }
		 }

		 public ReadOnlySpan<ComponentMetadata> GetComponentColumn(uint type) => getComponentColumn(type);

		 private ref ComponentMetadata[] getComponentColumn(uint type)
		 {
			 if (assignedComponentMap.TryGetValue(type, out var assigned))
				 return ref assigned.reference;
			 
			 assignedComponentMap[type] = assigned = new AssignedComponent {reference = new ComponentMetadata[0]};
			 return ref assigned.reference;
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
		 
		 public Span<GameEntity> Alive => MemoryMarshal.Cast<uint, GameEntity>(board.UsedRows);
	 }
 }