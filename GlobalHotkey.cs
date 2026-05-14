using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipTyper
{
    public class GlobalHotkey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WM_HOTKEY = 0x0312;

        public enum Modifiers
        {
            None = 0x0000,
            Alt = 0x0001,
            Control = 0x0002,
            Shift = 0x0004,
            Win = 0x0008
        }

        private readonly IntPtr _hWnd;
        private readonly int _id;
        private bool _isRegistered;

        public GlobalHotkey(IntPtr hWnd, int id, Modifiers modifier, Keys key)
        {
            _hWnd = hWnd;
            _id = id;
            Register(modifier, key);
        }

        private void Register(Modifiers modifier, Keys key)
        {
            _isRegistered = RegisterHotKey(_hWnd, _id, (uint)modifier, (uint)key);
            if (!_isRegistered)
            {
                // Handle registration failure if needed
            }
        }

        public void Unregister()
        {
            if (_isRegistered)
            {
                UnregisterHotKey(_hWnd, _id);
                _isRegistered = false;
            }
        }

        public void Dispose()
        {
            Unregister();
            GC.SuppressFinalize(this);
        }

        ~GlobalHotkey()
        {
            Unregister();
        }
    }
}
