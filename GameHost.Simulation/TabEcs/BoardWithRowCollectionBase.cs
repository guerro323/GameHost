using System;
using System.Diagnostics;
using System.Threading;
using GameHost.Simulation.TabEcs.LLAPI;

namespace GameHost.Simulation.TabEcs
{
    public abstract class BoardWithRowCollectionBase : GameWorldBoardBase
    {
        protected UIntRowCollectionBase Rows;

        internal Thread callerThread;
        protected bool CheckSafetyIssue;

        public BoardWithRowCollectionBase(GameWorld gameWorld) : base(gameWorld)
        {
            Rows = new UIntRowCollectionBase(0);

            callerThread = Thread.CurrentThread;
            CheckSafetyIssue = true;
        }

        public override void Dispose()
        {
            Rows.Clear();
        }

        public void SetCallerThread(Thread callerThread)
        {
            this.callerThread = callerThread;
        }

        [Conditional("DEBUG")]
        protected void CheckForThreadSafety()
        {
            if (CheckSafetyIssue && Thread.CurrentThread != callerThread)
                throw new InvalidOperationException(
                    $"Thread Safety Issue! Current={Thread.CurrentThread.Name}({Thread.CurrentThread.ManagedThreadId}), Original={callerThread.Name}({callerThread.ManagedThreadId})");
        }

        public virtual uint CreateRow()
        {
            CheckForThreadSafety();

            return Rows.CreateRow();
        }

        public virtual void CreateRowBulk(Span<uint> rows)
        {
            CheckForThreadSafety();

            Rows.CreateRowBulk(rows);
        }

        public virtual ref T GetColumn<T>(uint row, ref T[] array)
        {
            return ref Rows.GetColumn(row, ref array);
        }

        public virtual bool DeleteRow(uint row)
        {
            CheckForThreadSafety();

            return Rows.TrySetUnusedRow(row);
        }

        public virtual void Clear()
        {
            Rows.Clear();
        }
    }
}