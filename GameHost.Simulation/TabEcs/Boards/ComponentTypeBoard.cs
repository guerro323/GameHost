﻿using System;
 using System.Runtime.InteropServices;

 namespace GameHost.Simulation.TabEcs
 {
	 public class ComponentTypeBoardContainer : BoardContainer
	 {
		 private (string[] name, int[] size, ComponentBoardBase[] componentBoard) column;

		 public ComponentTypeBoardContainer(int capacity) : base(capacity)
		 {
			 column.name           = new string[0];
			 column.size           = new int[0];
			 column.componentBoard = new ComponentBoardBase[0];
		 }

		 public Span<string>                     NameColumns           => column.name;
		 public ReadOnlySpan<int>                SizeColumns           => column.size;
		 public ReadOnlySpan<ComponentBoardBase> ComponentBoardColumns => column.componentBoard;

		 public void SetRowName(uint row, string name)
		 {
			 GetColumn(row, ref column.name) = name;
		 }

		 public uint CreateRow(string name, ComponentBoardBase componentBoard)
		 {
			 var row = CreateRow();
			 GetColumn(row, ref column.name)           = name;
			 GetColumn(row, ref column.size)           = componentBoard.Size;
			 GetColumn(row, ref column.componentBoard) = componentBoard;
			 return row;
		 }

		 public Span<ComponentType> Registered => MemoryMarshal.Cast<uint, ComponentType>(board.UsedRows);

		 public override void Dispose()
		 {
			 base.Dispose();

			 column.name = null;
			 column.size = null;
			 foreach (var componentBoardBase in column.componentBoard)
				 componentBoardBase.Dispose();
		 }
	 }
 }