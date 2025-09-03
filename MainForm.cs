using System;
using System.Windows.Forms;

namespace HighlightRecorder
{
    public partial class MainForm : Form
    {
        private Recorder recorder;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public MainForm()
        {
            InitializeComponent();
            recorder = new Recorder();
            recorder.Logger = Log;
            InitializeTray();
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

        private void Log(string message)
        {
            txtLog.AppendText($"{DateTime.Now:T} - {message}{Environment.NewLine}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ExitApplication();
            base.OnFormClosing(e);
        }
    }
}
