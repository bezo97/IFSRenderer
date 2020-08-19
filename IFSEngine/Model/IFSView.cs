using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace IFSEngine.Model
{
    public class IFSView
    {
        public double Brightness = 1.0;
        public double Gamma = 1.0;
        public double GammaThreshold = 0.0;
        public double Vibrancy = 1.0;
        public double FogEffect = 0.0;
        public double DepthOfField = 0.0;
        public double FocusDistance = 2.0;
        public double FocusArea = 0.25;
        public Camera Camera = new Camera();
        public Color BackgroundColor = Color.Black;
        public Size ImageResolution = new Size(1920,1080);
    }
}
