using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class SpatialAdvancedFilterWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

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
                data = ConvertDepthToRGBData(data);
                var rect = new Int32Rect(0, 0, width, height);
                wbmp.WritePixels(rect, data, stride, 0);
            });
        }

        private static byte[] ConvertDepthToRGBData(byte[] depthData)
        {
            byte[] colorData = new byte[depthData.Length / 2 * 3];
            for (int i = 0; i < depthData.Length; i += 2)
            {
                ushort depthValue = (ushort)((depthData[i + 1] << 8) | depthData[i]);
                float depth = (float)depthValue / 1000;
                byte depthByte = (byte)(depth * 255);
                int index = i / 2 * 3;
                colorData[index] = depthByte; // Red
                colorData[index + 1] = depthByte; // Green
                colorData[index + 2] = depthByte; // Blue
            }
            return colorData;
        }

        public SpatialAdvancedFilterWindow()
        {
            InitializeComponent();

            Action<VideoFrame> updateDepth = null;
            Action<VideoFrame> updateDepthPP = null;

            try
            {
                Pipeline pipeline = new Pipeline();

                Device device = pipeline.GetDevice();

                Config config = new Config();

                config.EnableStream(StreamType.OB_STREAM_DEPTH);

                pipeline.Start(config);

                SpatialAdvancedFilter filter = new SpatialAdvancedFilter();

                Task.Factory.StartNew(() =>
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
                                SpatialAdvancedFilterParams filterParams = new SpatialAdvancedFilterParams
                                {
                                    magnitude = 1,
                                    alpha = 0.5f,
                                    disp_diff = 160,
                                    radius = 1
                                };
                                filter.SetFilterParams(filterParams);
                                processedFrame = filter.Process(processedFrame).As<DepthFrame>();
                            }
                            updateDepth = UpdateFrame(imgDepth, updateDepth, depthFrame);
                            updateDepthPP = UpdateFrame(imgDepthPP, updateDepthPP, processedFrame);
                        }
                    }
                }, tokenSource.Token).ContinueWith(t =>
                {
                    pipeline.Stop();
                    pipeline.Dispose();
                    filter.Dispose();
                    if (filter != null)
                        filter.Dispose();
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Application.Current.Shutdown();
            }
        }

        private Action<VideoFrame> UpdateFrame(Image image, Action<VideoFrame> updateAction, VideoFrame frame)
        {
            Dispatcher.Invoke(() =>
            {
                if (!(image.Source is WriteableBitmap) && updateAction == null)
                {
                    image.Visibility = Visibility.Visible;
                    image.Source = new WriteableBitmap((int)frame.GetWidth(), (int)frame.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                    updateAction = UpdateImage(image);
                }
                updateAction?.Invoke(frame);
            }, DispatcherPriority.Render);
            return updateAction;
        }

        private void Control_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}