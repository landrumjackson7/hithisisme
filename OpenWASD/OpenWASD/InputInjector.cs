using System;
using System.Runtime.InteropServices;

namespace OpenWASD
{
    /// <summary>Mouse buttons that can be driven from a gamepad.</summary>
    public enum MouseButton
    {
        None = 0,
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }

    /// <summary>Injects synthetic keyboard and mouse events through SendInput.</summary>
    public static class InputInjector
    {
        private const int InputMouse = 0;
        private const int InputKeyboard = 1;

        private const uint KeyEventKeyUp = 0x0002;
        private const uint KeyEventScanCode = 0x0008;
        private const uint KeyEventExtendedKey = 0x0001;
        private const uint KeyEventUnicode = 0x0004;

        private const uint MouseEventMove = 0x0001;
        private const uint MouseEventLeftDown = 0x0002;
        private const uint MouseEventLeftUp = 0x0004;
        private const uint MouseEventRightDown = 0x0008;
        private const uint MouseEventRightUp = 0x0010;
        private const uint MouseEventMiddleDown = 0x0020;
        private const uint MouseEventMiddleUp = 0x0040;
        private const uint MouseEventXDown = 0x0080;
        private const uint MouseEventXUp = 0x0100;
        private const uint MouseEventWheel = 0x0800;
        private const uint MouseEventHWheel = 0x1000;

        private const uint XButton1Data = 0x0001;
        private const uint XButton2Data = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int Dx;
            public int Dy;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HardwareInput
        {
            public uint Message;
            public ushort ParamL;
            public ushort ParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MouseInput Mouse;
            [FieldOffset(0)] public KeyboardInput Keyboard;
            [FieldOffset(0)] public HardwareInput Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public int Type;
            public InputUnion Union;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInput);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint code, uint mapType);

        private static readonly int InputSize = Marshal.SizeOf(typeof(Input));

        /// <summary>Presses or releases a virtual key, using scan codes so DirectInput games observe it.</summary>
        public static void SendKey(ushort virtualKey, bool pressed)
        {
            if (virtualKey == 0)
                return;

            uint flags = KeyEventScanCode;
            if (!pressed)
                flags |= KeyEventKeyUp;
            if (IsExtendedKey(virtualKey))
                flags |= KeyEventExtendedKey;

            var scanCode = (ushort)MapVirtualKey(virtualKey, 0);
            if (scanCode == 0)
                flags &= ~KeyEventScanCode;

            var input = new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = (flags & KeyEventScanCode) != 0 ? (ushort)0 : virtualKey,
                        ScanCode = scanCode,
                        Flags = flags
                    }
                }
            };
            SendInput(1, new[] { input }, InputSize);
        }

        /// <summary>Types a single character regardless of the active keyboard layout.</summary>
        public static void SendUnicode(char character, bool pressed)
        {
            uint flags = KeyEventUnicode;
            if (!pressed)
                flags |= KeyEventKeyUp;

            var input = new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInput { ScanCode = character, Flags = flags }
                }
            };
            SendInput(1, new[] { input }, InputSize);
        }

        public static void SendMouseButton(MouseButton button, bool pressed)
        {
            uint flags;
            uint data = 0;
            switch (button)
            {
                case MouseButton.Left:
                    flags = pressed ? MouseEventLeftDown : MouseEventLeftUp;
                    break;
                case MouseButton.Right:
                    flags = pressed ? MouseEventRightDown : MouseEventRightUp;
                    break;
                case MouseButton.Middle:
                    flags = pressed ? MouseEventMiddleDown : MouseEventMiddleUp;
                    break;
                case MouseButton.XButton1:
                    flags = pressed ? MouseEventXDown : MouseEventXUp;
                    data = XButton1Data;
                    break;
                case MouseButton.XButton2:
                    flags = pressed ? MouseEventXDown : MouseEventXUp;
                    data = XButton2Data;
                    break;
                default:
                    return;
            }

            var input = new Input
            {
                Type = InputMouse,
                Union = new InputUnion { Mouse = new MouseInput { Flags = flags, MouseData = data } }
            };
            SendInput(1, new[] { input }, InputSize);
        }

        /// <summary>Moves the cursor by a relative pixel delta.</summary>
        public static void MoveMouse(int dx, int dy)
        {
            if (dx == 0 && dy == 0)
                return;

            var input = new Input
            {
                Type = InputMouse,
                Union = new InputUnion { Mouse = new MouseInput { Dx = dx, Dy = dy, Flags = MouseEventMove } }
            };
            SendInput(1, new[] { input }, InputSize);
        }

        /// <summary>Scrolls the wheel. One notch is 120 units.</summary>
        public static void Scroll(int delta, bool horizontal)
        {
            if (delta == 0)
                return;

            var input = new Input
            {
                Type = InputMouse,
                Union = new InputUnion
                {
                    Mouse = new MouseInput
                    {
                        MouseData = unchecked((uint)delta),
                        Flags = horizontal ? MouseEventHWheel : MouseEventWheel
                    }
                }
            };
            SendInput(1, new[] { input }, InputSize);
        }

        private static bool IsExtendedKey(ushort virtualKey)
        {
            switch (virtualKey)
            {
                case 0x21: // PageUp
                case 0x22: // PageDown
                case 0x23: // End
                case 0x24: // Home
                case 0x25: // Left
                case 0x26: // Up
                case 0x27: // Right
                case 0x28: // Down
                case 0x2D: // Insert
                case 0x2E: // Delete
                case 0x90: // NumLock
                case 0x6F: // Divide
                case 0xA3: // RControl
                case 0xA5: // RMenu
                    return true;
                default:
                    return false;
            }
        }
    }
}
