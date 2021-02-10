﻿using System;
 using System.Diagnostics;
 using System.Threading;
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

		 private   Thread callerThread;
		 protected bool   CheckSafetyIssue;
		 
		 public BoardContainer(int capacity)
		 {
			 board = new UIntBoardBase(capacity);
			 
			 callerThread     = Thread.CurrentThread;
			 CheckSafetyIssue = true;
		 }

		 public void SetCallerThread(Thread callerThread) => this.callerThread = callerThread;

		 [Conditional("DEBUG")]
		 protected void CheckForThreadSafety()
		 {
			 if (CheckSafetyIssue && Thread.CurrentThread != callerThread)
				 throw new InvalidOperationException($"Thread Safety Issue! Current={Thread.CurrentThread.Name}({Thread.CurrentThread.ManagedThreadId}), Original={callerThread.Name}({callerThread.ManagedThreadId})");
		 }

		 public virtual uint CreateRow()
		 {
			 CheckForThreadSafety();

			 return board.CreateRow();
		 }

		 public virtual void CreateRowBulk(Span<uint> rows)
		 {
			 CheckForThreadSafety();
			 
			 board.CreateRowBulk(rows);
		 }

		 public virtual ref T GetColumn<T>(uint row, ref T[] array)
		 {
			 return ref board.GetColumn(row, ref array);
		 }

		 public virtual bool DeleteRow(uint row)
		 {
			 CheckForThreadSafety();
			 
			 return board.TrySetUnusedRow(row);
		 }

		 public virtual void Dispose()
		 {
			 while (board.Count > 0)
				 DeleteRow(board.UsedRows[0]);
		 }
	 }
 }