using System;
using System.Runtime.InteropServices;

namespace Orbbec
{
    public class RecommendedFilterList : IDisposable
    {
        private NativeHandle _handle;

        internal RecommendedFilterList(IntPtr handle)
        {
            _handle = new NativeHandle(handle, Delete);
        }

        /**
        * \if English
        * @brief Get filter by index
        *
        * @param index  To create a device cable, the range is [0, count-1], if the index exceeds the range, an exception will be thrown

        * @return Filter returns the Filter object
        * \else
        * @brief 通过索引获取Filter
        *
        * @param index 要创建设备的索，范围 [0, count-1]，如果index超出范围将抛异常
        * @return Filter 返回Filter对象
        * \endif
        */
        public Filter GetFilter(UInt32 index)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_filter_list_get_filter(_handle.Ptr, index, ref error);
            NativeException.HandleError(error);
            return new Filter(handle);
        }

        /**
        * \if English
        * @brief Get the number of filter in the list
        *
        * @return UInt32 return the number of filters
        * \else
        * @brief 获取列表中Filter的数量
        *
        * @return UInt32 返回Filter的数量
        * \endif
        */
        public UInt32 FilterCount()
        {
            IntPtr error = IntPtr.Zero;
            UInt32 count = obNative.ob_filter_list_get_count(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return count;
        }

        internal void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_filter_list(handle, ref error);
            NativeException.HandleError(error);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }

}