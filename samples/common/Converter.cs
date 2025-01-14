using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Orbbec;

namespace Common
{
    public class ImageConverter
    {
        public static byte[] ConvertDepthToRGBData(byte[] depthData)
        {
            byte[] colorData = new byte[depthData.Length / 2 * 3];

            for (int i = 0; i < depthData.Length; i += 2)
            {
                ushort depthValue = (ushort)((depthData[i + 1] << 8) | depthData[i]);
                float depth = (float)depthValue / 1000;
                byte depthByte = (byte)(depth * 255);
                int index = i / 2 * 3;
                colorData[index] = depthByte;     // Red
                colorData[index + 1] = depthByte; // Green
                colorData[index + 2] = depthByte; // Blue
            }
            return colorData;
        }

        public static byte[] ConvertIRToRGBData(byte[] irData, Format format)
        {
            byte[] colorData;
            switch (format)
            {
                case Format.OB_FORMAT_Y16:
                    colorData = new byte[irData.Length / 2 * 3];
                    for (int i = 0; i < irData.Length; i += 2)
                    {
                        ushort irValue = (ushort)((irData[i + 1] << 8) | irData[i]);
                        byte irByte = (byte)(irValue >> 8);

                        int index = i / 2 * 3;
                        colorData[index] = irByte;     // Red
                        colorData[index + 1] = irByte; // Green
                        colorData[index + 2] = irByte; // Blue
                    }
                    break;
                case Format.OB_FORMAT_Y8:
                    colorData = new byte[irData.Length * 3];
                    for (int i = 0; i < irData.Length; i++)
                    {
                        byte irByte = irData[i];

                        int index = i * 3;
                        colorData[index] = irByte;
                        colorData[index + 1] = irByte;
                        colorData[index + 2] = irByte;
                    }
                    break;
                default:
                    throw new Exception("IR Format is not supported!");
            }
            return colorData;
        }

        public static byte[] ConvertMJPGToRGBData(byte[] mjpgData)
        {
            using (var ms = new MemoryStream(mjpgData))
            {
                using (var jpegImage = new Bitmap(ms))
                {
                    Rectangle rect = new Rectangle(0, 0, jpegImage.Width, jpegImage.Height);
                    BitmapData bmpData = jpegImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    IntPtr ptr = bmpData.Scan0;
                    int size = Math.Abs(bmpData.Stride) * jpegImage.Height;
                    byte[] rgbData = new byte[size];

                    Marshal.Copy(ptr, rgbData, 0, size);

                    // Adjust the order of BGR to RGB
                    for (int i = 0; i < rgbData.Length; i += 3)
                    {
                        // BGR -> RGB: Exchange Blue and Red
                        byte temp = rgbData[i];      // Blue
                        rgbData[i] = rgbData[i + 2]; // Red
                        rgbData[i + 2] = temp;       // Exchange Blue and Red
                    }

                    return rgbData;
                }
            }
        }

        public static byte[] ConvertBGRAToRGBData(byte[] bgraData)
        {
            byte[] rgbData = new byte[bgraData.Length / 4 * 3];

            for (int i = 0; i < bgraData.Length / 4; i++)
            {
                int bgraIndex = i * 4;
                int rgbIndex = i * 3;

                rgbData[rgbIndex] = bgraData[bgraIndex + 2];
                rgbData[rgbIndex + 1] = bgraData[bgraIndex + 1];
                rgbData[rgbIndex + 2] = bgraData[bgraIndex];
            }

            return rgbData;
        }
    }
}