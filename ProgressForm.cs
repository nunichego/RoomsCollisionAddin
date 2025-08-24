using System;
using System.Windows.Forms;

namespace RoomsManagerAddin
{
    /// <summary>
    /// Simple progress form for collision analysis
    /// </summary>
    public class ProgressForm : System.Windows.Forms.Form
    {
        #region Fields
        private DateTime _startTime;
        private Timer _timer;
        
        private Label titleLabel;
        private Label stageLabel;
        private Label detailLabel;
        private ProgressBar stepProgressBar;
        private ProgressBar overallProgressBar;
        private Label timeLabel;
        #endregion

        #region Constructor
        public ProgressForm()
        {
            _startTime = DateTime.Now;
            InitializeComponent();
            
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += (s, e) => {
                var elapsed = DateTime.Now - _startTime;
                timeLabel.Text = $"Elapsed: {elapsed:mm\\:ss}";
            };
            _timer.Start();
        }
        #endregion

        #region Component Initialization
        private void InitializeComponent()
        {
            this.Text = "RoomDataSync Analysis Progress";
            this.Size = new System.Drawing.Size(500, 220);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);

            titleLabel = new Label();
            titleLabel.Text = "Rooms-Walls Collision Analysis";
            titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12, System.Drawing.FontStyle.Bold);
            titleLabel.Location = new System.Drawing.Point(20, 15);
            titleLabel.Size = new System.Drawing.Size(450, 25);
            titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            stageLabel = new Label();
            stageLabel.Text = "Initializing...";
            stageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, System.Drawing.FontStyle.Bold);
            stageLabel.Location = new System.Drawing.Point(20, 45);
            stageLabel.Size = new System.Drawing.Size(450, 20);

            detailLabel = new Label();
            detailLabel.Text = "";
            detailLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9);
            detailLabel.ForeColor = System.Drawing.Color.Gray;
            detailLabel.Location = new System.Drawing.Point(20, 70);
            detailLabel.Size = new System.Drawing.Size(450, 20);

            stepProgressBar = new ProgressBar();
            stepProgressBar.Location = new System.Drawing.Point(20, 100);
            stepProgressBar.Size = new System.Drawing.Size(450, 18);

            overallProgressBar = new ProgressBar();
            overallProgressBar.Location = new System.Drawing.Point(20, 130);
            overallProgressBar.Size = new System.Drawing.Size(450, 18);

            timeLabel = new Label();
            timeLabel.Text = "Elapsed: 00:00";
            timeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8);
            timeLabel.ForeColor = System.Drawing.Color.Gray;
            timeLabel.Location = new System.Drawing.Point(20, 160);
            timeLabel.Size = new System.Drawing.Size(450, 15);
            timeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            this.Controls.Add(titleLabel);
            this.Controls.Add(stageLabel);
            this.Controls.Add(detailLabel);
            this.Controls.Add(stepProgressBar);
            this.Controls.Add(overallProgressBar);
            this.Controls.Add(timeLabel);
        }
        #endregion

        #region Public Methods
        public void UpdateProgress(string title, string message, int stepCurrent, int stepTotal, int overallCurrent, int overallTotal)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateProgress(title, message, stepCurrent, stepTotal, overallCurrent, overallTotal)));
                    return;
                }

                stageLabel.Text = title ?? "Processing...";
                detailLabel.Text = message ?? "";

                if (stepTotal > 0)
                {
                    var stepPercent = (double)stepCurrent / stepTotal * 100;
                    stepProgressBar.Value = Math.Min(100, Math.Max(0, (int)stepPercent));
                }

                if (overallTotal > 0)
                {
                    var overallPercent = (double)overallCurrent / overallTotal * 100;
                    overallProgressBar.Value = Math.Min(100, Math.Max(0, (int)overallPercent));
                }

                this.Update();
                Application.DoEvents();
            }
            catch
            {
                // Ignore errors during progress updates
            }
        }
        #endregion

        #region Form Events
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            base.OnFormClosed(e);
        }
        #endregion
    }
}
