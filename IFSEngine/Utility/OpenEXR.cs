using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IFSEngine.Utility
{
    /// <summary>
    /// Helper class for the EXR hdr image file format.
    /// </summary>
    /// <remarks>
    /// Implementation based on OpenEXR documentation:
    /// <a href="https://www.openexr.com/documentation/TechnicalIntroduction.pdf">Technical docs</a>,
    /// <a href="https://www.openexr.com/documentation/openexrfilelayout.pdf">File layout</a>
    /// </remarks>
    public static class OpenEXR
    {
        /// <summary>
        /// Writes the given 4-channel image data to the specified stream in EXR format.
        /// Only a small subset of the specification is implemented:
        /// <list type="bullet">
        /// <item> 4 channel (RGBA), 32 bit floating-point pixel data </item>
        /// <item> No compression </item>
        /// <item> Regular single-part, scan line format</item>
        /// </list>
        /// </summary>
        /// <param name="stream">An empty stream where the exr file is written to.</param>
        /// <param name="data">Image data must be passed in this specific format: [y, x, rgba].</param>
        public static void WriteStream(Stream stream, float[,,] data)
        {
            int histogramHeight = data.GetLength(0);
            int histogramWidth = data.GetLength(1);
            const int chnum = 4;

            using (var bw = new BinaryWriter(stream))
            {
                bw.Write(20000630);//openexr "magic number"
                //version field
                bw.Write((byte)2);//file format version
                bw.Write((byte)0);//regular single-part scan line file
                bw.Write((byte)0);//unused
                bw.Write((byte)0);//unused
                //header (sequence of attributes)
                //attribute:
                //name (string0)
                //type (string0)
                //size in bytes (int)
                //value
                {
                    bw.WriteString0("channels");
                    bw.WriteString0("chlist");
                    const int chlist_sizeinbytes = (2 + 4 + 1 + 3 + 4 + 4) * chnum + 1;
                    bw.Write(chlist_sizeinbytes);//size in bytes
                    {
                        //A channel
                        bw.WriteString0("A");//channel name
                        bw.Write(2);//type: 32-bit float
                        bw.Write((byte)0);//pLinear
                        bw.Write((byte)0);//reserved
                        bw.Write((byte)0);
                        bw.Write((byte)0);
                        bw.Write(1);//xSampling (1: channel contains data for every pixel)
                        bw.Write(1);//ySampling (2: channel contains data for every pixel)
                    }
                    {
                        //B channel
                        bw.WriteString0("B");//channel name
                        bw.Write(2);//type: 32-bit float
                        bw.Write((byte)0);//pLinear
                        bw.Write((byte)0);//reserved
                        bw.Write((byte)0);
                        bw.Write((byte)0);
                        bw.Write(1);//xSampling (1: channel contains data for every pixel)
                        bw.Write(1);//ySampling (2: channel contains data for every pixel)
                    }
                    {
                        //G channel
                        bw.WriteString0("G");//channel name
                        bw.Write(2);//type: 32-bit float
                        bw.Write((byte)0);//pLinear
                        bw.Write((byte)0);//reserved
                        bw.Write((byte)0);
                        bw.Write((byte)0);
                        bw.Write(1);//xSampling (1: channel contains data for every pixel)
                        bw.Write(1);//ySampling (2: channel contains data for every pixel)
                    }
                    {
                        //R channel
                        bw.WriteString0("R");//channel name
                        bw.Write(2);//type: 32-bit float
                        bw.Write((byte)0);//pLinear
                        bw.Write((byte)0);//reserved
                        bw.Write((byte)0);
                        bw.Write((byte)0);
                        bw.Write(1);//xSampling (1: channel contains data for every pixel)
                        bw.Write(1);//ySampling (2: channel contains data for every pixel)
                    }
                    bw.Write((byte)0);//null terminator for chlist attribute value
                }
                {
                    bw.WriteString0("compression");//attribute name
                    bw.WriteString0("compression");//attribute type
                    bw.Write(1);//(int) attribute size in bytes
                    bw.Write((byte)0);//no compression
                }
                {
                    bw.WriteString0("dataWindow");//attribute name
                    bw.WriteString0("box2i");//attribute type
                    bw.Write(4 * 4);//4 ints
                    bw.Write(0);//xmin
                    bw.Write(0);//ymin
                    bw.Write(histogramWidth - 1);//xmax
                    bw.Write(histogramHeight - 1);//ymax
                }
                {
                    bw.WriteString0("displayWindow");
                    bw.WriteString0("box2i");
                    bw.Write(4 * 4);//4 ints
                    bw.Write(0);//xmin
                    bw.Write(0);//ymin
                    bw.Write(histogramWidth - 1);//xmax
                    bw.Write(histogramHeight - 1);//ymax
                }
                {
                    bw.WriteString0("lineOrder");
                    bw.WriteString0("lineOrder");
                    bw.Write(1);//(int) attribute size in bytes
                    bw.Write((byte)0);//INCREASING_Y
                }
                {
                    bw.WriteString0("pixelAspectRatio");//attribute name
                    bw.WriteString0("float");//attribute type
                    bw.Write(4);//(int) attribute size in bytes
                    bw.Write(1.0f);//value
                }
                {
                    bw.WriteString0("screenWindowCenter");//attribute name
                    bw.WriteString0("v2f");//attribute type
                    bw.Write(2 * 4);//(int) attribute size in bytes
                    bw.Write(0.0f);//value
                    bw.Write(0.0f);//value
                }
                {
                    bw.WriteString0("screenWindowWidth");//attribute name
                    bw.WriteString0("float");//attribute type
                    bw.Write(4);//(int) attribute size in bytes
                    bw.Write(1.0f);//value
                }
                bw.Write((byte)0);//supposed to be omitted??
                //Offset tables
                ulong headerLength = (ulong)stream.Length;//number of written bytes until this point
                int scanlineDataLength = chnum * histogramWidth * 4;
                int scanlineBlockLength = 2 * 4 + scanlineDataLength;
                int offsetTableLength = histogramHeight * 8;
                //sequence of offsets 
                //offset (ulong): distance in bytes from the start of the file
                //1 offset for each scan line
                for (int scanline = 0; scanline < histogramHeight; scanline++)
                    bw.Write(headerLength + (ulong)offsetTableLength + (ulong)scanline * (ulong)scanlineBlockLength);
                //Pixel data
                for (int scanline = 0; scanline < histogramHeight; scanline++)
                {
                    bw.Write(scanline);//(int) y coordinate of the scanline block
                    bw.Write(scanlineDataLength);//(int) data size in bytes
                    //channels must be written in alphabetical order
                    //channel A
                    for (int x = 0; x < histogramWidth; x++)
                        bw.Write(data[scanline, x, 3]);
                    //channel B
                    for (int x = 0; x < histogramWidth; x++)
                        bw.Write(data[scanline, x, 2]);
                    //channel G
                    for (int x = 0; x < histogramWidth; x++)
                        bw.Write(data[scanline, x, 1]);
                    //channel R
                    for (int x = 0; x < histogramWidth; x++)
                        bw.Write(data[scanline, x, 0]);
                }
            }
        }

        private static void WriteString0(this BinaryWriter bw, string str)
        {
            bw.Write(Encoding.ASCII.GetBytes(str));
            bw.Write((byte)0);//null terminator
        }

    }
}
