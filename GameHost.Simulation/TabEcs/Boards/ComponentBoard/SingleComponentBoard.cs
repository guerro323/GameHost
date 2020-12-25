﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

 namespace GameHost.Simulation.TabEcs
 {
	 public class SingleComponentBoard : ComponentBoardBase
	 {
		 private (byte[] data, byte h) column;

		 public SingleComponentBoard(int size, int capacity) : base(size, capacity)
		 {
			 column.data = new byte[0];
		 }
		 
		 public override void CreateRowBulk(Span<uint> rows)
		 {
			 base.CreateRowBulk(rows);
			 if (board.MaxId * Size >= column.data.Length)
			 {
				 Array.Resize(ref column.data, (int) ((board.MaxId + 1) * Size * 2));
			 }
		 }

		 public override uint CreateRow()
		 {
			 var row = base.CreateRow();
			 if (board.MaxId * Size >= column.data.Length)
			 {
				 Array.Resize(ref column.data, (int) ((board.MaxId + 1) * Size * 2));
			 }

			 return row;
		 }

		 public override bool DeleteRow(uint row)
		 {
			 // clear data
			 column.data
			       .AsSpan((int) row * Size, Size)
			       .Clear();
			 
			 return base.DeleteRow(row);
		 }

		 public Span<T> AsSpan<T>() where T : struct
		 {
			 if (Unsafe.SizeOf<T>() != Size)
				 throw new InvalidOperationException();

			 return MemoryMarshal.Cast<byte, T>(column.data);
		 }

		 public Span<byte> ReadRaw(uint row)
		 {
			 return column.data.AsSpan((int) row * Size, Size);
		 }

		 public ref T Read<T>(uint row)
			 where T : struct
		 {
			 return ref AsSpan<T>()[(int) row];
		 }

		 public T Read<T>(int row)
			 where T : struct
		 {
			 return AsSpan<T>()[row];
		 }

		 public void SetValue<T>(uint row, T value)
			 where T : struct
		 {
			 AsSpan<T>()[(int) row] = value;
		 }
	 }
 }