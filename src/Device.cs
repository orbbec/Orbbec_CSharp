using System;
using System.Runtime.InteropServices;

namespace Orbbec
{
    public delegate void DeviceStateCallback(UInt64 state, String message);
    public delegate void SetDataCallback(DataTranState state, uint percent);
    public delegate void GetDataCallback(DataTranState state, DataChunk dataChunk);
    public delegate void DeviceUpgradeCallback(UpgradeState state, String message, byte percent);
    public delegate void SendFileCallback(FileTranState state, String message, byte percent);

    public class Device : IDisposable
    {
        private NativeHandle _handle;
        private DeviceStateCallback _deviceStateCallback;
        private NativeDeviceStateCallback _nativeDeviceStateCallback;
        private SetDataCallback _setDataCallback;
        private NativeSetDataCallback _nativeSetDataCallback;
        private GetDataCallback _getDataCallback;
        private NativeGetDataCallback _nativeGetDataCallback;
        private DeviceUpgradeCallback _deviceUpgradeCallback;
        private NativeDeviceUpgradeCallback _nativeDeviceUpgradeCallback;
        private SendFileCallback _sendFileCallback;
        private NativeSendFileCallback _nativeSendFileCallback;

        private void OnDeviceState(UInt64 state, String message, IntPtr userData)
        {
            if(_deviceStateCallback != null)
            {
                _deviceStateCallback(state, message);
            }
        }

        private void OnSetData(DataTranState state, uint percent, IntPtr userData)
        {
            if(_setDataCallback != null)
            {
                _setDataCallback(state, percent);
            }
        }

        private void OnGetData(DataTranState state, DataChunk dataChunk, IntPtr userData)
        {
            if(_getDataCallback != null)
            {
                _getDataCallback(state, dataChunk);
            }
        }

        private void OnDeviceUpgrade(UpgradeState state, String message, byte percent, IntPtr userData)
        {
            if(_deviceUpgradeCallback != null)
            {
                _deviceUpgradeCallback(state, message, percent);
            }
        }

        private void OnFileSend(FileTranState state, String message, byte percent, IntPtr userData)
        {
            if(_sendFileCallback != null)
            {
                _sendFileCallback(state, message, percent);
            }
        }

        internal Device(IntPtr handle)
        {
            _handle = new NativeHandle(handle, Delete);
            _nativeDeviceStateCallback = new NativeDeviceStateCallback(OnDeviceState);
            _nativeSetDataCallback = new NativeSetDataCallback(OnSetData);
            _nativeGetDataCallback = new NativeGetDataCallback(OnGetData);
            _nativeDeviceUpgradeCallback = new NativeDeviceUpgradeCallback(OnDeviceUpgrade);
            _nativeSendFileCallback = new NativeSendFileCallback(OnFileSend);
        }

        internal NativeHandle GetNativeHandle()
        {
            return _handle;
        }

        /**
        * \if English
        * @brief Get device information
        *
        * @return DeviceInfo returns device information
        * \else
        * @brief 获取设备信息
        *
        * @return DeviceInfo 返回设备的信息
        * \endif
        */
        public DeviceInfo GetDeviceInfo()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_device_get_device_info(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return new DeviceInfo(handle);
        }

        /**
        * \if English
        * @brief Get device sensor list
        *
        * @return SensorList returns the sensor list
        * \else
        * @brief 获取设备传感器列表
        *
        * @return SensorList 返回传感器列表
        * \endif
        */
        public SensorList GetSensorList()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_device_get_sensor_list(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return new SensorList(handle);
        }

        /**
        * \if English
        * @brief Get specific type of sensor
        * if device not open, SDK will automatically open the connected device and return to the instance
        *
        * @return Sensor eturns the sensor example, if the device does not have the device,returns nullptr
        * \else
        * @brief 获取指定类型传感器
        * 如果设备没有打开传感器，在SDK内部会自动打开设备并返回实例
        *
        * @return Sensor 返回传感器示例，如果设备没有该设备，返回nullptr
        * \endif
        */
        public Sensor GetSensor(SensorType sensorType)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_device_get_sensor(_handle.Ptr, sensorType, ref error);
            NativeException.HandleError(error);
            return new Sensor(handle);
        }

