using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OpenWASD
{
    /// <summary>Human readable names for the virtual keys the UI offers.</summary>
    public static class KeyNames
    {
        private static readonly Dictionary<ushort, string> Overrides = new Dictionary<ushort, string>
        {
            { 0x08, "Backspace" }, { 0x09, "Tab" }, { 0x0D, "Enter" }, { 0x10, "Shift" },
            { 0x11, "Ctrl" }, { 0x12, "Alt" }, { 0x14, "Caps Lock" }, { 0x1B, "Esc" },
            { 0x20, "Space" }, { 0x21, "Page Up" }, { 0x22, "Page Down" }, { 0x23, "End" },
            { 0x24, "Home" }, { 0x25, "Left Arrow" }, { 0x26, "Up Arrow" }, { 0x27, "Right Arrow" },
            { 0x28, "Down Arrow" }, { 0x2D, "Insert" }, { 0x2E, "Delete" }, { 0x5B, "Left Windows" },
            { 0xA0, "Left Shift" }, { 0xA1, "Right Shift" }, { 0xA2, "Left Ctrl" }, { 0xA3, "Right Ctrl" },
            { 0xA4, "Left Alt" }, { 0xA5, "Right Alt" }, { 0xA6, "Browser Back" }, { 0xA7, "Browser Forward" },
            { 0xAD, "Volume Mute" }, { 0xAE, "Volume Down" }, { 0xAF, "Volume Up" },
            { 0xB0, "Next Track" }, { 0xB1, "Previous Track" }, { 0xB3, "Play/Pause" },
            { 0xBA, ";" }, { 0xBB, "=" }, { 0xBC, "," }, { 0xBD, "-" }, { 0xBE, "." },
            { 0xBF, "/" }, { 0xC0, "`" }, { 0xDB, "[" }, { 0xDC, "\\" }, { 0xDD, "]" }, { 0xDE, "'" }
        };

        public static string Describe(ushort virtualKey)
        {
            string name;
            if (Overrides.TryGetValue(virtualKey, out name))
                return name;
            if (virtualKey >= 0x30 && virtualKey <= 0x5A)
                return ((char)virtualKey).ToString();
            if (virtualKey >= 0x60 && virtualKey <= 0x69)
                return "Numpad " + (virtualKey - 0x60);
            if (virtualKey >= 0x70 && virtualKey <= 0x87)
                return "F" + (virtualKey - 0x6F);
            return "VK 0x" + virtualKey.ToString("X2");
        }

        /// <summary>Every key the binding dropdown exposes, in a sensible order.</summary>
        public static IEnumerable<ushort> Selectable()
        {
            var keys = new List<ushort>();
            for (ushort vk = 0x41; vk <= 0x5A; vk++) keys.Add(vk);           // A-Z
            for (ushort vk = 0x30; vk <= 0x39; vk++) keys.Add(vk);           // 0-9
            for (ushort vk = 0x70; vk <= 0x87; vk++) keys.Add(vk);           // F1-F24
            for (ushort vk = 0x60; vk <= 0x69; vk++) keys.Add(vk);           // Numpad
            keys.AddRange(Overrides.Keys);
            return keys.Distinct();
        }

        /// <summary>Maps a WinForms key press onto a virtual-key code, preferring the sided modifier.</summary>
        public static ushort FromKeyEvent(KeyEventArgs args)
        {
            switch (args.KeyCode)
            {
                case Keys.ShiftKey:
                    return (ushort)(args.KeyValue == (int)Keys.RShiftKey ? 0xA1 : 0xA0);
                case Keys.ControlKey:
                    return 0xA2;
                case Keys.Menu:
                    return 0xA4;
                default:
                    return (ushort)args.KeyCode;
            }
        }
    }
}
