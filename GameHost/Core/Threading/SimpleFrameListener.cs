using System.Collections.Generic;

namespace GameHost.Core.Threading
{
    public class SimpleFrameListener : IFrameListener
    {
        public int FrameLimit = 10;
        
        public List<WorkerFrame> Frames = new List<WorkerFrame>();

        public bool Add(WorkerFrame frame)
        {
            lock (Frames)
            {
                if (Frames.Count > FrameLimit)
                    return false;

                Frames.Add(frame);
                return true;
            }
        }

        public void DequeueAll(IList<WorkerFrame> list)
        {
            while (Frames.Count > 0)
            {
                list.Add(Frames[0]);
                Frames.RemoveAt(0);
            }
        }
    }
}
