# Orbbec SDK C# Wrapper

This is a wrapper for the Orbbec SDK for C#. It provides a simple and easy-to-use interface for accessing the Orbbec camera and performing various operations such as capturing depth images, color images, and performing point cloud generation.

The v2-main branch, C# Wrapper is based the open source version of [Orbbec SDK v2](https://github.com/orbbec/OrbbecSDK_v2). The C# wrapper for the open-source [Orbbec SDK v2](https://github.com/orbbec/OrbbecSDK_v2) is versioned as v2.x.x, while the C# wrapper for the [Orbbec SDK v1](https://github.com/orbbec/OrbbecSDK) is versioned as v1.x.x.

# Support Platforms
- Windows: Windows 10 (x64)

# Platform support
| **Products List** | **Firmware Version**        |
|-------------------|-----------------------------|
| Gemini 335        | 1.2.20                   |
| Gemini 335L        | 1.2.20                    |
| Gemini 336        | 1.2.20                      |
| Gemini 336L        | 1.2.20                    |
| Femto Bolt        | 1.0.6/1.0.9                  |
| Femto Mega        | 1.1.7/1.2.7                  |


# Compile and run sample
Here, it is assumed that you have installed Cmake correctly. If you have not installed Cmake, you can refer to the Cmake official website for installation.
- Open Cmake, set the source code path, and set the “build” folder as the path for generating binary files, as shown in the following figure.

- First, click Configure, then select x64 in the dialog box that appears, and finally click Finish.
![compile-1](image/compile-1.png)

- Click “Generate”, as shown below:
![compile-2](image/compile-2.png)

- Use the file explorer to directly start the Visual Studio project in the build directory, as shown in the following figure:
![compile-3](image/compile-3.png)

- Open the project, as shown below, First, select Release, then choose ALL BUILD to compile, and finally select INSTALL.
![compile-4](image/compile-4.png)

- The compiled sample is located in the build/install/bin directory. 
![compile-5](image/compile-5.png)
- Click on 0.basic.quick_start.exe, and the running result is as follows:
![compile-6](image/compile-6.png)

# Sample Features
| Example                | Description              |      level              |
| --------------------- | ------------------------ |----------------|
| 0.basic.quick_start | Quickly use the camera to capture color and depth video streams.|  ⭐   |
| 1.stream.color | Displays the color stream from the camera.|    ⭐   |
| 1.stream.depth | Displays the depth stream from the camera.|    ⭐   |
| 1.stream.imu | Demonstrates how to read IMU data.|   ⭐   |
| 1.stream.infrared | Displays the infrared stream from the camera.|   ⭐   |
| 1.stream.multi_streams | Use SDK to obtain multiple camera data streams and output them.|      ⭐   |
| 2.device.control | The SDK can be used to modify camera-related parameters, including laser switch, laser level intensity, white balance switch, etc.|   ⭐⭐  |
| 2.device.firmware_update | This sample demonstrates how to read a firmware file to perform firmware upgrades on the device.|   ⭐⭐    |
| 2.device.hot_plugin | Demonstrates how to detect hot plug events.| ⭐⭐    |
| 3.advanced.coordinate_transform | Use the SDK interface to transform different coordinate systems.| ⭐⭐⭐    |
| 3.advanced.hw_d2c_align | Demonstrates how to use hardware D2C.|  ⭐⭐⭐    |
| 3.advanced.multi_devices | Demonstrates how to use multiple devices.| ⭐⭐⭐    | 
| 3.advanced.point_cloud | Demonstrates how to save the point cloud to disk using a point cloud filter.| ⭐⭐⭐    |
| 3.advanced.post_processing | Demonstrates how to use post-processing filters.| ⭐⭐⭐    |
                                                                               

