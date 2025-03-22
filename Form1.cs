namespace screenCap;
using System.Drawing.Imaging;
using System.Windows.Forms;
using screenshot_tool;

public partial class Form1 : Form
{   
    // Add InitializeComponent method temporarily if Designer file is missing
    private void InitializeComponent()
    {
        this.SuspendLayout();
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Name = "Form1";
        this.ResumeLayout(false);
    }
    private readonly ScreenCapture _screenCapture;
    private Image? _capturedImage;
    private NotifyIcon trayIcon = null!;
    private KeyboardHook _keyboardHook = null!;
    
    public Form1()
    {   
        InitializeComponent();
        _screenCapture = new ScreenCapture();
        _keyboardHook = new KeyboardHook();
        
        // Set appropriate window title
        this.Text = "Screenshot Tool";
        
        // Remove the status label as we won't show the GUI
        // Remove the shortcut configuration button (if exists)
        var configButton = Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Set Shortcut");
        if (configButton != null)
        {
            Controls.Remove(configButton);
        }
        
        // Set up keyboard shortcuts
        SetupKeyboardShortcuts();
        
        // Hide the form completely
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;
        
        // Set up system tray icon and context menu
        SetupTrayIcon();
    }
    
    private void SetupKeyboardShortcuts()
    {
        _keyboardHook = new KeyboardHook();
        _keyboardHook.KeyPressed += KeyboardHook_KeyPressed;
        // Register PrintScreen key
        _keyboardHook.RegisterShortcutKeys(new HashSet<Keys> { Keys.PrintScreen });
    }
    
    private void KeyboardHook_KeyPressed(object? sender, Keys key)
    {   
        // Check if Shift is pressed
        bool shiftPressed = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        
        // If the key is PrintScreen
        if (key == Keys.PrintScreen)
        {
            // Invoke on UI thread since this comes from another thread
            this.BeginInvoke(new Action(() => {
                if (shiftPressed)
                {
                    // Shift+PrintScreen = region capture
                    CaptureRegion();
                }
                else
                {
                    // Just PrintScreen = full screen capture
                    CaptureScreen();
                }
            }));
        }
    }

    private void SetupTrayIcon()
    {   
        // Create context menu for tray icon with enhanced options
        ContextMenuStrip contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Capture Screen (PrintScreen)", null, (s, e) => CaptureScreen());
        contextMenu.Items.Add("Capture Region (Shift+PrintScreen)", null, (s, e) => CaptureRegion());
        contextMenu.Items.Add("Save Last Capture", null, (s, e) => SaveLastCapture());
        contextMenu.Items.Add("-"); // Separator
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
        
        // Create tray icon
        trayIcon = new NotifyIcon
        {
            Text = "Screenshot Tool",
            Icon = this.Icon ?? SystemIcons.Application, // Use form icon or default app icon
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        // Double-click on the tray icon to capture region
        trayIcon.DoubleClick += (s, e) => CaptureRegion();
        
        // Show balloon tip on startup to let user know the app is running
        trayIcon.ShowBalloonTip(3000, "Screenshot Tool", 
            "Application is running in system tray. Double-click icon to capture region or use right-click for more options.", 
            ToolTipIcon.Info);
    }
    
    // New method to show notification with screenshot preview
    private void ShowNotificationWithPreview(string title, string message, Image image)
    {
        // Create a clone of the image to avoid disposal issues
        Image previewImage = new Bitmap(image);
        
        // Show custom notification form on a separate thread to avoid UI blocking
        this.BeginInvoke(new Action(() => {
            NotificationForm notification = new NotificationForm(title, message, previewImage);
            notification.Show();
        }));
    }
    
    private void ExitApplication()
    {   
        trayIcon.Visible = false;
        Application.Exit();
    }
    
    private void CaptureScreen()
    {
        try
        {
            // If we have a previous image, dispose it
            if (_capturedImage != null)
            {
                _capturedImage.Dispose();
                _capturedImage = null;
            }
            
            _capturedImage = _screenCapture.CaptureScreenToImage();
            
            // Copy the captured image to clipboard
            if (_capturedImage != null)
            {
                try
                {
                    Clipboard.SetImage(_capturedImage);
                    
                    // Show a notification with preview
                    ShowNotificationWithPreview("Screenshot Captured", 
                        "Screenshot has been copied to clipboard", _capturedImage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Screenshot captured but couldn't copy to clipboard: " + ex.Message, 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error capturing screen: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private void CaptureRegion()
    {
        try
        {
            // Create and show the region selection form
            using (RegionCaptureForm regionForm = new RegionCaptureForm())
            {
                if (regionForm.ShowDialog() == DialogResult.OK)
                {
                    Rectangle selectedRegion = regionForm.SelectedRegion;
                    if (selectedRegion.Width > 0 && selectedRegion.Height > 0)
                    {
                        // If we have a previous image, dispose it
                        if (_capturedImage != null)
                        {
                            _capturedImage.Dispose();
                            _capturedImage = null;
                        }
                        
                        // Capture the selected region
                        _capturedImage = _screenCapture.CaptureRegionToImage(selectedRegion);
                        
                        // Copy the captured image to clipboard
                        if (_capturedImage != null)
                        {
                            try
                            {
                                Clipboard.SetImage(_capturedImage);
                                
                                // Show a notification with preview
                                ShowNotificationWithPreview("Region Captured", 
                                    "Selected region has been copied to clipboard", _capturedImage);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Region captured but couldn't copy to clipboard: " + ex.Message,
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error capturing region: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    // New method for saving the last capture directly from tray menu
    private void SaveLastCapture()
    {
        if (_capturedImage == null)
        {
            MessageBox.Show("No screenshot available to save. Please capture a screenshot first.",
                "No Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        using (SaveFileDialog saveDialog = new SaveFileDialog())
        {
            saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
            saveDialog.Title = "Save Screen Capture";
            saveDialog.DefaultExt = "png";
            
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                string extension = Path.GetExtension(saveDialog.FileName).ToLower();
                ImageFormat format = ImageFormat.Png;
                
                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;
                }
                
                _capturedImage.Save(saveDialog.FileName, format);
                MessageBox.Show($"Image saved to {saveDialog.FileName}", "Save Successful",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
    
    // Override form closing to minimize to tray instead of closing
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true; // Cancel the close
            this.Hide();     // Hide the form
            return;
        }
        
        // Otherwise, let the form close
        if (trayIcon != null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
        
        if (_keyboardHook != null)
        {
            _keyboardHook.Dispose();
        }
        
        base.OnFormClosing(e);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _capturedImage?.Dispose();
            trayIcon?.Dispose();
            _keyboardHook?.Dispose();
        }
        base.Dispose(disposing);
    }
}