        /**
        * \if English
        * @brief Set int type of device property
        *
        * @param propertyId Property id
        * @param property Property to be set
        * \else
        * @brief 设置int类型的设备属性
        *
        * @param propertyId 属性id
        * @param property 要设置的属性
        * \endif
        */
        public void SetIntProperty(PropertyId propertyId, Int32 property)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_set_int_property(_handle.Ptr, propertyId, property, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Get int type of device property
        *
        * @param propertyId Property id
        * @return Int32 Property to get
        * \else
        * @brief 获取int类型的设备属性
        *
        * @param propertyId 属性id
        * @return Int32 获取的属性数据
        * \endif
        */
        public Int32 GetIntProperty(PropertyId propertyId)
        {
            IntPtr error = IntPtr.Zero;
            Int32 value = obNative.ob_device_get_int_property(_handle.Ptr, propertyId, ref error);
            NativeException.HandleError(error);
            return value;
        }

        /**
        * \if English
        * @brief Set float type of device property
        *
        * @param propertyId Property id
        * @param property Property to be set
        * \else
        * @brief 设置float类型的设备属性
        *
        * @param propertyId 属性id
        * @param property 要设置的属性
        * \endif
        */
        public void SetFloatProperty(PropertyId propertyId, float property)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_set_float_property(_handle.Ptr, propertyId, property, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Get float type of device property
        *
        * @param propertyId Property id
        * @return float Property to get
        * \else
        * @brief 获取float类型的设备属性
        *
        * @param propertyId 属性id
        * @return float 获取的属性数据
        * \endif
        */
        public float GetFloatProperty(PropertyId propertyId)
        {
            IntPtr error = IntPtr.Zero;
            float value = obNative.ob_device_get_float_property(_handle.Ptr, propertyId, ref error);
            NativeException.HandleError(error);
            return value;
        }

        /**
        * \if English
        * @brief Set bool type of device property
        *
        * @param propertyId Property id
        * @param property Property to be set
        * \else
        * @brief 设置bool类型的设备属性
        *
        * @param propertyId 属性id
        * @param property 要设置的属性
        * \endif
        */
        public void SetBoolProperty(PropertyId propertyId, bool property)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_set_bool_property(_handle.Ptr, propertyId, property, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Get bool type of device property
        *
        * @param propertyId Property id
        * @return bool Property to get
        * \else
        * @brief 获取bool类型的设备属性
        *
        * @param propertyId 属性id
        * @return bool 获取的属性数据
        * \endif
        */
        public bool GetBoolProperty(PropertyId propertyId)
        {
            IntPtr error = IntPtr.Zero;
            bool value = obNative.ob_device_get_bool_property(_handle.Ptr, propertyId, ref error);
            NativeException.HandleError(error);
            return value;
        }

        /**
        * \if English
        * @brief Set structured data type of device property
        *
        * @param propertyId Property id
        * @param T Property data to be set
        * \else
        * @brief 设置structured data类型的设备属性
        *
        * @param propertyId 属性id
        * @param T 要设置的属性数据
        * \endif
        */
        public void SetStructuredData<T>(PropertyId propertyId, T data) where T : struct
        {
            IntPtr error = IntPtr.Zero;

            uint dataSize = (uint)Marshal.SizeOf(typeof(T));
            IntPtr dataPtr = Marshal.AllocHGlobal((int)dataSize);
            try
            {
                Marshal.StructureToPtr(data, dataPtr, false);

                obNative.ob_device_set_structured_data(_handle.Ptr, propertyId, dataPtr, dataSize, ref error);
                NativeException.HandleError(error);
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }

        /**
        * \if English
        * @brief Get structured data type of device property
        *
        * @param propertyId Property id
        * @param data Property data obtained
        * @param dataSize Get the size of the attribute
        * \else
        * @brief 获取structured data类型的设备属性
        *
        * @param propertyId 属性id
        * @param data 获取的属性数据
        * @param dataSize 获取的属性大小
        * \endif
        */
        public void GetStructuredData<T>(PropertyId propertyId, ref T data) where T : struct
        {
            IntPtr error = IntPtr.Zero;

            uint dataSize = (uint)Marshal.SizeOf(typeof(T));
            IntPtr dataPtr = Marshal.AllocHGlobal((int)dataSize);
            try
            {
                obNative.ob_device_get_structured_data(_handle.Ptr, propertyId, dataPtr, ref dataSize, ref error);
                NativeException.HandleError(error);

                data = Marshal.PtrToStructure<T>(dataPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }

        /**
        * \if English
        * @brief Judge property permission support
        *
        * @param propertyId Property id
        * @param permission Types of read and write permissions that need to be interpreted
        * @return bool returns whether it is supported
        * \else
        * @brief 判断属性权限支持情况
        *
        * @param propertyId 属性id
        * @param permission 需要判读的读写权限类型
        * @return bool 返回是否支持
        * \endif
        */
        public bool IsPropertySupported(PropertyId propertyId, PermissionType permissionType)
        {
            IntPtr error = IntPtr.Zero;
            bool isSupported = obNative.ob_device_is_property_supported(_handle.Ptr, propertyId, permissionType, ref error);
            NativeException.HandleError(error);
            return isSupported;
        }

        /**
        * \if English
        * @brief Get int type device property range (ncluding current valueand default value)
        *
        * @param propertyId Property id
        * @return IntPropertyRange Property range
        * \else
        * @brief 获取int类型的设备属性的范围(包括当前值和默认值)
        *
        * @param propertyId 属性id
        * @return IntPropertyRange 属性的范围
        * \endif
        */
        public IntPropertyRange GetIntPropertyRange (PropertyId propertyId)
        {
            IntPtr error = IntPtr.Zero;
            IntPropertyRange range;
            obNative.ob_device_get_int_property_range(out range, _handle.Ptr, propertyId, ref error);
            NativeException.HandleError(error);
            return range;
        }

        /**
        * \if English
        * @brief Get float type device property range((including current valueand default value)
        *
        * @param propertyId Property id
        * @return FloatPropertyRange Property range
        * \else
        * @brief 获取float类型的设备属性的范围(包括当前值和默认值)
        *
        * @param propertyId 属性id
        * @return FloatPropertyRange 属性的范围
        * \endif
        */
        public FloatPropertyRange GetFloatPropertyRange (PropertyId propertyId)
        {
            IntPtr error = IntPtr.Zero;
            FloatPropertyRange range;
            obNative.ob_device_get_float_property_range(out range, _handle.Ptr, propertyId, ref error);
            NativeException.HandleError(error);
            return range;
        }

        /**
        * \if English
        * @brief Get bool type device property range (including current value anddefault value)
        *
        * @param propertyId Property id
        * @return GetBoolPropertyRange Property range
        * \else
        * @brief 获取bool类型的设备属性的范围(包括当前值和默认值)
        *
        * @param propertyId 属性id
        * @return GetBoolPropertyRange 属性的范围
        * \endif
        */
        public BoolPropertyRange GetBoolPropertyRange(PropertyId propertyId)
        {
            IntPtr error = IntPtr.Zero;
            BoolPropertyRange range;
            obNative.ob_device_get_bool_property_range(out range, _handle.Ptr, propertyId, ref error);
            NativeException.HandleError(error);
            return range;
        }

        /**
        * \if English
        * @brief Get number of devices supported property
        *
        * @return UInt32 returns the number of supported attributes
        * \else
        * @brief 获取设备支持的属性的数量
        *
        * @return UInt32 返回支持的属性的数量
        * \endif
        */
        public UInt32 GetSupportedPropertyCount()
        {
            IntPtr error = IntPtr.Zero;
            UInt32 count = obNative.ob_device_get_supported_property_count(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return count;
        }

        /**
        * \if English
        * @brief Get device supported properties
        *
        * @param uint32_t Property index
        * @return PropertyItem returns the type of supported properties
        * \else
        * @brief 获取设备支持的属性
        *
        * @param uint32_t 属性的index
        * @return PropertyItem 返回支持的属性的类型
        * \endif
        */
        public PropertyItem GetSupportedProperty(UInt32 index)
        {
            IntPtr error = IntPtr.Zero;
            PropertyItem propertyItem;
            obNative.ob_device_get_supported_property_item(out propertyItem, _handle.Ptr, index, ref error);
            NativeException.HandleError(error);
            return propertyItem;
        }

        /**
        * \if English
        * @brief Upgrade the device firmware
        *
        * @param filePath Firmware path
        * @param callback  Firmware upgrade progress and status callback
        * @param async    Whether to execute asynchronously
        * \else
        * @brief 升级设备固件
        *
        * @param filePath 固件的路径
        * @param callback 固件升级进度及状态回调
        * @param async    是否异步执行
        * \endif
        */
        public void DeviceUpgrade(String filePath, DeviceUpgradeCallback callback, bool async = true)
        {
            _deviceUpgradeCallback = callback;
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_update_firmware(_handle.Ptr, filePath, _nativeDeviceUpgradeCallback, async, IntPtr.Zero, ref error);
            NativeException.HandleError(error);
        }

        public void DeviceUpgradeFromData(byte[] fileData, DeviceUpgradeCallback callback, bool async = true)
        {
            _deviceUpgradeCallback = callback;
            IntPtr error = IntPtr.Zero;
            GCHandle gcHandle = GCHandle.Alloc(fileData, GCHandleType.Pinned);
            IntPtr intPtr = gcHandle.AddrOfPinnedObject();
            obNative.ob_device_update_firmware_from_data(_handle.Ptr, intPtr, (UInt32)fileData.Length, _nativeDeviceUpgradeCallback, async, IntPtr.Zero, ref error);
            gcHandle.Free();
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Get the current state
        * @return UInt64 device state information
        * \else
        * @brief 获取当前设备状态
        * @return UInt64 设备状态信息
        * \endif
        */
        public UInt64 GetDeviceState()
        {
            IntPtr error = IntPtr.Zero;
            UInt64 state = obNative.ob_device_get_device_state(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return state;
        }

        /**
        * \if English
        * @brief Set the device state changed callbacks
        *
        * @param callback The callback function that is triggered when the device status changes (for example, the frame rate is automatically reduced or the
        * stream is closed due to high temperature, etc.) \else
        * @brief 设置设备状态改变回调函数
        *
        * @param callback 设备状态改变（如，由于温度过高自动降低帧率或关流等）时触发的回调函数
        * \endif
        */
        public void SetDeviceStateChangedCallback(DeviceStateCallback callback)
        {
            _deviceStateCallback = callback;
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_set_state_changed_callback(_handle.Ptr, _nativeDeviceStateCallback, IntPtr.Zero, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Get the original parameter list of camera calibration saved in the device. The parameters in the list do not correspond to the current
        * open-current configuration. You need to select the parameters according to the actual situation, and may need to do scaling, mirroring and other
        * processing. Non-professional users are recommended to use the Pipeline::getCameraParam() interface.
        *
        * @return CameraParamList camera parameter list
        * \else
        * @brief 获取设备内保存的相机标定的原始参数列表，列表内参数不与当前开流配置相对应，
        * 需要自行根据实际情况选用参数并可能需要做缩放、镜像等处理。非专业用户建议使用Pipeline::getCameraParam()接口。
        *
        * @return CameraParamList 相机参数列表
        * \endif
        */
        public CameraParamList GetCalibrationCameraParamList()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_device_get_calibration_camera_param_list(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return new CameraParamList(handle);
        }

        /**
        * \if English
        * @brief Get current depth work mode
        *
        * @return DepthWorkMode Current depth work mode
        * \else
        * @brief 查询当前的相机深度模式
        *
        * @return 返回当前的相机深度模式
        * \endif
        */
        public DepthWorkMode GetCurrentDepthWorkMode()
        {
            IntPtr error = IntPtr.Zero;
            DepthWorkMode workMode;
            obNative.ob_device_get_current_depth_work_mode(out workMode, _handle.Ptr, ref error);
            NativeException.HandleError(error);
            return workMode;
        }

        public String DepthWorkModeName()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_device_get_current_depth_work_mode_name(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return Marshal.PtrToStringAnsi(ptr);
        }

        /**
        * \if English
        * @brief Switch depth work mode by DepthWorkMode. Prefer invoke switchDepthWorkMode(const char *modeName) to switch depth mode
        *        when known the complete name of depth work mode.
        * @param[in] workMode Depth work mode come from ob_depth_work_mode_list which return by ob_device_get_depth_work_mode_list
        * \else
        * @brief 切换相机深度模式（根据深度工作模式对象），如果知道设备支持的深度工作模式名称，那么推荐用switchDepthWorkMode(const char *modeName)
        *
        * @param workMode 要切换的相机深度模式
        *
        * \endif
        */
        public void SwitchDepthWorkMode(DepthWorkMode workMode)
        {
            IntPtr error = IntPtr.Zero;
            GCHandle gcHandle = GCHandle.Alloc(workMode, GCHandleType.Pinned);
            IntPtr ptr = gcHandle.AddrOfPinnedObject();
            obNative.ob_device_switch_depth_work_mode(_handle.Ptr, ptr, ref error);
            gcHandle.Free();
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Switch depth work mode by work mode name.
        *
        * @param[in] modeName Depth work mode name which equals to OBDepthWorkMode.name
        * \else
        * @brief 切换相机深度模式（根据深度工作模式名称）
        *
        * @param modeName 相机深度工作模式的名称，模式名称必须与OBDepthWorkMode.name一致
        *
        * \endif
        */
        public void SwitchDepthWorkMode(String modeName)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_switch_depth_work_mode_by_name(_handle.Ptr, modeName, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Request support depth work mode list
        * @return OBDepthWorkModeList list of ob_depth_work_mode
        * \else
        * @brief 查询相机深度模式列表
        *
        * @return 相机深度模式列表
        * \endif
        */
        public DepthWorkModeList GetDepthWorkModeList()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_device_get_depth_work_mode_list(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return new DepthWorkModeList(ptr);
        }

        /**
        * \if English
        * @brief Device restart
        * @attention The device will be disconnected and reconnected. After the device is disconnected, the access to the Device object interface may be abnormal.
        *   Please delete the object directly and obtain it again after the device is reconnected.
        * \else
        * @brief 设备重启
        * @attention 设备会掉线重连，设备掉线后对Device对象接口访问可能会发生异常，请直接删除该对象，
        *   待设备重连后可重新获取。
        * \endif
        */
        public void Reboot()
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_reboot(_handle.Ptr, ref error);
            NativeException.HandleError(error);
        }

        public UInt16 GetSupportedMultiDeviceSyncModeBitmap()
        {
            IntPtr error = IntPtr.Zero;
            UInt16 result = obNative.ob_device_get_supported_multi_device_sync_mode_bitmap(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return result;
        }

        public void SetMultiDeviceSyncConfig(MultiDeviceSyncConfig config)
        {
            IntPtr error = IntPtr.Zero;
            GCHandle gcHandle = GCHandle.Alloc(config, GCHandleType.Pinned);
            IntPtr ptr = gcHandle.AddrOfPinnedObject();
            obNative.ob_device_set_multi_device_sync_config(_handle.Ptr, ptr, ref error);
            gcHandle.Free();
            NativeException.HandleError(error);
        }

        public MultiDeviceSyncConfig GetMultiDeviceSyncConfig()
        {
            IntPtr error = IntPtr.Zero;
            MultiDeviceSyncConfig config;
            obNative.ob_device_get_multi_device_sync_config(out config, _handle.Ptr, ref error);
            NativeException.HandleError(error);
            return config;
        }

        public void TriggerCapture()
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_trigger_capture(_handle.Ptr, ref error);
            NativeException.HandleError(error);
        }

        public void SetTimestampResetConfig(DeviceTimestampResetConfig config)
        {
            IntPtr error = IntPtr.Zero;
            GCHandle gcHandle = GCHandle.Alloc(config, GCHandleType.Pinned);
            IntPtr ptr = gcHandle.AddrOfPinnedObject();
            obNative.ob_device_set_timestamp_reset_config(_handle.Ptr, ptr, ref error);
            gcHandle.Free();
            NativeException.HandleError(error);
        }

        public DeviceTimestampResetConfig GetTimestampResetConfig()
        {
            IntPtr error = IntPtr.Zero;
            DeviceTimestampResetConfig config = obNative.ob_device_get_timestamp_reset_config(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return config;
        }

        public void TimestampReset()
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_timestamp_reset(_handle.Ptr, ref error);
            NativeException.HandleError(error);
        }

        public void TimerSyncWithHost()
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_timer_sync_with_host(_handle.Ptr, ref error);
            NativeException.HandleError(error);
        }

        public void EnableHeartbeat(bool enable)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_enable_heartbeat(_handle.Ptr, enable, ref error);
            NativeException.HandleError(error);
        }

        public void SendAndReceiveData(IntPtr sendData, UInt32 sendDataSize, IntPtr receiveData, ref UInt32 receiveDataSize)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_send_and_receive_data(_handle.Ptr, sendData, sendDataSize, receiveData, ref receiveDataSize, ref error);
            NativeException.HandleError(error);
        }

        public bool IsExtensionInfoExist(String infoKey)
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_device_is_extension_info_exist(_handle.Ptr, infoKey, ref error);
            NativeException.HandleError(error);
            return result;
        }

        public String GetExtensionInfo(String infoKey)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_device_get_extension_info(_handle.Ptr, infoKey, ref error);
            NativeException.HandleError(error);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public bool IsSupportedGlobalTimestamp()
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_device_is_global_timestamp_supported(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return result;
        }

        public bool EnableGlobalTimestamp(bool enable)
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_device_enable_global_timestamp(_handle.Ptr, enable, ref error);
            NativeException.HandleError(error);
            return result;
        }

        public String GetCurrentPresetName()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_device_get_current_preset_name(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public void LoadPreset(String presetName)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_load_preset(_handle.Ptr, presetName, ref error);
            NativeException.HandleError(error);
        }

        public void LoadPresetFromJsonFile(String jsonFilePath)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_load_preset_from_json_file(_handle.Ptr, jsonFilePath, ref error);
            NativeException.HandleError(error);
        }

        public void LoadPresetFromJsonData(String presetName, IntPtr data, UInt32 size)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_load_preset_from_json_data(_handle.Ptr, presetName, data, size, ref error);
            NativeException.HandleError(error);
        }

        public void ExportCurrentSettingsAsPresetJsonFile(String jsonFilePath)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_export_current_settings_as_preset_json_file(_handle.Ptr, jsonFilePath, ref error);
            NativeException.HandleError(error);
        }

        public void ExportCurrentSettingsAsPresetJsonData(String presetName, IntPtr data, UInt32 size)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_device_export_current_settings_as_preset_json_data(_handle.Ptr, presetName, data, size, ref error);
            NativeException.HandleError(error);
        }

        internal void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_device(handle, ref error);
            NativeException.HandleError(error);
        }

        public PresetList GetPresetList()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_device_get_available_preset_list(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return new PresetList(ptr);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}