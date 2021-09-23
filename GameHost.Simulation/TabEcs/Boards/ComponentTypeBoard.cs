using System;
using System.Runtime.InteropServices;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs.Boards
{
    public class ComponentTypeBoardContainer : BoardWithRowCollectionBase
    {
        private (string[] name, int[] size, ComponentBoardBase[] componentBoard, ComponentType[] parentType) column;

        public ComponentTypeBoardContainer(int capacity) : base(capacity)
        {
            column.name = new string[0];
            column.size = new int[0];
            column.componentBoard = new ComponentBoardBase[0];
            column.parentType = new ComponentType[0];

            CheckSafetyIssue = false;
        }

        public Span<string> NameColumns => column.name;
        public ReadOnlySpan<int> SizeColumns => column.size;
        public ReadOnlySpan<ComponentBoardBase> ComponentBoardColumns => column.componentBoard;
        public ReadOnlySpan<ComponentType> ParentTypeColumns => column.parentType;

        public Span<ComponentType> Registered => MemoryMarshal.Cast<uint, ComponentType>(Rows.UsedRows);

        public void SetRowName(uint row, string name)
        {
            GetColumn(row, ref column.name) = name;
        }

        public uint CreateRow(string name, ComponentBoardBase componentBoard,
            ComponentType optionalParentType = default)
        {
            var row = CreateRow();
            GetColumn(row, ref column.name) = name;
            GetColumn(row, ref column.size) = componentBoard.Size;
            GetColumn(row, ref column.componentBoard) = componentBoard;
            GetColumn(row, ref column.parentType) = optionalParentType;
            return row;
        }

        public override void Dispose()
        {
            base.Dispose();

            for (var i = 0; i < column.componentBoard.Length; i++)
            {
                var componentBoardBase = column.componentBoard[i];
                if (componentBoardBase == null)
                    continue;

                try
                {
                    componentBoardBase.Dispose();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error when disposing component type {column.name[i]}");
                    throw;
                }
            }

            column.name = null;
            column.size = null;
        }

        public override void Clear()
        {
            base.Clear();

            for (var i = 0; i < column.componentBoard.Length; i++)
            {
                var componentBoardBase = column.componentBoard[i];
                if (componentBoardBase == null)
                    continue;

                try
                {
                    componentBoardBase.Clear();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error when clearing component type {column.name[i]}");
                    throw;
                }
            }

            column.name.AsSpan().Clear();
            column.size.AsSpan().Clear();
        }
    }
}