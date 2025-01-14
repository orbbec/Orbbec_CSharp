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
    public partial class QuickStartWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Task processingTask;

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
                else if(frame.GetFrameType() == FrameType.OB_FRAME_DEPTH)
                {
                    data = ImageConverter.ConvertDepthToRGBData(data);
                }
                var rect = new Int32Rect(0, 0, width, height);
                wbmp.WritePixels(rect, data, stride, 0);
            });
        }

        public QuickStartWindow()
        {
            InitializeComponent();

            Action<VideoFrame> updateColor;
            Action<VideoFrame> updateDepth;

            try
            {
                Pipeline pipeline = new Pipeline();
                StreamProfile colorProfile = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_COLOR).GetVideoStreamProfile(0, 0, Format.OB_FORMAT_ANY, 0);
                StreamProfile depthProfile = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH).GetVideoStreamProfile(0, 0, Format.OB_FORMAT_ANY, 0);
                Config config = new Config();
                config.EnableStream(colorProfile);
                config.EnableStream(depthProfile);

                pipeline.Start(config);

                SetupWindow(colorProfile, depthProfile, out updateColor, out updateDepth);

                processingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            var colorFrame = frames?.GetColorFrame(); 
                            var depthFrame = frames?.GetDepthFrame();

                            if (colorFrame != null)
                            {
                                Dispatcher.InvokeAsync(() => updateColor(colorFrame), DispatcherPriority.Render);
                            }
                            if (depthFrame != null)
                            {
                                Dispatcher.InvokeAsync(() => updateDepth(depthFrame), DispatcherPriority.Render);
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

        private void SetupWindow(StreamProfile colorProfile, StreamProfile depthProfile,
                                    out Action<VideoFrame> color, out Action<VideoFrame> depth)
        {
            using (var p = colorProfile.As<VideoStreamProfile>())
            {
                imgColor.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                color = UpdateImage(imgColor);
            }

            using (var p = depthProfile.As<VideoStreamProfile>())
            {
                imgDepth.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                depth = UpdateImage(imgDepth);
            }
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