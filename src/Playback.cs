using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Orbbec
{
    public delegate void PlaybackCallback(Frame frame);
    public delegate void MediaStateCallback(MediaState state);

    public class Playback : IDisposable
    {
        private NativeHandle _handle;
        private PlaybackCallback _playbackCallback;
        private NativePlaybackCallback _nativePlaybackCallback;
        private MediaStateCallback _mediaStateCallback;
        private NativeMediaStateCallback _nativeMediaStateCallback;

        private void OnPlayback(IntPtr framePtr, IntPtr userData)
        {
            Frame frame = new Frame(framePtr);
            if(_playbackCallback != null)
            {
                _playbackCallback(frame);
            }
            else
            {
                frame.Dispose();
            }
        }

        private void OnMediaState(MediaState state, IntPtr userData)
        {
            if(_nativePlaybackCallback != null)
            {
                _mediaStateCallback(state);
            }
        }

        internal Playback(IntPtr handle)
        {
            _handle = new NativeHandle(handle, Delete);
            _nativePlaybackCallback = new NativePlaybackCallback(OnPlayback);
            _nativeMediaStateCallback = new NativeMediaStateCallback(OnMediaState);
        }

        internal NativeHandle GetNativeHandle()
        {
            return _handle;
        }

        /**
        * \if English
        * @brief Create playback object
        * @param filename Playback filename
        * \else
        * @brief 创建回放对象
        * @param filename 回放的文件名
        * \endif
        */
        public Playback(String fileName)
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_create_playback(fileName, ref error);
            NativeException.HandleError(error);
            _handle = new NativeHandle(handle, Delete);
        }

        /**
        * \if English
        * @brief Start playback, playback data is returned from the callback
        *
        * @param callback Callback for playback data
        * @param user_data User data
        * @param type Type of playback data
        * \else
        * @brief 开启回放，回放数据从回调中返回
        *
        * @param callback 回放数据的回调
        * @param user_data 用户数据
        * @param type 回放数据的类型
        * \endif
        */
        public void Start(PlaybackCallback callback, MediaType mediaType)
        {
            _playbackCallback = callback;
            IntPtr error = IntPtr.Zero;
            obNative.ob_playback_start(_handle.Ptr, _nativePlaybackCallback, IntPtr.Zero, mediaType, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief stop playback
        * \else
        * @brief 停止回放
        * \endif
        */
        public void Stop()
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_playback_stop(_handle.Ptr, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Set playback state
        * @param state playback status callback
        * \else
        * @brief 设置回放状态
        * @param state 回放状态回调
        * \endif
        */
        public void SetPlaybackStateCallback(MediaStateCallback callback)
        {
            _mediaStateCallback = callback;
            IntPtr error = IntPtr.Zero;
            obNative.ob_set_playback_state_callback(_handle.Ptr, _nativeMediaStateCallback, IntPtr.Zero, ref error);
            NativeException.HandleError(error);
        }

        /**
        * \if English
        * @brief Get the device information in the recording file
        *
        * @return DeviceInfo returns device information
        * \else
        * @brief 获取录制文件内的设备信息
        *
        * @return DeviceInfo returns device information
        * \endif
        */
        public DeviceInfo GetDeviceInfo()
        {
            IntPtr error = IntPtr.Zero;
            IntPtr handle = obNative.ob_playback_get_device_info(_handle.Ptr, ref error);
            NativeException.HandleError(error);
            return new DeviceInfo(handle);
        }

        /**
        * \if English
        * @brief Get the intrinsic and extrinsic parameter information in the recording file
        *
        * @return CameraParam returns internal and external parameter information
        * \else
        * @brief 获取录制文件内的内外参信息
        *
        * @return CameraParam 返回的内外参信息
        * \endif
        */
        public CameraParam GetCameraParam()
        {
            IntPtr error = IntPtr.Zero;
            CameraParam cameraParam;
            obNative.ob_playback_get_camera_param(out cameraParam, _handle.Ptr, ref error);
            NativeException.HandleError(error);
            return cameraParam;
        }

        internal void Delete(IntPtr handle)
        {
            IntPtr error = IntPtr.Zero;
            obNative.ob_delete_playback(handle, ref error);
            NativeException.HandleError(error);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}