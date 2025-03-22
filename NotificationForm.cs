using System;
using System.Drawing;
using System.Windows.Forms;

namespace screenCap
{
    public class NotificationForm : Form
    {
        private readonly System.Windows.Forms.Timer _closeTimer = new System.Windows.Forms.Timer();
        private const int DisplayTime = 4000; // 4 seconds
        
        public NotificationForm(string title, string message, Image screenshot)
        {
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            
            // Calculate size based on screenshot (with max limits)
            int maxWidth = 300;
            int maxHeight = 200;
            int previewWidth = Math.Min(screenshot.Width, maxWidth);
            int previewHeight = Math.Min(screenshot.Height, maxHeight);
            
            // Scale to maintain aspect ratio
            float ratio = Math.Min((float)maxWidth / screenshot.Width, (float)maxHeight / screenshot.Height);
            if (ratio < 1)
            {
                previewWidth = (int)(screenshot.Width * ratio);
                previewHeight = (int)(screenshot.Height * ratio);
            }
            
            // Set form size
            this.ClientSize = new Size(previewWidth, previewHeight + 60); // Extra space for text
            
            // Create title label
            Label titleLabel = new Label
            {
                Text = title,
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold),
                Location = new Point(10, 5),
                AutoSize = true
            };
            
            // Create message label
            Label messageLabel = new Label
            {
                Text = message,
                Location = new Point(10, 25),
                AutoSize = true
            };
            
            // Create PictureBox for preview
            PictureBox preview = new PictureBox
            {
                Image = screenshot,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(10, 45),
                Size = new Size(previewWidth - 20, previewHeight)
            };
            
            // Add controls
            this.Controls.Add(titleLabel);
            this.Controls.Add(messageLabel);
            this.Controls.Add(preview);
            
            // Position the form in the bottom-right corner
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(
                workingArea.Right - this.Width - 10,
                workingArea.Bottom - this.Height - 10);
                
            // Set up auto-close timer
            _closeTimer.Interval = DisplayTime;
            _closeTimer.Tick += (sender, e) => this.Close();
            _closeTimer.Start();
            
            // Add mouse event to close on click
            this.Click += (sender, e) => this.Close();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw a border around the form
            e.Graphics.DrawRectangle(Pens.Gray, 0, 0, this.Width - 1, this.Height - 1);
        }
    }
}
