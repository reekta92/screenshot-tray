using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms; // For SendKeys and Clipboard
using System.Threading;     // For Thread.Sleep
using System.Runtime.InteropServices; // For Windows API calls

namespace screenshot_tool;

public class ScreenCapture
{
    // Windows API imports for window management
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Additional APIs for better window control
    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);
    
    // ShowWindow commands
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;

    // Configuration for fullscreen capture
    public int MaxRetryAttempts { get; set; } = 3;
   

    /// <summary>
    /// Captures the entire screen and saves it to a file
    /// </summary>
    public void CaptureScreenToFile(string filePath, ImageFormat format = null!, bool fullscreenCompatible = false)
    {
        if (format == null)
            format = ImageFormat.Png;
            
        using (Image screenshot = CaptureScreenToImage(fullscreenCompatible))
        {
            screenshot.Save(filePath, format);
        }
    }

    /// <summary>
    /// Captures the entire screen across all monitors
    /// </summary>
    public Image CaptureScreenToImage(bool fullscreenCompatible = false)
    {
        if (fullscreenCompatible)
        {
            return CaptureFullscreenCompatible();
        }
        
        try
        {
            // Get the entire virtual screen bounds (all monitors)
            Rectangle bounds = SystemInformation.VirtualScreen;
            
            // Create a new bitmap
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height);
            
            // Create a graphics object from the bitmap
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                // Set high quality mode for better screenshot quality
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                
                // Copy the screen to the bitmap
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
            }
            
            return screenshot;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to capture screen", ex);
        }
    }
    
    /// <summary>
    /// Captures the active window, works with fullscreen apps
    /// </summary>
    public Image CaptureActiveWindowToImage()
    {
        try
        {
            // Clear clipboard
            Clipboard.Clear();
            
            // Send Alt+PrintScreen to capture active window to clipboard
            SendKeys.SendWait("%{PRTSC}");
            
            return GetImageFromClipboardWithRetry();
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to capture active window", ex);
        }
    }
    
    /// <summary>
    /// Captures the screen including fullscreen applications using PrintScreen key simulation
    /// </summary>
    private Image CaptureFullscreenCompatible()
    {
        try
        {
            // Clear clipboard
            Clipboard.Clear();
            
            // Send PrintScreen key to capture screen to clipboard
            SendKeys.SendWait("{PRTSC}");
            
            return GetImageFromClipboardWithRetry();
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to capture fullscreen application", ex);
        }
    }
    
    private Image GetImageFromClipboardWithRetry()
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            int ClipboardWaitTimeMs = 200;
            // Give system time to process the PrintScreen command
            Thread.Sleep(ClipboardWaitTimeMs);
            
            try
            {
                // Check if we got an image in the clipboard
                if (!Clipboard.ContainsImage())
                {
                    lastException = new ApplicationException("No image found in clipboard");
                    continue;
                }
                
                // Get image from clipboard
                Image? image = Clipboard.GetImage();
                if (image == null)
                {
                    lastException = new ApplicationException("Failed to retrieve image from clipboard");
                    continue;
                }
                
                return image;
            }
            catch (Exception ex)
            {
                lastException = ex;
                // On exception, we'll retry after waiting
            }
        }
        
        throw new ApplicationException($"Failed to get image from clipboard after {MaxRetryAttempts} attempts", lastException);
    }
    
    /// <summary>
    /// Captures a specific region of the screen
    /// </summary>
    public Image CaptureRegionToImage(Rectangle region, bool fullscreenCompatible = false)
    {
        if (region.Width <= 0 || region.Height <= 0)
            throw new ArgumentException("Invalid region dimensions");
            
        // Always use fullscreen compatible mode for now since it's more reliable
        // We can revisit this if performance becomes an issue
        fullscreenCompatible = true;
        
        if (fullscreenCompatible)
        {
            // For region capture in fullscreen apps, capture the full screen and then crop
            using (Image fullImage = CaptureFullscreenCompatible())
            {
                // Validate region is within screen bounds
                Rectangle screenBounds = new Rectangle(0, 0, fullImage.Width, fullImage.Height);
                Rectangle validRegion = Rectangle.Intersect(region, screenBounds);
                
                if (validRegion.IsEmpty)
                    throw new ArgumentException("Region is outside screen bounds");
                
                // Create a new bitmap of the region
                Bitmap regionImage = new Bitmap(validRegion.Width, validRegion.Height);
                using (Graphics g = Graphics.FromImage(regionImage))
                {
                    g.DrawImage(fullImage, 
                        new Rectangle(0, 0, validRegion.Width, validRegion.Height),
                        validRegion,
                        GraphicsUnit.Pixel);
                }
                return regionImage;
            }
        }
        
        try
        {
            // Create a new bitmap of the correct size
            Bitmap screenshot = new Bitmap(region.Width, region.Height);
            
            // Create a graphics object from the bitmap
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                // Set high quality mode for better screenshot quality
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                
                // Copy the selected region to the bitmap
                g.CopyFromScreen(region.X, region.Y, 0, 0, region.Size);
            }
            
            return screenshot;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Failed to capture region: {region}", ex);
        }
    }
    
    /// <summary>
    /// Captures a region of the active window, works with fullscreen apps
    /// </summary>
    public Image CaptureActiveWindowRegionToImage(Rectangle regionRelativeToWindow)
    {
        // Get the active window first
        using (Image windowImage = CaptureActiveWindowToImage())
        {
            // Ensure the region is within the image bounds
            Rectangle bounds = new Rectangle(0, 0, windowImage.Width, windowImage.Height);
            Rectangle validRegion = Rectangle.Intersect(regionRelativeToWindow, bounds);
            
            if (validRegion.IsEmpty)
                throw new ArgumentException("Region is outside window bounds");
            
            // Create a bitmap for the region
            Bitmap regionImage = new Bitmap(validRegion.Width, validRegion.Height);
            using (Graphics g = Graphics.FromImage(regionImage))
            {
                g.DrawImage(windowImage, 
                    new Rectangle(0, 0, validRegion.Width, validRegion.Height),
                    validRegion,
                    GraphicsUnit.Pixel);
            }
            return regionImage;
        }
    }

    /// <summary>
    /// Creates an interactive selection for capturing a region in any application (including fullscreen)
    /// </summary>
    public Image CaptureRegionInteractive()
    {
        // First capture the entire screen with fullscreen compatibility
        // This captures the screen as-is without disturbing the fullscreen application
        using (Image fullscreenImage = CaptureFullscreenCompatible())
        {
            // Remember the current foreground window
            IntPtr originalForegroundWindow = GetForegroundWindow();
            
            // Create the selector form with the captured screenshot
            using (var selector = new RegionSelectorForm(fullscreenImage))
            {
                try
                {
                    // Configure the form for proper display
                    selector.TopMost = true;
                    
                    // Allow our application to set foreground window
                    AllowSetForegroundWindow(System.Diagnostics.Process.GetCurrentProcess().Id);
                    
                    // Show the form before trying to activate it
                    selector.Show();
                    
                    // Force our form to be the active window
                    BringWindowToTop(selector.Handle);
                    SetForegroundWindow(selector.Handle);
                    
                    // Ensure form is visible and ready
                    Application.DoEvents();
                    
                    // Run a modal loop manually since ShowDialog might not work well with fullscreen apps
                    while (selector.IsSelecting)
                    {
                        Application.DoEvents();
                        Thread.Sleep(10);
                    }
                    
                    // Check if selection was completed or canceled
                    if (selector.DialogResult == DialogResult.OK)
                    {
                        // Get the selected region
                        Rectangle selectedRegion = selector.SelectedRegion;
                        
                        // Create a bitmap of the selected region
                        Bitmap regionImage = new Bitmap(selectedRegion.Width, selectedRegion.Height);
                        using (Graphics g = Graphics.FromImage(regionImage))
                        {
                            g.DrawImage(fullscreenImage, 
                                new Rectangle(0, 0, selectedRegion.Width, selectedRegion.Height), 
                                selectedRegion, 
                                GraphicsUnit.Pixel);
                        }
                        
                        return regionImage;
                    }
                    else
                    {
                        throw new OperationCanceledException("Region selection was canceled");
                    }
                }
                finally
                {
                    // Return focus to original window if it exists
                    if (originalForegroundWindow != IntPtr.Zero)
                    {
                        SetForegroundWindow(originalForegroundWindow);
                    }
                }
            }
        }
    }
}

