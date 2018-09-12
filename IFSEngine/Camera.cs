using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine
{
    internal struct CameraSettings
    {
        internal float ox;//pos
        internal float oy;
        internal float oz;

        internal float dx;//dir
        internal float dy;
        internal float dz;

        //dir
        internal float theta;//0-pi
        internal float phi;//0-2pi
        //helpers
        internal float sin_theta;
        internal float cos_theta;
        internal float sin_phi;
        internal float cos_phi;
        internal float sin_phi_hack;
        internal float cos_phi_hack;

        internal float focallength;//for perspective
        
        internal float focusdistance;//for dof
        internal float dof_effect;
    }

    public class Camera
    {
        internal CameraSettings Settings = new CameraSettings(){ ox = 0.0f, oy = -5.0f, oz = 0.0f, focallength = 0.65f, theta = 3.1415926f/2.0f, phi = 3.1415926f / 2.0f, focusdistance = 0.1f, dof_effect = 0.01f };

        public float Theta {
            get => Settings.theta;
            set {
                Settings.theta = value % 3.141592f;
                Settings.sin_theta = (float)Math.Sin(Settings.theta);
                Settings.cos_theta = (float)Math.Cos(Settings.theta);
                getDir(Settings.theta, Settings.phi, out Settings.dx, out Settings.dy, out Settings.dz);
            }
        }
        public float Phi {
            get => Settings.phi;
            set {
                Settings.phi = value % (2.0f * 3.141592f);
                Settings.sin_phi = (float)Math.Sin(Settings.phi);
                Settings.cos_phi = (float)Math.Cos(Settings.phi);
                Settings.sin_phi_hack = (float)Math.Sin(Settings.phi - 3.1415926f / 2.0f);
                Settings.cos_phi_hack = (float)Math.Cos(Settings.phi - 3.1415926f / 2.0f);
                getDir(Settings.theta, Settings.phi, out Settings.dx, out Settings.dy, out Settings.dz);
            }
        }
        public (float x,float y,float z) Pos { get => (Settings.ox, Settings.oy, Settings.oz); set  { Settings.ox = value.x; Settings.oy = value.y; Settings.oz = value.z; } }

        public float FocalLength { get => Settings.focallength; set => Settings.focallength = value; }
        public float FocusDistance { get => Settings.focusdistance; set => Settings.focusdistance = value; }
        public float DepthOfField { get => Settings.dof_effect; set => Settings.dof_effect = value; }

        public Camera() { }
        public Camera((float x, float y, float z) position, (float x, float y, float z) direction)
        {
            Normalize(ref direction.x, ref direction.y, ref direction.z);
            Theta = (float)Math.Acos(direction.y);
            Phi = (float)Math.Atan2(direction.z, direction.x);
            Pos = position;
        }

        private void getDir(float t, float p, out float dx, out float dy, out float dz)
        {
            dx = (float)(Math.Sin(t) * Math.Cos(p));
            dy = (float)(Math.Sin(t) * Math.Sin(p));
            dz = (float) Math.Cos(t);
            Normalize(ref dx, ref dy, ref dz);
        }

        private void Normalize(ref float x, ref float y, ref float z)
        {
            float l = (float)Math.Sqrt(x * x + y * y + z * z);
            x /= l;
            y /= l;
            z /= l;
        }

        private void getRight(out float rx, out float ry, out float rz)
        {
            getDir(3.1415926f / 2.0f, Settings.phi - 3.1415926f / 2.0f, out rx, out ry, out rz);//???
            //Normalize(ref rx, ref ry, ref rz);
        }

        private void getUp(out float dx, out float dy, out float dz)
        {
            getDir(Settings.theta - 3.1415926f / 2.0f, Settings.phi, out dx, out dy, out dz);
            //Normalize(ref dx, ref dy, ref dz);
            /*dx *= -1;
            dy *= -1;
            dz *= -1;*/
        }

        public void Translate(float forward/*z*/, float strafe/*x*/, float height/*y*/)
        {
            getDir(Settings.theta, Settings.phi, out float fx, out float fy, out float fz);//forward
            getUp(out float ux, out float uy, out float uz);//up
            getRight(out float rx, out float ry, out float rz);//right
            float tx = fx * forward + rx * strafe + ux * height;
            float ty = fy * forward + ry * strafe + uy * height;
            float tz = fz * forward + rz * strafe + uz * height;
            Settings.ox += tx;
            Settings.oy += ty;
            Settings.oz += tz;
        }

    }
}
