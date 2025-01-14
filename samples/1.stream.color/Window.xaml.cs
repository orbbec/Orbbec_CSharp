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
    public partial class ColorWindow : Window
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
                var rect = new Int32Rect(0, 0, width, height);
                wbmp.WritePixels(rect, data, stride, 0);
            });
        }

        public ColorWindow()
        {
            InitializeComponent();

            Action<VideoFrame> updateColor;

            try
            {
                Pipeline pipeline = new Pipeline();
                StreamProfile colorProfile = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_COLOR).GetVideoStreamProfile(0, 0, Format.OB_FORMAT_ANY, 0);
                Config config = new Config();
                config.EnableStream(colorProfile);

                pipeline.Start(config);

                SetupWindow(colorProfile, out updateColor);

                processingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            var colorFrame = frames?.GetColorFrame();

                            if (colorFrame != null)
                            {
                                Dispatcher.InvokeAsync(() => updateColor(colorFrame), DispatcherPriority.Render);
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

        private void SetupWindow(StreamProfile colorProfile, out Action<VideoFrame> color)
        {
            using (var p = colorProfile.As<VideoStreamProfile>())
            {
                imgColor.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                color = UpdateImage(imgColor);
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