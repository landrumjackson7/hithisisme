using System;
using System.Runtime.InteropServices;

namespace OpenWASD
{
    /// <summary>Raw XInput gamepad button flags.</summary>
    [Flags]
    public enum GamepadButton : ushort
    {
        None = 0x0000,
        DPadUp = 0x0001,
        DPadDown = 0x0002,
        DPadLeft = 0x0004,
        DPadRight = 0x0008,
        Start = 0x0010,
        Back = 0x0020,
        LeftThumb = 0x0040,
        RightThumb = 0x0080,
        LeftShoulder = 0x0100,
        RightShoulder = 0x0200,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XInputVibration
    {
        public ushort LeftMotorSpeed;
        public ushort RightMotorSpeed;
    }

    /// <summary>
    /// Thin managed wrapper over XInput. Resolves the newest available XInput
    /// runtime at first use (1_4 on Win8+, falling back to 1_3 / 9_1_0).
    /// </summary>
    public static class XInput
    {
        public const int MaxControllers = 4;
        private const int ErrorSuccess = 0;

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern int GetState14(int userIndex, ref XInputState state);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern int SetState14(int userIndex, ref XInputVibration vibration);

        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
        private static extern int GetState13(int userIndex, ref XInputState state);

        [DllImport("xinput1_3.dll", EntryPoint = "XInputSetState")]
        private static extern int SetState13(int userIndex, ref XInputVibration vibration);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern int GetState910(int userIndex, ref XInputState state);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputSetState")]
        private static extern int SetState910(int userIndex, ref XInputVibration vibration);

        private enum Runtime { Unresolved, V14, V13, V910, Unavailable }

        private static Runtime _runtime = Runtime.Unresolved;

        /// <summary>True when any usable XInput runtime is present on this machine.</summary>
        public static bool IsAvailable
        {
            get
            {
                Resolve();
                return _runtime != Runtime.Unavailable;
            }
        }

        private static void Resolve()
        {
            if (_runtime != Runtime.Unresolved)
                return;

            var probe = new XInputState();
            foreach (var candidate in new[] { Runtime.V14, Runtime.V13, Runtime.V910 })
            {
                try
                {
                    switch (candidate)
                    {
                        case Runtime.V14: GetState14(0, ref probe); break;
                        case Runtime.V13: GetState13(0, ref probe); break;
                        default: GetState910(0, ref probe); break;
                    }
                    _runtime = candidate;
                    return;
                }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }
            }
            _runtime = Runtime.Unavailable;
        }

        /// <summary>Reads the state of a controller slot. Returns false when nothing is plugged in.</summary>
        public static bool TryGetState(int userIndex, out XInputState state)
        {
            state = new XInputState();
            Resolve();
            switch (_runtime)
            {
                case Runtime.V14: return GetState14(userIndex, ref state) == ErrorSuccess;
                case Runtime.V13: return GetState13(userIndex, ref state) == ErrorSuccess;
                case Runtime.V910: return GetState910(userIndex, ref state) == ErrorSuccess;
                default: return false;
            }
        }

        /// <summary>Sets rumble motor speeds (0..65535) for a controller slot.</summary>
        public static void SetVibration(int userIndex, ushort leftMotor, ushort rightMotor)
        {
            Resolve();
            var vibration = new XInputVibration { LeftMotorSpeed = leftMotor, RightMotorSpeed = rightMotor };
            switch (_runtime)
            {
                case Runtime.V14: SetState14(userIndex, ref vibration); break;
                case Runtime.V13: SetState13(userIndex, ref vibration); break;
                case Runtime.V910: SetState910(userIndex, ref vibration); break;
            }
        }
    }
}
