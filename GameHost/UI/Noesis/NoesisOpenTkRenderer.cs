using System;
using System.Collections.Concurrent;
using System.IO.Compression;
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

        private ConcurrentQueue<MouseButtonEventArgs> mouseButtonEvents  = new ConcurrentQueue<MouseButtonEventArgs>();
        private ConcurrentQueue<MouseWheelEventArgs>  mouseWheelEvents   = new ConcurrentQueue<MouseWheelEventArgs>();
        private ConcurrentQueue<MouseMoveEventArgs>   mouseMoveEvents    = new ConcurrentQueue<MouseMoveEventArgs>();
        private ConcurrentQueue<ResizeEventArgs>      windowResizeEvents = new ConcurrentQueue<ResizeEventArgs>();

        public override void Update(double time)
        {
            while (mouseButtonEvents.TryDequeue(out var mbEvent))
            {
                if (mbEvent.IsPressed)
                    View?.MouseButtonDown((int)Window.MousePosition.X, (int)Window.MousePosition.Y, ToNoesis(mbEvent.Button));
                else
                    View?.MouseButtonUp((int)Window.MousePosition.X, (int)Window.MousePosition.Y, ToNoesis(mbEvent.Button));
            }

            while (mouseWheelEvents.TryDequeue(out var mwEvent))
            {
                View?.MouseWheel((int)mwEvent.OffsetX, (int)mwEvent.OffsetY, 0);
            }

            while (mouseMoveEvents.TryDequeue(out var mvEvent))
            {
                View?.MouseMove((int)mvEvent.X, (int)mvEvent.Y);
            }

            while (windowResizeEvents.TryDequeue(out var wrEvent))
            {
                SetSize(wrEvent.Width, wrEvent.Height);
            }

            mouseButtonEvents.Clear();
            mouseWheelEvents.Clear();
            mouseMoveEvents.Clear();
            windowResizeEvents.Clear();

            base.Update(time);
        }

        private void WindowOnMouseAct(MouseButtonEventArgs  obj) => mouseButtonEvents.Enqueue(obj);
        private void WindowOnMouseWheel(MouseWheelEventArgs obj) => mouseWheelEvents.Enqueue(obj);
        private void WindowOnMouseMove(MouseMoveEventArgs   obj) => mouseMoveEvents.Enqueue(obj);
        private void WindowOnResize(ResizeEventArgs         obj) => windowResizeEvents.Enqueue(obj);

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
