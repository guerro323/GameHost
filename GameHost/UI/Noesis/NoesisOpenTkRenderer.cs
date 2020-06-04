using System;
using System.Collections.Concurrent;
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
                Window.KeyDown -= WindowOnKeyDown;
                Window.KeyUp -= WindowOnKeyUp;
                Window.TextInput -= WindowOnKeyInput;
            }

            Window = window;
            if (Window != null)
            {
                Window.Resize     += WindowOnResize;
                Window.MouseMove  += WindowOnMouseMove;
                Window.MouseWheel += WindowOnMouseWheel;
                Window.MouseDown  += WindowOnMouseAct;
                Window.MouseUp    += WindowOnMouseAct;
                Window.KeyDown += WindowOnKeyDown;
                Window.KeyUp   += WindowOnKeyUp;
                Window.TextInput += WindowOnKeyInput;
            }
        }

        private ConcurrentQueue<MouseButtonEventArgs> mouseButtonEvents  = new ConcurrentQueue<MouseButtonEventArgs>();
        private ConcurrentQueue<MouseWheelEventArgs>  mouseWheelEvents   = new ConcurrentQueue<MouseWheelEventArgs>();
        private ConcurrentQueue<MouseMoveEventArgs>   mouseMoveEvents    = new ConcurrentQueue<MouseMoveEventArgs>();
        private ConcurrentQueue<ResizeEventArgs>      windowResizeEvents = new ConcurrentQueue<ResizeEventArgs>();
        private ConcurrentQueue<KeyboardKeyEventArgs>      keyDownEvents = new ConcurrentQueue<KeyboardKeyEventArgs>();
        private ConcurrentQueue<KeyboardKeyEventArgs>      keyUpEvents = new ConcurrentQueue<KeyboardKeyEventArgs>();
        private ConcurrentQueue<TextInputEventArgs>      textInputEvents = new ConcurrentQueue<TextInputEventArgs>();

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

            while (keyDownEvents.TryDequeue(out var kEvent))
            {
                View?.KeyDown(ToNoesis(kEvent.Key));
            }
            
            while (keyUpEvents.TryDequeue(out var kEvent))
            {
                View?.KeyUp(ToNoesis(kEvent.Key));
            }

            while (textInputEvents.TryDequeue(out var tiEvent))
            {
                View?.Char((uint) tiEvent.Unicode);
            }

            base.Update(time);
        }

        private void WindowOnMouseAct(MouseButtonEventArgs  obj) => mouseButtonEvents.Enqueue(obj);
        private void WindowOnMouseWheel(MouseWheelEventArgs obj) => mouseWheelEvents.Enqueue(obj);
        private void WindowOnMouseMove(MouseMoveEventArgs   obj) => mouseMoveEvents.Enqueue(obj);
        private void WindowOnResize(ResizeEventArgs         obj) => windowResizeEvents.Enqueue(obj);
        private void WindowOnKeyDown(KeyboardKeyEventArgs obj) => keyDownEvents.Enqueue(obj);
        private void WindowOnKeyUp(KeyboardKeyEventArgs   obj) => keyUpEvents.Enqueue(obj);
        private void WindowOnKeyInput(TextInputEventArgs   obj) => textInputEvents.Enqueue(obj);

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
        
        private static Key ToNoesis(OpenToolkit.Windowing.Common.Input.Key key)
        {
            return key switch
            {
                OpenToolkit.Windowing.Common.Input.Key.Unknown => Key.None,
                OpenToolkit.Windowing.Common.Input.Key.ShiftLeft => Key.LeftShift,
                OpenToolkit.Windowing.Common.Input.Key.ShiftRight => Key.RightShift,
                OpenToolkit.Windowing.Common.Input.Key.ControlLeft => Key.LeftCtrl,
                OpenToolkit.Windowing.Common.Input.Key.ControlRight => Key.RightCtrl,
                OpenToolkit.Windowing.Common.Input.Key.AltLeft => Key.LeftAlt,
                OpenToolkit.Windowing.Common.Input.Key.AltRight => Key.RightAlt,
                OpenToolkit.Windowing.Common.Input.Key.WinLeft => Key.LWin,
                OpenToolkit.Windowing.Common.Input.Key.WinRight => Key.RWin,
                OpenToolkit.Windowing.Common.Input.Key.F1 => Key.F1,
                OpenToolkit.Windowing.Common.Input.Key.F2 => Key.F2,
                OpenToolkit.Windowing.Common.Input.Key.F3 => Key.F3,
                OpenToolkit.Windowing.Common.Input.Key.F4 => Key.F4,
                OpenToolkit.Windowing.Common.Input.Key.F5 => Key.F5,
                OpenToolkit.Windowing.Common.Input.Key.F6 => Key.F6,
                OpenToolkit.Windowing.Common.Input.Key.F7 => Key.F7,
                OpenToolkit.Windowing.Common.Input.Key.F8 => Key.F8,
                OpenToolkit.Windowing.Common.Input.Key.F9 => Key.F9,
                OpenToolkit.Windowing.Common.Input.Key.F10 => Key.F10,
                OpenToolkit.Windowing.Common.Input.Key.F11 => Key.F11,
                OpenToolkit.Windowing.Common.Input.Key.F12 => Key.F12,
                OpenToolkit.Windowing.Common.Input.Key.F13 => Key.F13,
                OpenToolkit.Windowing.Common.Input.Key.F14 => Key.F14,
                OpenToolkit.Windowing.Common.Input.Key.F15 => Key.F15,
                OpenToolkit.Windowing.Common.Input.Key.F16 => Key.F16,
                OpenToolkit.Windowing.Common.Input.Key.F17 => Key.F17,
                OpenToolkit.Windowing.Common.Input.Key.F18 => Key.F18,
                OpenToolkit.Windowing.Common.Input.Key.F19 => Key.F19,
                OpenToolkit.Windowing.Common.Input.Key.F20 => Key.F20,
                OpenToolkit.Windowing.Common.Input.Key.F21 => Key.F21,
                OpenToolkit.Windowing.Common.Input.Key.F22 => Key.F22,
                OpenToolkit.Windowing.Common.Input.Key.F23 => Key.F23,
                OpenToolkit.Windowing.Common.Input.Key.F24 => Key.F24,
                OpenToolkit.Windowing.Common.Input.Key.Up => Key.Up,
                OpenToolkit.Windowing.Common.Input.Key.Down => Key.Down,
                OpenToolkit.Windowing.Common.Input.Key.Left => Key.Left,
                OpenToolkit.Windowing.Common.Input.Key.Right => Key.Right,
                OpenToolkit.Windowing.Common.Input.Key.Enter => Key.Enter,
                OpenToolkit.Windowing.Common.Input.Key.Escape => Key.Escape,
                OpenToolkit.Windowing.Common.Input.Key.Space => Key.Space,
                OpenToolkit.Windowing.Common.Input.Key.Tab => Key.Tab,
                OpenToolkit.Windowing.Common.Input.Key.BackSpace => Key.Back,
                OpenToolkit.Windowing.Common.Input.Key.Insert => Key.Insert,
                OpenToolkit.Windowing.Common.Input.Key.Delete => Key.Delete,
                OpenToolkit.Windowing.Common.Input.Key.PageUp => Key.PageUp,
                OpenToolkit.Windowing.Common.Input.Key.PageDown => Key.PageDown,
                OpenToolkit.Windowing.Common.Input.Key.Home => Key.Home,
                OpenToolkit.Windowing.Common.Input.Key.End => Key.End,
                OpenToolkit.Windowing.Common.Input.Key.CapsLock => Key.CapsLock,
                OpenToolkit.Windowing.Common.Input.Key.ScrollLock => Key.Scroll,
                OpenToolkit.Windowing.Common.Input.Key.PrintScreen => Key.PrintScreen,
                OpenToolkit.Windowing.Common.Input.Key.Pause => Key.Pause,
                OpenToolkit.Windowing.Common.Input.Key.NumLock => Key.NumLock,
                OpenToolkit.Windowing.Common.Input.Key.Clear => Key.Clear,
                OpenToolkit.Windowing.Common.Input.Key.Sleep => Key.Sleep,
                OpenToolkit.Windowing.Common.Input.Key.Keypad0 => Key.NumPad0,
                OpenToolkit.Windowing.Common.Input.Key.Keypad1 => Key.NumPad1,
                OpenToolkit.Windowing.Common.Input.Key.Keypad2 => Key.NumPad2,
                OpenToolkit.Windowing.Common.Input.Key.Keypad3 => Key.NumPad3,
                OpenToolkit.Windowing.Common.Input.Key.Keypad4 => Key.NumPad4,
                OpenToolkit.Windowing.Common.Input.Key.Keypad5 => Key.NumPad5,
                OpenToolkit.Windowing.Common.Input.Key.Keypad6 => Key.NumPad6,
                OpenToolkit.Windowing.Common.Input.Key.Keypad7 => Key.NumPad7,
                OpenToolkit.Windowing.Common.Input.Key.Keypad8 => Key.NumPad8,
                OpenToolkit.Windowing.Common.Input.Key.Keypad9 => Key.NumPad9,
                OpenToolkit.Windowing.Common.Input.Key.KeypadDivide => Key.Divide,
                OpenToolkit.Windowing.Common.Input.Key.KeypadMultiply => Key.Multiply,
                OpenToolkit.Windowing.Common.Input.Key.KeypadSubtract => Key.Subtract,
                OpenToolkit.Windowing.Common.Input.Key.KeypadAdd => Key.Add,
                OpenToolkit.Windowing.Common.Input.Key.KeypadEnter => Key.Enter,
                OpenToolkit.Windowing.Common.Input.Key.A => Key.A,
                OpenToolkit.Windowing.Common.Input.Key.B => Key.B,
                OpenToolkit.Windowing.Common.Input.Key.C => Key.C,
                OpenToolkit.Windowing.Common.Input.Key.D => Key.D,
                OpenToolkit.Windowing.Common.Input.Key.E => Key.E,
                OpenToolkit.Windowing.Common.Input.Key.F => Key.F,
                OpenToolkit.Windowing.Common.Input.Key.G => Key.G,
                OpenToolkit.Windowing.Common.Input.Key.H => Key.H,
                OpenToolkit.Windowing.Common.Input.Key.I => Key.I,
                OpenToolkit.Windowing.Common.Input.Key.J => Key.J,
                OpenToolkit.Windowing.Common.Input.Key.K => Key.K,
                OpenToolkit.Windowing.Common.Input.Key.L => Key.L,
                OpenToolkit.Windowing.Common.Input.Key.M => Key.M,
                OpenToolkit.Windowing.Common.Input.Key.N => Key.N,
                OpenToolkit.Windowing.Common.Input.Key.O => Key.O,
                OpenToolkit.Windowing.Common.Input.Key.P => Key.P,
                OpenToolkit.Windowing.Common.Input.Key.Q => Key.Q,
                OpenToolkit.Windowing.Common.Input.Key.R => Key.R,
                OpenToolkit.Windowing.Common.Input.Key.S => Key.S,
                OpenToolkit.Windowing.Common.Input.Key.T => Key.T,
                OpenToolkit.Windowing.Common.Input.Key.U => Key.U,
                OpenToolkit.Windowing.Common.Input.Key.V => Key.V,
                OpenToolkit.Windowing.Common.Input.Key.W => Key.W,
                OpenToolkit.Windowing.Common.Input.Key.X => Key.X,
                OpenToolkit.Windowing.Common.Input.Key.Y => Key.Y,
                OpenToolkit.Windowing.Common.Input.Key.Z => Key.Z,
                OpenToolkit.Windowing.Common.Input.Key.Number0 => Key.D0,
                OpenToolkit.Windowing.Common.Input.Key.Number1 => Key.D1,
                OpenToolkit.Windowing.Common.Input.Key.Number2 => Key.D2,
                OpenToolkit.Windowing.Common.Input.Key.Number3 => Key.D3,
                OpenToolkit.Windowing.Common.Input.Key.Number4 => Key.D4,
                OpenToolkit.Windowing.Common.Input.Key.Number5 => Key.D5,
                OpenToolkit.Windowing.Common.Input.Key.Number6 => Key.D6,
                OpenToolkit.Windowing.Common.Input.Key.Number7 => Key.D7,
                OpenToolkit.Windowing.Common.Input.Key.Number8 => Key.D8,
                OpenToolkit.Windowing.Common.Input.Key.Number9 => Key.D9,
                OpenToolkit.Windowing.Common.Input.Key.Tilde => Key.OemTilde,
                OpenToolkit.Windowing.Common.Input.Key.Minus => Key.OemMinus,
                OpenToolkit.Windowing.Common.Input.Key.Plus => Key.OemPlus,
                OpenToolkit.Windowing.Common.Input.Key.BracketLeft => Key.OemOpenBrackets,
                OpenToolkit.Windowing.Common.Input.Key.BracketRight => Key.OemCloseBrackets,
                OpenToolkit.Windowing.Common.Input.Key.Semicolon => Key.OemSemicolon,
                OpenToolkit.Windowing.Common.Input.Key.Quote => Key.OemQuotes,
                OpenToolkit.Windowing.Common.Input.Key.Comma => Key.OemComma,
                OpenToolkit.Windowing.Common.Input.Key.Period => Key.OemPeriod,
                OpenToolkit.Windowing.Common.Input.Key.Slash => Key.OemBackslash,
                OpenToolkit.Windowing.Common.Input.Key.BackSlash => Key.OemBackslash,
                OpenToolkit.Windowing.Common.Input.Key.NonUSBackSlash => Key.OemBackslash,
                _ => Key.None
            };
        }
    }
}
