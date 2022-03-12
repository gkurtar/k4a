using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K4ACalibration {
	using System.Globalization;
	using System.IO;

    class SaveDataHelper {

        //public static void saveImageToFile(Image acptSave) {

        //    String strType = SelectedOutput.OutputType.ToString();
        //    BitmapEncoder encoder = new PngBitmapEncoder();
        //    encoder.Frames.Add(BitmapFrame.Create(acptSave.CreateBitmapSource()));

        //    string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
        //    string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        //    string path = Path.Combine(myPhotos, "KinectScreenshotAvg_" + CAPTURE_CAPACITY + "_" + strType + "-" + time + ".png");

        //    // Write the new file to disk
        //    try {
        //        using (FileStream fs = new FileStream(path, FileMode.Create)) {
        //            encoder.Save(fs);
        //        }
        //        this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
        //    } catch (IOException) {
        //        this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
        //    }
        //    return;
        //}


        public static void saveDepthDataToFile(MainWindow winMain, float[,] aseqPoints) {

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string pathDepth = System.IO.Path.Combine(myPhotos, "DepthAvg_" + MainWindow.KINECT_CAPTURE_CAPACITY + "-" + time + ".txt");

            // write the new file to disk
            try {
                using (StreamWriter sw = new StreamWriter(pathDepth)) {
                    // loop over each row and column of the depth
                    for (int x = 0; x < winMain.DepthHeight; ++x) {
                        for (int y = 0; y < winMain.DepthWidth; ++y) {
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
