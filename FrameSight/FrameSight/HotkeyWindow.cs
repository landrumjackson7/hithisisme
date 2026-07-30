using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FrameSight
{
    /// <summary>
    /// A message-only window that owns the global show/hide hotkey, so the hotkey
    /// survives the overlay being hidden or recreated.
    /// </summary>
    public class HotkeyWindow : NativeWindow, IDisposable
    {
        private const int WmHotkey = 0x0312;
        private const int HotkeyId = 0xB170;

        private int _registeredKey;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        public event Action Pressed;

        /// <summary>Registers (or re-registers) the hotkey. Returns false when another app owns it.</summary>
        public bool Register(int virtualKey)
        {
            Unregister();
            if (virtualKey == 0)
                return true;

            if (!RegisterHotKey(Handle, HotkeyId, 0, (uint)virtualKey))
                return false;

            _registeredKey = virtualKey;
            return true;
        }

        public void Unregister()
        {
            if (_registeredKey == 0)
                return;
            UnregisterHotKey(Handle, HotkeyId);
            _registeredKey = 0;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
            {
                var handler = Pressed;
                if (handler != null)
                    handler();
            }
            base.WndProc(ref m);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr window, int id);

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }
    }
}
