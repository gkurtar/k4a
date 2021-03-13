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

        /// <summary> Status of the application </summary>
        private bool running = true;

        private SynchronizationContext _uiContext;

        private OutputOption _selectedOutput;
        public ObservableCollection<OutputOption> Outputs { get; set; }

        private ImageSource _bitmap;

        /// <summary> INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource CurrentCameraImage => _bitmap;

        private int xPosImage = 0;

        private int yPosImage = 0;

        private long _nCaptureCounter = 0;

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
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true
            });

            this.colorWidth = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
            this.colorHeight = this.kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;
            this.bitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            this.DataContext = this;
            //this.KinectImage.MouseMove += this.mouseMoveOverStream;
            //this.KinectImage.MouseLeave += this.mouseLeaveFromStream;
            this.InitializeComponent();
            this._uiContext = SynchronizationContext.Current;
            return;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            while (running)
            {
                using (Capture capture = await Task.Run(() => { return this.kinect.GetCapture(); }))
                {
                    this._nCaptureCounter++;
                    switch (SelectedOutput.OutputType)
                    {
                        case OutputType.Depth:
                            lblInfo.Content = "DEPTH " + capture.Depth.WidthPixels + " X "
                                    + capture.Depth.HeightPixels + ", Format: " + capture.Depth.Format + ", Count: " + this._nCaptureCounter;

                            //Memory<byte> sa = capture.Depth.Memory;
                            //lblDene.Content = sa.Length;
                            //updateMemory(sa);
                            //capture.Depth.SetPixel()

                            _uiContext.Send(x =>
                            {
                                //Image dd = updateImage(capture.Depth);
                                //GeneralUtil.updateImage(capture.Depth);
                                //_bitmap = capture.Depth.CreateBitmapSource();

                                Image dd = GeneralUtil.updateImage(capture.Depth);
                                _bitmap = dd.CreateBitmapSource();

                                //Image dd = updateImage(capture.Depth);
                                //_bitmap = dd.CreateBitmapSource();

                                //for (int i = 0; i < 100; i++)
                                //{
                                //    for (int j = 0; j < 100; j++)
                                //    {
                                //        //capture.Depth.SetPixel(i, j, (int)rnd.Next(180, 255));
                                //        capture.Depth.SetPixel(i, j, 0xFFFFFFFF);
                                //    }
                                //}
                                //_bitmap = capture.Depth.CreateBitmapSource();

                                _bitmap.Freeze();
                            }, null);

                            if (this.KinectImage.IsMouseOver)
                            {
                                if (capture.Depth.WidthPixels > xPosImage && capture.Depth.HeightPixels > yPosImage)
                                {
                                    short sPixelValue = capture.Depth.GetPixel<short>(yPosImage, xPosImage);
                                    this.lblPos.Content = String.Format("x:{0, -3}, y:{1, -3}, val: {2, -5} ", xPosImage, yPosImage, sPixelValue);
                                }
                            }

                            break;
                        case OutputType.IR:
                            //PresentIR(capture);
                            //BitmapSource bmpsInfraRed = capture.IR.CreateBitmapSource();
                            //bmpsInfraRed.Freeze();
                            lblInfo.Content = "IR width: " + capture.IR.WidthPixels + " height: "
                                    + capture.IR.HeightPixels + " format:" + capture.IR.Format;

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

                                } // end of if
                            }

                            break;
                        case OutputType.Colour:
                        default:
                            //PresentColour(capture);
                            lblInfo.Content = "Color width: " + capture.Color.WidthPixels + " height: "
                                    + capture.Color.HeightPixels + " format:" + capture.Color.Format;

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

        //void mouseMoveOverRightStream(Object sender, MouseEventArgs e)
        //{
        //    int xPos = (int)Mouse.GetPosition(this).X;
        //    int yPos = (int)Mouse.GetPosition(this).Y;
        //    int xPosRelToRightStream = (int)Mouse.GetPosition(this.imgRight).X;
        //    int yPosRelToRightStream = (int)Mouse.GetPosition(this.imgRight).Y;
        //    this.xPosDepthStream = (int)Mouse.GetPosition(this.imgRight).X;
        //    this.yPosDepthStream = (int)Mouse.GetPosition(this.imgRight).Y;
        //    return;
        //}

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
            MessageBox.Show("sdfsdsdfsdfsdf");
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
