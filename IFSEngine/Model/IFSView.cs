using IFSEngine.Model.Camera;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Model
{
    public class IFSView
    {
        public CameraBase Camera { get; set; } = new QuatCamera();
        //public float Width { get; set; } = 1920;
        //public float Height { get; set; } = 1080; //FOV miatt egyelore kameraban maradt
        public float Brightness { get; set; } = 1.0f;
        public float Gamma { get; set; } = 4.0f;
        public float FogEffect { get; set; } = 2.0f;
        public float Dof { get; set; } = 0.05f;
        public float FocusDistance { get; set; } = 2.0f;
        public float FocusArea { get; set; } = 0.25f;

        public void ResetCamera()
        {
            //HACK: ameddig kameraban van a resolution, megjegyezzuk
            int w = Camera.Width;
            int h = Camera.Height;

            Camera = new QuatCamera();

            Camera.Width = w;
            Camera.Height = h;
        }
    }
}
