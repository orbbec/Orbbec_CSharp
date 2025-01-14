using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using Common;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class MultiStreamWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Task processingTask;
        private Dictionary<string, Action<VideoFrame>> imageUpdateActions = new Dictionary<string, Action<VideoFrame>>();

        private static Action<VideoFrame> UpdateImage(Image img, Format format)
        {
            var wbmp = img.Source as WriteableBitmap;
            return new Action<VideoFrame>(frame =>
            {
                int width = (int)frame.GetWidth();
                int height = (int)frame.GetHeight();
                int stride = wbmp.BackBufferStride;
                byte[] data = new byte[frame.GetDataSize()];
                frame.CopyData(ref data);
                if (frame.GetFrameType() == FrameType.OB_FRAME_COLOR && 
                    frame.GetFormat() == Format.OB_FORMAT_MJPG)
                {
                    data = ImageConverter.ConvertMJPGToRGBData(data);
                }
                else if (frame.GetFrameType() == FrameType.OB_FRAME_DEPTH)
                {
                    data = ImageConverter.ConvertDepthToRGBData(data);
                }
                else if (frame.GetFrameType() == FrameType.OB_FRAME_IR ||
                    frame.GetFrameType() == FrameType.OB_FRAME_IR_LEFT ||
                    frame.GetFrameType() == FrameType.OB_FRAME_IR_RIGHT)
                {
                    data = ImageConverter.ConvertIRToRGBData(data, format);
                }
                var rect = new Int32Rect(0, 0, width, height);
                wbmp.WritePixels(rect, data, stride, 0);
            });
        }

        public MultiStreamWindow()
        {
            InitializeComponent();

            try
            {
                //Context.SetLoggerToFile(LogSeverity.OB_LOG_SEVERITY_DEBUG, "C:\\Log\\OrbbecSDK");
                // Create a pipeline with default device.
                Pipeline pipeline = new Pipeline();
                Config config = new Config();

                Device device = pipeline.GetDevice();
                SensorList sensorList = device.GetSensorList();
                for (uint i = 0; i < sensorList.SensorCount(); i++)
                {
                    SensorType sensorType = sensorList.SensorType(i);

                    if (sensorType == SensorType.OB_SENSOR_ACCEL || sensorType == SensorType.OB_SENSOR_GYRO)
                    {
                        continue;
                    }
                    config.EnableStream(sensorType);
                }
                pipeline.Start(config);

                Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            if (frames == null)
                            {
                                continue;
                            }

                            var colorFrame = frames.GetFrame(FrameType.OB_FRAME_COLOR)?.As<VideoFrame>();
                            var depthFrame = frames.GetFrame(FrameType.OB_FRAME_DEPTH)?.As<VideoFrame>();
                            var irFrame = frames.GetFrame(FrameType.OB_FRAME_IR)?.As<VideoFrame>();
                            var irLeftFrame = frames.GetFrame(FrameType.OB_FRAME_IR_LEFT)?.As<VideoFrame>();
                            var irRightFrame = frames.GetFrame(FrameType.OB_FRAME_IR_RIGHT)?.As<VideoFrame>();

                            if (colorFrame != null)
                            {
                                UpdateFrame("color", imgColor, colorFrame);
                            }
                            if (depthFrame != null)
                            {
                                UpdateFrame("depth", imgDepth, depthFrame);
                            }
                            if (irFrame != null)
                            {
                                UpdateFrame("ir", imgIr, irFrame);
                            }
                            if (irLeftFrame != null)
                            {
                                UpdateFrame("irLeft", imgIrLeft, irLeftFrame);
                            }
                            if (irRightFrame != null)
                            {
                                UpdateFrame("irRight", imgIrRight, irRightFrame);
                            }
                        }
                    }
                }, tokenSource.Token);
                
                Pipeline imuPipeline = new Pipeline(device);

                Config imuConfig = new Config();
                imuConfig.EnableAccelStream();
                imuConfig.EnableGyroStream();
                imuPipeline.Start(imuConfig);

                Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var renderImuFrameSet = imuPipeline.WaitForFrames(100))
                        {
                            if (renderImuFrameSet == null)
                            {
                                continue;
                            }

                            var accelFrame = renderImuFrameSet.GetFrame(FrameType.OB_FRAME_ACCEL)?.As<AccelFrame>();
                            var gyroFrame = renderImuFrameSet.GetFrame(FrameType.OB_FRAME_GYRO)?.As<GyroFrame>();

                            if (accelFrame != null)
                            {
                                var accelValue = accelFrame.GetAccelValue();
                                var accelTimestamp = accelFrame.GetTimeStampUs();
                                var accelTemperature = accelFrame.GetTemperature();
                                Dispatcher.InvokeAsync(() =>
                                {
                                    tbAccel.Text = string.Format("Accel tsp:{0}\nAccelTemperature:{1}\nAccel.x:{2}\nAccel.y:{3}\nAccel.z:{4}",
                                        accelTimestamp, accelTemperature.ToString("F2"),
                                        accelValue.x, accelValue.y, accelValue.z);
                                });
                            }

                            if (gyroFrame != null)
                            {
                                var gyroValue = gyroFrame.GetGyroValue();
                                var gyroTimestamp = gyroFrame.GetTimeStampUs();
                                var gyroTemperature = gyroFrame.GetTemperature();
                                Dispatcher.InvokeAsync(() =>
                                {
                                    tbGyro.Text = string.Format("Gyro tsp:{0}\nGyroTemperature:{1}\nGyro.x:{2}\nGyro.y:{3}\nGyro.z:{4}",
                                        gyroTimestamp, gyroTemperature.ToString("F2"),
                                        gyroValue.x, gyroValue.y, gyroValue.z);
                                });
                            }
                        }
                    }
                }, tokenSource.Token).ContinueWith(t => imuPipeline.Stop());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Application.Current.Shutdown();
            }
        }

        private void UpdateFrame(string type, Image image, VideoFrame frame)
        {
            Dispatcher.InvokeAsync(() => 
            {
                if (!(image.Source is WriteableBitmap writeableBitmap))
                {
                    if (frame.GetFrameType() == FrameType.OB_FRAME_IR)
                    {
                        irGrid.Visibility = Visibility.Visible;
                    }
                    else if (frame.GetFrameType() == FrameType.OB_FRAME_IR_LEFT)
                    {
                        irLeftGrid.Visibility = Visibility.Visible;
                    }
                    else if (frame.GetFrameType() == FrameType.OB_FRAME_IR_RIGHT)
                    {
                        irRightGrid.Visibility = Visibility.Visible;
                    }
                    image.Source = new WriteableBitmap((int)frame.GetWidth(), (int)frame.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);

                    imageUpdateActions[type] = UpdateImage(image, frame.GetFormat());
                }
                if (imageUpdateActions.TryGetValue(type, out var action))
                {
                    action?.Invoke(frame);
                }
            }, DispatcherPriority.Render);
        }

        private async void Control_Closing(object sender, CancelEventArgs e)
        {
            tokenSource.Cancel();
            if (processingTask != null)
            {
                await processingTask;
            }
        }
    }
}