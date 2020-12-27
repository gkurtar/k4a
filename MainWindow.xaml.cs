using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Azure.Kinect.Sensor;

namespace K4ACalibration
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine("device tests");
            //Color_Image.Width = 200;
            //Color_Image.Height = 200;
            Task.Run(() => Go());
        }

        //Int32Rect color_rect;
        //int color_stride;
        //WriteableBitmap color_bitmap;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task Go()
        {
            try
            {
                using (Device device = Device.Open())
                {
                    //MessageBox.Show("device open start for " + device.SerialNum);
                    Console.WriteLine("device open start for " + device.SerialNum);

                    device.StartCameras(new DeviceConfiguration
                    {
                        CameraFPS = FPS.FPS30,
                        ColorResolution = ColorResolution.R1080p,
                        ColorFormat = ImageFormat.ColorBGRA32,

                        DepthMode = DepthMode.NFOV_2x2Binned,

                        SynchronizedImagesOnly = true
                    });

                    Capture capture1 = null;
                    int i = 0;
                    while (true)
                    {
                        i++;
                        capture1 = device.GetCapture();

                        //int color_width = device.GetCalibration().ColorCameraCalibration.ResolutionWidth;
                        //int color_height = device.GetCalibration().ColorCameraCalibration.ResolutionHeight;

                        //MessageBox.Show("device capture null check #" + i + " is " + (null == capture1)
                        //    + "width " + color_width + ", and height " + color_height);
                        // MessageBox.Show("width " + color_width + ", and height " + color_height);

                        //const int color_channles = 4;
                        //color_stride = color_width * sizeof(byte) * color_channles;
                        //color_rect = new Int32Rect(0, 0, color_width, color_height);
                        //color_bitmap = new WriteableBitmap(color_width, color_height, 96.0, 96.0, PixelFormats.Bgra32, null);

                        //Microsoft.Azure.Kinect.Sensor.Image colorimg = capture1.Color;
                        //byte[] color_buffer = colorimg.Memory.ToArray();
                        //color_bitmap.WritePixels(color_rect, color_buffer, color_stride, 0, 0);

                        // Create a BitmapSource for the unmodified color image.
                        // Creating the BitmapSource is slow, so do it asynchronously on another thread
                        Task<BitmapSource> createInputColorBitmapTask = Task.Run(() => {
                            BitmapSource source = capture1.Color.CreateBitmapSource();
                            // Allow the bitmap to move threads
                            source.Freeze();
                            return source;
                        });


                        // Wait for both bitmaps to be ready and assign them.
                        BitmapSource inputColorBitmap = await createInputColorBitmapTask.ConfigureAwait(true);

                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            // Color_Image.Source = color_bitmap;
                            //Color_Image.Width = color_width;
                            //Color_Image.Height = color_height;
                            lblDene.Content = " # " + i;
                            this.Color_Image.Source = inputColorBitmap;
                        }));

                        /*Color_Image.Dispatcher.Invoke(() =>
                        {
                            // UI operation goes inside of Invoke
                            Color_Image.Source = color_bitmap;
                            Color_Image.Width = color_width;
                            Color_Image.Height = color_height;
                        });
                        */
                    }

                }
                //MessageBox.Show("device open end");
            }
            catch (Exception ex)
            {
            }
            return;
        }

        private WriteableBitmap CreateNewBitmap(
            Microsoft.Azure.Kinect.Sensor.Image image,
            int xLeft, int yLeft,
            int xRight, int yRight)
        {

            WriteableBitmap wb = new WriteableBitmap(
                image.WidthPixels + 30,
                image.HeightPixels + 30,
                96,
                96,
                PixelFormats.Bgra32, null);

            var region = new Int32Rect(0, 0, image.WidthPixels, image.HeightPixels);

            unsafe
            {
                using (var pin = image.Memory.Pin())
                {
                    wb.WritePixels(region, (IntPtr)pin.Pointer, (int)image.Size, image.StrideBytes);
                }
            }
            using (wb.GetBitmapContext())
            {
                wb.FillEllipse(xLeft, yLeft, xLeft + 40, yLeft + 40, Colors.Red);
                wb.FillEllipse(xRight, yRight, xRight + 40, yRight + 40, Colors.Blue);
            }

            return wb;
        }

        private void btnSaveRGBIR_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnSaveDepthClick(object sender, RoutedEventArgs e)
        {
        }
    }

    /*
                    var calibration = device.GetCalibration();
                    Transformation transformation = calibration.CreateTransformation();

                    Tracker tracker = Tracker.Create(calibration, new TrackerConfiguration()
                    {
                        ProcessingMode = TrackerProcessingMode.Gpu,
                        SensorOrientation = SensorOrientation.Default
                    });

                    while (true)
                    {
                        Capture capture1 = device.GetCapture();

                        tracker.EnqueueCapture(capture1);
                        var frame = tracker.PopResult(TimeSpan.Zero, throwOnTimeout: false);
                        if (frame != null)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                for (uint i = 0; i < frame.NumberOfBodies; i++)
                                {
                                    var bodyId = frame.GetBodyId(i);
                                    var body = frame.GetBody(i);

                                    var handLeft = body.Skeleton.GetJoint(JointId.HandLeft);
                                    var handRight = body.Skeleton.GetJoint(JointId.HandRight);

                                    var pointLeft = calibration.TransformTo2D(handLeft.Position, CalibrationDeviceType.Depth, CalibrationDeviceType.Color);
                                    var pointRight = calibration.TransformTo2D(handRight.Position, CalibrationDeviceType.Depth, CalibrationDeviceType.Color);

                                    try
                                    {
                                        if (pointLeft.HasValue && pointRight.HasValue)
                                        {
                                            Image1.Source = CreateNewBitmap(capture1.Color,
                                                (int)pointLeft.Value.X, (int)pointLeft.Value.Y,
                                                (int)pointRight.Value.X, (int)pointRight.Value.Y);
                                        }
                                    }
                                    catch { }
                                }
                            });

                        }
                    }
                    */

}
