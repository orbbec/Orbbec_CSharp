using System;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Text;
using System.Linq;
using System.ComponentModel;

namespace Orbbec
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class FirmwareUpdateWindow : Window
    {
        private Context context;
        private Dictionary<string, Device> devices;
        private Device curDevice;

        private string curFilePath;
        private bool isUpgradeSuccess = false;

        public FirmwareUpdateWindow()
        {
            InitializeComponent();

            devices = new Dictionary<string, Device>();

            try
            {
                context = new Context();
                context.SetDeviceChangedCallback((removedList, addedList) =>
                {
                    for (uint i = 0; i < removedList.DeviceCount(); i++)
                    {
                        string rmSN = removedList.SerialNumber(i);
                        if (devices.ContainsKey(rmSN))
                        {
                            devices.Remove(rmSN);
                        }
                    }

                    for (uint i = 0; i < addedList.DeviceCount(); i++)
                    {
                        int pid = addedList.Pid(i);
                        if (pid == 0x066B || pid == 0x0669)
                        {
                            MessageBox.Show(addedList.Name(i) + " does not support the current firmware upgrade method");
                            continue;
                        }
                        string serialNumber = addedList.SerialNumber(0);
                        if (!devices.ContainsKey(serialNumber))
                        {
                            devices.Add(serialNumber, addedList.GetDevice(i));
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        UpdateDeviceSelector();
                        if (devices.Count > 0)
                        {
                            curDevice = devices.Values.ElementAt(0);
                            UpdateDeviceInfo(curDevice.GetDeviceInfo());
                        }
                        else
                        {
                            UpdateDeviceInfo(null);
                        }
                    });
                });
                DeviceList deviceList = context.QueryDeviceList();
                if (deviceList.DeviceCount() > 0)
                {
                    for (uint i = 0; i < deviceList.DeviceCount(); i++)
                    {
                        int pid = deviceList.Pid(i);
                        if (pid == 0x066B || pid == 0x0669)
                        {
                            MessageBox.Show(deviceList.Name(i) + " does not support the current firmware upgrade method");
                            continue;
                        }
                        var device = deviceList.GetDevice(i);
                        string sn = deviceList.SerialNumber(i);
                        if (!devices.ContainsKey(sn))
                        {
                            devices.Add(sn, device);
                        }
                    }

                    deviceList.Dispose();
                    OnDeviceInit();
                }
                else
                {
                    deviceList.Dispose();
                }

                deviceSelector.SelectionChanged += (s, e) => OnDeviceSelect(deviceSelector.SelectedIndex);
                filePickerButton.Click += (s, e) => OnFilePickerSelect();
                filePathTextBox.TextChanged += (s, e) => OnFilePathChange();
                firmwareUpdateButton.Click += (s, e) => FirmwareUpdate();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Application.Current.Shutdown();
            }
        }

        private void OnDeviceInit()
        {
            if (devices.Count > 0)
            {
                curDevice = devices.Values.ElementAt(0);
                UpdateDeviceSelector();
                UpdateDeviceInfo(curDevice.GetDeviceInfo());
            }
        }

        private void UpdateDeviceSelector()
        {
            deviceSelector.Items.Clear();
            foreach (var device in devices.Values)
            {
                deviceSelector.Items.Add(device.GetDeviceInfo().Name());
            }
            if (devices.Count > 0)
            {
                deviceSelector.SelectedIndex = 0;
            }
            else
            {
                deviceSelector.SelectedIndex = -1;
            }
        }

        private void OnDeviceSelect(int index)
        {
            if (index >= 0 && index < devices.Count)
            {
                curDevice = devices.Values.ElementAt(index);
                UpdateDeviceInfo(curDevice.GetDeviceInfo());
            }
        }

        private void OnFilePickerSelect()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Binary files (*.bin)|*.bin";

            if (openFileDialog.ShowDialog() == true)
            {
                filePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void OnFilePathChange()
        {
            curFilePath = filePathTextBox.Text;
            Console.WriteLine(curFilePath);
        }

        private async void FirmwareUpdate()
        {
            if (!curFilePath.EndsWith(".bin"))
            {
                Console.WriteLine("invalid firmware file");
                return;
            }

            firmwareUpdateButton.IsEnabled = false;
            try
            {
                var upgradeTask = new TaskCompletionSource<bool>();

                curDevice.DeviceUpgrade(curFilePath, (state, percent, msg) =>
                {
                    Console.WriteLine($"state={state}, percent={percent}, msg={msg}");
                    if (state == UpgradeState.STAT_DONE)
                    {
                        isUpgradeSuccess = true;
                        upgradeTask.SetResult(true);
                    }
                    else if (state == UpgradeState.ERR_VERIFY || state == UpgradeState.ERR_PROGRAM || state == UpgradeState.ERR_ERASE ||
                            state == UpgradeState.ERR_FLASH_TYPE || state == UpgradeState.ERR_IMAGE_SIZE || state == UpgradeState.ERR_OTHER ||
                            state == UpgradeState.ERR_DDR || state == UpgradeState.ERR_TIMEOUT)
                    {
                        isUpgradeSuccess = false;
                        upgradeTask.SetResult(false);
                    }
                });

                await upgradeTask.Task;

                if (isUpgradeSuccess)
                {
                    firmwareUpdateButton.IsEnabled = true;
                    curDevice.Reboot();
                    UpdateDeviceInfo(curDevice.GetDeviceInfo());
                    Console.WriteLine("Upgrade Firmware ob success!");
                }
                else
                {
                    firmwareUpdateButton.IsEnabled = true;
                    Console.WriteLine("Upgrade failed.");
                }
            }
            catch (Exception e)
            {
                firmwareUpdateButton.IsEnabled = true;
                Console.WriteLine("Upgrade Firmware ob error!");
                Console.WriteLine(e);
            }
        }

        private void UpdateDeviceInfo(DeviceInfo deviceInfo)
        {
            if (deviceInfo != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Device name: {deviceInfo.Name()}");
                sb.AppendLine($"Device pid: {deviceInfo.Pid()}");
                sb.AppendLine($"Firmware version: {deviceInfo.FirmwareVersion()}");
                sb.AppendLine($"Serial number: {deviceInfo.SerialNumber()}");

                deviceInfoTextBlock.Text = sb.ToString();
            }
            else
            {
                deviceInfoTextBlock.Text = "";
            }
        }

        private void Control_Closing(object sender, CancelEventArgs e)
        {
            foreach (var device in devices.Values)
            {
                device.Dispose();
            }
            devices.Clear();
        }
    }
}