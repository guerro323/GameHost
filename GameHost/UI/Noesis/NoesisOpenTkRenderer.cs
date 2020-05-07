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

        private void WindowOnMouseAct(MouseButtonEventArgs obj)
        {
            if (obj.IsPressed)
            {
                View.MouseButtonDown((int)Window.MousePosition.X, (int)Window.MousePosition.Y, ToNoesis(obj.Button));
            }
            else
            {
                View.MouseButtonUp((int)Window.MousePosition.X, (int)Window.MousePosition.Y, ToNoesis(obj.Button));
            }
        }

        private void WindowOnMouseWheel(MouseWheelEventArgs obj)
        {
            // what is the last argument?
            View.MouseWheel((int)obj.OffsetX, (int)obj.OffsetY, 0);
        }

        private void WindowOnMouseMove(MouseMoveEventArgs obj)
        {
            View.MouseMove((int)obj.X, (int)obj.Y);
        }

        private void WindowOnResize(ResizeEventArgs obj)
        {
            SetSize(obj.Width, obj.Height);
        }

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
