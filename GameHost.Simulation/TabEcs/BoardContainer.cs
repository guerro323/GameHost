﻿using System;
 using TabEcs;

 namespace GameHost.Simulation.TabEcs
 {
	 public static class BoardContainerExt
	 {
		 public static ref UIntBoardBase GetBoard(BoardContainer container)
		 {
			 return ref container.board;
		 }
	 }
	 
	 public abstract class BoardContainer : IDisposable
	 {
		 protected internal UIntBoardBase board;
		 
		 public BoardContainer(int capacity)
		 {
			 board = new UIntBoardBase(capacity);
		 }

		 public virtual uint CreateRow()
		 {
			 return board.CreateRow();
		 }

		 public virtual void CreateRowBulk(Span<uint> rows)
		 {
			 board.CreateRowBulk(rows);
		 }

		 public virtual ref T GetColumn<T>(uint row, ref T[] array)
		 {
			 return ref board.GetColumn(row, ref array);
		 }

		 public virtual bool DeleteRow(uint row)
		 {
			 return board.TrySetUnusedRow(row);
		 }

		 public virtual void Dispose()
		 {
			 while (board.Count > 0)
				 DeleteRow(board.UsedRows[0]);
		 }
	 }
 }