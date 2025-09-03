namespace HighlightRecorder
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private Button btnStart;
        private Button btnSave;
        private Button btnCancel;
        private TextBox txtLog;

        private void InitializeComponent()
        {
            this.btnStart = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();

            this.SuspendLayout();

            this.btnStart.Location = new System.Drawing.Point(12, 12);
            this.btnStart.Size = new System.Drawing.Size(100, 30);
            this.btnStart.Text = "Start Recording";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);

            this.btnSave.Location = new System.Drawing.Point(120, 12);
            this.btnSave.Size = new System.Drawing.Size(100, 30);
            this.btnSave.Text = "Save Recording";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            this.btnCancel.Location = new System.Drawing.Point(230, 12);
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.Text = "Cancel Recording";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            this.txtLog.Location = new System.Drawing.Point(12, 50);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(320, 200);
            this.txtLog.ReadOnly = true;

            this.ClientSize = new System.Drawing.Size(350, 270);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.txtLog);
            this.Text = "Screen Recorder";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
