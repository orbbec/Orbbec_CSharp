using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace Orbbec
{
    public class CoordinateTransformHelper
    {
        public static bool Transformation2dto2d(Point2f sourcePixel, float depthValue, CameraIntrinsic sourceIntrinsic,
            CameraDistortion sourceDistortion, CameraIntrinsic targetIntrinsic, CameraDistortion targetDistortion,
            Extrinsic extrinsicD2C, ref Point2f targetPixel)
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_transformation_2d_to_2d(sourcePixel, depthValue, sourceIntrinsic, sourceDistortion,
                targetIntrinsic, targetDistortion, extrinsicD2C, ref targetPixel, ref error);
            NativeException.HandleError(error);
            return result;
        }

        public static bool Transformation2dto3d(Point2f sourcePixel, float depthValue, CameraIntrinsic sourceIntrinsic,
            Extrinsic extrinsicD2C, ref Point3f targetPixel)
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_transformation_2d_to_3d(sourcePixel, depthValue, sourceIntrinsic, 
                extrinsicD2C, ref targetPixel, ref error);
            NativeException.HandleError(error);
            return result;
        }

        public static bool Transformation3dto3d(Point3f sourcePixel, Extrinsic extrinsicD2C, ref Point3f targetPixel)
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_transformation_3d_to_3d(sourcePixel, extrinsicD2C, ref targetPixel, ref error);
            NativeException.HandleError(error);
            return result;
        }

        public static bool Transformation3dto2d(Point3f sourcePixel, CameraIntrinsic sourceIntrinsic, CameraDistortion sourceDistortion,
            Extrinsic extrinsicD2C, ref Point2f targetPixel)
        {
            IntPtr error = IntPtr.Zero;
            bool result = obNative.ob_transformation_3d_to_2d(sourcePixel, sourceIntrinsic, sourceDistortion,
                extrinsicD2C, ref targetPixel, ref error);
            NativeException.HandleError(error);
            return result;
        }
    }
}
