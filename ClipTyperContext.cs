using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipTyper
{
    public class ClipTyperContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private GlobalHotkey _hotkey;
        private const int HotkeyId = 1;

        // Hidden form to receive Windows messages
        private class HotkeyForm : Form
        {
            public event Action? HotkeyPressed;

            public HotkeyForm()
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.Load += (s, e) => { this.Size = new Size(0, 0); };
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == GlobalHotkey.WM_HOTKEY)
                {
                    if (m.WParam.ToInt32() == HotkeyId)
                    {
                        HotkeyPressed?.Invoke();
                    }
                }
                base.WndProc(ref m);
            }
        }

        private HotkeyForm _hiddenForm;

        /// <summary>
        /// Loads the embedded app.ico from the assembly resources.
        /// Falls back to the default application icon if not found.
        /// </summary>
        private static Icon LoadEmbeddedIcon()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // Embedded resource name = DefaultNamespace.Filename
                using var stream = assembly.GetManifestResourceStream("ClipTyper.app.ico");
                if (stream != null)
                {
                    return new Icon(stream, 32, 32);
                }
            }
            catch { /* fall through to default */ }

            return SystemIcons.Application;
        }

        public int PasteDelaySeconds { get; set; } = 3;

        public ClipTyperContext()
        {
            _trayIcon = new NotifyIcon()
            {
                Icon = LoadEmbeddedIcon(),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "ClipTyper"
            };

            _trayIcon.MouseClick += OnTrayIconClick;

            _trayIcon.ContextMenuStrip.Items.Add("Set Paste Delay...", null, OnSetDelay);
            _trayIcon.ContextMenuStrip.Items.Add("About", null, OnAbout);
            _trayIcon.ContextMenuStrip.Items.Add("Exit", null, OnExit);

            _hiddenForm = new HotkeyForm();
            _hiddenForm.HotkeyPressed += OnHotkeyPressed;
            
            var handle = _hiddenForm.Handle;

            _hotkey = new GlobalHotkey(_hiddenForm.Handle, HotkeyId, GlobalHotkey.Modifiers.Control | GlobalHotkey.Modifiers.Shift, Keys.T);
        }

        private void OnTrayIconClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TriggerPaste(PasteDelaySeconds * 1000);
            }
        }

        private void OnSetDelay(object? sender, EventArgs e)
        {
            using var form = new SetDelayForm(PasteDelaySeconds);
            if (form.ShowDialog() == DialogResult.OK)
            {
                PasteDelaySeconds = form.DelaySeconds;
            }
        }

        private void OnHotkeyPressed()
        {
            TriggerPaste(100);
        }

        private void TriggerPaste(int delayMs)
        {
            string textToType = "";
            try
            {
                if (Clipboard.ContainsText())
                {
                    textToType = Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Clipboard error: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(textToType))
            {
                Task.Run(() =>
                {
                    Thread.Sleep(delayMs);
                    KeyboardSimulator.SendText(textToType);
                });
            }
        }

        private void OnAbout(object? sender, EventArgs e)
        {
            MessageBox.Show("ClipTyper\n\nPress Ctrl + Shift + T to type the clipboard contents.\n\nCreated as a lightweight portable typing tool.", "About ClipTyper", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _hotkey?.Dispose();
            _hiddenForm?.Dispose();
            Application.Exit();
        }
    }
}
