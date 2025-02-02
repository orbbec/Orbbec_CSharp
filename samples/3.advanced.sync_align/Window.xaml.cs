using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Common;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class SyncAlignWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Task processingTask;
        private Pipeline pipeline;

        private static Action<VideoFrame> UpdateImage(Image img)
        {
            var wbmp = img.Source as WriteableBitmap;
            return new Action<VideoFrame>(frame =>
            {
                int width = (int)frame.GetWidth();
                int height = (int)frame.GetHeight();
                int stride = wbmp.BackBufferStride;
                byte[] data = new byte[frame.GetDataSize()];
                frame.CopyData(ref data);
                if (frame.GetFormat() == Format.OB_FORMAT_MJPG)
                {
                    data = ImageConverter.ConvertMJPGToRGBData(data);
                }
                else if (frame.GetFrameType() == FrameType.OB_FRAME_DEPTH)
                {
                    data = ImageConverter.ConvertDepthToRGBData(data);
                }
                var rect = new Int32Rect(0, 0, width, height);
                wbmp.WritePixels(rect, data, stride, 0);
            });
        }

        public SyncAlignWindow()
        {
            InitializeComponent();

            Action<VideoFrame> updateColor = null;
            Action<VideoFrame> updateDepth = null;

            try
            {
                //Context.SetLoggerToFile(LogSeverity.OB_LOG_SEVERITY_DEBUG, "C:\\Log\\OrbbecSDK");
                pipeline = new Pipeline();

                Config config = new Config();
                config.EnableVideoStream(SensorType.OB_SENSOR_DEPTH, 0, 0, 0, Format.OB_FORMAT_Y16);
                config.EnableVideoStream(SensorType.OB_SENSOR_COLOR, 0, 0, 0, Format.OB_FORMAT_RGB);
                config.SetFrameAggregateOutputMode(FrameAggregateOutputMode.OB_FRAME_AGGREGATE_OUTPUT_ALL_TYPE_FRAME_REQUIRE);

                pipeline.Start(config);

                // Create a filter to align depth frame to color frame
                AlignFilter depth2colorAlign = new AlignFilter(StreamType.OB_STREAM_COLOR);
                // Create a filter to align color frame to depth frame
                AlignFilter color2depthAlign = new AlignFilter(StreamType.OB_STREAM_DEPTH);

                syncCheckBox.IsChecked = true;
                d2cAlign.IsChecked = true;

                depth2colorAlign.SetCallback(frame =>
                {
                    RenderImage(frame, ref updateColor, ref updateDepth);
                });
                color2depthAlign.SetCallback(frame =>
                {
                    RenderImage(frame, ref updateColor, ref updateDepth);
                });

                bool isC2DAlignChecked = false;
                processingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            if (frames == null) continue;

                            Dispatcher.Invoke(() =>
                            {
                                isC2DAlignChecked = c2dAlign.IsChecked == true;
                            });

                            Filter alignFilter = isC2DAlignChecked ? color2depthAlign : depth2colorAlign;
                            alignFilter.PushFrame(frames);
                        }
                    }
                }, tokenSource.Token);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Application.Current.Shutdown();
            }
        }

        private void Sync_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                pipeline.EnableFrameSync();
            }
            else
            {
                pipeline.DisableFrameSync();
            }
        }

        private Action<VideoFrame> UpdateFrame(Image image, Action<VideoFrame> updateAction, VideoFrame frame)
        {
            Dispatcher.Invoke(() =>
            {
                if (!(image.Source is WriteableBitmap writeableBitmap) ||
                    writeableBitmap.PixelWidth != (int)frame.GetWidth() || writeableBitmap.PixelHeight != (int)frame.GetHeight())
                {
                    image.Visibility = Visibility.Visible;
                    image.Source = new WriteableBitmap((int)frame.GetWidth(), (int)frame.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                    updateAction = UpdateImage(image);
                }
                updateAction?.Invoke(frame);
            }, DispatcherPriority.Render);
            return updateAction;
        }

        private void RenderImage(Frame frame, ref Action<VideoFrame> updateColor, ref Action<VideoFrame> updateDepth)
        {
            using (var frames = frame.As<Frameset>())
            {
                var colorFrame = frames?.GetColorFrame();
                var depthFrame = frames?.GetDepthFrame();
                if (colorFrame != null)
                {
                    updateColor = UpdateFrame(imgColor, updateColor, colorFrame);
                }
                if (depthFrame != null)
                {
                    updateDepth = UpdateFrame(imgDepth, updateDepth, depthFrame);
                }
            }
        }

        private async void Control_Closing(object sender, CancelEventArgs e)
        {
            tokenSource.Cancel();
            if (pipeline != null)
            {
                pipeline.Stop();
            }
            if (processingTask != null)
            {
                await processingTask;
            }
        }
    }
}