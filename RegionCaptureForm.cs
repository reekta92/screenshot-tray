using System;
using System.Drawing;
using System.Windows.Forms;

namespace screenshot_tool
{
    public class RegionCaptureForm : Form
    {
        public Rectangle SelectedRegion { get; private set; }
        
        private Point startPoint;
        private Point currentPoint;
        private bool isSelecting = false;
        private Image screenImage;
        private Rectangle screenBounds;
        private Font infoFont;
        
        public RegionCaptureForm()
        {
            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;
            
            // Set to cover all screens
            screenBounds = SystemInformation.VirtualScreen;
            this.Bounds = screenBounds;
            
            // Capture the screen to show under the selection
            screenImage = CaptureFullScreen();
            
            // Semi-transparent background
            this.BackColor = Color.Black;
            this.Opacity = 0.5;
            
            // Create font for information display
            infoFont = new Font("Arial", 10, FontStyle.Bold);
            
            // Instruction label with better visibility
            Label instructionLabel = new Label()
            {
                Text = "Click and drag to select a region. Press ESC to cancel.",
                BackColor = Color.Black,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Arial", 12, FontStyle.Bold),
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(instructionLabel);
            
            // Handle mouse events
            this.MouseDown += RegionCaptureForm_MouseDown;
            this.MouseMove += RegionCaptureForm_MouseMove;
            this.MouseUp += RegionCaptureForm_MouseUp;
            
            // Handle key events
            this.KeyDown += RegionCaptureForm_KeyDown;
        }
        
        private Image CaptureFullScreen()
        {
            Bitmap bitmap = new Bitmap(screenBounds.Width, screenBounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, bitmap.Size);
            }
            return bitmap;
        }

        private void RegionCaptureForm_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                SelectedRegion = new Rectangle();
            }
        }

        private void RegionCaptureForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                currentPoint = e.Location;
                
                // Update the selection rectangle
                int x = Math.Min(startPoint.X, currentPoint.X);
                int y = Math.Min(startPoint.Y, currentPoint.Y);
                int width = Math.Abs(currentPoint.X - startPoint.X);
                int height = Math.Abs(currentPoint.Y - startPoint.Y);
                
                SelectedRegion = new Rectangle(x, y, width, height);
                
                // Make the selection more visible
                this.Opacity = 0.3;
                
                this.Invalidate();
            }
        }

        private void RegionCaptureForm_MouseUp(object? sender, MouseEventArgs e)
        {
            if (isSelecting && e.Button == MouseButtons.Left)
            {
                isSelecting = false;
                
                // Convert the selected region from screen coordinates
                SelectedRegion = new Rectangle(
                    SelectedRegion.X + screenBounds.X,
                    SelectedRegion.Y + screenBounds.Y,
                    SelectedRegion.Width,
                    SelectedRegion.Height
                );
                
                // Close the form with OK result
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void RegionCaptureForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // Cancel selection
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (isSelecting && SelectedRegion.Width > 0 && SelectedRegion.Height > 0)
            {
                // Draw the image of what's under the form
                e.Graphics.DrawImage(screenImage, 0, 0, this.Width, this.Height);
                
                // Draw the selection rectangle with a more visible border
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, SelectedRegion);
                    
                    // Make the area inside the selection fully visible
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(1, 255, 255, 255)), SelectedRegion);
                    
                    // Draw selection size information
                    string sizeInfo = $"{SelectedRegion.Width} x {SelectedRegion.Height}";
                    
                    // Create a background for the text
                    SizeF textSize = e.Graphics.MeasureString(sizeInfo, infoFont);
                    int infoX = SelectedRegion.Right - (int)textSize.Width - 5;
                    int infoY = SelectedRegion.Bottom + 5;
                    
                    // Ensure text is visible on screen
                    if (infoY + textSize.Height > this.Height)
                        infoY = SelectedRegion.Top - (int)textSize.Height - 5;
                    
                    // Draw text background
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), 
                        infoX - 2, infoY - 2, textSize.Width + 4, textSize.Height + 4);
                    
                    // Draw text
                    e.Graphics.DrawString(sizeInfo, infoFont, Brushes.White, infoX, infoY);
                }
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                screenImage?.Dispose();
                infoFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
