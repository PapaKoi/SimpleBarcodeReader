using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using ZXing;

namespace SimpleBarcodeReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BarcodeReader barcodeReader = new();
        private readonly object capturedFrameLock = new();
        private readonly Mat capturedFrame = new();
        private readonly string initialCaptureButtonText;
        private bool captureStarted = false;

        // Some cameras are slow to initialize when
        // set to API.Any or API.Msmf.
        // 
        // API.DShow lacks HW acceleration and
        // is slowly being phased out per Microsoft.
        private readonly VideoCapture captureDevice = new(0, VideoCapture.API.DShow);

        public MainWindow()
        {
            InitializeComponent();
            initialCaptureButtonText = (string)ToggleCaptureButton.Content;
            captureDevice.ImageGrabbed += ProcessFrame;
            CompositionTarget.Rendering += UpdateCameraCaptureImage;
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (captureDevice.Ptr == IntPtr.Zero)
            {
                return;
            }

            lock (capturedFrameLock)
            {
                bool anyFramesGrabbed = captureDevice.Retrieve(capturedFrame);
            }
        }

        private void UpdateCameraCaptureImage(object sender, EventArgs e)
        {
            BitmapSource source = null;

            lock (capturedFrameLock)
            {
                if (capturedFrame.Size.IsEmpty)
                {
                    return;
                }

                source = capturedFrame.ToBitmapSource();
            }

            CameraCaptureImage.Source = source;
            DecodeBarcodeImage(source);
        }

        private void DecodeBarcodeImage(BitmapSource source)
        {
            Result result = barcodeReader.Decode(source);

            if (result is null)
            {
                // No barcode was found.
                return;
            }

            InfoText.Text = string.Join(
                Environment.NewLine,
                $"Barcode Format :  {result.BarcodeFormat}",
                $"Barcode Value  :  {result.Text}");
        }

        private void ToggleCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            var captureButton = (Button)sender;

            if (!captureStarted)
            {
                captureButton.Content = "Stop Capture";
                captureDevice.Start();
            }
            else
            {
                captureButton.Content = initialCaptureButtonText;
                captureDevice.Pause();
            }

            captureStarted = !captureStarted;
        }
    }
}
