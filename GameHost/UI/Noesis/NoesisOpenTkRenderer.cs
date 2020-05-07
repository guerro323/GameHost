using System;
using System.Collections.Concurrent;
using System.Threading;
using Noesis;
using OpenToolkit.Windowing.Common;
using MouseButtonEventArgs = OpenToolkit.Windowing.Common.MouseButtonEventArgs;
using MouseWheelEventArgs = OpenToolkit.Windowing.Common.MouseWheelEventArgs;

namespace GameHost.UI.Noesis
{
    public class NoesisOpenTkRenderer : NoesisBasicRenderer
    {
        public NoesisOpenTkRenderer(INativeWindow window)
        {
            Console.WriteLine("created on: " + Thread.CurrentThread.Name);

            Window = window;
            SetWindow(window);
        }

        public INativeWindow Window { get; private set; }

        public override void Dispose()
        {
            base.Dispose();
            SetWindow(null);
        }

        private void SetWindow(INativeWindow window)
        {
            if (Window != null)
            {
                Window.Resize     -= WindowOnResize;
                Window.MouseMove  -= WindowOnMouseMove;
                Window.MouseWheel -= WindowOnMouseWheel;
                Window.MouseDown  -= WindowOnMouseAct;
                Window.MouseUp    -= WindowOnMouseAct;
            }

            Window = window;
            if (Window != null)
            {
                Window.Resize     += WindowOnResize;
                Window.MouseMove  += WindowOnMouseMove;
                Window.MouseWheel += WindowOnMouseWheel;
                Window.MouseDown  += WindowOnMouseAct;
                Window.MouseUp    += WindowOnMouseAct;
            }
        }

        private ConcurrentStack<MouseButtonEventArgs> mouseButtonEvents  = new ConcurrentStack<MouseButtonEventArgs>();
        private ConcurrentStack<MouseWheelEventArgs>  mouseWheelEvents   = new ConcurrentStack<MouseWheelEventArgs>();
        private ConcurrentStack<MouseMoveEventArgs>   mouseMoveEvents    = new ConcurrentStack<MouseMoveEventArgs>();
        private ConcurrentStack<ResizeEventArgs>      windowResizeEvents = new ConcurrentStack<ResizeEventArgs>();

        public override void Update(double time)
        {
            while (mouseButtonEvents.TryPop(out var mbEvent))
            {
                if (mbEvent.IsPressed)
                    View.MouseButtonDown((int)Window.MousePosition.X, (int)Window.MousePosition.Y, ToNoesis(mbEvent.Button));
                else
                    View.MouseButtonUp((int)Window.MousePosition.X, (int)Window.MousePosition.Y, ToNoesis(mbEvent.Button));
            }

            while (mouseWheelEvents.TryPop(out var mwEvent))
            {
                View.MouseWheel((int)mwEvent.OffsetX, (int)mwEvent.OffsetY, 0);
            }

            while (mouseMoveEvents.TryPop(out var mvEvent))
            {
                View.MouseMove((int)mvEvent.X, (int)mvEvent.Y);
            }

            while (windowResizeEvents.TryPop(out var wrEvent))
            {
                SetSize(wrEvent.Width, wrEvent.Height);
            }

            mouseButtonEvents.Clear();
            mouseWheelEvents.Clear();
            mouseMoveEvents.Clear();
            windowResizeEvents.Clear();

            base.Update(time);
        }

        private void WindowOnMouseAct(MouseButtonEventArgs  obj) => mouseButtonEvents.Push(obj);
        private void WindowOnMouseWheel(MouseWheelEventArgs obj) => mouseWheelEvents.Push(obj);
        private void WindowOnMouseMove(MouseMoveEventArgs   obj) => mouseMoveEvents.Push(obj);
        private void WindowOnResize(ResizeEventArgs         obj) => windowResizeEvents.Push(obj);

        private static MouseButton ToNoesis(OpenToolkit.Windowing.Common.Input.MouseButton button)
        {
            return button switch
            {
                OpenToolkit.Windowing.Common.Input.MouseButton.Left => MouseButton.Left,
                OpenToolkit.Windowing.Common.Input.MouseButton.Middle => MouseButton.Middle,
                OpenToolkit.Windowing.Common.Input.MouseButton.Right => MouseButton.Right,
                OpenToolkit.Windowing.Common.Input.MouseButton.Button4 => MouseButton.XButton1,
                OpenToolkit.Windowing.Common.Input.MouseButton.Button5 => MouseButton.XButton2,
                _ => MouseButton.Left
            };
        }
    }
}
