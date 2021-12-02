using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace IFSEngine.Model
{
    /// <summary>
    /// Traditional 1D flame fractal color palette. *.ugr / *.gradient
    /// </summary>
    public class FlamePalette
    {
        public string Name { get; set; } = "Empty Palette";
        public List<Vector4> Colors { get; set; } = new List<Vector4>();

        public static FlamePalette Default { get; } = new FlamePalette
        {
            Name = "Default Palette",
            Colors = new List<Vector4>
            {
                new Vector4(1,1,1,1),
                new Vector4(1,1,1,1)
            }
        };

        /// <summary>
        /// Parse *.gradient files. Compatible with ChaosHelper.
        /// </summary>
        public static async Task<List<FlamePalette>> FromFileAsync(string filePath)
        {
            List<FlamePalette> palettes = new List<FlamePalette>();
            int state = 0;
            FlamePalette tmp = new FlamePalette();
            using (StreamReader sr = new StreamReader(filePath))
            {
                while (!sr.EndOfStream)
                {
                    string sor = await sr.ReadLineAsync();
                    switch (state)
                    {
                        case 0://new palette
                            if (sor.Contains("title"))
                            {
                                tmp = new FlamePalette
                                {
                                    Name = sor.Split('\"')[1]
                                };
                                state = 1;
                            }
                            break;
                        case 1://colors
                            if (sor.Contains("index"))
                            {
                                int dec = int.Parse(sor.Split('=')[2]);
                                byte[] bytes = BitConverter.GetBytes(dec);
                                Vector4 s1 = new Vector4(bytes[0] / 255.0f, bytes[1] / 255.0f, bytes[2] / 255.0f, 1.0f);
                                tmp.Colors.Add(s1);
                            }
                            else//end of palette
                            {
                                palettes.Add(tmp);
                                tmp = null;
                                state = 0;
                            }
                            break;
                    }
                }
            }
            return palettes;
        }

    }
}
