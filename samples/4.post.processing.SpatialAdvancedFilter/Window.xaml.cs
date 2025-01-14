using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using Common;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class SpatialAdvancedFilterWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Task postProcessingTask;
        private Dictionary<string, Action<VideoFrame>> imageUpdateActions = new Dictionary<string, Action<VideoFrame>>();

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
                data = ImageConverter.ConvertDepthToRGBData(data);
                var rect = new Int32Rect(0, 0, width, height);
                wbmp.WritePixels(rect, data, stride, 0);
            });
        }

        public SpatialAdvancedFilterWindow()
        {
            InitializeComponent();

            try
            {
                Pipeline pipeline = new Pipeline();

                Config config = new Config();

                config.EnableStream(StreamType.OB_STREAM_DEPTH);

                pipeline.Start(config);

                Device device = pipeline.GetDevice();
                Sensor sensor = device.GetSensor(SensorType.OB_SENSOR_DEPTH);
                List<Filter> filterList = sensor.CreateRecommendedFilters();
                SpatialAdvancedFilter filter = null;
                foreach (var f in filterList)
                {
                    if (f.Name().Equals("SpatialAdvancedFilter"))
                    {
                        filter = f.As<SpatialAdvancedFilter>();
                    }
                }
                if (filter == null)
                {
                    pipeline.Stop();
                    MessageBox.Show("The current device does not support SpatialAdvancedFilter!", "´íÎó", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                    return;
                }
                SpatialAdvancedFilterParams filterParams = new SpatialAdvancedFilterParams
                {
                    magnitude = 1,
                    alpha = 0.5f,
                    disp_diff = 160,
                    radius = 1
                };
                filter.SetFilterParams(filterParams);

                postProcessingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            var depthFrame = frames?.GetDepthFrame();
                            if (depthFrame == null) continue;

                            var processedFrame = depthFrame;
                            if (filter != null)
                            {
                                processedFrame = filter.Process(processedFrame).As<DepthFrame>();
                            }
                            UpdateFrame("depth", imgDepth, depthFrame);
                            UpdateFrame("depthPP", imgDepthPP, processedFrame);
                        }
                    }
                }, tokenSource.Token).ContinueWith(t =>
                {
                    if (filter != null)
                        filter.Dispose();
                    pipeline.Stop();
                });
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
                    imageUpdateActions[type] = UpdateImage(image);
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
            if (postProcessingTask != null)
            {
                await postProcessingTask;
            }
        }
    }
}