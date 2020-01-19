using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Util
{
    public class IFSControllerPipeServer
    {
        public EventHandler<Quaternion> OrientationDataReceived;
        public EventHandler<Vector3> AccelerationDataReceived;

        private static IFSControllerPipeServer _instance;
        public static IFSControllerPipeServer Instance => _instance ?? (_instance = new IFSControllerPipeServer());
        private NamedPipeServerStream orientationPipe;
        private NamedPipeServerStream accelerationPipe;


        private IFSControllerPipeServer()
        {
            orientationPipe = new NamedPipeServerStream("IFSControllerOrientationPipe", PipeDirection.In);
            accelerationPipe = new NamedPipeServerStream("IFSControllerAccelerationPipe", PipeDirection.In);

            Task.Run(() => orientationPipe.WaitForConnectionAsync().ContinueWith(OrientationClientConnectedCallback));
            Task.Run(() => accelerationPipe.WaitForConnectionAsync().ContinueWith(AccelerationClientConnectedCallback));
        }

        private void OrientationClientConnectedCallback(IAsyncResult r)
        {
            byte[] b = new byte[4*sizeof(float)];
            float[] f = new float[4];
            while (orientationPipe.IsConnected)
            {
                orientationPipe.Read(b, 0, b.Length);
                Buffer.BlockCopy(b, 0, f, 0, b.Length);
                Console.WriteLine(String.Join(", ", f));
                OrientationDataReceived?.Invoke(this, new Quaternion(f[0],f[1],f[2],f[3]));
            }
        }

        private void AccelerationClientConnectedCallback(IAsyncResult r)
        {
            byte[] b = new byte[3 * sizeof(float)];
            float[] f = new float[3];
            while (accelerationPipe.IsConnected)
            {
                accelerationPipe.Read(b, 0, b.Length);
                Buffer.BlockCopy(b, 0, f, 0, b.Length);
                Console.WriteLine(String.Join(", ", f));
                AccelerationDataReceived?.Invoke(this, new Vector3(f[0], f[1], f[2]));
            }
        }

    }
}
