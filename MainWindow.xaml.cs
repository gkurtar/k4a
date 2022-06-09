//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace K4ACalibration
{
    using System;
    using System.Configuration;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Azure.Kinect.Sensor;

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Text;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary> Azure Kinect sensor </summary>
        private readonly Device kinect = null;

        /// <summary> Bitmap to display </summary>
        private WriteableBitmap bitmap = null;

        /// <summary> Current status text to display </summary>
        private string statusText = null;

        internal static readonly string DIR_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        /// <summary> The width in pixels of the color image from the Azure Kinect DK </summary>
        internal readonly int colorWidth = 0;

        /// <summary> The height in pixels of the color image from the Azure Kinect DK </summary>
        internal readonly int colorHeight = 0;

        internal static int CAPTURE_CAPACITY; //Default value is 10 in the config file

        internal readonly AutoResetEvent autoReset = new AutoResetEvent(false);

        internal readonly int depthWidth = 0;

        internal readonly int depthHeight = 0;

        internal readonly int infraRedWidth = 0;

        internal readonly int infraRedHeight = 0;

        internal readonly DeviceConfiguration kinectDevConfig = new DeviceConfiguration{
            ColorFormat = ImageFormat.ColorBGRA32,
            ColorResolution = ColorResolution.R2160p,
            DepthMode = DepthMode.NFOV_2x2Binned,
            //DepthMode = DepthMode.WFOV_2x2Binned,
            //DepthMode = DepthMode.NFOV_Unbinned,
            SynchronizedImagesOnly = false
        };

        /// <summary> Status of the application </summary>
        private bool running = true;

        private int xPosImage = 0;

        private int yPosImage = 0;

        private long _nCaptureCounter = 0;

        private volatile bool _bSaveAverageFlag = false;

        private readonly List<Capture> _lstDepthCaptures = new List<Capture>();

        private readonly List<Capture> _lstIrCaptures = new List<Capture>();

        private readonly List<Capture> _lstRgbCaptures = new List<Capture>();

        private OutputOption _selectedOutput;

        private SynchronizationContext _uiContext;

        private ImageSource _bitmap;

        private readonly StringBuilder _sbdCaptureColorInfo = new StringBuilder();

        private readonly StringBuilder _sbdCaptureInfraRedInfo = new StringBuilder();

        private readonly StringBuilder _sbdCaptureDepthInfo = new StringBuilder();

        private readonly StringBuilder _sbdPositionInfo = new StringBuilder();

        public ObservableCollection<OutputOption> Outputs { get; set; }

        /// <summary> INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource CurrentCameraImage => _bitmap;

        private SaveDataHelper objSaveDataHelper;

        /// <summary> Initializes a new instance of the MainWindow class. </summary>
        public MainWindow()
        {
            Outputs = new ObservableCollection<OutputOption>
            {
                new OutputOption{Name = "Colour", OutputType = OutputType.Colour},
                new OutputOption{Name = "Depth", OutputType = OutputType.Depth},
                new OutputOption{Name = "IR", OutputType = OutputType.IR}
            };

            SelectedOutput = Outputs.First();

            String strCapturesToAverage = ConfigurationManager.AppSettings["NUMBER_OF_CAPTURES_TO_AVERAGE"];

            int.TryParse(strCapturesToAverage, out CAPTURE_CAPACITY);
            if (CAPTURE_CAPACITY == 0) {
                CAPTURE_CAPACITY = 10;
            }

            // Open the default device
            this.kinect = Device.Open();

            // Configure camera modes
            //this.kinect.StartCameras(new DeviceConfiguration {
            //    ColorFormat = ImageFormat.ColorBGRA32,
            //    ColorResolution = ColorResolution.R2160p,
            //    DepthMode = DepthMode.NFOV_2x2Binned,
            //    //DepthMode = DepthMode.WFOV_2x2Binned,
            //    //DepthMode = DepthMode.NFOV_Unbinned,
            //    SynchronizedImagesOnly = false
            //});

            this.kinect.StartCameras(this.kinectDevConfig);

            this.colorWidth = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            this.colorHeight = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

            this.depthWidth = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
            this.depthHeight = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;

            this.infraRedWidth = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
            this.infraRedHeight = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;

            this._sbdCaptureColorInfo.Append(String.Format("{0} :: {1} X {2} :: ",
                ImageFormat.ColorBGRA32, this.colorWidth, this.colorHeight));

            this._sbdCaptureDepthInfo.Append(String.Format("{0} :: {1} X {2} :: ",
                ImageFormat.Depth16, this.depthWidth, this.depthHeight));

            this._sbdCaptureInfraRedInfo.Append(String.Format("{0} :: {1} X {2} :: ",
                ImageFormat.IR16, this.infraRedWidth, this.infraRedHeight));

            this.bitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.DataContext = this;
            //this.KinectImage.MouseMove += this.mouseMoveOverStream;
            //this.KinectImage.MouseLeave += this.mouseLeaveFromStream;
            this.InitializeComponent();
            this._uiContext = SynchronizationContext.Current;

            objSaveDataHelper = new SaveDataHelper(this);

            /*
            Task.Factory.StartNew(() => {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ======StartDelay()======");
                //for (int i = 1; i <= 10; i++) {
                 //   Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ======Delay====== ==>" + i);
                  //  Task.Delay(5000);// Asynchronous delay
                //}
                Thread.Sleep(25000);// Asynchronous delay
                MessageBox.Show("sdf");
                this.ScreenshotButton_Click(null, null);
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ======End Delay()======");
                Console.WriteLine();
            });
            */

            return;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int nPixelValue = 0;
            int nLastIndex = 0;
            //lblInfo.Content = this._sbdCaptureInfo.ToString();
            while (running)
            {
                using (Capture capture = await Task.Run(() => { return this.kinect.GetCapture(); }))
                {
                    this._nCaptureCounter++;
                    switch (SelectedOutput.OutputType)
                    {
                        case OutputType.Depth:
                            if (null == capture.Depth) {
                                continue;
                            }

                            //Memory<byte> sa = capture.Depth.Memory;
                            //lblDene.Content = sa.Length;
                            //updateMemory(sa);
                            //capture.Depth.SetPixel()

                            if (this._bSaveAverageFlag)
                            {
                                if (_lstDepthCaptures.Count >= CAPTURE_CAPACITY)
                                {
                                    this._bSaveAverageFlag = false;
                                    autoReset.Set();
                                }
                                this._lstDepthCaptures.Add(capture.Reference());
                            }

                            this._uiContext.Send(x =>
                            {
                                Image imgUpdated = GeneralUtil.updateImage(capture.Depth);
                                _bitmap = imgUpdated.CreateBitmapSource();

                                _bitmap.Freeze();
                            }, null);

                            if (this.KinectImage.IsMouseOver)
                            {
                                if (capture.Depth.WidthPixels > xPosImage && capture.Depth.HeightPixels > yPosImage)
                                {
                                    nPixelValue = capture.Depth.GetPixel<short>(yPosImage, xPosImage);
                                    //this.lblPos.Content = String.Format("x:{0, -3}, y:{1, -3}, val: {2, -5} ", xPosImage, yPosImage, sPixelValue);

                                    this._sbdPositionInfo.Clear();
                                    this._sbdPositionInfo.Append(String.Format(
                                        " :: x: {0, -3}, y: {1, -3}, value: {2, -5} ", xPosImage, yPosImage, nPixelValue));
                                }
                            } 
                            
                            nLastIndex = this._sbdCaptureDepthInfo.ToString().LastIndexOf(":");
                            nLastIndex = (nLastIndex < 0 && nLastIndex >= this._sbdCaptureDepthInfo.Length -1 ? 0 : nLastIndex + 2);
                            this._sbdCaptureDepthInfo.Remove(nLastIndex, this._sbdCaptureDepthInfo.Length - nLastIndex);
                            this._sbdCaptureDepthInfo.Append(this._nCaptureCounter);

                            lblInfo.Content = this._sbdCaptureDepthInfo.ToString() + this._sbdPositionInfo.ToString();

                            if (this._nCaptureCounter == 300L) {
                                MessageBox.Show("updated " + this.kinect.CurrentDepthMode);
                                objSaveDataHelper.saveDepthDataToFile(
                                    new int[,] { { 15, 32, 93, 22 }, { 19, 37, 24, 23 } });
                                
                            }

                            break;
                        case OutputType.IR:
                            if (null == capture.IR) {
                                continue;
                            }
                            //lblInfo.Content = "IR " + capture.IR.WidthPixels + " X "
                            //        + capture.IR.HeightPixels + ", Format: " + capture.IR.Format + ", Count: " + this._nCaptureCounter;

                            if (this._bSaveAverageFlag)
                            {
                                if (_lstIrCaptures.Count >= CAPTURE_CAPACITY)
                                {
                                    this._bSaveAverageFlag = false;
                                    autoReset.Set();
                                }
                                this._lstIrCaptures.Add(capture.Reference());
                            }

                            this._uiContext.Send(x =>
                            {
                                _bitmap = capture.IR.CreateBitmapSource();
                                _bitmap.Freeze();
                            }, null);

                            if (this.KinectImage.IsMouseOver)
                            {
                                //int nArrayPos = yPosImage * capture.IR.WidthPixels + xPosImage;
                                //if (capture.IR.Memory.Length > nArrayPos)
                                if (capture.IR.WidthPixels > xPosImage && capture.IR.HeightPixels > yPosImage)
                                {
                                    nPixelValue = capture.IR.GetPixel<short>(yPosImage, xPosImage);
                                    //this.lblPos.Content = String.Format("x:{0, -3}, y:{1, -3}, val: {2, -5} ", xPosImage, yPosImage, sPixelValue);

                                    this._sbdPositionInfo.Clear();
                                    this._sbdPositionInfo.Append(String.Format(" :: x: {0, -3}, y: {1, -3}, value: {2, -5} ",
                                        xPosImage, yPosImage, nPixelValue));
                                } // end of if
                            }

                            nLastIndex = this._sbdCaptureInfraRedInfo.ToString().LastIndexOf(":");
                            nLastIndex = (nLastIndex < 0 && nLastIndex >= this._sbdCaptureInfraRedInfo.Length -1 ? 0 : nLastIndex + 2);
                            this._sbdCaptureInfraRedInfo.Remove(nLastIndex, this._sbdCaptureInfraRedInfo.Length - nLastIndex);
                            this._sbdCaptureInfraRedInfo.Append(this._nCaptureCounter);

                            lblInfo.Content = this._sbdCaptureInfraRedInfo.ToString() + this._sbdPositionInfo.ToString();

                            break;
                        case OutputType.Colour:
                        default:
                            if (null == capture.Color) {
                                continue;
                            }
                            
                            //lblInfo.Content = "Color " + capture.Color.WidthPixels + " X "
                            //        + capture.Color.HeightPixels + ", Format: " + capture.Color.Format + ", Count: " + this._nCaptureCounter;
                            
                            if (this._bSaveAverageFlag)
                            {
                                if (_lstRgbCaptures.Count >= CAPTURE_CAPACITY)
                                {
                                    this._bSaveAverageFlag = false;
                                    autoReset.Set();
                                }
                                this._lstRgbCaptures.Add(capture.Reference());
                            }

                            this._uiContext.Send(x =>
                            {
                                _bitmap = capture.Color.CreateBitmapSource();
                                _bitmap.Freeze();
                            }, null);

                            if (this.KinectImage.IsMouseOver)
                            {
                                if (capture.Color.WidthPixels > xPosImage && capture.Color.HeightPixels > yPosImage)
                                {
                                    nPixelValue = capture.Color.GetPixel<int>(yPosImage, xPosImage);
                                    int nRedValue = nPixelValue >> 16 & 0x000000FF;
                                    int nGreenValue = nPixelValue >> 8 & 0x000000FF;
                                    int nBlueValue = nPixelValue & 0x000000FF;
                                    //this.lblPos.Content = String.Format("x:{0, -4}, y:{1, -4}," +
                                        //"  red: {2,-3}, g: {3,-3}, b: {4,-3}  ", xPosImage, yPosImage, nRedValue, nGreenValue, nBlueValue);

                                    this._sbdPositionInfo.Clear();
                                    this._sbdPositionInfo.Append(String.Format(" :: x: {0, -4}, y: {1, -4}," +
                                        "  red: {2,-3}, green: {3,-3}, blue: {4,-3}  ", xPosImage, yPosImage, nRedValue, nGreenValue, nBlueValue));
                                } // end of if
                            }

                            //this.StatusText = "Received Capture: " + capture.Depth.DeviceTimestamp;
                            //this.bitmap.Lock();
                            //var color = capture.Color;
                            //var region = new Int32Rect(0, 0, color.WidthPixels, color.HeightPixels);
                            //unsafe                 //{
                            //    using (var pin = color.Memory.Pin()) {
                            //        this.bitmap.WritePixels(region, (IntPtr)pin.Pointer, (int)color.Size, color.StrideBytes);
                            //    } }
                            //this.bitmap.AddDirtyRect(region);
                            //this.bitmap.Unlock();

                            nLastIndex = this._sbdCaptureColorInfo.ToString().LastIndexOf(":");
                            //MessageBox.Show("count: " + _nCaptureCounter + ", last: " + nLastIndex
                             //   + ", cap before remove__" + this._sbdCaptureInfo.ToString() + "__" + this._sbdCaptureInfo.Length);
                            nLastIndex = (nLastIndex < 0 && nLastIndex >= this._sbdCaptureColorInfo.Length -1 ? 0 : nLastIndex + 2);
                            this._sbdCaptureColorInfo.Remove(nLastIndex, this._sbdCaptureColorInfo.Length - nLastIndex);
                            this._sbdCaptureColorInfo.Append(this._nCaptureCounter);

                            lblInfo.Content = this._sbdCaptureColorInfo.ToString() + this._sbdPositionInfo.ToString();

                            break;
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentCameraImage"));
                }
            }
            return;
        }

        void mouseLeaveFromStream(Object sender, MouseEventArgs e) {
            //this.lblPos.Content = "";
            this._sbdPositionInfo.Clear();
            return;
        }

        void mouseMoveOverStream(Object sender, MouseEventArgs e) {
            //this.lblPos.Content = "x: " + (int)Mouse.GetPosition(this.KinectImage).X
            //    + " y: " + (int)Mouse.GetPosition(this.KinectImage).Y + " counter: " + counter ;
            this.xPosImage = (int) Mouse.GetPosition(this.KinectImage).X;
            this.yPosImage = (int) Mouse.GetPosition(this.KinectImage).Y;
            return;
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a render target to which we'll render our composite image
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)CompositeImage.ActualWidth, (int)CompositeImage.ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);
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
            //string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = Path.Combine(DIR_PATH, "KinectScreenshot-" + time + ".png");

            // Write the new file to disk
            try {
                using (FileStream fs = new FileStream(path, FileMode.Create)) {
                    encoder.Save(fs);
                }
                this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            } catch (IOException) {
                this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
            }

            saveCalibrationDataToFile();

            return;
        }

        private void saveCalibrationDataToFile() {

            Calibration obj = kinect.GetCalibration(this.kinect.CurrentDepthMode, this.kinect.CurrentColorResolution);
            
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string pathDepth = System.IO.Path.Combine(myPhotos, "CALIB_DATA.txt");

            // write the new file to disk
            try {
                using (StreamWriter sw = new StreamWriter(pathDepth)) {
                    //sw.WriteLine(x + " " + y + " " + aseqPoints[x, y]);
                    sw.WriteLine(obj.ToString());
                    sw.WriteLine(obj.ColorCameraCalibration.ToString());
                    
                    CameraCalibration objColorCal = obj.ColorCameraCalibration;
                    sw.WriteLine(objColorCal.Intrinsics.ToString());
                    //objColorCal.Extrinsics.Rotation.

                    sw.WriteLine(objColorCal.Intrinsics.Parameters.ToString());

					for (int i = 0; i < objColorCal.Intrinsics.ParameterCount; i++) {
                        sw.WriteLine(i + " " + objColorCal.Intrinsics.Parameters[i]);
                    }
                }

            } catch (IOException) {
                this.StatusText = string.Format(CultureInfo.InvariantCulture,
                    "{0}", Properties.Resources.FailedScreenshotStatusTextFormat);
            }
            return;
        }

        private void SaveAverage_Click(object sender, RoutedEventArgs e)
        {
            String strInfo = " " + this._lstDepthCaptures.Count;
            this._lstDepthCaptures.Clear();
            this._lstIrCaptures.Clear();
            this._lstRgbCaptures.Clear();

            //Thread thdSaveAvg = new Thread(new ThreadStart(saveAverageCapture));
            //Thread thdSaveAvg = new Thread(new ParameterizedThreadStart(SaveDataHelper.saveAverageCapture));
            
            Thread thdSaveAvg = new Thread(new ThreadStart(objSaveDataHelper.saveAverageCapture));
            //autoReset.Reset();
            thdSaveAvg.Start();
            return;
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            running = false;

            if (this.kinect != null)
            {
                this.kinect.Dispose();
            }
            return;
        }

        public OutputOption SelectedOutput
        {
            get => _selectedOutput;
            set
            {
                _selectedOutput = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedOutput"));
            }
        }

        ///// <summary> Gets the bitmap to display </summary>
        //public ImageSource ImageSource
        //{
        //    get
        //    {
        //        return this._bitmap;
        //    }
        //    set { this._bitmap = value; }
        //}

        /// <summary> Gets or sets the current status text to display </summary>
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

		public bool SaveAverageFlag { get => _bSaveAverageFlag; set => _bSaveAverageFlag = value; }

		public List<Capture> DepthCaptureList => _lstDepthCaptures;

		public List<Capture> IrCaptureList => _lstIrCaptures;

		public List<Capture> RgbCaptureList => _lstRgbCaptures;
	}

    public enum OutputType {
        Colour,
        Depth,
        IR
    }

    public class OutputOption
    {
        public string Name { get; set; }

        public OutputType OutputType { get; set; }
    }
}
