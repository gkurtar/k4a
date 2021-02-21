using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Azure.Kinect.Sensor;


namespace K4ACalibration
{
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Azure Kinect sensor
        /// </summary>
        private readonly Device kinect = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private readonly WriteableBitmap bitmap = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// The width in pixels of the color image from the Azure Kinect DK
        /// </summary>
        private readonly int colorWidth = 0;

        /// <summary>
        /// The height in pixels of the color image from the Azure Kinect DK
        /// </summary>
        private readonly int colorHeight = 0;

        /// <summary>
        /// Status of the application
        /// </summary>
        private bool running = true;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public Window1()
        {
            // Open the default device
            this.kinect = Device.Open();

            // Configure camera modes
            this.kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                //DepthMode = DepthMode.NFOV_2x2Binned,
                DepthMode = DepthMode.WFOV_2x2Binned,
                SynchronizedImagesOnly = true
            });

            this.colorWidth = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            this.colorHeight = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

            this.bitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Gray16, null);

            this.DataContext = this;

            this.InitializeComponent();

            MessageBox.Show("dene 2");
            this.StatusText = "ctor";
			this.Closing += Window1_Closing;
        }

		/// <summary>
		/// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Window1_Closing(object sender, CancelEventArgs e)
        {
            running = false;

            if (this.kinect != null)
            {
                this.kinect.Dispose();
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a render target to which we'll render our composite image
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)CompositeImage.ActualWidth, (int)CompositeImage.ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush brush = new VisualBrush(CompositeImage);
                dc.DrawRectangle(brush, null, new System.Windows.Rect(new Point(), new Size(CompositeImage.ActualWidth, CompositeImage.ActualHeight)));
            }

            renderBitmap.Render(dv);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(myPhotos, "KinectScreenshot-" + time + ".png");

            // Write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            }
            catch (IOException)
            {
                this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
            }
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.StatusText = "loaded";
            while (running)
            {
                using (Capture capture = await Task.Run(() => { return this.kinect.GetCapture(); }))
                {
                    this.StatusText = "Received Capture: " + capture.Depth.DeviceTimestamp;

                    this.bitmap.Lock();

					var color = capture.IR;
                    
					var region = new Int32Rect(0, 0, color.WidthPixels, color.HeightPixels);

                    BitmapSource dd = capture.IR.CreateBitmapSource();
                    //int FF = dd.Width;
                    //this.bitmap.

                    unsafe
					{
						using (var pin = color.Memory.Pin())
						{
							this.bitmap.WritePixels(region, (IntPtr)pin.Pointer, (int)color.Size, color.StrideBytes);
						}
					}

					this.bitmap.AddDirtyRect(region);
                    this.bitmap.Unlock();
                }
            }
        }


        //private async void Window_LoadedOrginal(object sender, RoutedEventArgs e)
        //{
        //    while (running)
        //    {
        //        using (Capture capture = await Task.Run(() => { return this.kinect.GetCapture(); }))
        //        {
        //            this.StatusText = "Received Capture: " + capture.Depth.DeviceTimestamp;

        //            this.bitmap.Lock();

        //            var color = capture.Color;
        //            var region = new Int32Rect(0, 0, color.WidthPixels, color.HeightPixels);

        //            unsafe
        //            {
        //                using (var pin = color.Memory.Pin())
        //                {
        //                    this.bitmap.WritePixels(region, (IntPtr)pin.Pointer, (int)color.Size, color.StrideBytes);
        //                }
        //            }

        //            this.bitmap.AddDirtyRect(region);
        //            this.bitmap.Unlock();
        //        }
        //    }
        //}

    }
}

