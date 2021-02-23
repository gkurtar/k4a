using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace K4ACalibration
{
    public class GeneralUtil
    {
        static Random rnd = new Random();
        public static void updateMemory(Memory<byte> aobjMemory)
        {
            byte[] bytArray = aobjMemory.ToArray();
            for (int i = 0; i < bytArray.Length; i++)
            {
                bytArray[i] = (byte) rnd.Next(0, 255);
            }
        }

        public static void sdf()
        {
            return;
        }

        public static void updateImage2(Image aimgDepth)
        {
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    //aimgDepth.SetPixel(i, j, (short)rnd.Next(180, 255839));
                    //aimgDepth.SetPixel(i, j, (int.MaxValue));
                    aimgDepth.SetPixel(i, j, 0xFFFFFFFF);
                }
            }
            return;
        }

        public static Image updateImage(Image aimgDepth)
        {
            short sMinDepth = short.MaxValue;
            short sMaxDepth = short.MinValue;
            for (int i = 0; i < aimgDepth.HeightPixels; i++)
            {
                for (int j = 0; j < aimgDepth.WidthPixels; j++)
                {
                    //aimgDepth.SetPixel(i, j, (short)rnd.Next(180, 255839));
                    //aimgDepth.SetPixel(i, j, short.MaxValue);
                    //aimgDepth.SetPixel(i, j, (int.MaxValue));
                    //aimgDepth.SetPixel(i, j, 0xFFFFFFFF);
                    if (aimgDepth.GetPixel<int>(i, j) < sMinDepth) {
                        sMinDepth = aimgDepth.GetPixel<short>(i, j);
                    }

                    if (aimgDepth.GetPixel<short>(i, j) > sMaxDepth) {
                        sMaxDepth = aimgDepth.GetPixel<short>(i, j);
                    }
                }
            }

            Image imgBGRA = new Image(ImageFormat.ColorBGRA32, aimgDepth.WidthPixels, aimgDepth.HeightPixels);
            for (int i = 0; i < aimgDepth.HeightPixels; i++)
            {
                for (int j = 0; j < aimgDepth.WidthPixels; j++)
                {
                    imgBGRA.SetPixel(i, j, 0xFF2F3FFF);
                    imgBGRA.SetPixel(i, j, ColorizeBlueToRed(aimgDepth.GetPixel<short>(i, j), sMinDepth, sMaxDepth));
                    //aimgDepth.SetPixel(i, j, short.MaxValue);
                    //aimgDepth.SetPixel(i, j, (int.MaxValue));
                    //aimgDepth.SetPixel(i, j, 0xFFFFFFFF);

                }
            }
            return imgBGRA;
        }

        static int ColorizeBlueToRed(short asDepthPixel, short asMin, short asMax)
        {
            //constexpr uint8_t PixelMax = std::numeric_limits<uint8_t>::max();
            // Default to opaque black.
            //BgraPixel result = { 0, 0, 0, PixelMax };
            int result = unchecked ((int)0x000000FF);
            
            // If the pixel is actual zero and not just below the min value, make it black
            if (asDepthPixel == 0) {
                return 0x000000FF;
            }

            short sClampedValue = asDepthPixel;
            sClampedValue = Math.Min(sClampedValue, asMax);
            sClampedValue = Math.Max(sClampedValue, asMin);

            // Normalize to [0, 1]
            float hue = (sClampedValue - asMin) / (float)(asMax - asMin);

            // The 'hue' coordinate in HSV is a polar coordinate, so it 'wraps'.
            // Purple starts after blue and is close enough to red to be a bit unclear,
            // so we want to go from blue to red.  Purple starts around .6666667,
            // so we want to normalize to [0, .6666667].
    
            float range = 2f / 3f;
            hue *= range;

            // We want blue to be close and red to be far, so we need to reflect the
            // hue across the middle of the range.
            hue = range - hue;

            float fRed = 0f;
            float fGreen = 0f;
            float fBlue = 0f;

            //ImGuiNET.
            //ImGui::ColorConvertHSVtoRGB(hue, 1.f, 1.f, fRed, fGreen, fBlue);

            int r, g, b;
            HsvToRgb(hue, 1f, 1f, out r, out g, out b);

            result |= r << 24;
            result |= g << 16;
            result |= b << 8;
            //result = result| 0xFFFFFFFF;

            //result.Red = static_cast<uint8_t>(fRed* PixelMax);
            //result.Green = static_cast<uint8_t>(fGreen* PixelMax);
            //result.Blue = static_cast<uint8_t>(fBlue* PixelMax);

            return result;
        }

        static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

        //    static inline BgraPixel ColorizeBlueToRed(const DepthPixel &depthPixel,
        //                                          const DepthPixel &min,
        //                                          const DepthPixel &max)
        //{
        //    constexpr uint8_t PixelMax = std::numeric_limits<uint8_t>::max();

        //    // Default to opaque black.
        //    //
        //    BgraPixel result = { 0, 0, 0, PixelMax };

        //    // If the pixel is actual zero and not just below the min value, make it black
        //    //
        //    if (depthPixel == 0)
        //    {
        //        return result;
        //    }

        //uint16_t clampedValue = depthPixel;
        //clampedValue = std::min(clampedValue, max);
        //    clampedValue = std::max(clampedValue, min);

        //    // Normalize to [0, 1]
        //    //
        //    float hue = (clampedValue - min) / static_cast<float>(max - min);

        //// The 'hue' coordinate in HSV is a polar coordinate, so it 'wraps'.
        //// Purple starts after blue and is close enough to red to be a bit unclear,
        //// so we want to go from blue to red.  Purple starts around .6666667,
        //// so we want to normalize to [0, .6666667].
        ////
        //constexpr float range = 2.f / 3.f;
        //hue *= range;

        //    // We want blue to be close and red to be far, so we need to reflect the
        //    // hue across the middle of the range.
        //    //
        //    hue = range - hue;

        //    float fRed = 0.f;
        //float fGreen = 0.f;
        //float fBlue = 0.f;
        //ImGui::ColorConvertHSVtoRGB(hue, 1.f, 1.f, fRed, fGreen, fBlue);

        //    result.Red = static_cast<uint8_t>(fRed* PixelMax);
        //    result.Green = static_cast<uint8_t>(fGreen* PixelMax);
        //    result.Blue = static_cast<uint8_t>(fBlue* PixelMax);

        //    return result;
        //}
    }
}
