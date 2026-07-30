using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace OpenWASD
{
    /// <summary>Every physical control that can carry a binding.</summary>
    public enum ControlId
    {
        A,
        B,
        X,
        Y,
        LeftShoulder,
        RightShoulder,
        LeftTrigger,
        RightTrigger,
        Back,
        Start,
        LeftThumbClick,
        RightThumbClick,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        LeftStickUp,
        LeftStickDown,
        LeftStickLeft,
        LeftStickRight,
        RightStickUp,
        RightStickDown,
        RightStickLeft,
        RightStickRight
    }

    /// <summary>What a control emits when it is activated.</summary>
    public enum BindingKind
    {
        None = 0,
        Key,
        Mouse,
        ScrollUp,
        ScrollDown
    }

    /// <summary>How a whole analog stick behaves.</summary>
    public enum StickMode
    {
        /// <summary>Directions act as four digital buttons using their bindings.</summary>
        DigitalDirections = 0,
        /// <summary>Stick drives the mouse cursor; directional bindings are ignored.</summary>
        MouseCursor,
        /// <summary>Stick drives the scroll wheel; directional bindings are ignored.</summary>
        ScrollWheel
    }

    [DataContract]
    public class Binding
    {
        [DataMember(Order = 0)]
        public ControlId Control { get; set; }

        [DataMember(Order = 1)]
        public BindingKind Kind { get; set; }

        /// <summary>Win32 virtual-key code used when <see cref="Kind"/> is <see cref="BindingKind.Key"/>.</summary>
        [DataMember(Order = 2)]
        public ushort VirtualKey { get; set; }

        [DataMember(Order = 3)]
        public MouseButton Mouse { get; set; }

        /// <summary>Repeats the output while held, e.g. for menu navigation.</summary>
        [DataMember(Order = 4)]
        public bool Turbo { get; set; }

        public bool IsEmpty
        {
            get { return Kind == BindingKind.None || (Kind == BindingKind.Key && VirtualKey == 0); }
        }

        public Binding Clone()
        {
            return new Binding
            {
                Control = Control,
                Kind = Kind,
                VirtualKey = VirtualKey,
                Mouse = Mouse,
                Turbo = Turbo
            };
        }

        public string Describe()
        {
            switch (Kind)
            {
                case BindingKind.Key:
                    return VirtualKey == 0 ? "—" : KeyNames.Describe(VirtualKey) + (Turbo ? " (turbo)" : string.Empty);
                case BindingKind.Mouse:
                    return "Mouse " + Mouse + (Turbo ? " (turbo)" : string.Empty);
                case BindingKind.ScrollUp:
                    return "Scroll up";
                case BindingKind.ScrollDown:
                    return "Scroll down";
                default:
                    return "—";
            }
        }
    }

    /// <summary>A named, serializable set of bindings and tuning values.</summary>
    [DataContract]
    public class MappingProfile
    {
        public const int MinPollIntervalMs = 1;
        public const int MaxPollIntervalMs = 50;

        [DataMember(Order = 0)]
        public string Name { get; set; }

        [DataMember(Order = 1)]
        public List<Binding> Bindings { get; set; }

        [DataMember(Order = 2)]
        public StickMode LeftStickMode { get; set; }

        [DataMember(Order = 3)]
        public StickMode RightStickMode { get; set; }

        /// <summary>Fraction of full stick travel ignored around centre, 0..0.9.</summary>
        [DataMember(Order = 4)]
        public double Deadzone { get; set; }

        /// <summary>Pixels per poll at full stick deflection.</summary>
        [DataMember(Order = 5)]
        public double MouseSensitivity { get; set; }

        /// <summary>Exponent applied to stick magnitude for finer aim near centre.</summary>
        [DataMember(Order = 6)]
        public double MouseAcceleration { get; set; }

        /// <summary>Trigger travel, 0..1, at which a trigger counts as pressed.</summary>
        [DataMember(Order = 7)]
        public double TriggerThreshold { get; set; }

        [DataMember(Order = 8)]
        public bool InvertMouseY { get; set; }

        [DataMember(Order = 9)]
        public int PollIntervalMs { get; set; }

        public MappingProfile()
        {
            Name = "New profile";
            Bindings = new List<Binding>();
            Deadzone = 0.20;
            MouseSensitivity = 14;
            MouseAcceleration = 1.6;
            TriggerThreshold = 0.35;
            PollIntervalMs = 8;
            RightStickMode = StickMode.MouseCursor;
        }

        public Binding this[ControlId control]
        {
            get
            {
                var existing = Bindings.FirstOrDefault(b => b.Control == control);
                if (existing == null)
                {
                    existing = new Binding { Control = control };
                    Bindings.Add(existing);
                }
                return existing;
            }
        }

        public void Normalize()
        {
            if (Bindings == null)
                Bindings = new List<Binding>();
            Deadzone = Clamp(Deadzone, 0, 0.9);
            MouseSensitivity = Clamp(MouseSensitivity, 1, 60);
            MouseAcceleration = Clamp(MouseAcceleration, 1, 3);
            TriggerThreshold = Clamp(TriggerThreshold, 0.05, 0.95);
            PollIntervalMs = (int)Clamp(PollIntervalMs, MinPollIntervalMs, MaxPollIntervalMs);
            if (string.IsNullOrWhiteSpace(Name))
                Name = "Unnamed profile";
        }

        public MappingProfile Clone()
        {
            return new MappingProfile
            {
                Name = Name,
                Bindings = Bindings.Select(b => b.Clone()).ToList(),
                LeftStickMode = LeftStickMode,
                RightStickMode = RightStickMode,
                Deadzone = Deadzone,
                MouseSensitivity = MouseSensitivity,
                MouseAcceleration = MouseAcceleration,
                TriggerThreshold = TriggerThreshold,
                InvertMouseY = InvertMouseY,
                PollIntervalMs = PollIntervalMs
            };
        }

        /// <summary>Keyboard-and-mouse layout that suits most first person games.</summary>
        public static MappingProfile CreateFpsDefault()
        {
            var profile = new MappingProfile
            {
                Name = "FPS (WASD + mouse)",
                LeftStickMode = StickMode.DigitalDirections,
                RightStickMode = StickMode.MouseCursor
            };

            Bind(profile, ControlId.LeftStickUp, 'W');
            Bind(profile, ControlId.LeftStickDown, 'S');
            Bind(profile, ControlId.LeftStickLeft, 'A');
            Bind(profile, ControlId.LeftStickRight, 'D');
            Bind(profile, ControlId.A, (ushort)0x20);   // Space
            Bind(profile, ControlId.B, (ushort)0x11);   // Ctrl
            Bind(profile, ControlId.X, 'R');
            Bind(profile, ControlId.Y, 'F');
            Bind(profile, ControlId.LeftShoulder, 'Q');
            Bind(profile, ControlId.RightShoulder, 'E');
            Bind(profile, ControlId.Back, (ushort)0x09); // Tab
            Bind(profile, ControlId.Start, (ushort)0x1B); // Escape
            Bind(profile, ControlId.LeftThumbClick, (ushort)0x10); // Shift
            Bind(profile, ControlId.DPadUp, '1');
            Bind(profile, ControlId.DPadRight, '2');
            Bind(profile, ControlId.DPadDown, '3');
            Bind(profile, ControlId.DPadLeft, '4');

            profile[ControlId.LeftTrigger].Kind = BindingKind.Mouse;
            profile[ControlId.LeftTrigger].Mouse = MouseButton.Right;
            profile[ControlId.RightTrigger].Kind = BindingKind.Mouse;
            profile[ControlId.RightTrigger].Mouse = MouseButton.Left;
            profile[ControlId.RightThumbClick].Kind = BindingKind.Mouse;
            profile[ControlId.RightThumbClick].Mouse = MouseButton.Middle;

            return profile;
        }

        /// <summary>Turns the pad into a couch-friendly mouse and media remote.</summary>
        public static MappingProfile CreateDesktopDefault()
        {
            var profile = new MappingProfile
            {
                Name = "Desktop (cursor + scroll)",
                LeftStickMode = StickMode.MouseCursor,
                RightStickMode = StickMode.ScrollWheel,
                MouseSensitivity = 10
            };

            profile[ControlId.A].Kind = BindingKind.Mouse;
            profile[ControlId.A].Mouse = MouseButton.Left;
            profile[ControlId.B].Kind = BindingKind.Mouse;
            profile[ControlId.B].Mouse = MouseButton.Right;
            profile[ControlId.X].Kind = BindingKind.Mouse;
            profile[ControlId.X].Mouse = MouseButton.Middle;

            Bind(profile, ControlId.Y, (ushort)0x0D);   // Enter
            Bind(profile, ControlId.Back, (ushort)0x08); // Backspace
            Bind(profile, ControlId.Start, (ushort)0x5B); // Left Windows
            Bind(profile, ControlId.LeftShoulder, (ushort)0xA6); // Browser back
            Bind(profile, ControlId.RightShoulder, (ushort)0xA7); // Browser forward
            Bind(profile, ControlId.LeftTrigger, (ushort)0xAE);  // Volume down
            Bind(profile, ControlId.RightTrigger, (ushort)0xAF); // Volume up
            Bind(profile, ControlId.DPadUp, (ushort)0x26);
            Bind(profile, ControlId.DPadDown, (ushort)0x28);
            Bind(profile, ControlId.DPadLeft, (ushort)0x25);
            Bind(profile, ControlId.DPadRight, (ushort)0x27);

            foreach (var control in new[] { ControlId.DPadUp, ControlId.DPadDown, ControlId.DPadLeft, ControlId.DPadRight })
                profile[control].Turbo = true;

            return profile;
        }

        private static void Bind(MappingProfile profile, ControlId control, ushort virtualKey)
        {
            var binding = profile[control];
            binding.Kind = BindingKind.Key;
            binding.VirtualKey = virtualKey;
        }

        private static void Bind(MappingProfile profile, ControlId control, char key)
        {
            Bind(profile, control, (ushort)char.ToUpperInvariant(key));
        }

        private static double Clamp(double value, double min, double max)
        {
            if (double.IsNaN(value))
                return min;
            return Math.Min(max, Math.Max(min, value));
        }
    }
}
