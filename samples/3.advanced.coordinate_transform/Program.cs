using System;

namespace Orbbec
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run();
        }

        public void Run()
        {
            Pipeline pipeline = null;
            try
            {
                Context.SetLoggerToFile(LogSeverity.OB_LOG_SEVERITY_DEBUG, "C:\\Log\\OrbbecSDK");
                Config config = new Config();
                config.EnableVideoStream(StreamType.OB_STREAM_DEPTH, 0, 0, 0, Format.OB_FORMAT_Y16);
                config.EnableVideoStream(StreamType.OB_STREAM_COLOR, 0, 0, 0, Format.OB_FORMAT_RGB);

                config.SetFrameAggregateOutputMode(FrameAggregateOutputMode.OB_FRAME_AGGREGATE_OUTPUT_ALL_TYPE_FRAME_REQUIRE);

                pipeline = new Pipeline();

                pipeline.Start(config);

                string testType = "1";

                while (true)
                {
                    PrintUsage();
                    testType = InputWatcher();

                    using (var frames = pipeline.WaitForFrames(100))
                    {
                        if (frames == null)
                        {
                            continue;
                        }

                        var colorFrame = frames.GetColorFrame();
                        var depthFrame = frames.GetDepthFrame();

                        if (testType == "1")
                        {
                            Transformation2dto2d(colorFrame, depthFrame);
                        }
                        else if (testType == "2")
                        {
                            Transformation2dto3d(colorFrame, depthFrame);
                        }
                        else if (testType == "3")
                        {
                            Transformation3dto3d(colorFrame, depthFrame);
                        }
                        else if (testType == "4")
                        {
                            Transformation3dto2d(colorFrame, depthFrame);
                        }
                        else
                        {
                            Console.WriteLine("Invalid command");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.Exit(-1);
            }
            finally
            {
                if (pipeline != null)
                {
                    pipeline.Stop();
                    pipeline.Dispose();
                }
            }
        }

        // test the transformation from one 2D coordinate system to another
        private void Transformation2dto2d(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            // Get the width and height of the color and depth frames
            uint colorFrameWidth = colorFrame.GetWidth();
            uint depthFrameWidth = depthFrame.GetWidth();
            uint colorFrameHeight = colorFrame.GetHeight();
            uint depthFrameHeight = depthFrame.GetHeight();

            // Get the stream profiles for the color and depth frames
            StreamProfile colorProfile = colorFrame.GetStreamProfile();
            StreamProfile depthProfile = depthFrame.GetStreamProfile();
            Extrinsic extrinsicD2C = depthProfile.GetExtrinsicTo(colorProfile);

            // Get the intrinsic and distortion parameters for the color and depth streams
            VideoStreamProfile colorVideoStreamProfile = colorProfile.As<VideoStreamProfile>();
            CameraIntrinsic colorIntrinsic = colorVideoStreamProfile.GetIntrinsic();
            CameraDistortion colorDistortion = colorVideoStreamProfile.GetDistortion();
            VideoStreamProfile depthVideoStreamProfile = depthProfile.As<VideoStreamProfile>();
            CameraIntrinsic depthIntrinsic = depthVideoStreamProfile.GetIntrinsic();
            CameraDistortion depthDistortion = depthVideoStreamProfile.GetDistortion();
            // Access the depth data from the frame
            byte[] pDepthData = new byte[depthFrame.GetDataSize()];
            depthFrame.CopyData(ref pDepthData);
            uint convertAreaWidth = 3;
            uint convertAreaHeight = 3;

            // Transform depth values to the color frame's coordinate system
            for (uint i = depthFrameHeight / 2; i < (depthFrameHeight / 2 + convertAreaHeight); i++)
            {
                for (uint j = depthFrameWidth / 2; j < (depthFrameWidth / 2 + convertAreaWidth); j++)
                {
                    Point2f sourcePixel = new Point2f { x = j, y = i };
                    Point2f targetPixel = new Point2f();
                    float depthValue = pDepthData[i * depthFrameWidth + j];
                    if (depthValue == 0)
                    {
                        Console.WriteLine("The depth value is 0, so it's recommended to point the camera at a flat surface");
                        continue;
                    }

                    // Demonstrate Depth 2D converted to Color 2D
                    bool result = CoordinateTransformHelper.Transformation2dto2d(sourcePixel, depthValue, depthIntrinsic, depthDistortion, colorIntrinsic,
                                                                                      colorDistortion, extrinsicD2C, ref targetPixel);

                    // Check transformation result and whether the target pixel is within the color frame
                    if (!result || targetPixel.y < 0 || targetPixel.x < 0 || targetPixel.y >= colorFrameHeight || targetPixel.x >= colorFrameWidth)
                    {
                        continue;
                    }

                    // Calculate the index position of the target pixel in the transformation data buffer
                    uint index = (uint)(targetPixel.y * colorFrameWidth + targetPixel.x);
                    if (index >= colorFrameWidth * colorFrameHeight)
                    {
                        continue;
                    }

                    PrintRuslt("depth to color: depth image coordinate transform to color image coordinate", sourcePixel, targetPixel);
                }
            }
        }

        // test the transformation from 2D to 3D coordinates
        void Transformation2dto3d(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            // Get the width and height of the color and depth frames
            uint depthFrameWidth = depthFrame.GetWidth();
            uint depthFrameHeight = depthFrame.GetHeight();

            // Get the stream profiles for the color and depth frames
            StreamProfile colorProfile = colorFrame.GetStreamProfile();
            StreamProfile depthProfile = depthFrame.GetStreamProfile();
            Extrinsic extrinsicD2C = depthProfile.GetExtrinsicTo(colorProfile);

            // Get the intrinsic and distortion parameters for the color and depth streams
            CameraIntrinsic depthIntrinsic = depthProfile.As<VideoStreamProfile>().GetIntrinsic();
            // Access the depth data from the frame
            byte[] pDepthData = new byte[depthFrame.GetDataSize()];
            depthFrame.CopyData(ref pDepthData);
            uint convertAreaWidth = 3;
            uint convertAreaHeight = 3;

            // Transform depth values to the color frame's coordinate system
            for (uint i = depthFrameHeight / 2; i < (depthFrameHeight / 2 + convertAreaHeight); i++)
            {
                for (uint j = depthFrameWidth / 2; j < (depthFrameWidth / 2 + convertAreaWidth); j++)
                {
                    // Get the coordinates of the current pixel
                    Point2f sourcePixel = new Point2f { x = j, y = i };
                    Point3f targetPixel = new Point3f();
                    // Get the depth value of the current pixel
                    float depthValue = pDepthData[i * depthFrameWidth + j];
                    if (depthValue == 0)
                    {
                        Console.WriteLine("The depth value is 0, so it's recommended to point the camera at a flat surface");
                        continue;
                    }

                    // Perform the 2D to 3D transformation
                    bool result = CoordinateTransformHelper.Transformation2dto3d(sourcePixel, depthValue, depthIntrinsic, extrinsicD2C, ref targetPixel);
                    if (!result)
                    {
                        continue;
                    }

                    PrintRuslt("2d to 3D: pixel coordinates and depth transform to point in 3D space", sourcePixel, targetPixel, depthValue);
                }
            }
        }

        // test the transformation from 3D coordinates to 3D coordinates
        void Transformation3dto3d(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            // Get the width and height of the color and depth frames
            uint depthFrameWidth = depthFrame.GetWidth();
            uint depthFrameHeight = depthFrame.GetHeight();

            // Get the stream profiles for the color and depth frames
            StreamProfile colorProfile = colorFrame.GetStreamProfile();
            StreamProfile depthProfile = depthFrame.GetStreamProfile();
            Extrinsic extrinsicC2D = colorProfile.GetExtrinsicTo(depthProfile);
            Extrinsic extrinsicD2C = depthProfile.GetExtrinsicTo(colorProfile);

            // Get the intrinsic and distortion parameters for the color and depth streams
            CameraIntrinsic depthIntrinsic = depthProfile.As<VideoStreamProfile>().GetIntrinsic();
            // Access the depth data from the frame
            byte[] pDepthData = new byte[depthFrame.GetDataSize()];
            depthFrame.CopyData(ref pDepthData);
            uint convertAreaWidth = 3;
            uint convertAreaHeight = 3;

            // Transform depth values to the color frame's coordinate system
            for (uint i = depthFrameHeight / 2; i < (depthFrameHeight / 2 + convertAreaHeight); i++)
            {
                for (uint j = depthFrameWidth / 2; j < (depthFrameWidth / 2 + convertAreaWidth); j++)
                {
                    // Get the coordinates of the current pixel
                    Point2f sourcePixel = new Point2f { x = j, y = i };
                    Point3f tmpTargetPixel = new Point3f();
                    Point3f targetPixel = new Point3f();
                    // Get the depth value of the current pixel
                    float depthValue = pDepthData[i * depthFrameWidth + j];
                    if (depthValue == 0)
                    {
                        Console.WriteLine("The depth value is 0, so it's recommended to point the camera at a flat surface");
                        continue;
                    }

                    // Perform the 2D to 3D transformation
                    bool result = CoordinateTransformHelper.Transformation2dto3d(sourcePixel, depthValue, depthIntrinsic, extrinsicD2C, ref tmpTargetPixel);
                    if (!result)
                    {
                        continue;
                    }
                    PrintRuslt("2d to 3D: pixel coordinates and depth transform to point in 3D space", sourcePixel, tmpTargetPixel, depthValue);

                    // Perform the 3D to 3D transformation
                    result = CoordinateTransformHelper.Transformation3dto3d(tmpTargetPixel, extrinsicC2D, ref targetPixel);
                    if (!result)
                    {
                        continue;
                    }
                    PrintRuslt("3d to 3D: transform 3D coordinates relative to one sensor to 3D coordinates relative to another viewpoint", tmpTargetPixel, targetPixel);
                }
            }
        }

        // test the transformation from 3D coordinates back to 2D coordinates
        void Transformation3dto2d(ColorFrame colorFrame, DepthFrame depthFrame)
        {
            // Get the width and height of the color and depth frames
            uint depthFrameWidth = depthFrame.GetWidth();
            uint depthFrameHeight = depthFrame.GetHeight();

            // Get the stream profiles for the color and depth frames
            StreamProfile colorProfile = colorFrame.GetStreamProfile();
            StreamProfile depthProfile = depthFrame.GetStreamProfile();
            Extrinsic extrinsicC2D = colorProfile.GetExtrinsicTo(depthProfile);
            Extrinsic extrinsicD2C = depthProfile.GetExtrinsicTo(colorProfile);

            // Get the intrinsic and distortion parameters for the color and depth streams
            VideoStreamProfile depthVideoStreamProfile = depthProfile.As<VideoStreamProfile>();
            CameraIntrinsic depthIntrinsic = depthVideoStreamProfile.GetIntrinsic();
            CameraDistortion depthDistortion = depthVideoStreamProfile.GetDistortion();
            // Access the depth data from the frame
            byte[] pDepthData = new byte[depthFrame.GetDataSize()];
            depthFrame.CopyData(ref pDepthData);
            uint convertAreaWidth = 3;
            uint convertAreaHeight = 3;

            // Transform depth values to the color frame's coordinate system
            for (uint i = depthFrameHeight / 2; i < (depthFrameHeight / 2 + convertAreaHeight); i++)
            {
                for (uint j = depthFrameWidth / 2; j < (depthFrameWidth / 2 + convertAreaWidth); j++)
                {
                    // Get the coordinates of the current pixel
                    Point2f sourcePixel = new Point2f { x = (float)j, y = (float)i };
                    Point3f tmpTargetPixel = new Point3f();
                    Point2f targetPixel = new Point2f();
                    // Get the depth value of the current pixel
                    float depthValue = (float)pDepthData[i * depthFrameWidth + j];
                    if (depthValue == 0)
                    {
                        Console.WriteLine("The depth value is 0, so it's recommended to point the camera at a flat surface");
                        continue;
                    }

                    // Perform the 2D to 3D transformation
                    bool result = CoordinateTransformHelper.Transformation2dto3d(sourcePixel, depthValue, depthIntrinsic,
                                                    extrinsicD2C, ref tmpTargetPixel);
                    if (!result)
                    {
                        continue;
                    }
                    PrintRuslt("depth 2d to 3D: pixel coordinates and depth transform to point in 3D space", sourcePixel, tmpTargetPixel, depthValue);

                    // Perform the 3D to 2D transformation
                    result = CoordinateTransformHelper.Transformation3dto2d(tmpTargetPixel, depthIntrinsic, depthDistortion, extrinsicC2D, ref targetPixel);
                    if (!result)
                    {
                        continue;
                    }
                    PrintRuslt("3d to depth 2d : point in 3D space transform to the corresponding pixel coordinates in an image", tmpTargetPixel, targetPixel);
                }
            }
        }


        private void PrintRuslt(string msg, Point2f sourcePixel, Point2f targetPixel)
        {
            Console.WriteLine($"{msg}: ({sourcePixel.x}, {sourcePixel.y}) -> ({targetPixel.x}, {targetPixel.y})");
        }

        private void PrintRuslt(string msg, Point2f sourcePixel, Point3f targetPixel, float depthValue)
        {
            Console.WriteLine($"{msg}: depth {depthValue} ({sourcePixel.x}, {sourcePixel.y}) -> ({targetPixel.x}, {targetPixel.y}, {targetPixel.z})");
        }

        private void PrintRuslt(string msg, Point3f sourcePixel, Point2f targetPixel)
        {
            Console.WriteLine($"{msg}: ({sourcePixel.x}, {sourcePixel.y}, {sourcePixel.z}) -> ({targetPixel.x}, {targetPixel.y})");
        }

        private void PrintRuslt(string msg, Point3f sourcePixel, Point3f targetPixel)
        {
            Console.WriteLine($"{msg}: ({sourcePixel.x}, {sourcePixel.y}, {sourcePixel.z}) -> ({targetPixel.x}, {targetPixel.y}, {targetPixel.z})");
        }

        private string InputWatcher()
        {
            while (true)
            {
                Console.Write("\nInput command:  ");
                string cmd = Console.ReadLine();
                if (cmd == "quit" || cmd == "q")
                {
                    Environment.Exit(0);
                }
                return cmd;
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine("\nAvailable commands:");
            Console.WriteLine("1: Transformation 2D to 2D");
            Console.WriteLine("2: Transformation 2D to 3D");
            Console.WriteLine("3: Transformation 3D to 3D");
            Console.WriteLine("4: Transformation 3D to 2D");
            Console.WriteLine("q or quit: Exit");
        }
    }
}