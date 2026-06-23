using System;
using System.Windows.Forms;

namespace ClipTyper
{
    public class SetDelayForm : Form
    {
        private TextBox _delayTextBox;
        private Button _okButton;
        private Label _delayLabel;

        public int DelaySeconds { get; private set; }

        public SetDelayForm(int currentDelaySeconds)
        {
            DelaySeconds = currentDelaySeconds;

            Width = 270;
            Height = 130;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Text = "Set Paste Delay";
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            _delayLabel = new Label() { Left = 20, Top = 20, Text = "Delay (seconds):", AutoSize = true };
            _delayTextBox = new TextBox() { Left = 120, Top = 18, Width = 100, Text = currentDelaySeconds.ToString() };
            _okButton = new Button() { Text = "OK", Left = 120, Top = 50, Width = 100, DialogResult = DialogResult.OK };

            Controls.Add(_delayLabel);
            Controls.Add(_delayTextBox);
            Controls.Add(_okButton);
            AcceptButton = _okButton;

            _okButton.Click += OnOkClicked;
        }

        private void OnOkClicked(object? sender, EventArgs e)
        {
            if (int.TryParse(_delayTextBox.Text, out int result) && result >= 0)
            {
                DelaySeconds = result;
            }
            else
            {
                MessageBox.Show("Invalid delay. Please enter a valid number of seconds.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None; // Prevent the form from closing
            }
        }
    }
}
