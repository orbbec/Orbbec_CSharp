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
    public partial class InfraredWindow : Window
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
                data = ImageConverter.ConvertIRToRGBData(data, format);
                var rect = new Int32Rect(0, 0, width, height);
                wbmp.WritePixels(rect, data, stride, 0);
            });
        }

        public InfraredWindow()
        {
            InitializeComponent();

            try
            {
                Pipeline pipeline = new Pipeline();
                Device device = pipeline.GetDevice();
                SensorList sensorList = device.GetSensorList();
                Config config = new Config();
                for (uint i = 0, N = sensorList.SensorCount(); i < N; i++)
                {
                    SensorType sensorType = sensorList.SensorType(i);
                    if (sensorType == SensorType.OB_SENSOR_IR ||
                        sensorType == SensorType.OB_SENSOR_IR_LEFT ||
                        sensorType == SensorType.OB_SENSOR_IR_RIGHT)
                    {
                        config.EnableVideoStream(sensorType, 0, 0, 30, Format.OB_FORMAT_ANY);
                    }
                }

                pipeline.Start(config);

                processingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            var irFrame = frames?.GetIRFrame();
                            var irLeftFrame = frames?.GetFrame(FrameType.OB_FRAME_IR_LEFT)?.As<VideoFrame>();
                            var irRightFrame = frames?.GetFrame(FrameType.OB_FRAME_IR_RIGHT)?.As<VideoFrame>();

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
                }, tokenSource.Token).ContinueWith(t => pipeline.Stop());
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
                if (!(image.Source is WriteableBitmap))
                {
                    image.Visibility = Visibility.Visible;
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