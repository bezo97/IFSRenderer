using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WpfDisplay.Models;
using System.Windows;
using IFSEngine.Rendering;

namespace WpfDisplay.Helper;

public static class BitmapHelper
{
    /// <summary>
    /// Converts the raw pixel data into a BitmapSource with WPF specific calls.
    /// The Alpha channel is optionally removed and the image is flipped vertically, as required by CopyPixelDataToBitmap()
    /// </summary>
    /// <returns></returns>
    public static async Task<BitmapSource> GetExportBitmapSource(this RendererGL renderer, bool transparentBackground)
    {
        BitmapSource bs;
        WriteableBitmap wbm = new WriteableBitmap(renderer.HistogramWidth, renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
        await renderer.CopyPixelDataToBitmap(wbm.BackBuffer);
        wbm.Freeze();
        bs = wbm;
        if (!transparentBackground)
        {//option to remove alpha channel
            var fcb = new FormatConvertedBitmap(wbm, PixelFormats.Bgr32, null, 0);
            fcb.Freeze();
            bs = fcb;
        }
        //flip vertically
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {//This transformation must happen on ui thread
            var tb = new TransformedBitmap();
            tb.BeginInit();
            tb.Source = bs;
            tb.Transform = new ScaleTransform(1, -1, 0, 0);
            tb.EndInit();
            tb.Freeze();
            bs = tb;
        });
        return bs;
    }
}
