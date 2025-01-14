using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using Common;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class MultiDevicesWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Dictionary<string, TextBlock> deviceInfoTexts;
        private Dictionary<string, Image> deviceColorImages;
        private Dictionary<string, Image> deviceDepthImages;
        private Dictionary<string, Action<VideoFrame>> updateColors;
        private Dictionary<string, Action<VideoFrame>> updateDepths;
        Dictionary<string, Pipeline> pipes;
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
                if (frame.GetFrameType() == FrameType.OB_FRAME_COLOR &&
                    frame.GetFormat() == Format.OB_FORMAT_MJPG)
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

        public MultiDevicesWindow()
        {
            InitializeComponent();

            deviceInfoTexts = new Dictionary<string, TextBlock>();
            deviceColorImages = new Dictionary<string, Image>();
            deviceDepthImages = new Dictionary<string, Image>();
            updateColors = new Dictionary<string, Action<VideoFrame>>();
            updateDepths = new Dictionary<string, Action<VideoFrame>>();

            try
            {
                //Context.SetLoggerToFile(LogSeverity.OB_LOG_SEVERITY_DEBUG, "C:\\Log\\OrbbecSDK");
                Context context = new Context();

                DeviceList deviceList = context.QueryDeviceList();

                uint devCount = deviceList.DeviceCount();

                pipes = new Dictionary<string, Pipeline>();

                context.SetDeviceChangedCallback((removedList, addedList) =>
                {
                    for (uint i = 0; i < addedList.DeviceCount(); i++)
                    {
                        try
                        {
                            Device device = addedList.GetDevice(i);
                            string deviceSN = addedList.SerialNumber(i);

                            Pipeline pipe = new Pipeline(device);
                            lock (pipes)
                            {
                                pipes.Add(deviceSN, pipe);
                            }

                            Dispatcher.Invoke(() =>
                            {
                                AddUI(device, deviceSN, pipes.Count);
                            });

                            updateColors.Add(deviceSN, null);
                            updateDepths.Add(deviceSN, null);

                            StartStream(deviceSN, pipe);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error adding device: {ex.Message}");
                        }
                    }

                    for (uint i = 0; i < removedList.DeviceCount(); i++)
                    {
                        try
                        {
                            string deviceSN = removedList.SerialNumber(i);

                            lock (pipes)
                            {
                                if (pipes.ContainsKey(deviceSN))
                                {
                                    pipes.Remove(deviceSN);
                                }
                            }

                            Dispatcher.Invoke(() =>
                            {
                                RemoveUI(pipes, deviceSN);
                            });

                            updateColors.Remove(deviceSN);
                            updateDepths.Remove(deviceSN);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error removing device: {ex.Message}");
                        }
                    }
                });

                UpdateRow((int)devCount);

                for (uint i = 0; i < devCount; i++)
                {
                    Device device = deviceList.GetDevice(i);
                    string deviceSN = deviceList.SerialNumber(i);

                    Pipeline pipe = new Pipeline(device);
                    pipes.Add(deviceSN, pipe);

                    AddUI(device, deviceSN, pipes.Count);

                    updateColors.Add(deviceSN, null);
                    updateDepths.Add(deviceSN, null);

                    StartStream(deviceSN, pipe);
                }

                processingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        lock (pipes)
                        {
                            foreach (KeyValuePair<string, Pipeline> item in pipes)
                            {
                                string deviceSN = item.Key;
                                Pipeline pipe = item.Value;
                                using (Frameset frames = pipe.WaitForFrames(100))
                                {
                                    var colorFrame = frames?.GetColorFrame();
                                    var depthFrame = frames?.GetDepthFrame();

                                    if (colorFrame != null && deviceColorImages.ContainsKey(deviceSN))
                                    {
                                        var colorImageControl = deviceColorImages[deviceSN];
                                        UpdateFrame(deviceSN, updateColors, colorImageControl, colorFrame);
                                    }

                                    if (depthFrame != null && deviceDepthImages.ContainsKey(deviceSN))
                                    {
                                        var depthImageControl = deviceDepthImages[deviceSN];
                                        UpdateFrame(deviceSN, updateDepths, depthImageControl, depthFrame);
                                    }
                                }
                            }
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

        void StartStream(string deviceSN, Pipeline pipe)
        {
            try
            {
                Config config = new Config();
                config.EnableVideoStream(StreamType.OB_STREAM_COLOR);
                config.EnableVideoStream(StreamType.OB_STREAM_DEPTH);

                pipe.Start(config);
                
                Console.WriteLine($"Stream started for device {deviceSN}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting stream for device {deviceSN}: {ex.Message}");
            }
        }

        private void UpdateRow(int count)
        {
            MainGrid.RowDefinitions.Clear();
            for (int i = 0; i < count; i++)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition());
            }
        }

        private void AddUI(Device device, string deviceSN, int count)
        {
            UpdateRow(count);

            DeviceInfo info = device.GetDeviceInfo();
            TextBlock deviceInfoText = new TextBlock
            {
                Text = $"Device Info:\nName: {info.Name()}\n" +
                $"FirmwareVersion: {info.FirmwareVersion()}\n" +
                $"SN: {info.SerialNumber()}",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14
            };

            Image colorImage = new Image
            {
                Name = $"imgColorDevice{deviceSN}",
            };

            Image depthImage = new Image
            {
                Name = $"imgDepthDevice{deviceSN}",
            };

            int row = count - 1;

            Grid.SetRow(deviceInfoText, row);
            Grid.SetColumn(deviceInfoText, 0);
            MainGrid.Children.Add(deviceInfoText);

            Grid.SetRow(colorImage, row);
            Grid.SetColumn(colorImage, 1);
            MainGrid.Children.Add(colorImage);

            Grid.SetRow(depthImage, row);
            Grid.SetColumn(depthImage, 2);
            MainGrid.Children.Add(depthImage);

            deviceInfoTexts.Add(deviceSN, deviceInfoText);
            deviceColorImages.Add(deviceSN, colorImage);
            deviceDepthImages.Add(deviceSN, depthImage);
        }

        private void RemoveUI(Dictionary<string, Pipeline> pipes, string deviceSN)
        {
            if (deviceInfoTexts.ContainsKey(deviceSN))
            {
                MainGrid.Children.Remove(deviceInfoTexts[deviceSN]);
                deviceInfoTexts.Remove(deviceSN);
            }

            if (deviceColorImages.ContainsKey(deviceSN))
            {
                MainGrid.Children.Remove(deviceColorImages[deviceSN]);
                deviceColorImages.Remove(deviceSN);
            }

            if (deviceDepthImages.ContainsKey(deviceSN))
            {
                MainGrid.Children.Remove(deviceDepthImages[deviceSN]);
                deviceDepthImages.Remove(deviceSN);
            }
            UpdateRow(pipes.Count);

            int currentRow = 0;
            foreach (var deviceSNKey in pipes.Keys)
            {
                TextBlock deviceInfoText = deviceInfoTexts[deviceSNKey];
                Image colorImage = deviceColorImages[deviceSNKey];
                Image depthImage = deviceDepthImages[deviceSNKey];

                Grid.SetRow(deviceInfoText, currentRow);
                Grid.SetRow(colorImage, currentRow);
                Grid.SetRow(depthImage, currentRow);
                currentRow++;
            }
        }

        private void UpdateFrame(string type, Dictionary<string, Action<VideoFrame>> imageUpdateActions, Image image, VideoFrame frame)
        {
            Dispatcher.InvokeAsync(() => 
            {
                if (!(image.Source is WriteableBitmap))
                {
                    image.Visibility = Visibility.Visible;
                    image.Source = new WriteableBitmap((int)frame.GetWidth(), (int)frame.GetHeight(), 96d, 96d, PixelFormats.Rgb24, null);

                    imageUpdateActions[type] = UpdateImage(image);
                    //Console.WriteLine($"Updated action for type '{type}': {imageUpdateActions[type]}");
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
            foreach (var pipe in pipes.Values)
            {
                pipe.Stop();
            }
            pipes.Clear();
            if (processingTask != null)
            {
                await processingTask;
            }
            if (deviceInfoTexts != null)
            {
                deviceInfoTexts.Clear();
            }
            if (deviceColorImages != null)
            {
                deviceColorImages.Clear();
            }
            if (deviceDepthImages != null)
            {
                deviceDepthImages.Clear();
            }
            if (updateColors != null)
            {
                updateColors.Clear();
            }
            if (updateDepths != null)
            {
                updateDepths.Clear();
            }
        }
    }
}