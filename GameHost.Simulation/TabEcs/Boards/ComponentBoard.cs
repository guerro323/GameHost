﻿using System;
 using Collections.Pooled;

 namespace GameHost.Simulation.TabEcs
 {
	 public abstract class ComponentBoardBase : BoardContainer
	 {
		 public readonly int Size;

		 private (GameEntity[] owner, PooledList<GameEntity>[] references, byte h) column;

		 public ComponentBoardBase(int size, int capacity) : base(capacity)
		 {
			 Size = size;

			 column.owner      = new GameEntity[0];
			 column.references = new PooledList<GameEntity>[0];
		 }

		 public Span<GameEntity> OwnerColumn => column.owner;

		 protected virtual void OnResize()
		 {
			 Array.Resize(ref column.owner, (int) ((board.MaxId + 1) * 2));

			 var previousLength = column.references.Length;
			 Array.Resize(ref column.references, (int) ((board.MaxId + 1) * 2));
			 for (var i = previousLength; i < column.references.Length; i++)
			 {
				 column.references[i] = new PooledList<GameEntity>();
			 }
		 }

		 public override void CreateRowBulk(Span<uint> rows)
		 {
			 base.CreateRowBulk(rows);
			 if (board.MaxId >= column.owner.Length)
				 OnResize();
		 }

		 public override uint CreateRow()
		 {
			 var row = base.CreateRow();
			 if (board.MaxId >= column.owner.Length)
				 OnResize();

			 return row;
		 }

		 public virtual int AddReference(uint row, in GameEntity entity)
		 {
			 ref var list = ref GetColumn(row, ref column.references);
			 list.Add(entity);
			 return list.Count;
		 }

		 public virtual int RemoveReference(uint row, in GameEntity entity)
		 {
			 ref var list = ref GetColumn(row, ref column.references);
			 list.Remove(entity);
			 return list.Count;
		 }

		 public virtual Span<GameEntity> GetReferences(uint row)
		 {
			 return GetColumn(row, ref column.references).Span;
		 }
	 }
 }