using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClipTyper
{
    public static class KeyboardSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        /// <summary>
        /// Retrieves the status of a virtual key at call time.
        /// If the high-order bit is set, the key is currently pressed.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
            public static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx; public int dy; public uint mouseData; 
            public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg; public ushort wParamL; public ushort wParamH;
        }

        public const int INPUT_KEYBOARD = 1;
        public const uint KEYEVENTF_UNICODE = 0x0004;
        public const uint KEYEVENTF_KEYUP = 0x0002;

        // Virtual key codes for modifier keys
        private const ushort VK_SHIFT    = 0x10;
        private const ushort VK_CONTROL  = 0x11;
        private const ushort VK_MENU     = 0x12;  // Alt
        private const ushort VK_LWIN     = 0x5B;
        private const ushort VK_RWIN     = 0x5C;

        /// <summary>
        /// Sends KeyUp events for all modifier keys (Ctrl, Shift, Alt, Win)
        /// to prevent them from interfering with typed characters.
        /// This is critical because the hotkey (Ctrl+Shift+T) leaves those
        /// keys in a "pressed" state from the OS perspective.
        /// </summary>
        public static void ReleaseModifiers()
        {
            ushort[] modifiers = { VK_SHIFT, VK_CONTROL, VK_MENU, VK_LWIN, VK_RWIN };

            foreach (var vk in modifiers)
            {
                // Only release keys that are actually pressed
                if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                {
                    INPUT[] inputs = new INPUT[1];
                    inputs[0] = new INPUT { type = INPUT_KEYBOARD };
                    inputs[0].U.ki.wVk = vk;
                    inputs[0].U.ki.dwFlags = KEYEVENTF_KEYUP;
                    SendInput(1, inputs, INPUT.Size);
                }
            }
        }

        /// <summary>
        /// Simulates typing text by sending Unicode characters directly.
        /// Automatically releases modifier keys first to prevent interference.
        /// KeyDown and KeyUp are sent as separate calls to avoid key-repeat
        /// artifacts caused by the OS not processing batched up/down pairs
        /// quickly enough.
        /// </summary>
        /// <param name="text">The text to type.</param>
        /// <param name="delayMs">Delay between each keystroke in milliseconds.</param>
        public static void SendText(string text, int delayMs = 25)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Release any held modifier keys (Ctrl, Shift, Alt) before typing.
            // Without this, the hotkey combo bleeds into the typed characters.
            ReleaseModifiers();
            Thread.Sleep(50); // Let the OS process the key releases

            foreach (char c in text)
            {
                // Skip carriage returns — we handle newlines via '\n' → VK_RETURN
                if (c == '\r') continue;

                // For newlines, send the Enter key via virtual key code
                // instead of Unicode, as some apps don't handle Unicode LF well.
                if (c == '\n')
                {
                    SendKeyPress(0x0D, isVirtualKey: true); // VK_RETURN
                }
                else
                {
                    SendKeyPress(c, isVirtualKey: false);
                }

                // Delay between keystrokes so target apps don't drop characters
                if (delayMs > 0)
                {
                    Thread.Sleep(delayMs);
                }
            }
        }

        /// <summary>
        /// Sends a single key press (down + up) as two separate SendInput calls
        /// with a small gap to ensure the OS fully processes each event.
        /// </summary>
        private static void SendKeyPress(ushort keyOrChar, bool isVirtualKey)
        {
            INPUT[] down = new INPUT[1];
            down[0] = new INPUT { type = INPUT_KEYBOARD };

            INPUT[] up = new INPUT[1];
            up[0] = new INPUT { type = INPUT_KEYBOARD };

            if (isVirtualKey)
            {
                down[0].U.ki.wVk = keyOrChar;
                up[0].U.ki.wVk = keyOrChar;
                up[0].U.ki.dwFlags = KEYEVENTF_KEYUP;
            }
            else
            {
                down[0].U.ki.wVk = 0;
                down[0].U.ki.wScan = keyOrChar;
                down[0].U.ki.dwFlags = KEYEVENTF_UNICODE;

                up[0].U.ki.wVk = 0;
                up[0].U.ki.wScan = keyOrChar;
                up[0].U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            }

            // Send KeyDown, wait, then send KeyUp.
            // Generous gap prevents key-repeat and ensures the target
            // application fully registers each character — critical for
            // password fields and remote desktop sessions.
            SendInput(1, down, INPUT.Size);
            Thread.Sleep(5);
            SendInput(1, up, INPUT.Size);
        }
    }
}
