using System;

namespace Orbbec
{
    public class FilterConfigSchemaList : IDisposable
    {
        private NativeHandle _handle;

        internal FilterConfigSchemaList(IntPtr handle)
        {
            _handle = new NativeHandle(handle, Delete);
        }

        public UInt32 Count()
        {
            IntPtr error = IntPtr.Zero;
            UInt32 count = obNative.ob_filter_config_schema_list_get_count(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return count;
        }

        public FilterConfigSchemaItem GetProfile(UInt32 index)
        {
            IntPtr error = IntPtr.Zero;
            FilterConfigSchemaItem item;
            obNative.ob_filter_config_schema_list_get_item(out item, _handle.Ptr, index, ref error);
            NativeException.HandleError(error);
            return item;
        }

        internal void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_filter_config_schema_list(handle, ref error);
            NativeException.HandleError(error);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}