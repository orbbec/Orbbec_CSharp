using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Orbbec
{
    public partial class ImuWindow : Window
    {
        private Sensor accelSensor;
        private Sensor gyroSensor;
        private DispatcherTimer timer = new DispatcherTimer();

        // Accel and Gyro data
        private AccelValue accelValue;
        private GyroValue gyroValue;
        private ulong accelTimestamp;
        private ulong gyroTimestamp;
        private double accelTemperature;
        private double gyroTemperature;

        public ImuWindow()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += Timer_Tick;
            timer.Start();

            // Start the pipeline in an asynchronous method
            StartPipelineAsync();
        }

        private async void StartPipelineAsync()
        {
            try
            {
                using (Pipeline pipeline = new Pipeline())
                {
                    using (Config config = new Config())
                    {
                        config.EnableAccelStream();
                        config.EnableGyroStream();
                        config.SetFrameAggregateOutputMode(FrameAggregateOutputMode.OB_FRAME_AGGREGATE_OUTPUT_ALL_TYPE_FRAME_REQUIRE);
                        pipeline.Start(config);

                        while (true) // Continuous reading
                        {
                            var frames = await Task.Run(() => pipeline.WaitForFrames(100));
                            if (frames != null)
                            {
                                // Process specific frame types
                                ProcessFrame(frames.GetFrame(FrameType.OB_FRAME_ACCEL));
                                ProcessFrame(frames.GetFrame(FrameType.OB_FRAME_GYRO));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Show error on the UI thread
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(e.Message);
                    Stop();
                    Application.Current.Shutdown();
                });
            }
        }

        private void ProcessFrame(Frame frame)
        {
            if (frame == null) return;

            if (frame.GetFrameType() == FrameType.OB_FRAME_ACCEL)
            {
                var accelFrame = frame.As<AccelFrame>();
                ProcessAccelFrame(accelFrame);
            }
            else if (frame.GetFrameType() == FrameType.OB_FRAME_GYRO)
            {
                var gyroFrame = frame.As<GyroFrame>();
                ProcessGyroFrame(gyroFrame);
            }
            frame.Dispose(); // Dispose the frame after processing
        }

        private void ProcessAccelFrame(AccelFrame accelFrame)
        {
            if (accelFrame != null)
            {
                accelValue = accelFrame.GetAccelValue();
                accelTimestamp = accelFrame.GetTimeStampUs();
                accelTemperature = accelFrame.GetTemperature();

                UpdateAccelUI();
            }
        }

        private void ProcessGyroFrame(GyroFrame gyroFrame)
        {
            if (gyroFrame != null)
            {
                gyroValue = gyroFrame.GetGyroValue();
                gyroTimestamp = gyroFrame.GetTimeStampUs();
                gyroTemperature = gyroFrame.GetTemperature();

                UpdateGyroUI();
            }
        }

        private void UpdateAccelUI()
        {
            Dispatcher.Invoke(() =>
            {
                tbAccel.Text = string.Format("Accel tsp:{0}\nAccelTemperature:{1}\nAccel.x:{2}\nAccel.y:{3}\nAccel.z:{4}",
                    accelTimestamp, accelTemperature.ToString("F2"),
                    accelValue.x, accelValue.y, accelValue.z);
            });
        }

        private void UpdateGyroUI()
        {
            Dispatcher.Invoke(() =>
            {
                tbGyro.Text = string.Format("Gyro tsp:{0}\nGyroTemperature:{1}\nGyro.x:{2}\nGyro.y:{3}\nGyro.z:{4}",
                    gyroTimestamp, gyroTemperature.ToString("F2"),
                    gyroValue.x, gyroValue.y, gyroValue.z);
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // This can be left empty since UI updates are handled in OnFrame.
        }

        private void Stop()
        {
            timer.Stop();
            timer.Tick -= Timer_Tick;

            // Dispose sensors if they were created
            accelSensor?.Stop();
            accelSensor?.Dispose();
            gyroSensor?.Stop();
            gyroSensor?.Dispose();
        }

        private void Control_Closing(object sender, CancelEventArgs e)
        {
            Stop();
        }
    }
}
