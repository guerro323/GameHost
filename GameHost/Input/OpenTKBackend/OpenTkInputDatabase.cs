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
            public bool IsActive;
            public bool IsDown, IsUp;
        }

        private Dictionary<string, KeyState> keyPresses;
        private List<string>                 wantedKeysDown;
        private List<string>                 wantedKeysUp;

        private IGameWindow window;

        private Action resetKeyFunction;

        protected override void OnInit()
        {
            base.OnInit();
            var comparer = StringComparer.InvariantCultureIgnoreCase;
            keyPresses     = new Dictionary<string, KeyState>((int)Key.NonUSBackSlash, comparer);
            wantedKeysDown = new List<string>(keyPresses.Count);
            wantedKeysUp   = new List<string>(keyPresses.Count);

            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                keyPresses[InputManagerNaming.GetKeyId(key)] = new KeyState();
            }

            resetKeyFunction = () =>
            {
                foreach (var kvp in keyPresses)
                {
                    kvp.Value.IsDown = false;
                    kvp.Value.IsUp   = false;
                }

                for (var i = 0; i != wantedKeysDown.Count; i++)
                {
                    // this disable support for when GLFW invoke repeated buttons
                    // todo: we should have proper support for repeated buttons
                    if (keyPresses[wantedKeysDown[i]].IsActive)
                        continue;
                    
                    keyPresses[wantedKeysDown[i]].IsDown   = true;
                    keyPresses[wantedKeysDown[i]].IsActive = true;
                }

                for (var i = 0; i != wantedKeysUp.Count; i++)
                {
                    keyPresses[wantedKeysUp[i]].IsUp     = true;
                    keyPresses[wantedKeysUp[i]].IsActive = false;
                }

                wantedKeysDown.Clear();
                wantedKeysUp.Clear();
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
            window.KeyUp   += OnKeyUp;
        }

        public override InputState GetInputState(string inputName)
        {
            var p = keyPresses[inputName];
            return new InputState {Down = p.IsDown ? 1u : 0, Up = p.IsUp ? 1u : 0, Real = p.IsActive ? 1 : 0, Active = p.IsActive};
        }

        private void OnKeyDown(KeyboardKeyEventArgs obj)
        {
            scheduler.Add(() =>
            {
                wantedKeysDown.Add(InputManagerNaming.GetKeyId(obj.Key));
            });
        }

        private void OnKeyUp(KeyboardKeyEventArgs obj)
        {
            scheduler.Add(() =>
            {
                wantedKeysUp.Add(InputManagerNaming.GetKeyId(obj.Key));
            });
        }

        public OpenTkInputBackend(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref scheduler);
        }
    }
}
