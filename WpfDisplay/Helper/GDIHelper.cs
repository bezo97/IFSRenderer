using IFSEngine.Rendering;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime;
using System.Threading.Tasks;

namespace WpfDisplay.Helper
{
    class GDIHelper
    {
        /// <summary>
        /// Sample code showing how to save the render output with gdi. 
        /// Currently not in use
        /// </summary>
        public static async Task SaveImageWithGDI(RendererGL renderer)
        {
            Bitmap b = null;
            Task genTask = Task.Run(async () =>
            {
                b = new Bitmap(renderer.HistogramWidth, renderer.HistogramHeight);

                var bits = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                await renderer.CopyPixelDataToBitmap(bits.Scan0);
                b.UnlockBits(bits);

                b.RotateFlip(RotateFlipType.RotateNoneFlipY);
                //TODO: option to remove alpha channel

            });

            if (DialogHelper.ShowExportImageDialog(renderer.LoadedParams.Title, out string path))
            {
                await genTask;
                b.Save(path, ImageFormat.Png);
            }
            b.Dispose();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }
    }
}
