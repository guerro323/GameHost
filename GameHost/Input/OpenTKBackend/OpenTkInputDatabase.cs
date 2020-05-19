using System;
using System.Collections.Generic;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;

namespace GameHost.Input.OpenTKBackend
{
    public class OpenTkInputBackend : InputBackendBase
    {
        private IScheduler scheduler;
        
        private class KeyState
        {
            public bool IsDown;
        }
        
        private Dictionary<string, KeyState> keyPresses;
        private List<string> wantedKeysDown;
        
        private IGameWindow window;

        private Action resetKeyFunction;
        
        protected override void OnInit()
        {
            base.OnInit();
            keyPresses = new Dictionary<string, KeyState>((int) Key.NonUSBackSlash);
            wantedKeysDown = new List<string>(keyPresses.Count);
            
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                keyPresses[InputManagerNaming.GetKeyId(key)] = new KeyState();
            }

            resetKeyFunction = () =>
            {
                foreach (var kvp in keyPresses) 
                    kvp.Value.IsDown = false;
                for (var i = 0; i != wantedKeysDown.Count; i++)
                    keyPresses[wantedKeysDown[i]].IsDown = true;
                wantedKeysDown.Clear();
            };
        }

        protected override void OnUpdate()
        {
            scheduler.AddOnce(resetKeyFunction);
        }

        protected internal override void OnDisable()
        {
            window = null;
        }

        protected internal override void OnEnable()
        {
            var strategy = new ContextBindingStrategy(Context, resolveInParent: true);
            window = strategy.Resolve<IGameWindow>();
            
            window.KeyDown += OnKeyDown;
        }

        public bool IsKeyDown(Key key)
        {
            return keyPresses[InputManagerNaming.GetKeyId(key)].IsDown;
        }

        private void OnKeyDown(KeyboardKeyEventArgs obj)
        {
            scheduler.Add(() =>
            {
                wantedKeysDown.Add(InputManagerNaming.GetKeyId(obj.Key));
            });
        }

        public OpenTkInputBackend(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref scheduler);
        }
    }
}
