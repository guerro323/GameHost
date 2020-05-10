using System;
using System.Collections.Generic;
using DryIoc;
using GameHost.Core.Ecs;
using GameHost.Entities;
using GameHost.Injection;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;

namespace GameHost.Input.OpenTKBackend
{
    public class OpenTkInputBackend : InputBackendBase
    {
        private Dictionary<string, bool> keyPresses;
        private IGameWindow window;

        protected override void OnInit()
        {
            base.OnInit();
            keyPresses = new Dictionary<string, bool>((int) Key.NonUSBackSlash);
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                keyPresses[InputManagerNaming.GetKeyId(key)] = false;
            }
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
            window.KeyUp += OnKeyUp;
        }

        public bool IsKeyDown(Key key)
        {
            return keyPresses[InputManagerNaming.GetKeyId(key)];
        }

        private void OnKeyDown(KeyboardKeyEventArgs obj)
        {
            keyPresses[InputManagerNaming.GetKeyId(obj.Key)] = true;
        }
        
        private void OnKeyUp(KeyboardKeyEventArgs obj)
        {
            keyPresses[InputManagerNaming.GetKeyId(obj.Key)] = false;
        }

        public OpenTkInputBackend(WorldCollection collection) : base(collection)
        {
        }
    }
}
