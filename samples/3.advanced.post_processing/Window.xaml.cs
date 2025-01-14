using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Common;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class PostProcessingWindow : Window
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

        public PostProcessingWindow()
        {
            InitializeComponent();

            try
            {
                Pipeline pipeline = new Pipeline();

                Device device = pipeline.GetDevice();
                Sensor sensor = device.GetSensor(SensorType.OB_SENSOR_DEPTH);
                List<Filter> filterList = sensor.CreateRecommendedFilters();

                Config config = new Config();
                config.EnableStream(StreamType.OB_STREAM_DEPTH);

                pipeline.Start(config);

                PrintFiltersInfo(filterList, true);

                Task.Run(() => FilterControl(filterList));

                postProcessingTask = Task.Factory.StartNew(() =>
                {
                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames(100))
                        {
                            var depthFrame = frames?.GetDepthFrame();
                            if (depthFrame == null) continue;

                            var processedFrame = depthFrame;
                            foreach (var filter in filterList)
                            {
                                if (filter.IsEnabled())
                                {
                                    processedFrame = filter.Process(processedFrame).As<DepthFrame>();
                                }
                            }

                            UpdateFrame("depth", imgDepth, depthFrame);
                            UpdateFrame("depthPP", imgDepthPP, processedFrame);
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

        private void PrintFiltersInfo(List<Filter> filterList, bool isInit)
        {
            Console.WriteLine($"{filterList.Count} post processing filters recommended:");
            foreach (var filter in filterList)
            {
                Console.WriteLine($" - {filter.Name()}: {(filter.IsEnabled() ? "enabled" : "disabled")}");
                var configSchemaList = filter.GetConfigSchemaList();

                foreach (var configSchema in configSchemaList)
                {
                    Console.WriteLine($"    - {{ {Marshal.PtrToStringAnsi(configSchema.name)}, {configSchema.type}, " +
                        $"{configSchema.min}, {configSchema.max}, {configSchema.step}, {configSchema.def}, {Marshal.PtrToStringAnsi(configSchema.desc)} }}");
                }

                if (isInit)
                {
                    filter.Enable(false); // Disable the filter
                    AddCheckBox(filter);
                }
            }
        }

        private void AddCheckBox(Filter filter)
        {
            var checkBox = new CheckBox
            {
                Content = filter.Name(),
                Tag = filter,
                Margin = new Thickness(8),
                FontSize = 14
            };

            checkBox.Click += CheckBox_Click;
            checkBox.IsChecked = filter.IsEnabled();

            FilterPanel.Children.Add(checkBox);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var filter = checkBox.Tag as Filter;
                bool isChecked = checkBox.IsChecked ?? false;
                filter.Enable(isChecked);
            }
        }

        private void FilterControl(List<Filter> filterList)
        {
            Action PrintHelp = () =>
            {
                Console.WriteLine("Available commands:");
                Console.WriteLine("- Enter `[Filter]` to list the config values for the filter");
                Console.WriteLine("- Enter `[Filter] list` to list the config schema for the filter");
                Console.WriteLine("- Enter `[Filter] [Config]` to show the config values for the filter");
                Console.WriteLine("- Enter `[Filter] [Config] [Value]` to set a config value");
                Console.WriteLine("- Enter `L` or `l` to list all available filters");
                Console.WriteLine("- Enter `H` or `h` to print this help message");
                Console.WriteLine("- Enter `Q` or `q` to quit");
            };

            PrintHelp();

            while (!tokenSource.IsCancellationRequested)
            {
                Console.WriteLine("---------------------------");
                Console.Write("Enter your input (h for help): ");

                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    tokenSource.Cancel();
                    postProcessingTask.Wait();
                    Console.WriteLine("Exiting the post-processing ...");
                    Environment.Exit(0);
                }
                else if (input.Equals("l", StringComparison.OrdinalIgnoreCase))
                {
                    PrintFiltersInfo(filterList, false);
                    continue;
                }
                else if (input.Equals("h", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    continue;
                }

                string[] tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;

                bool foundFilter = false;
                foreach (var filter  in filterList)
                {
                    if (filter.Name() != tokens[0]) continue;

                    foundFilter = true;
                    if (tokens.Length == 1)
                    {
                        Console.WriteLine($"Config values for {filter.Name()}:");
                        foreach (var configSchema in filter.GetConfigSchemaList())
                        {
                            string name = Marshal.PtrToStringAnsi(configSchema.name);
                            Console.WriteLine($" - {name}: {filter.GetConfigValue(name)}");
                        }
                    }
                    else if (tokens.Length == 2)
                    {
                        if (tokens[1].Equals("list", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"Config schema for {filter.Name()}:");
                            foreach (var configSchema in filter.GetConfigSchemaList())
                            {
                                Console.WriteLine($" - {{ {Marshal.PtrToStringAnsi(configSchema.name)}, {configSchema.type}, " +
                                    $"{configSchema.min}, {configSchema.max}, {configSchema.step}, {configSchema.def}, {Marshal.PtrToStringAnsi(configSchema.desc)} }}");
                            }
                        }
                        else
                        {
                            var configSchemaList = filter.GetConfigSchemaList();
                            bool foundConfig = false;
                            foreach ( var configSchema in configSchemaList)
                            {
                                string name = Marshal.PtrToStringAnsi(configSchema.name);
                                if (name.Equals(tokens[1], StringComparison.OrdinalIgnoreCase))
                                {
                                    foundConfig = true;
                                    Console.WriteLine($"Config values for {filter.Name()}@{name}: {filter.GetConfigValue(name)}");
                                    break;
                                }
                            }
                            if (!foundConfig)
                            {
                                Console.WriteLine($"Error: Config {tokens[1]} not found for filter {filter.Name()}");
                            }
                        }
                    }
                    else if (tokens.Length == 3)
                    {
                        try
                        {
                            double value = double.Parse(tokens[2]);
                            filter.SetConfigValue(tokens[1], value);
                            Console.WriteLine($"Success: Config value of {tokens[1]} for filter {filter.Name()} is set to {value}.");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error: {e.Message}");
                        }
                    }
                    break;
                }

                if (!foundFilter)
                {
                    Console.WriteLine($"Error: Filter {tokens[0]} not found.");
                }
            }
        }

        private void UpdateFrame(string type, Image image, VideoFrame frame)
        {
            Dispatcher.InvokeAsync(() => 
            {
                if (!(image.Source is WriteableBitmap writeableBitmap) ||
                    writeableBitmap.PixelWidth != (int)frame.GetWidth() || writeableBitmap.PixelHeight != (int)frame.GetHeight())
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