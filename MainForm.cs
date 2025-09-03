using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HighlightRecorder
{
    public partial class MainForm : Form
    {
        private Recorder recorder;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private const int HOTKEY_ID = 1;

        public MainForm()
        {
            InitializeComponent();
            recorder = new Recorder();
            recorder.Logger = Log;
            InitializeTray();

            HotkeyManager.RegisterHotKey(this.Handle, HOTKEY_ID,
                HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT,
                (uint)Keys.S);
        }

        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("Exit", null, (s, e) => { ExitApplication(); });

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Screen Recorder";
            trayIcon.Icon = System.Drawing.SystemIcons.Application;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
                this.Hide();
        }

        private void ExitApplication()
        {
            if (recorder.IsRecording)
                recorder.StopAndSave();

            trayIcon.Visible = false;
            HotkeyManager.UnregisterHotKey(this.Handle, HOTKEY_ID);
            Application.Exit();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                recorder.Start();
                Log("Recording started.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveRecording();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (!recorder.IsRecording)
            {
                Log("No active recording to cancel.");
                return;
            }

            recorder.Cancel();
            Log("Recording cancelled.");
        }

        private void SaveRecording()
        {
            if (!recorder.IsRecording)
            {
                Log("No active recording to save.");
                return;
            }

            try
            {
                string file = recorder.StopAndSave();
                if (file != null)
                    Log($"Recording saved to {file}");
            }
            catch (Exception ex)
            {
                Log("Error saving recording: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            txtLog.AppendText($"{DateTime.Now:T} - {message}{Environment.NewLine}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ExitApplication();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == HotkeyManager.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID)
                {
                    SaveRecording();
                }
            }
            base.WndProc(ref m);
        }
    }

    public static class HotkeyManager
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        public const int WM_HOTKEY = 0x0312;
    }
}
