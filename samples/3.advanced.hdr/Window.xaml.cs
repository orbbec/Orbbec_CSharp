using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Common;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class HDRWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Task processingTask;
        private Device device;
        private HdrConfig hdrConfig;

        public delegate bool ControlCtrlDelegate(int CtrlType);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);
        private ControlCtrlDelegate cancelHandler;
        private bool HandlerRoutine(int CtrlType)
        {
            switch (CtrlType)
            {
                case 0: // Ctrl+C close
                    CleanupDevice();
                    Environment.Exit(0);
                    break;
                case 2: // Press the console close button to close
                    CleanupDevice();
                    break;
            }
            return false; // Return false to indicate continued message delivery
        }

        private static Action<VideoFrame> UpdateImage(Image img, Format format)
        {
            var wbmp = img.Source as WriteableBitmap;
            return new Action<VideoFrame>(frame =>
            {
                int width = (int)frame.GetWidth();
                int height = (int)frame.GetHeight();
                int stride = wbmp.BackBufferStride;
                int dataSize = (int)frame.GetDataSize();
                byte[] data = new byte[frame.GetDataSize()];
                frame.CopyData(ref data);
                if (frame.GetFrameType() == FrameType.OB_FRAME_DEPTH)
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

        public HDRWindow()
        {
            InitializeComponent();
            
            cancelHandler = new ControlCtrlDelegate(HandlerRoutine);
            SetConsoleCtrlHandler(cancelHandler, true);

            Action<VideoFrame> updateDepth1;
            Action<VideoFrame> updateIrLeft1;
            Action<VideoFrame> updateIrRight1;
            Action<VideoFrame> updateDepth2;
            Action<VideoFrame> updateIrLeft2;
            Action<VideoFrame> updateIrRight2;
            Action<VideoFrame> updateHDR;

            try
            {
                Pipeline pipeline = new Pipeline();

                device = pipeline.GetDevice();

                if (!device.IsPropertySupported(PropertyId.OB_STRUCT_DEPTH_HDR_CONFIG, PermissionType.OB_PERMISSION_READ_WRITE))
                {
                    pipeline.Stop();
                    MessageBox.Show("Current default device does not support HDR merge!", "´íÎó", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                    return;
                }

                StreamProfile depthProfile = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH).GetVideoStreamProfile(0, 0, Format.OB_FORMAT_Y16, 0);
                StreamProfile irLeftProfile = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_IR_LEFT).GetVideoStreamProfile(0, 0, Format.OB_FORMAT_Y8, 0);
                StreamProfile irRightProfile = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_IR_RIGHT).GetVideoStreamProfile(0, 0, Format.OB_FORMAT_Y8, 0);
                // Configure which streams to enable or disable for the Pipeline by creating a Config.
                Config config = new Config();
                config.EnableStream(depthProfile);
                config.EnableStream(irLeftProfile);
                config.EnableStream(irRightProfile);

                HdrMerge hdrMerge = new HdrMerge();
                hdrConfig = new HdrConfig
                {
                    enable = 1,
                    exposure_1 = 7500,
                    gain_1 = 24,
                    exposure_2 = 100,
                    gain_2 = 16
                };
                device.SetStructuredData(PropertyId.OB_STRUCT_DEPTH_HDR_CONFIG, hdrConfig);

                pipeline.Start(config);

                SetupWindow(depthProfile, irLeftProfile, irRightProfile,
                    out updateDepth1, out updateIrLeft1, out updateIrRight1,
                    out updateDepth2, out updateIrLeft2, out updateIrRight2,
                    out updateHDR);

                processingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            if (frames == null) continue;

                            var depthFrame = frames.GetFrame(FrameType.OB_FRAME_DEPTH)?.As<DepthFrame>();
                            var irLeftFrame = frames.GetFrame(FrameType.OB_FRAME_IR_LEFT)?.As<IRFrame>();
                            var irRightFrame = frames.GetFrame(FrameType.OB_FRAME_IR_RIGHT)?.As<IRFrame>();

                            int groupId = (int)depthFrame.GetMetadataValue(FrameMetadataType.OB_FRAME_METADATA_TYPE_HDR_SEQUENCE_INDEX);
                            if (groupId == 0)
                            {
                                Dispatcher.InvokeAsync(() => updateDepth1(depthFrame), DispatcherPriority.Render);
                                Dispatcher.InvokeAsync(() => updateIrLeft1(irLeftFrame), DispatcherPriority.Render);
                                Dispatcher.InvokeAsync(() => updateIrRight1(irRightFrame), DispatcherPriority.Render);
                            }
                            else if (groupId == 1)
                            {
                                Dispatcher.InvokeAsync(() => updateDepth2(depthFrame), DispatcherPriority.Render);
                                Dispatcher.InvokeAsync(() => updateIrLeft2(irLeftFrame), DispatcherPriority.Render);
                                Dispatcher.InvokeAsync(() => updateIrRight2(irRightFrame), DispatcherPriority.Render);
                            }

                            try
                            {
                                var result = hdrMerge.Process(frames);
                                if (result == null) continue;

                                var resultFrameSet = result.As<Frameset>();
                                var resultDepthFrame = resultFrameSet.GetFrame(FrameType.OB_FRAME_DEPTH).As<DepthFrame>();

                                Dispatcher.InvokeAsync(() => updateHDR(resultDepthFrame), DispatcherPriority.Render);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("HDRMerge error: " + e.Message);
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

        private void SetupWindow(StreamProfile depthProfile, StreamProfile irLeftProfile, StreamProfile irRightProfile,
                                    out Action<VideoFrame> depth1, out Action<VideoFrame> irLeft1, out Action<VideoFrame> irRight1,
                                    out Action<VideoFrame> depth2, out Action<VideoFrame> irLeft2, out Action<VideoFrame> irRight2,
                                    out Action<VideoFrame> hdr)
        {
            using (var p = depthProfile.As<VideoStreamProfile>())
            {
                imgDepth_1.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                depth1 = UpdateImage(imgDepth_1, depthProfile.GetFormat());

                imgDepth_2.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                depth2 = UpdateImage(imgDepth_2, depthProfile.GetFormat());

                imgHDR.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                hdr = UpdateImage(imgHDR, depthProfile.GetFormat());
            }

            using (var p = irLeftProfile.As<VideoStreamProfile>())
            {
                imgIrLeft_1.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                irLeft1 = UpdateImage(imgIrLeft_1, irLeftProfile.GetFormat());

                imgIrLeft_2.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                irLeft2 = UpdateImage(imgIrLeft_2, irLeftProfile.GetFormat());
            }

            using (var p = irRightProfile.As<VideoStreamProfile>())
            {
                imgIrRight_1.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                irRight1 = UpdateImage(imgIrRight_1, irRightProfile.GetFormat());

                imgIrRight_2.Source = new WriteableBitmap((int)p.GetWidth(), (int)p.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);
                irRight2 = UpdateImage(imgIrRight_2, irRightProfile.GetFormat());
            }
        }

        private void CleanupDevice()
        {
            if (device != null)
            {
                if (device.IsPropertySupported(PropertyId.OB_STRUCT_DEPTH_HDR_CONFIG, PermissionType.OB_PERMISSION_READ_WRITE))
                {
                    hdrConfig.enable = 0;
                    device.SetStructuredData(PropertyId.OB_STRUCT_DEPTH_HDR_CONFIG, hdrConfig);
                }

                device.Dispose();
                device = null;
            }
        }

        private async void Control_Closing(object sender, CancelEventArgs e)
        {
            tokenSource.Cancel();
            CleanupDevice();
            if (processingTask != null)
            {
                await processingTask;
            }
        }
    }
}