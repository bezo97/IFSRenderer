using IFSWebController.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IFSWebController.Hubs
{
    public class IFSControllerHub : Hub
    {
        public async Task UploadStreamOrientation(IAsyncEnumerable<IEnumerable<float>> stream)
        {
            await foreach (var item in stream)
            {
                Console.WriteLine(String.Join(", ", item));
                IFSControllerPipeClient.Instance.WriteOrientationStream(item);
            }
        }

        public async Task UploadStreamAcceleration(IAsyncEnumerable<IEnumerable<float>> stream)
        {
            await foreach (var item in stream)
            {
                Console.WriteLine(String.Join(", ", item));
                IFSControllerPipeClient.Instance.WriteAccelerationStream(item);
            }
        }
    }
}
