using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IFSEngine.Model
{
    /// <summary>
    /// Traditional 1D Ultra Fractal color palette. *.ugr / *.gradient
    /// </summary>
    public class UFPalette
    {
        public string Name { get; set; } = "Empty Palette";
        public List<Vector4> Colors { get; set; } = new List<Vector4>();

        public static UFPalette Default
        {
            get => new UFPalette
            {
                Name = "Default Palette",
                Colors = new List<Vector4>
                {
                    new Vector4(1,0,0,1),
                    new Vector4(1,1,0,1),
                    new Vector4(0,0,1,1),
                    new Vector4(1,0,0,1)
                }
            };
        }

        /// <summary>
        /// Parse *.gradient files. Compatible with ChaosHelper.
        /// </summary>
        public static List<UFPalette> FromFile(string filePath)
        {
            List<UFPalette> palettes = new List<UFPalette>();
            int state = 0;
            UFPalette tmp = new UFPalette();
            using (StreamReader sr = new StreamReader(filePath))
            {
                while (!sr.EndOfStream)
                {
                    string sor = sr.ReadLine();
                    switch (state)
                    {
                        case 0://new palette
                            if (sor.Contains("title"))
                            {
                                tmp = new UFPalette
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
                                Byte[] bytes = BitConverter.GetBytes(dec);
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
