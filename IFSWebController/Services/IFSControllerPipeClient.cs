using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace IFSWebController.Services
{
    public class IFSControllerPipeClient
    {
        private static IFSControllerPipeClient _instance;
        public static IFSControllerPipeClient Instance => _instance ?? (_instance = new IFSControllerPipeClient());
        private NamedPipeClientStream orientationPipe;
        private NamedPipeClientStream accelerationPipe;

        private IFSControllerPipeClient()
        {
            orientationPipe = new NamedPipeClientStream(".", "IFSControllerOrientationPipe", PipeDirection.Out);
            accelerationPipe = new NamedPipeClientStream(".", "IFSControllerAccelerationPipe", PipeDirection.Out);
            Task.Run(() =>
            {
                while (!orientationPipe.IsConnected)
                    try {
                        orientationPipe.Connect(3000);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("IFSControllerOrientationPipe connect timeout");
                    }
            });
            Task.Run(() =>
            {
                while (!accelerationPipe.IsConnected)
                    try
                    {
                        accelerationPipe.Connect(3000);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("IFSControllerAccelerationPipePipe connect timeout");
                    }
            });
        }

        public void WriteOrientationStream(IEnumerable<float> a)
        {
            byte[] b = new byte[4 * sizeof(float)];
            Buffer.BlockCopy(a.ToArray(), 0, b, 0, b.Length);
            if (orientationPipe.IsConnected)
                orientationPipe.WriteAsync(b, 0, b.Length);
        }

        public void WriteAccelerationStream(IEnumerable<float> a)
        {
            byte[] b = new byte[3 * sizeof(float)];
            Buffer.BlockCopy(a.ToArray(), 0, b, 0, b.Length);
            if (accelerationPipe.IsConnected)
                accelerationPipe.WriteAsync(b, 0, b.Length);
        }


    }
}
