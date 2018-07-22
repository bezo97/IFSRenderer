using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IFSEngine;
using System.Drawing;
using System.Diagnostics;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestMethod1()
        {
            int w = 500, h = 500;
            Renderer r = new Renderer(w,h, 1000, IntPtr.Zero, -1);
            int times = 10;
            for (int i = 0; i < times; i++)
                r.Render();
            double[,][] o = r.Img(1.0f,4.0f);
            r.Dispose();


            Bitmap img = new Bitmap(o.GetLength(0), o.GetLength(1));
            for (int i = 0; i < img.Width; i++)
            {
                for (int ii = 0; ii< img.Height; ii++)
                {
                    int rr = (int)Math.Floor(o[i, ii][0] * 255);
                    rr = Math.Max(Math.Min(255, rr), 0);
                    int gg = (int)Math.Floor(o[i, ii][1] * 255);
                    gg = Math.Max(Math.Min(255, gg), 0);
                    int bb = (int)Math.Floor(o[i, ii][2] * 255);
                    bb = Math.Max(Math.Min(255, bb), 0);
                    img.SetPixel(i,ii, Color.FromArgb(255, rr,gg,bb));
                }
            }
            img.Save(@"D:\Downloads\tmp.png", System.Drawing.Imaging.ImageFormat.Png);
            Process.Start(@"D:\Downloads\tmp.png");
        }
    }
}
