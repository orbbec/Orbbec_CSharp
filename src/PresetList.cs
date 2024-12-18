using System;
using System.Runtime.InteropServices;

namespace Orbbec
{
    public class PresetList : IDisposable
    {
        private NativeHandle _handle;

        internal PresetList(IntPtr handle)
        {
            _handle = new NativeHandle(handle, Delete);
        }

        /**
        * \if English
        * @brief Get the number of preset in the preset list
        *
        * @return The number of preset in the preset list
        * \else
        * @brief 获取预置位列表中预置位的数量
        *
        * @return 预设列表中的预设数量
        * \endif
        */
        public UInt32 Count()
        {
            IntPtr error = IntPtr.Zero;
            UInt32 count = obNative.ob_device_preset_list_get_count(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return count;
        }

        public String GetName(uint index)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_device_preset_list_get_name(_handle.Ptr, index, ref error);
            NativeException.HandleError(error);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public bool HasPreset(String presetName)
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_device_preset_list_has_preset(_handle.Ptr, presetName, ref error);
            NativeException.HandleError(error);
            return result;
        }

        internal void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_preset_list(handle, ref error);
            NativeException.HandleError(error);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}