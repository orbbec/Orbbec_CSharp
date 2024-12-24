using System;

namespace Orbbec
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run();
        }

        public void Run()
        {
            Pipeline pipeline = null;
            try
            {
                pipeline = new Pipeline();
                Config config = new Config();
                pipeline.Start(config);
                var profile_list = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_COLOR);
                // Get color_profile
                var color_profile = profile_list.GetVideoStreamProfile(0,0, Format.OB_FORMAT_UNKNOWN,0);
                profile_list = pipeline.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH);
                //Get depth_profile
                var depth_profile = profile_list.GetVideoStreamProfile(0, 0, Format.OB_FORMAT_UNKNOWN, 0);
                //Get external parameters
                var extrinsic = depth_profile.GetExtrinsicTo(color_profile);
                Console.WriteLine($"extrinsic={extrinsic}");
                //Get depth inernal parameters
                var depth_intrinsics = depth_profile.GetIntrinsic();
                Console.WriteLine($"depth_intrinsics={depth_intrinsics}");
                //Get depth distortion parameter
                var depth_distortion = depth_profile.GetDistortion();
                Console.WriteLine($"depth_distortion={depth_distortion}");
                //Get color internala parameters
                var color_intrinsics = color_profile.GetIntrinsic();
                Console.WriteLine($"color_intrinsics.cx={color_intrinsics.cx},color_intrinsics.cy={color_intrinsics.cy}," +
                    $"color_intrinsics.fx={color_intrinsics.fx},color_intrinsics.fy={color_intrinsics.fy},");
                //Get color distortion parameter
                var color_distortion = color_profile.GetDistortion();
                Console.WriteLine($"color_distortion={color_distortion}");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.Exit(-1);
            }
            finally
            {
                if (pipeline != null)
                {
                    pipeline.Stop();
                    pipeline.Dispose();
                }
            }
        }



    }
}