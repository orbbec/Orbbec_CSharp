using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Orbbec
{
    public delegate void FilterCallback(Frame frame);

    public class Filter : IDisposable
    {
        protected NativeHandle _handle;
        private FilterCallback _callback;
        private NativeFilterCallback _nativeCallback;
        protected string _name;
        protected List<FilterConfigSchemaItem> _configSchemaList = new List<FilterConfigSchemaItem>();

        private void OnFilter(IntPtr framePtr, IntPtr userData)
        {
            Frame frame = new Frame(framePtr);
            if (_callback != null)
            {
                _callback(frame);
            }
            else
            {
                frame.Dispose();
            }
        }

        protected void Init()
        {
            IntPtr error = IntPtr.Zero;

            _name = Marshal.PtrToStringAnsi(obNative.ob_filter_get_name(_handle.Ptr, ref error));
            NativeException.HandleError(error);

            IntPtr configSchemaListPtr = obNative.ob_filter_get_config_schema_list(_handle.Ptr, ref error);
            NativeException.HandleError(error);

            uint count = obNative.ob_filter_config_schema_list_get_count(configSchemaListPtr, ref error);
            NativeException.HandleError(error);

            for (uint i = 0; i < count; i++)
            {
                FilterConfigSchemaItem item;
                obNative.ob_filter_config_schema_list_get_item(out item, configSchemaListPtr, i, ref error);
                NativeException.HandleError(error);

                _configSchemaList.Add(item);
            }

            obNative.ob_delete_filter_config_schema_list(configSchemaListPtr, ref error);
            NativeException.HandleError(error);
        }

        protected T GetPropertyRange<T>(string configName, Func<double, T> rangeFactory) where T : struct
        {
            if (_configSchemaList.Count == 0)
            {
                throw new InvalidOperationException("The configuration schema vector is empty.");
            }

            IntPtr error = IntPtr.Zero;
            var cur = obNative.ob_filter_get_config_value(_handle.Ptr, configName, ref error);
            NativeException.HandleError(error);

            return rangeFactory(cur);
        }

        internal Filter()
        {
            _nativeCallback = new NativeFilterCallback(OnFilter);
        }

        internal Filter(IntPtr handle)
        {
            _handle = new NativeHandle(handle, Delete);
            _nativeCallback = new NativeFilterCallback(OnFilter);
            Init();
        }

        public T As<T>() where T : Filter
        {
            switch (Name())
            {
                case "DecimationFilter":
                    _handle.Retain();
                    return new DecimationFilter(_handle.Ptr) as T;
                case "HDRMerge":
                    _handle.Retain();
                    return new HdrMerge(_handle.Ptr) as T;
                case "SequenceIdFilter":
                    _handle.Retain();
                    return new SequenceIdFilter(_handle.Ptr) as T;
                case "SpatialAdvancedFilter":
                    _handle.Retain();
                    return new SpatialAdvancedFilter(_handle.Ptr, "") as T;
                case "TemporalFilter":
                    _handle.Retain();
                    return new TemporalFilter(_handle.Ptr, "") as T;
                case "HoleFillingFilter":
                    _handle.Retain();
                    return new HoleFillingFilter(_handle.Ptr, "") as T;
                case "NoiseRemovalFilter":
                    _handle.Retain();
                    return new NoiseRemovalFilter(_handle.Ptr, "") as T;
                case "DisparityTransform":
                    _handle.Retain();
                    return new DisparityTransform(_handle.Ptr, "") as T;
                case "ThresholdFilter":
                    _handle.Retain();
                    return new ThresholdFilter(_handle.Ptr) as T;
            }
            return null;
        }

        internal Filter CreatePrivateFilter(string name, string activationKey = "")
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_private_filter(name, activationKey, ref error);
            NativeException.HandleError(error);
            return new Filter(handle);
        }

        /**
        * \if English
        * @brief filter reset, free the internal cache, stop the processing thread and clear the pending buffer frame when asynchronous processing
        * \else
        * @brief filter重置，释放内部缓存，异步处理时停止处理线程并清空待处理的缓存帧
        * \endif
        */
        public void Reset()
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_filter_reset(_handle.Ptr, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Processing frames (synchronous interface)
        *
        * @param frame frame to be processed
        * @return Frame processed frame
        * \else
        * @brief 处理帧（同步接口）
        *
        * @param frame 需要处理的frame
        * @return Frame 处理后的frame
        * \endif
        */
        public Frame Process(Frame frame)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_filter_process(_handle.Ptr, frame.GetNativeHandle().Ptr, ref error);
            if (handle == IntPtr.Zero)
            {
                return null;
            }
            return new Frame(handle);
        }

        /**
        * \if English
        * @brief Set the callback function (asynchronous callback interface)
        *
        * @param callback Processing result callback
        * \else
        * @brief 设置回调函数（异步回调接口）
        *
        * @param callback 处理结果回调
        * \endif
        */
        public void SetCallback(FilterCallback callback)
        {
            _callback = callback;
            IntPtr error = IntPtr.Zero;
            obNative.ob_filter_set_callback(_handle.Ptr, _nativeCallback, IntPtr.Zero, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Push the pending frame into the cache (asynchronous callback interface)
        *
        * @param frame The pending frame processing result is returned by the callback function
        * \else
        * @brief 压入待处理frame到缓存（异步回调接口）
        *
        * @param frame 待处理的frame处理结果通过回调函数返回
        * \endif
        */
        public void PushFrame(Frame frame)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_filter_push_frame(_handle.Ptr, frame.GetNativeHandle().Ptr, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Enable the frame post processing
        * @brief The filter default is enable
        *
        * \else
        * @brief 启用帧后处理
        * @brief 滤波默认处于启用状态
        * 
        * \endif
        */
        public void Enable(bool enable)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_filter_enable(_handle.Ptr, enable, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Get the enable status of the frame post processing
        *
        * @return bool The post processing filter status. True: enable; False: disable
        * \else
        * @brief 获取帧后处理的启用状态
        * 
        * @return bool 后处理filter状态。True：启用；错误：禁用
        * \endif
        */
        public bool IsEnabled()
        {
            IntPtr error = IntPtr.Zero;
            bool res = obNative.ob_filter_is_enabled(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return res;
        }

        public string Name()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_filter_get_name(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return Marshal.PtrToStringAnsi(ptr);
        }

        public string GetConfigSchema()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr ptr = obNative.ob_filter_get_config_schema(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return Marshal.PtrToStringAnsi(ptr);
        }

        //public FilterConfigSchemaList GetConfigSchemaList()
        //{
        //    IntPtr error = IntPtr.Zero;
        //    IntPtr handle = obNative.ob_filter_get_config_schema_list(_handle.Ptr, ref error);
        //    if (error != IntPtr.Zero)
        //    {
        //        throw new NativeException(new Error(error));
        //    }
        //    return new FilterConfigSchemaList(handle);
        //}

        public void UpdateConfig(UInt16 argc, string argv)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_filter_update_config(_handle.Ptr, argc, argv, ref error);
            NativeException.HandleError(error);
        }

        public List<FilterConfigSchemaItem> GetConfigSchemaList()
        {
            return _configSchemaList;
        }

        public void SetConfigValue(string configName, double value)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_filter_set_config_value(_handle.Ptr, configName, value, ref error);
            NativeException.HandleError(error);
        }

        public double GetConfigValue(string configName)
        {
            IntPtr error = IntPtr.Zero;
            double value = obNative.ob_filter_get_config_value(_handle.Ptr, configName, ref error);
            NativeException.HandleError(error);
            return value;
        }

        internal virtual void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_filter(handle, ref error);
            NativeException.HandleError(error);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }

    public class FilterFactory
    {
        /**
        * \if English
        * @brief Create a filter by name.
        * \else
        * @brief 按名称创建filter。
        * \endif
        */
        public static Filter CreateFilter(string name)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_filter(name, ref error);
            NativeException.HandleError(error);
            return new Filter(handle);
        }

        /**
         * \if English
         * @brief Create a private filter by name and activation key.
         * @brief Some private filters require an activation key to be activated, its depends on the vendor of the filter.
         *
         * @param name The name of the filter.
         * @param activationKey The activation key of the filter.
         * \else
         * @brief 按名称和激活密钥创建私有filter。
         * @brief 一些私有filter需要激活激活密钥，这取决于filter的供应商。
         * 
         * @param name filter的名称。
         * @param activationKey filter的激活密钥。
         */
        public static Filter CreatePrivateFilter(string name, string activationKey)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_private_filter(name, activationKey, ref error);
            NativeException.HandleError(error);
            return new Filter(handle);
        }

        /**
         * \if English
         * @brief Get the vendor specific code of a filter by filter name.
         * @brief A private filter can define its own vendor specific code for specific purposes.
         *
         * @param name The name of the filter.
         * @return string The vendor specific code of the filter.
         * \else
         * @brief 按filter名称获取filter的供应商特定代码。
         * @brief 私有filter可以为特定目的定义自己的供应商特定代码。
         * 
         * @param name filter的名称。
         * @return string filter的供应商特定代码。
         */
        public static string GetFilterVendorSpecificCode(string name)
        {
            IntPtr error = IntPtr.Zero;
            string code = obNative.ob_filter_get_vendor_specific_code(name, ref error);
            NativeException.HandleError(error);
            return code;
        }
    }

    public class PointCloudFilter : Filter
    {
        public PointCloudFilter()
        {
            IntPtr error = IntPtr.Zero;
            //IntPtr handle = obNative.ob_create_pointcloud_filter(ref error);
            IntPtr handle = obNative.ob_create_filter("PointCloudFilter", ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }

        /**
        * \if English
        * @brief Set camera parameters
        *
        * @param param Camera internal and external parameters
        * \else
        * @brief 设置相机参数
        *
        * @param param 相机内外参数
        * \endif
        */
        [Obsolete]
        public void SetCameraParam(CameraParam cameraParam)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_pointcloud_filter_set_camera_param(_handle.Ptr, cameraParam, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Set point cloud type parameters
        *
        * @param type Point cloud type depth point cloud or RGBD point cloud
        * \else
        * @brief 设置点云类型参数
        *
        * @param type 点云类型深度点云或RGBD点云
        * \endif
        */
        public void SetCreatePointFormat(Format format)
        {
            SetConfigValue("pointFormat", (double)format);
        }

        /**
        * \if English
        * @brief  Set the frame alignment state that will be input to generate point cloud (it needs to be enabled in D2C mode, as the basis for the algorithm to
        * select the set of camera internal parameters)
        *
        * @param state Alignment status, True: enable alignment; False: disable alignment
        * \else
        * @brief  设置将要输入的用于生成点云的帧对齐状态（D2C模式下需要开启，作为算法选用那组相机内参的依据）
        *
        * @param state 对齐状态，True：开启对齐； False：关闭对齐
        * \endif
        */
        [Obsolete]
        public void SetAlignState(bool state)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_pointcloud_filter_set_frame_align_state(_handle.Ptr, state, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief  Set the point cloud coordinate data zoom factor
        *
        * @attention Calling this function to set the scale will change the point coordinate scaling factor of the output point cloud frame: posScale = posScale /
        * scale.The point coordinate scaling factor for the output point cloud frame can be obtained via @ref PointsFrame::getPositionValueScale function
        *
        * @param scale Zoom factor
        * \else
        * @brief  设置点云坐标数据缩放比例
        *
        * @attention 调用该函数设置缩放比例会改变输出点云帧的点坐标缩放系数：posScale = posScale / scale;
        *  输出点云帧的点坐标缩放系数可通过 @ref PointsFrame::getPositionValueScale 函数获取
        *
        * @param scale 缩放比例
        * \endif
        */
        [Obsolete]
        public void SetPositionDataScaled(float scale)
        {
            SetCoordinateDataScaled(scale);
        }

        public void SetCoordinateDataScaled(float factor)
        {
            SetConfigValue("coordinateDataScale", factor);
        }

        /**
        * \if English
        * @brief  Set point cloud color data normalization
        *
        * @param state Whether normalization is required
        * \else
        * @brief  设置点云颜色数据归一化
        *
        * @param state 是否需要归一化
        * \endif
        */
        public void SetColorDataNormalization(bool state)
        {
            SetConfigValue("colorDataNormalization", state ? 1 : 0);
        }

        /**
        * \if English
        * @brief  Set point cloud coordinate system
        *
        * @param type coordinate system type
        * \else
        * @brief  设置点云坐标系
        *
        * @param type 坐标系类型
        * \endif
        */
        public void SetCoordinateSystem(CoordinateSystemType type)
        {
            SetConfigValue("coordinateSystemType", (double)type);
        }
    }

    public class AlignFilter : Filter
    {
        public AlignFilter(StreamType alignType)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_filter("Align", ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();

            SetConfigValue("AlignType", (double)alignType);
        }

        public StreamType GetAlignMode()
        {
            return (StreamType)GetConfigValue("AlignType");
        }

        public void SetMatchTargetResolution(bool state)
        {
            SetConfigValue("MatchTargetRes", state ? 1 : 0);
        }
    }

    public class FormatConvertFilter : Filter
    {
        public FormatConvertFilter()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_filter("FormatConverter", ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
        }

        /**
        * \if English
        * @brief Set format conversion type
        *
        * @param type Format conversion type
        * \else
        * @brief 设置格式转化类型
        *
        * @param format 格式转化类型
        * \endif
        */
        public void SetConvertFormat(ConvertFormat type)
        {
            SetConfigValue("convertType", (double)type);
        }
    }

    public class HdrMerge : Filter
    {
        internal HdrMerge(IntPtr handle) : base(handle) { }

        public HdrMerge()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_filter("HDRMerge", ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }
    }

    public class SequenceIdFilter : Filter
    {
        private Dictionary<float, string> sequenceIdList = new Dictionary<float, string>
        {
            { 0f, "all" },
            { 1f, "1" }
        };
        private SequenceIdItem[] _outputSequenceIdList;

        private void InitSeqenceIdList()
        {
            _outputSequenceIdList = new SequenceIdItem[sequenceIdList.Count];

            int i = 0;
            foreach (var pair in sequenceIdList)
            {
                var name = pair.Value.Length > 8 ? pair.Value.Substring(0, 8).PadRight(8, '\0') : pair.Value.PadRight(8, '\0');
                _outputSequenceIdList[i] = new SequenceIdItem
                {
                    sequenceSelectId = (int)pair.Key,
                    name = name.ToCharArray()
                };
                i++;
            }
        }

        internal SequenceIdFilter(IntPtr handle) : base(handle) 
        {
            InitSeqenceIdList();
        }

        public SequenceIdFilter()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_filter("SequenceIdFilter", ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
            InitSeqenceIdList();
        }

        /**
        * \if English
        * @brief Set the sequenceId filter params
        *
        * @param sequence id to pass the filter
        * \else
        * @brief 设置sequenceId滤波参数
        *
        * @param 通过滤波的序列 ID
        * \endif
        */
        public void SelectSequenceId(int sequenceId)
        {
            SetConfigValue("sequenceid", sequenceId);
        }

        public int GetSelectSequenceId()
        {
            return (int)GetConfigValue("sequenceid");
        }

        public SequenceIdItem[] GetSequenceIdList()
        {
            return _outputSequenceIdList;
        }

        public int GetSequenceIdListSize()
        {
            return sequenceIdList.Count;
        }

        internal override void Delete(IntPtr handle)
        {
            base.Delete(handle);

            if (_outputSequenceIdList != null)
            {
                _outputSequenceIdList = null;
            }
        }

    }

    public class DecimationFilter : Filter
    {
        internal DecimationFilter(IntPtr handle) : base(handle) { }

        public DecimationFilter()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_filter("DecimationFilter", ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }

        public void SetScaleValue(byte value)
        {
            SetConfigValue("decimate", value);
        }

        public byte GetScaleValue()
        {
            return (byte)GetConfigValue("decimate");
        }

        public UInt8PropertyRange GetScaleRange()
        {
            IntPtr error = IntPtr.Zero;
            UInt8PropertyRange scaleRange = new UInt8PropertyRange();
            if (_configSchemaList.Count > 0)
            {
                scaleRange = GetPropertyRange<UInt8PropertyRange>("decimate", cur => new UInt8PropertyRange
                {
                    cur = (byte)cur,
                    def = (byte)_configSchemaList[0].def,
                    max = (byte)_configSchemaList[0].max,
                    min = (byte)_configSchemaList[0].min,
                    step = (byte)_configSchemaList[0].step
                });
            }
            return scaleRange;
        }
    }

    public class ThresholdFilter : Filter
    {
        internal ThresholdFilter(IntPtr handle) : base(handle) { }

        public ThresholdFilter()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_filter("ThresholdFilter", ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }

        public IntPropertyRange GetMinRange()
        {
            IntPtr error = IntPtr.Zero;
            IntPropertyRange range = new IntPropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("min"))
                {
                    range = GetPropertyRange<IntPropertyRange>("min", cur => new IntPropertyRange
                    {
                        cur = (int)cur,
                        def = (int)item.def,
                        max = (int)item.max,
                        min = (int)item.min,
                        step = (int)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public IntPropertyRange GetMaxRange()
        {
            IntPtr error = IntPtr.Zero;
            IntPropertyRange range = new IntPropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("max"))
                {
                    range = GetPropertyRange<IntPropertyRange>("max", cur => new IntPropertyRange
                    {
                        cur = (int)cur,
                        def = (int)item.def,
                        max = (int)item.max,
                        min = (int)item.min,
                        step = (int)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public bool SetValueRange(ushort min, ushort max)
        {
            if (min >= max)
            {
                return false;
            }
            SetConfigValue("min", min);
            SetConfigValue("max", max);
            return true;
        }
    }

    public class SpatialAdvancedFilter : Filter
    {
        internal SpatialAdvancedFilter(IntPtr handle, string activationKey) : base(handle) {}

        public SpatialAdvancedFilter(string activationKey = "")
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_private_filter("SpatialAdvancedFilter", activationKey, ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }

        public FloatPropertyRange GetAlphaRange()
        {
            IntPtr error = IntPtr.Zero;
            FloatPropertyRange range = new FloatPropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("alpha"))
                {
                    range = GetPropertyRange<FloatPropertyRange>("alpha", cur => new FloatPropertyRange
                    {
                        cur = (float)cur,
                        def = (float)item.def,
                        max = (float)item.max,
                        min = (float)item.min,
                        step = (float)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public UInt16PropertyRange GetDispDiffRange()
        {
            IntPtr error = IntPtr.Zero;
            UInt16PropertyRange range = new UInt16PropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("disp_diff"))
                {
                    range = GetPropertyRange<UInt16PropertyRange>("disp_diff", cur => new UInt16PropertyRange
                    {
                        cur = (ushort)cur,
                        def = (ushort)item.def,
                        max = (ushort)item.max,
                        min = (ushort)item.min,
                        step = (ushort)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public UInt16PropertyRange GetRadiusRange()
        {
            IntPtr error = IntPtr.Zero;
            UInt16PropertyRange range = new UInt16PropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("radius"))
                {
                    range = GetPropertyRange<UInt16PropertyRange>("radius", cur => new UInt16PropertyRange
                    {
                        cur = (ushort)cur,
                        def = (ushort)item.def,
                        max = (ushort)item.max,
                        min = (ushort)item.min,
                        step = (ushort)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public IntPropertyRange GetMagnitudeRange()
        {
            IntPtr error = IntPtr.Zero;
            IntPropertyRange range = new IntPropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("magnitude"))
                {
                    range = GetPropertyRange<IntPropertyRange>("magnitude", cur => new IntPropertyRange
                    {
                        cur = (int)cur,
                        def = (int)item.def,
                        max = (int)item.max,
                        min = (int)item.min,
                        step = (int)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public SpatialAdvancedFilterParams GetFilterParams()
        {
            SpatialAdvancedFilterParams params_ = new SpatialAdvancedFilterParams();
            params_.alpha = (float)GetConfigValue("alpha");
            params_.disp_diff = (ushort)GetConfigValue("disp_diff");
            params_.magnitude = (byte)GetConfigValue("magnitude");
            params_.radius = (ushort)GetConfigValue("radius");
            return params_;
        }

        public void SetFilterParams(SpatialAdvancedFilterParams params_)
        {
            SetConfigValue("alpha", params_.alpha);
            SetConfigValue("disp_diff", params_.disp_diff);
            SetConfigValue("magnitude", params_.magnitude);
            SetConfigValue("radius", params_.radius);
        }
    }

    public class HoleFillingFilter : Filter
    {
        internal HoleFillingFilter(IntPtr handle, string activationKey) : base(handle) { }

        public HoleFillingFilter(string activationKey = "")
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_private_filter("HoleFillingFilter", activationKey, ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }

        public void SetFilterMode(HoleFillingMode mode)
        {
            SetConfigValue("hole_filling_mode", (double)mode);
        }

        public HoleFillingMode GetFilterMode()
        {
            return (HoleFillingMode)GetConfigValue("hole_filling_mode");
        }
    }

    public class NoiseRemovalFilter : Filter
    {
        internal NoiseRemovalFilter(IntPtr handle, string activationKey) : base(handle) { }

        public NoiseRemovalFilter(string activationKey = "")
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_private_filter("NoiseRemovalFilter", activationKey, ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }

        public void SetFilterParams(NoiseRemovalFilterParams filterParams)
        {
            SetConfigValue("max_size", filterParams.max_size);
            SetConfigValue("min_diff", filterParams.disp_diff);
        }

        public NoiseRemovalFilterParams GetFilterParams()
        {
            NoiseRemovalFilterParams filterParams = new NoiseRemovalFilterParams();
            filterParams.max_size = (UInt16)GetConfigValue("max_size");
            filterParams.disp_diff = (UInt16)GetConfigValue("min_diff");
            return filterParams;
        }

        public UInt16PropertyRange GetDispDiffRange()
        {
            IntPtr error = IntPtr.Zero;
            UInt16PropertyRange range = new UInt16PropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("min_diff"))
                {
                    range = GetPropertyRange<UInt16PropertyRange>("min_diff", cur => new UInt16PropertyRange
                    {
                        cur = (ushort)cur,
                        def = (ushort)item.def,
                        max = (ushort)item.max,
                        min = (ushort)item.min,
                        step = (ushort)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public UInt16PropertyRange GetMaxSizeRange()
        {
            IntPtr error = IntPtr.Zero;
            UInt16PropertyRange range = new UInt16PropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("max_size"))
                {
                    range = GetPropertyRange<UInt16PropertyRange>("max_size", cur => new UInt16PropertyRange
                    {
                        cur = (ushort)cur,
                        def = (ushort)item.def,
                        max = (ushort)item.max,
                        min = (ushort)item.min,
                        step = (ushort)item.step
                    });
                    break;
                }
            }
            return range;
        }
    }

    public class TemporalFilter : Filter
    {
        internal TemporalFilter(IntPtr handle, string activationKey) : base(handle) { }

        public TemporalFilter(string activationKey = "")
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_private_filter("TemporalFilter", activationKey, ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }

        public FloatPropertyRange GetDiffScaleRange()
        {
            IntPtr error = IntPtr.Zero;
            FloatPropertyRange range = new FloatPropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("diff_scale"))
                {
                    range = GetPropertyRange<FloatPropertyRange>("diff_scale", cur => new FloatPropertyRange
                    {
                        cur = (float)cur,
                        def = (float)item.def,
                        max = (float)item.max,
                        min = (float)item.min,
                        step = (float)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public void SetDiffScale(float value)
        {
            SetConfigValue("diff_scale", value);
        }

        public FloatPropertyRange GetWeightRange()
        {
            IntPtr error = IntPtr.Zero;
            FloatPropertyRange range = new FloatPropertyRange();
            foreach (var item in _configSchemaList)
            {
                if (Marshal.PtrToStringAnsi(item.name).Equals("weight"))
                {
                    range = GetPropertyRange<FloatPropertyRange>("weight", cur => new FloatPropertyRange
                    {
                        cur = (float)cur,
                        def = (float)item.def,
                        max = (float)item.max,
                        min = (float)item.min,
                        step = (float)item.step
                    });
                    break;
                }
            }
            return range;
        }

        public void SetWeight(float value)
        {
            SetConfigValue("weight", value);
        }

    }

    public class DisparityTransform : Filter
    {
        internal DisparityTransform(IntPtr handle, string activationKey) : base(handle) { }

        public DisparityTransform(string activationKey = "")
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_private_filter("DisparityTransform", activationKey, ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
            Init();
        }
    }

    [Obsolete]
    public class CompressionFilter : Filter
    {
        public CompressionFilter()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_compression_filter(ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
        }

        /**
        * \if English
        * @brief Set compression params
        *
        * @param mode Compression mode OB_COMPRESSION_LOSSLESS or OB_COMPRESSION_LOSSY
        * @param params Compression params, when mode is OB_COMPRESSION_LOSSLESS, params is NULL
        * \else
        * @brief 设置压缩参数
        *
        * @param mode 压缩模式 OB_COMPRESSION_LOSSLESS or OB_COMPRESSION_LOSSY
        * @param params 压缩参数, 当mode为OB_COMPRESSION_LOSSLESS时，params为NULL
        * \endif
        */
        public void SetCompressionParams(CompressionMode mode, IntPtr param)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_compression_filter_set_compression_params(_handle.Ptr, mode, param, ref error);
            NativeException.HandleError(error);
        }
    }

    [Obsolete]
    public class DecompressionFilter : Filter
    {
        public DecompressionFilter()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_decompression_filter(ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
        }
    }

}