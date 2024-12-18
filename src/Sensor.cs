using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Orbbec
{
    public delegate void FrameCallback(Frame frame);

    public class Sensor : IDisposable
    {
        private NativeHandle _handle;
        private FrameCallback _callback;
        private NativeFrameCallback _nativeCallback;

        private void OnFrame(IntPtr framePtr, IntPtr userData)
        {
            Frame frame = new Frame(framePtr);
            if(_callback != null)
            {
                _callback(frame);
            }
            else
            {
                frame.Dispose();
            }
        }

        internal Sensor(IntPtr handle)
        {
            _handle = new NativeHandle(handle, Delete);
            _nativeCallback = new NativeFrameCallback(OnFrame);
        }

        internal NativeHandle GetNativeHandle()
        {
            return _handle;
        }

        /**
        * \if English
        * @brief sensor type
        *
        * @return SensorType returns the sensor type
        * \else
        * @brief 传感器类型
        *
        * @return SensorType 返回传感器类型
        * \endif
        */
        public SensorType GetSensorType()
        {
            IntPtr error = IntPtr.Zero;
            SensorType sensorType = obNative.ob_sensor_get_type(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return sensorType;
        }

        /**
        * \if English
        * @brief Get the list of stream profiles
        *
        * @return StreamProfileList returns the stream configuration list
        * \else
        * @brief 获取传感器的流配置列表
        *
        * @return StreamProfileList 返回流配置列表
        * \endif
        */
        public StreamProfileList GetStreamProfileList()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_sensor_get_stream_profile_list(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return new StreamProfileList(handle);
        }

        /**
        * \if English
        * @brief Open frame data stream and set up a callback
        *
        * @param streamProfile Stream configuration
        * @param callback Set the callback when frame data arrives
        * \else
        * @brief 开启流并设置帧数据回调
        *
        * @param streamProfile 流的配置
        * @param callback 设置帧数据到达时的回调
        * \endif
        */
        public void Start(StreamProfile streamProfile, FrameCallback callback)
        {
            _callback = callback;
            IntPtr error = IntPtr.Zero;
            obNative.ob_sensor_start(_handle.Ptr, streamProfile.GetNativeHandle().Ptr, _nativeCallback, IntPtr.Zero, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Stop stream
        * \else
        * @brief 停止流
        * \endif
        */
        public void Stop()
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_sensor_stop(_handle.Ptr, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Dynamically switch resolutions
        *
        * @param streamProfile Resolution to switch
        * \else
        * @brief 动态切换分辨率
        *
        * @param streamProfile 需要切换的分辨率
        * \endif
        */
        public void SwitchProfile(StreamProfile streamProfile)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_sensor_switch_profile(_handle.Ptr, streamProfile.GetNativeHandle().Ptr, ref error);
            NativeException.HandleError(error);
        }

        public List<Filter> CreateRecommendedFilters()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr listPtr = obNative.ob_sensor_create_recommended_filter_list(_handle.Ptr, ref error);
            NativeException.HandleError(error);

            uint count = obNative.ob_filter_list_get_count(listPtr, ref error);
            NativeException.HandleError(error);

            List<Filter> filters = new List<Filter>();
            for (uint i = 0; i < count; i++)
            {
                IntPtr filterPtr = obNative.ob_filter_list_get_filter(listPtr, i, ref error);
                NativeException.HandleError(error);
                Filter filter = new Filter(filterPtr);
                filters.Add(filter);
            }

            obNative.ob_delete_filter_list(listPtr, ref error);
            NativeException.HandleError(error);

            return filters;
        }

        internal void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_sensor(handle, ref error);
            NativeException.HandleError(error);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}