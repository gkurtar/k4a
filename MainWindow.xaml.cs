//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace K4ACalibration
{
    using System;
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

        /// <summary> The width in pixels of the color image from the Azure Kinect DK </summary>
        private readonly int colorWidth = 0;

        /// <summary> The height in pixels of the color image from the Azure Kinect DK </summary>
        private readonly int colorHeight = 0;

        private readonly int depthWidth = 0;

        private readonly int depthHeight = 0;

        private readonly int infraRedWidth = 0;

        private readonly int infraRedHeight = 0;

        /// <summary> Status of the application </summary>
        private bool running = true;

        private int xPosImage = 0;

        private int yPosImage = 0;

        private long _nCaptureCounter = 0;

        private volatile bool _bSaveAverageFlag = false;

        private static readonly int CAPTURE_CAPACITY = 10;

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
            //this.KinectImage.MouseMove += KinectImage_MouseMove;

            // Open the default device
            this.kinect = Device.Open();

            // Configure camera modes
            this.kinect.StartCameras(new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R2160p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = false
            });


            this.colorWidth = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            this.colorHeight = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

            this.depthWidth = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
            this.depthHeight = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;

            this.infraRedWidth = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
            this.infraRedHeight = this.kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;

            this.bitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.DataContext = this;
            //this.KinectImage.MouseMove += this.mouseMoveOverStream;
            //this.KinectImage.MouseLeave += this.mouseLeaveFromStream;
            this.InitializeComponent();
            this._uiContext = SynchronizationContext.Current;

            this._sbdCaptureColorInfo.Append(String.Format("{0} :: {1} X {2} :: ",
                ImageFormat.ColorBGRA32, this.colorWidth, this.colorHeight));

            this._sbdCaptureDepthInfo.Append(String.Format("{0} :: {1} X {2} :: ",
                ImageFormat.Depth16, this.depthWidth, this.depthHeight));

            this._sbdCaptureInfraRedInfo.Append(String.Format("{0} :: {1} X {2} :: ",
                ImageFormat.IR16, this.infraRedWidth, this.infraRedHeight));
            return;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

                            //lblInfo.Content = capture.Depth.Format + " " + capture.Depth.WidthPixels + " X "
                            //        + capture.Depth.HeightPixels
                            //        + ", Count: " + this._nCaptureCounter;

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
                            
                            _uiContext.Send(x =>
                            {
                                Image dd = GeneralUtil.updateImage(capture.Depth);
                                _bitmap = dd.CreateBitmapSource();

                                _bitmap.Freeze();
                            }, null);

                            if (this.KinectImage.IsMouseOver)
                            {
                                if (capture.Depth.WidthPixels > xPosImage && capture.Depth.HeightPixels > yPosImage)
                                {
                                    short sPixelValue = capture.Depth.GetPixel<short>(yPosImage, xPosImage);
                                    this.lblPos.Content = String.Format("x:{0, -3}, y:{1, -3}, val: {2, -5} ", xPosImage, yPosImage, sPixelValue);

                                    this._sbdPositionInfo.Clear();
                                    this._sbdPositionInfo.Append(String.Format(
                                        " :: x: {0, -3}, y: {1, -3}, value: {2, -5} ", xPosImage, yPosImage, sPixelValue));
                                }
                            } 
                            
                            int nLastIndex = this._sbdCaptureDepthInfo.ToString().LastIndexOf(":");
                            nLastIndex = (nLastIndex < 0 && nLastIndex >= this._sbdCaptureDepthInfo.Length -1 ? 0 : nLastIndex + 2);
                            this._sbdCaptureDepthInfo.Remove(nLastIndex, this._sbdCaptureDepthInfo.Length - nLastIndex);
                            this._sbdCaptureDepthInfo.Append(this._nCaptureCounter);

                            lblInfo.Content = this._sbdCaptureDepthInfo.ToString() + this._sbdPositionInfo.ToString();
                            
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

                            _uiContext.Send(x =>
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
                                    short sPixelValue = capture.IR.GetPixel<short>(yPosImage, xPosImage);
                                    this.lblPos.Content = String.Format("x:{0, -3}, y:{1, -3}, val: {2, -5} ", xPosImage, yPosImage, sPixelValue);

                                    this._sbdPositionInfo.Clear();
                                    this._sbdPositionInfo.Append(String.Format(" :: x: {0, -3}, y: {1, -3}, value: {2, -5} ", xPosImage, yPosImage, sPixelValue));
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

                            _uiContext.Send(x =>
                            {
                                _bitmap = capture.Color.CreateBitmapSource();
                                _bitmap.Freeze();
                            }, null);

                            if (this.KinectImage.IsMouseOver)
                            {
                                if (capture.Color.WidthPixels > xPosImage && capture.Color.HeightPixels > yPosImage)
                                {
                                    int nPixelValue = capture.Color.GetPixel<int>(yPosImage, xPosImage);
                                    int nRedValue = nPixelValue >> 16 & 0x000000FF;
                                    int nGreenValue = nPixelValue >> 8 & 0x000000FF;
                                    int nBlueValue = nPixelValue & 0x000000FF;
                                    this.lblPos.Content = String.Format("x:{0, -4}, y:{1, -4}," +
                                        "  red: {2,-3}, g: {3,-3}, b: {4,-3}  ", xPosImage, yPosImage, nRedValue, nGreenValue, nBlueValue);

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

                    //this.StatusText = "Received Capture: " + capture.Depth.DeviceTimestamp;
                    //this.bitmap.Lock();
                    //var color = capture.Color;
                    //var region = new Int32Rect(0, 0, color.WidthPixels, color.HeightPixels);
                    //unsafe
                    //{
                    //    using (var pin = color.Memory.Pin())
                    //    {
                    //        this.bitmap.WritePixels(region, (IntPtr)pin.Pointer, (int)color.Size, color.StrideBytes);
                    //    }
                    //}
                    //this.bitmap.AddDirtyRect(region);
                    //this.bitmap.Unlock();
                }
            }
            return;
        }

        void mouseLeaveFromStream(Object sender, MouseEventArgs e)
        {
            this.lblPos.Content = "";
            this._sbdPositionInfo.Clear();
            return;
        }

        void mouseMoveOverStream(Object sender, MouseEventArgs e)
        {
            //this.lblPos.Content = "x: " + (int)Mouse.GetPosition(this.KinectImage).X
            //    + " y: " + (int)Mouse.GetPosition(this.KinectImage).Y + " counter: " + counter ;
            //this.yPosRgbIrStream = (int)Mouse.GetPosition(this.imgLeft).Y;
            this.xPosImage = (int)Mouse.GetPosition(this.KinectImage).X;
            this.yPosImage = (int)Mouse.GetPosition(this.KinectImage).Y;
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
            return;
        }

        private void SaveAverage_Click(object sender, RoutedEventArgs e)
        {
            String strInfo = " " + this._lstDepthCaptures.Count;
            //int count = this._lstDepthCaptures.Count;
            //foreach (Capture aPart in this._lstDepthCaptures)
            //{
            //    long lTimes = aPart.Depth.SystemTimestampNsec;
            //    strInfo += lTimes + " _ ";
            //}

            //Capture tempRef = this._lstDepthCaptures.ElementAt(0);
            //Capture capDepthAverage = new Capture();
            //capDepthAverage.Depth = new Image(tempRef.Depth.Format, tempRef.Depth.WidthPixels, tempRef.Depth.HeightPixels, tempRef.Depth.StrideBytes);
            ////capDepthAverage.Depth.SetPixel
            this._lstDepthCaptures.Clear();
            this._lstIrCaptures.Clear();
            this._lstRgbCaptures.Clear();
            //this._bSaveAverageFlag = true;
            //MessageBox.Show("sdfsdsdfsdfsdf: " + strInfo + " flag:" + _bSaveAverageFlag);
            Thread thdSaveAvg = new Thread(new ThreadStart(saveAverageCapture));
            //autoReset.Reset();
            thdSaveAvg.Start();
            return;
        }

        private readonly AutoResetEvent autoReset = new AutoResetEvent(false);

        private void saveAverageCapture()
        {
            
            Console.WriteLine(Thread.CurrentThread.Name + " is started!");
            this._bSaveAverageFlag = true;
            MessageBox.Show("sta, count: " + this._lstDepthCaptures.Count + ", " + this._bSaveAverageFlag);
            autoReset.WaitOne();

            switch (SelectedOutput.OutputType)
            {
                case OutputType.Depth:

                    if (this._lstDepthCaptures.Count >= CAPTURE_CAPACITY) {
                        Capture cptSample = this._lstDepthCaptures.ElementAt(0);
                        Capture cptDepthAverage = new Capture();
                        cptDepthAverage.Depth = new Image(cptSample.Depth.Format, cptSample.Depth.WidthPixels,
                            cptSample.Depth.HeightPixels, cptSample.Depth.StrideBytes);
                        cptDepthAverage.Depth.WhiteBalance = cptSample.Depth.WhiteBalance;
                        cptDepthAverage.Depth.DeviceTimestamp = cptSample.Depth.DeviceTimestamp;
                        cptDepthAverage.Depth.SystemTimestampNsec = cptSample.Depth.SystemTimestampNsec;

                        //foreach (Capture cptDepth in this._lstDepthCaptures) {
                        //    //long lTimes = aPart.Depth.SystemTimestampNsec;
                        //    //strInfo += lTimes + " _ ";
                        //}
                        short sValue = 0;
                        short sTotal = 0;
                        short sCount = 0;
                        int index = 0;
                        float[,] depthVals = new float[depthHeight, depthWidth];
                        float[,,] depthAll = new float[this._lstDepthCaptures.Count, depthHeight, depthWidth];
                        List<float[,]> lstDepthAll = new List<float[,]>();

                        for (int i = 0; i < depthHeight; i++) {
                            for (int j = 0; j < depthWidth; j++) {
                                sTotal = sCount = 0;
                                index = 0;
                                foreach (Capture cptDepth in this._lstDepthCaptures) {
                                    sValue = cptDepth.Depth.GetPixel<short>(i, j);
                                    if (sValue > 0) {
                                        sTotal += sValue;
                                        sCount++;
                                    }
                                    depthAll[index, i, j] = sValue;
                                    index++;
                                }
                                depthVals[i, j] = sCount == 0 ? 0 : sTotal / (float)sCount;
                                cptDepthAverage.Depth.SetPixel<short>(i, j, (short) depthVals[i, j]);
                            } // end of for
                        } // end of for

                        //saveImageToFile(cptDepthAverage.Depth);
                        Image dd = GeneralUtil.updateImage(cptDepthAverage.Depth);
                        saveImageToFile(dd);

                        saveDepthDataToFile(depthVals);
                        Thread.Sleep(1000);

                        ////dogrulama icin
                        //for (int i = 0; i < this._lstDepthCaptures.Count; i++) {
                        //    float[,] temp = new float[depthHeight, depthWidth];

                        //    for (int j = 0; j < depthHeight; j++) {
                        //        for (int k = 0; k < depthWidth; k++) {
                        //            temp[j, k] = depthAll[i, j, k];
                        //        }
                        //    }
                        //    Thread.Sleep(1000);
                        //    saveDepthDataToFile(temp);
                        //}
                    } // end of if

                    break;
                case OutputType.IR:

                    if (this._lstIrCaptures.Count >= CAPTURE_CAPACITY) {
                        Capture cptSample = this._lstIrCaptures.ElementAt(0);
                        Capture cptIrAverage = new Capture();
                        cptIrAverage.IR = new Image(cptSample.IR.Format, cptSample.IR.WidthPixels,
                            cptSample.IR.HeightPixels, cptSample.IR.StrideBytes);
                        cptIrAverage.IR.WhiteBalance = cptSample.IR.WhiteBalance;
                        cptIrAverage.IR.DeviceTimestamp = cptSample.IR.DeviceTimestamp;
                        cptIrAverage.IR.SystemTimestampNsec = cptSample.IR.SystemTimestampNsec;

                        short sValue = 0;
                        short sTotal = 0;
                        short sCount = 0;
                        int index = 0;
                        float[,] irVals = new float[infraRedHeight, infraRedWidth];
                        
                        for (int i = 0; i < infraRedHeight; i++) {
                            for (int j = 0; j < infraRedWidth; j++) {
                                sTotal = sCount = 0;
                                index = 0;
                                foreach (Capture cptInfraRed in this._lstIrCaptures) {
                                    sValue = cptInfraRed.IR.GetPixel<short>(i, j);
                                    if (sValue > 0) {
                                        sTotal += sValue;
                                        sCount++;
                                    }
                                    index++;
                                }
                                irVals[i, j] = sCount == 0 ? 0 : sTotal / (float)sCount;
                                cptIrAverage.IR.SetPixel<short>(i, j, (short) irVals[i, j]);
                            } // end of for
                        } // end of for

                        saveImageToFile(cptIrAverage.IR);
                        Thread.Sleep(1000);
                    } // end of if

                    break;
                case OutputType.Colour:
                default:

                    if (this._lstRgbCaptures.Count >= CAPTURE_CAPACITY) {
                        
                        Capture cptSample = this._lstRgbCaptures.ElementAt(0);
                        Capture cptRgbAverage = new Capture();
                        cptRgbAverage.Color = new Image(cptSample.Color.Format, cptSample.Color.WidthPixels,
                            cptSample.Color.HeightPixels, cptSample.Color.StrideBytes);
                        cptRgbAverage.Color.WhiteBalance = cptSample.Color.WhiteBalance;
                        cptRgbAverage.Color.DeviceTimestamp = cptSample.Color.DeviceTimestamp;
                        cptRgbAverage.Color.SystemTimestampNsec = cptSample.Color.SystemTimestampNsec;

                        MessageBox.Show("sdf w-" + colorWidth + " h- " + colorHeight + " " + cptSample.Color.Format);

                        int nValue = 0;
                        int nTotal = 0;
                        int nCount = 0;
                        int[,] rgbVals = new int[colorHeight, colorWidth];

                        for (int i = 0; i < colorHeight; i++) {
                            for (int j = 0; j < colorWidth; j++) {
                                nTotal = nCount = 0;
                                foreach (Capture cptColor in this._lstRgbCaptures) {
                                    //nValue = cptColor.Color.GetPixel<int>(i, j);
                                    if (nValue > 0) {
                                        nTotal += nValue;
                                        nCount++;
                                    }
                                }
                                rgbVals[i, j] = nCount == 0 ? 0 : nTotal / nCount;
                                //cptRgbAverage.Color.SetPixel<int>(i, j, rgbVals[i, j]);
                            } // end of for
                        } // end of for
                        MessageBox.Show("sdf 2");
                        saveImageToFile(cptRgbAverage.Color);
                        Thread.Sleep(1000);
                    } // end of if
                    break;
            }

            MessageBox.Show("end, count: " + this._lstDepthCaptures.Count + ", " + this._bSaveAverageFlag);
            //MessageBox.Show("end");
            Console.WriteLine(Thread.CurrentThread.Name + " is ended!");
            return;
        }

        private void saveImageToFile(Image acptSave) {

            String strType = SelectedOutput.OutputType.ToString();
            BitmapEncoder encoder = new PngBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            encoder.Frames.Add(BitmapFrame.Create(acptSave.CreateBitmapSource()));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = Path.Combine(myPhotos, "KinectScreenshot_" + strType + "-" + time + ".png");

            // Write the new file to disk
            try {
                using (FileStream fs = new FileStream(path, FileMode.Create)) {
                    encoder.Save(fs);
                }
                this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            } catch (IOException) {
                this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
            }
            return;
        }

        private void saveDepthDataToFile(float[,] aseqPoints) {
            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string pathDepth = System.IO.Path.Combine(myPhotos, "Depth" + time + ".txt");

            // write the new file to disk
            try {
                using (StreamWriter sw = new StreamWriter(pathDepth)) {
                    // loop over each row and column of the depth
                    for (int x = 0; x < depthHeight; ++x) {
                        for (int y = 0; y < depthWidth; ++y) {
                            // calculate index into depth array
                            sw.WriteLine(x + " " + y + " " + aseqPoints[x, y]);
                        }
                    }
                }

                this.StatusText = string.Format(CultureInfo.InvariantCulture, "{0} Saved depth data to {1}",
                    Properties.Resources.SavedScreenshotStatusTextFormat, pathDepth);

            } catch (IOException) {
                this.StatusText = string.Format(CultureInfo.InvariantCulture, "{0}", Properties.Resources.FailedScreenshotStatusTextFormat);
            }
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

        /// <summary> Gets the bitmap to display </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this._bitmap;
            }
            set { this._bitmap = value; }
        }

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
    }

    public enum OutputType
    {
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
