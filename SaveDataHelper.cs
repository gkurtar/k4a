using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K4ACalibration {

	using System.Globalization;
	using System.IO;
    using Microsoft.Azure.Kinect.Sensor;
    using System.Windows.Media.Imaging;
    using System.Threading;
	using System.Windows;

	class SaveDataHelper {

        private MainWindow winMain;

        internal SaveDataHelper(MainWindow objWinMain) {
            winMain = objWinMain;
        }

        internal void saveAverageCapture() {

            winMain.SaveAverageFlag = true;
            winMain.autoReset.WaitOne();

            switch (winMain.SelectedOutput.OutputType) {
                
                case OutputType.Depth:

                    if (winMain.DepthCaptureList.Count >= MainWindow.CAPTURE_CAPACITY) {
                        Capture cptSample = winMain.DepthCaptureList.ElementAt(0);

                        Capture cptDepthAverage = new Capture();
                        cptDepthAverage.Depth = new Image(cptSample.Depth.Format, cptSample.Depth.WidthPixels,
                            cptSample.Depth.HeightPixels, cptSample.Depth.StrideBytes);
                        cptDepthAverage.Depth.WhiteBalance = cptSample.Depth.WhiteBalance;
                        cptDepthAverage.Depth.DeviceTimestamp = cptSample.Depth.DeviceTimestamp;
                        cptDepthAverage.Depth.SystemTimestampNsec = cptSample.Depth.SystemTimestampNsec;

                        short sValue = 0;
                        short sTotal = 0;
                        short sCount = 0;
                        int index = 0;
                        float[,] depthVals = new float[winMain.depthHeight, winMain.depthWidth];
                        float[,,] depthAll = new float[winMain.DepthCaptureList.Count, winMain.depthHeight, winMain.depthWidth];
                        List<float[,]> lstDepthAll = new List<float[,]>();

                        for (int i = 0; i < winMain.depthHeight; i++) {
                            for (int j = 0; j < winMain.depthWidth; j++) {
                                sTotal = sCount = 0;
                                index = 0;
                                foreach (Capture cptDepth in winMain.DepthCaptureList) {
                                    sValue = cptDepth.Depth.GetPixel<short>(i, j);
                                    if (sValue > 0) {
                                        sTotal += sValue;
                                        sCount++;
                                    }
                                    depthAll[index, i, j] = sValue;
                                    index++;
                                }
                                depthVals[i, j] = sCount == 0 ? 0 : sTotal / (float)sCount;
                                cptDepthAverage.Depth.SetPixel<short>(i, j, (short)depthVals[i, j]);
                            } // end of for
                        } // end of for

                        //saveImageToFile(cptDepthAverage.Depth);
                        Image imgUpdated = GeneralUtil.updateImage(cptDepthAverage.Depth);
                        saveImageToFile(imgUpdated);
                        //SaveDataHelper.saveImageToFile(winMain, imgUpdated);

                        saveDepthDataToFile(depthVals);
                        //SaveDataHelper.saveDepthDataToFile(winMain, depthVals);
                        Thread.Sleep(1000);
                    } // end of if

                    break;
                case OutputType.IR:

                    if (winMain.IrCaptureList.Count >= MainWindow.CAPTURE_CAPACITY) {
                        Capture cptSample = winMain.IrCaptureList.ElementAt(0);
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
                        float[,] irVals = new float[winMain.infraRedHeight, winMain.infraRedWidth];

                        for (int i = 0; i < winMain.infraRedHeight; i++) {
                            for (int j = 0; j < winMain.infraRedWidth; j++) {
                                sTotal = sCount = 0;
                                index = 0;
                                foreach (Capture cptInfraRed in winMain.IrCaptureList) {
                                    sValue = cptInfraRed.IR.GetPixel<short>(i, j);
                                    if (sValue > 0) {
                                        sTotal += sValue;
                                        sCount++;
                                    }
                                    index++;
                                }
                                irVals[i, j] = sCount == 0 ? 0 : sTotal / (float)sCount;
                                cptIrAverage.IR.SetPixel<short>(i, j, (short)irVals[i, j]);
                            } // end of for
                        } // end of for

                        saveImageToFile(cptIrAverage.IR);
                        //SaveDataHelper.saveImageToFile(winMain, cptIrAverage.IR);
                        Thread.Sleep(1000);
                    } // end of if

                    break;
                case OutputType.Colour:
                default:

                    if (winMain.RgbCaptureList.Count >= MainWindow.CAPTURE_CAPACITY) {

                        Capture cptSample = winMain.RgbCaptureList.ElementAt(0);
                        Capture cptRgbAverage = new Capture();
                        cptRgbAverage.Color = new Image(cptSample.Color.Format, cptSample.Color.WidthPixels,
                            cptSample.Color.HeightPixels, cptSample.Color.StrideBytes);
                        cptRgbAverage.Color.WhiteBalance = cptSample.Color.WhiteBalance;
                        cptRgbAverage.Color.DeviceTimestamp = cptSample.Color.DeviceTimestamp;
                        cptRgbAverage.Color.SystemTimestampNsec = cptSample.Color.SystemTimestampNsec;

                        MessageBox.Show("sdf w-" + winMain.colorWidth + " h- " + winMain.colorHeight + " " + cptSample.Color.Format);

                        int nValue = 0;
                        int nTotal = 0;
                        int nCount = 0;
                        int[,] rgbVals = new int[winMain.colorHeight, winMain.colorWidth];

                        for (int i = 0; i < winMain.colorHeight; i++) {
                            for (int j = 0; j < winMain.colorWidth; j++) {
                                nTotal = nCount = 0;
                                foreach (Capture cptColor in winMain.RgbCaptureList) {
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
                        
                        saveImageToFile(cptRgbAverage.Color);
                        //SaveDataHelper.saveImageToFile(winMain, cptRgbAverage.Color);
                        Thread.Sleep(1000);
                    } // end of if
                    break;
            }

            if (MainWindow.CAPTURE_CAPACITY >= 1) {
                MessageBox.Show(String.Format("Average values found for {0} captures.", MainWindow.CAPTURE_CAPACITY));
            }

            Console.WriteLine(Thread.CurrentThread.Name + " is ended!");
            return;
        }

        private void saveImageToFile(Image acptSave) {

            String strType = winMain.SelectedOutput.OutputType.ToString();
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(acptSave.CreateBitmapSource()));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = Path.Combine(myPhotos, "KinectScreenshotAvg_"
                + MainWindow.CAPTURE_CAPACITY + "_" + strType + "-" + time + ".png");

            // Write the new file to disk
            try {
                using (FileStream fs = new FileStream(path, FileMode.Create)) {
                    encoder.Save(fs);
                }
                winMain.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            } catch (IOException) {
                winMain.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
            }
            return;
        }

        private void saveDepthDataToFile(float[,] aseqPoints) {

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string pathDepth = System.IO.Path.Combine(myPhotos, "DepthAvg_" + MainWindow.CAPTURE_CAPACITY + "-" + time + ".txt");

            // write the new file to disk
            try {
                using (StreamWriter sw = new StreamWriter(pathDepth)) {
                    // loop over each row and column of the depth
                    for (int x = 0; x < winMain.depthHeight; ++x) {
                        for (int y = 0; y < winMain.depthWidth; ++y) {
                            // calculate index into depth array
                            sw.WriteLine(x + " " + y + " " + aseqPoints[x, y]);
                        }
                    }
                }
                winMain.StatusText = string.Format(CultureInfo.InvariantCulture, "Saved depth data to {0}", pathDepth);

            } catch (IOException) {
                winMain.StatusText = string.Format(CultureInfo.InvariantCulture,
                    "{0}", Properties.Resources.FailedScreenshotStatusTextFormat);
            }
            return;
        }
    }
}
