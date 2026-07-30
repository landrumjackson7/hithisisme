using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenWASD
{
    /// <summary>Normalized snapshot of a controller, used by both the engine and the UI.</summary>
    public class ControllerSnapshot
    {
        public bool Connected { get; set; }
        public GamepadButton Buttons { get; set; }
        public double LeftTrigger { get; set; }
        public double RightTrigger { get; set; }
        public double LeftX { get; set; }
        public double LeftY { get; set; }
        public double RightX { get; set; }
        public double RightY { get; set; }

        public bool IsDown(GamepadButton button)
        {
            return (Buttons & button) != 0;
        }
    }

    /// <summary>
    /// Polls a controller slot and translates its state into keyboard and mouse
    /// output according to the active profile. All output is released when the
    /// engine stops, so no key can be left stuck down.
    /// </summary>
    public class MappingEngine : IDisposable
    {
        private const double TurboIntervalMs = 60;

        private readonly System.Windows.Forms.Timer _timer;
        private readonly Dictionary<ControlId, bool> _active = new Dictionary<ControlId, bool>();
        private readonly Dictionary<ControlId, Stopwatch> _turboClocks = new Dictionary<ControlId, Stopwatch>();
        private readonly Dictionary<ControlId, bool> _turboPhase = new Dictionary<ControlId, bool>();

        private double _mouseRemainderX;
        private double _mouseRemainderY;
        private double _scrollRemainder;
        private MappingProfile _profile;

        public MappingEngine()
        {
            _profile = MappingProfile.CreateFpsDefault();
            _timer = new System.Windows.Forms.Timer { Interval = _profile.PollIntervalMs };
            _timer.Tick += (s, e) => Poll();
        }

        /// <summary>Raised after every poll with the latest controller state.</summary>
        public event Action<ControllerSnapshot> Polled;

        /// <summary>XInput slot being remapped, 0..3.</summary>
        public int ControllerIndex { get; set; }

        public bool IsRunning { get; private set; }

        public MappingProfile Profile
        {
            get { return _profile; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                ReleaseAll();
                value.Normalize();
                _profile = value;
                _timer.Interval = value.PollIntervalMs;
            }
        }

        public void Start()
        {
            if (IsRunning)
                return;
            IsRunning = true;
            _timer.Interval = _profile.PollIntervalMs;
            _timer.Start();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;
            _timer.Stop();
            IsRunning = false;
            ReleaseAll();
        }

        /// <summary>Reads the controller without emitting output, for live UI preview.</summary>
        public ControllerSnapshot Read()
        {
            XInputState state;
            if (!XInput.TryGetState(ControllerIndex, out state))
                return new ControllerSnapshot();

            var pad = state.Gamepad;
            return new ControllerSnapshot
            {
                Connected = true,
                Buttons = (GamepadButton)pad.Buttons,
                LeftTrigger = pad.LeftTrigger / 255.0,
                RightTrigger = pad.RightTrigger / 255.0,
                LeftX = Normalize(pad.ThumbLX),
                LeftY = Normalize(pad.ThumbLY),
                RightX = Normalize(pad.ThumbRX),
                RightY = Normalize(pad.ThumbRY)
            };
        }

        private void Poll()
        {
            var snapshot = Read();
            if (snapshot.Connected)
                Apply(snapshot);
            else
                ReleaseAll();

            var handler = Polled;
            if (handler != null)
                handler(snapshot);
        }

        private void Apply(ControllerSnapshot snapshot)
        {
            SetControl(ControlId.A, snapshot.IsDown(GamepadButton.A));
            SetControl(ControlId.B, snapshot.IsDown(GamepadButton.B));
            SetControl(ControlId.X, snapshot.IsDown(GamepadButton.X));
            SetControl(ControlId.Y, snapshot.IsDown(GamepadButton.Y));
            SetControl(ControlId.LeftShoulder, snapshot.IsDown(GamepadButton.LeftShoulder));
            SetControl(ControlId.RightShoulder, snapshot.IsDown(GamepadButton.RightShoulder));
            SetControl(ControlId.Back, snapshot.IsDown(GamepadButton.Back));
            SetControl(ControlId.Start, snapshot.IsDown(GamepadButton.Start));
            SetControl(ControlId.LeftThumbClick, snapshot.IsDown(GamepadButton.LeftThumb));
            SetControl(ControlId.RightThumbClick, snapshot.IsDown(GamepadButton.RightThumb));
            SetControl(ControlId.DPadUp, snapshot.IsDown(GamepadButton.DPadUp));
            SetControl(ControlId.DPadDown, snapshot.IsDown(GamepadButton.DPadDown));
            SetControl(ControlId.DPadLeft, snapshot.IsDown(GamepadButton.DPadLeft));
            SetControl(ControlId.DPadRight, snapshot.IsDown(GamepadButton.DPadRight));

            SetControl(ControlId.LeftTrigger, snapshot.LeftTrigger >= _profile.TriggerThreshold);
            SetControl(ControlId.RightTrigger, snapshot.RightTrigger >= _profile.TriggerThreshold);

            ApplyStick(_profile.LeftStickMode, snapshot.LeftX, snapshot.LeftY,
                ControlId.LeftStickUp, ControlId.LeftStickDown, ControlId.LeftStickLeft, ControlId.LeftStickRight);
            ApplyStick(_profile.RightStickMode, snapshot.RightX, snapshot.RightY,
                ControlId.RightStickUp, ControlId.RightStickDown, ControlId.RightStickLeft, ControlId.RightStickRight);
        }

        private void ApplyStick(StickMode mode, double x, double y,
            ControlId up, ControlId down, ControlId left, ControlId right)
        {
            var deflection = ApplyDeadzone(x, y);
            switch (mode)
            {
                case StickMode.MouseCursor:
                    ReleaseControl(up);
                    ReleaseControl(down);
                    ReleaseControl(left);
                    ReleaseControl(right);
                    DriveCursor(deflection.X, deflection.Y);
                    break;
                case StickMode.ScrollWheel:
                    ReleaseControl(up);
                    ReleaseControl(down);
                    ReleaseControl(left);
                    ReleaseControl(right);
                    DriveScroll(deflection.Y);
                    break;
                default:
                    SetControl(up, deflection.Y > 0.5);
                    SetControl(down, deflection.Y < -0.5);
                    SetControl(left, deflection.X < -0.5);
                    SetControl(right, deflection.X > 0.5);
                    break;
            }
        }

        private void DriveCursor(double x, double y)
        {
            var magnitude = Math.Sqrt(x * x + y * y);
            if (magnitude <= 0)
                return;

            var curved = Math.Pow(magnitude, _profile.MouseAcceleration) / magnitude;
            var scale = _profile.MouseSensitivity * curved;
            var dy = _profile.InvertMouseY ? y : -y;

            _mouseRemainderX += x * scale;
            _mouseRemainderY += dy * scale;

            var stepX = (int)_mouseRemainderX;
            var stepY = (int)_mouseRemainderY;
            _mouseRemainderX -= stepX;
            _mouseRemainderY -= stepY;

            InputInjector.MoveMouse(stepX, stepY);
        }

        private void DriveScroll(double y)
        {
            if (Math.Abs(y) <= 0)
                return;

            _scrollRemainder += y * _profile.MouseSensitivity * 4;
            var notches = (int)(_scrollRemainder / 120);
            if (notches == 0)
                return;

            _scrollRemainder -= notches * 120.0;
            InputInjector.Scroll(notches * 120, false);
        }

        private void SetControl(ControlId control, bool pressed)
        {
            var binding = _profile[control];
            if (binding.IsEmpty)
                return;

            bool wasActive;
            _active.TryGetValue(control, out wasActive);

            if (!pressed)
            {
                if (wasActive)
                    Emit(binding, false);
                _active[control] = false;
                _turboClocks.Remove(control);
                _turboPhase.Remove(control);
                return;
            }

            if (binding.Turbo && (binding.Kind == BindingKind.Key || binding.Kind == BindingKind.Mouse))
            {
                PulseTurbo(control, binding);
                _active[control] = true;
                return;
            }

            if (!wasActive)
                Emit(binding, true);
            else if (binding.Kind == BindingKind.ScrollUp || binding.Kind == BindingKind.ScrollDown)
                Emit(binding, true); // scrolling has no held state, so keep ticking
            _active[control] = true;
        }

        private void PulseTurbo(ControlId control, Binding binding)
        {
            Stopwatch clock;
            if (!_turboClocks.TryGetValue(control, out clock))
            {
                clock = Stopwatch.StartNew();
                _turboClocks[control] = clock;
                _turboPhase[control] = true;
                Emit(binding, true);
                return;
            }

            if (clock.Elapsed.TotalMilliseconds < TurboIntervalMs)
                return;

            clock.Restart();
            var down = !_turboPhase[control];
            _turboPhase[control] = down;
            Emit(binding, down);
        }

        private void ReleaseControl(ControlId control)
        {
            bool wasActive;
            if (_active.TryGetValue(control, out wasActive) && wasActive)
                Emit(_profile[control], false);
            _active[control] = false;
            _turboClocks.Remove(control);
            _turboPhase.Remove(control);
        }

        private void ReleaseAll()
        {
            foreach (var control in new List<ControlId>(_active.Keys))
                ReleaseControl(control);
            _active.Clear();
            _mouseRemainderX = 0;
            _mouseRemainderY = 0;
            _scrollRemainder = 0;
        }

        private static void Emit(Binding binding, bool pressed)
        {
            switch (binding.Kind)
            {
                case BindingKind.Key:
                    InputInjector.SendKey(binding.VirtualKey, pressed);
                    break;
                case BindingKind.Mouse:
                    InputInjector.SendMouseButton(binding.Mouse, pressed);
                    break;
                case BindingKind.ScrollUp:
                    if (pressed)
                        InputInjector.Scroll(120, false);
                    break;
                case BindingKind.ScrollDown:
                    if (pressed)
                        InputInjector.Scroll(-120, false);
                    break;
            }
        }

        /// <summary>Scales a raw stick pair so that output starts just outside the deadzone.</summary>
        private Deflection ApplyDeadzone(double x, double y)
        {
            var magnitude = Math.Sqrt(x * x + y * y);
            if (magnitude <= _profile.Deadzone)
                return new Deflection();

            var normalized = Math.Min(1.0, (magnitude - _profile.Deadzone) / (1.0 - _profile.Deadzone));
            var factor = normalized / magnitude;
            return new Deflection { X = x * factor, Y = y * factor };
        }

        private static double Normalize(short axis)
        {
            return axis < 0 ? axis / 32768.0 : axis / 32767.0;
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }

        private struct Deflection
        {
            public double X;
            public double Y;
        }
    }
}
