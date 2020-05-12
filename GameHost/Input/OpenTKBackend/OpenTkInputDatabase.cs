using System;
using System.Collections.Generic;
using DryIoc;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Entities;
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
        private IGameWindow window;

        protected override void OnInit()
        {
            base.OnInit();
            keyPresses = new Dictionary<string, KeyState>((int) Key.NonUSBackSlash);
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                keyPresses[InputManagerNaming.GetKeyId(key)] = new KeyState();
            }
        }

        private void resetKeys()
        {
            foreach (var keyPress in keyPresses)
                keyPresses[keyPress.Key].IsDown = false;
        }

        protected override void OnUpdate()
        {
            scheduler.AddOnce(resetKeys);
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
            scheduler.Add(() => keyPresses[InputManagerNaming.GetKeyId(obj.Key)].IsDown = true);
        }

        public OpenTkInputBackend(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref scheduler);
        }
    }
}
