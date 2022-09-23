using System;
using System.Runtime.InteropServices;

namespace Orbbec
{
    public delegate void DeviceChangedCallback(DeviceList removed, DeviceList added);
    internal delegate void DeviceChangedCallbackInternal(IntPtr removedPtr, IntPtr addedPtr, IntPtr userDataPtr);

    public class Context : IDisposable
    {
        private NativeHandle _handle;
        private DeviceChangedCallback _callback;
        private DeviceChangedCallbackInternal _internalCallback;

        /**
        * @brief context是描述SDK的runtime一个管理类，负责SDK的资源申请与释放
        * context具备多设备的管理能力，负责枚举设备，监听设备回调，启用多设备同步等功能
        *
        */
        public Context()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_context(out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
            _handle = new NativeHandle(handle, Delete);
            _internalCallback = new DeviceChangedCallbackInternal(OnDeviceChanged);
        }

        /**
        * @brief 可以传入自定义的配置文件
        *
        * @param configPath 配置文件的路径
        */
        public Context(String configPath)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_context_with_config(configPath, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
            _handle = new NativeHandle(handle, Delete);
        }

        /**
        * @brief 获取枚举到设备列表
        *
        * @return DeviceList返回设备列表类的指针
        */
        public DeviceList QueryDeviceList()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_query_device_list(_handle.Ptr, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
            return new DeviceList(handle);
        }

        /**
        * @brief 创建网络设备
        *
        * @param address 设备ip地址
        * @param port 设备端口
        * @return Device 返回设备对象
        */
        public Device CreateNetDevice(string address, UInt16 port)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_net_device(_handle.Ptr, address, port, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
            return new Device(handle);
        }

        /**
        * @brief 设置设备插拔回调函数
        *
        * @param callback 设备插拔时触发的回调函数
        */
        public void SetDeviceChangedCallback(DeviceChangedCallback callback)
        {
            _callback = callback;
            IntPtr error = IntPtr.Zero;
            obNative.ob_set_device_changed_callback(_handle.Ptr, _internalCallback, IntPtr.Zero, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
        }

        /**
         * @brief 启动多设备同步功能，同步已创建设备的时钟(需要使用的设备支持该功能)
         *
         * @param[in]  repeatInterval 定时同步时间间隔（单位ms；如果repeatInterval=0，表示只同步一次，不再定时执行）
         */
        public void EnableMultiDeviceSync(UInt64 repeatInterval)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_enable_multi_device_sync(_handle.Ptr, repeatInterval, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
        }

        /**
        * @brief 设置日志的输出等级
        *
        * @param logSeverity log的输出等级
        */
        public void SetLoggerServerity(LogSeverity logSeverity)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_set_logger_serverity(logSeverity, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
        }

        /**
        * @brief 设置日志输出到文件
        *
        * @param logSeverity 日志的输出等级
        * @param fileName 输出的文件名
        */
        public void SetLoggerToFile(LogSeverity logSeverity, String fileName)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_set_logger_to_file(logSeverity, fileName, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
        }

        /**
        * @brief 设置日志输出到终端
        *
        * @param logSeverity 日志的输出等级
        */
        public void SetLoggerToConsole(LogSeverity logSeverity)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_set_logger_to_console(logSeverity, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
        }

        internal void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_context(handle, out error);
            if (error != IntPtr.Zero)
            {
                throw new NativeException(new Error(error));
            }
        }

        public void Dispose()
        {
            _handle.Dispose();
        }

        private void OnDeviceChanged(IntPtr removedPtr, IntPtr addedPtr, IntPtr userDataPtr)
        {
            DeviceList removed = new DeviceList(removedPtr);
            DeviceList added = new DeviceList(addedPtr);
            if (_callback != null)
            {
                _callback(removed, added);
            }
            else
            {
                removed.Dispose();
                added.Dispose();
            }
        }
    }
}