/// <summary>
/// Form that allows selecting a region on a captured screenshot
/// </summary>
public class RegionSelectorForm : Form
{
    private Image _screenshot;
    private Point _startPoint;
    private Point _endPoint;
    private bool _selecting;
    private bool _selectionComplete;
    
    // Windows API for window Z-order management
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    // SetWindowPos parameters
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public Rectangle SelectedRegion { get; private set; }
    public bool IsSelecting { get; private set; } = true;

    public RegionSelectorForm(Image screenshot)
    {
        _screenshot = screenshot;
        
        // Configure form properties for fullscreen overlay
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(0, 0);
        Size = new Size(screenshot.Width, screenshot.Height);
        Cursor = Cursors.Cross;
        ShowInTaskbar = false;
        
        // Make form appear above all other windows
        TopMost = true;
        
        // Set double buffering for smooth drawing
        DoubleBuffered = true;
        
        // Handle input events
        MouseDown += RegionSelectorForm_MouseDown;
        MouseMove += RegionSelectorForm_MouseMove;
        MouseUp += RegionSelectorForm_MouseUp;
        KeyDown += RegionSelectorForm_KeyDown;
        
        // Handle form shown event to ensure we're truly on top
        Shown += (s, e) => {
            // Force the form on top of everything using Windows API
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            
            // Focus the form to receive keyboard input
            this.Focus();
            this.BringToFront();
            this.Activate();
            
            // Small delay to ensure UI is responsive
            Application.DoEvents();
        };
    }

