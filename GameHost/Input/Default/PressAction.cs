using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;

namespace GameHost.Input.Default
{
    public struct PressAction : IInputAction
    {
        public class Layout : InputLayoutBase
        {
            public Layout(string id, params CInput[] inputs) : base(id)
            {
                Inputs = new ReadOnlyCollection<CInput>(inputs);
            }
        }

        public uint DownCount, UpCount;

        public bool IsDown => DownCount > 0;

        public class Provider : InputProviderSystemBase<Provider, PressAction>
        {
            protected override void OnInit()
            {
                base.OnInit();

                Console.WriteLine("??????????????????????????????????????????????????????????");
            }

            protected override void OnInputThreadUpdate()
            {
                var currentLayout = World.Mgr.Get<InputCurrentLayout>()[0];
                using (InputSynchronizationBarrier.SafeTake())
                {
                    //Console.WriteLine("--init " + InputSet.Count);
                    foreach (ref readonly var entity in InputSet.GetEntities())
                    {
                        var layouts = entity.Get<InputActionLayouts>();
                        if (!layouts.TryGetOrDefault(currentLayout.Id, out var layout))
                            continue;

                        ref var action = ref entity.Get<PressAction>();
                        foreach (var input in layout.Inputs)
                        {
                            action.DownCount += Backend.GetInputState(input.Target).Down;
                        }
                    }
                }
            }

            protected override void OnReceiverUpdate()
            {
                using (InputSynchronizationBarrier.SafeTake())
                {
                    foreach (ref readonly var entity in InputSet.GetEntities())
                    {
                        ref var inputFromThread = ref entity.Get<InputThreadTarget>().Target.Get<PressAction>();
                        ref var selfInput       = ref entity.Get<PressAction>();

                        selfInput.DownCount = inputFromThread.DownCount;
                        inputFromThread     = default;
                    }
                }
            }

            public Provider(WorldCollection collection) : base(collection)
            {
            }
        }
    }
}