    private void RegionSelectorForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _selecting = true;
            _selectionComplete = false;
            _startPoint = e.Location;
            _endPoint = e.Location;
            Invalidate();
        }
    }

    private void RegionSelectorForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_selecting)
        {
            _endPoint = e.Location;
            Invalidate();
        }
    }

    private void RegionSelectorForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_selecting && e.Button == MouseButtons.Left)
        {
            _selecting = false;
            _selectionComplete = true;
            _endPoint = e.Location;
            
            // Calculate the selection rectangle
            int x = Math.Min(_startPoint.X, _endPoint.X);
            int y = Math.Min(_startPoint.Y, _endPoint.Y);
            int width = Math.Abs(_startPoint.X - _endPoint.X);
            int height = Math.Abs(_startPoint.Y - _endPoint.Y);
            
            // Ensure we have a valid selection
            if (width > 0 && height > 0)
            {
                SelectedRegion = new Rectangle(x, y, width, height);
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }
            
            IsSelecting = false;
            Close();
        }
    }

    private void RegionSelectorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            IsSelecting = false;
            Close();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        
        // Draw the screenshot
        e.Graphics.DrawImage(_screenshot, 0, 0, _screenshot.Width, _screenshot.Height);
        
        // Draw selection overlay
        if (_selecting || _selectionComplete)
        {
            int x = Math.Min(_startPoint.X, _endPoint.X);
            int y = Math.Min(_startPoint.Y, _endPoint.Y);
            int width = Math.Abs(_startPoint.X - _endPoint.X);
            int height = Math.Abs(_startPoint.Y - _endPoint.Y);
            
            // Create semi-transparent overlay for non-selected area
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                // Draw four rectangles around the selected region
                e.Graphics.FillRectangle(brush, 0, 0, Width, y); // Top
                e.Graphics.FillRectangle(brush, 0, y + height, Width, Height - (y + height)); // Bottom
                e.Graphics.FillRectangle(brush, 0, y, x, height); // Left
                e.Graphics.FillRectangle(brush, x + width, y, Width - (x + width), height); // Right
            }
            
            // Draw selection rectangle border
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, x, y, width, height);
            }
        }
    }
}
