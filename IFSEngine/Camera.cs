using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine
{

    public class Camera
    {
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public float FogEffect { get; set; } = 2.0f;

        internal CameraParameters Params = new CameraParameters(){ ox = 0.0f, oy = -2.0f, oz = 0.0f, focallength = 0.65f, theta = 3.1415926f/2.0f, phi = 3.1415926f / 2.0f, focusdistance = 2.0f, dof_effect = 0.01f };

public float Theta {
    get => Params.theta;
    set {
        Params.theta = value % (3.141592f);
        Params.sin_theta = (float)Math.Sin(Params.theta);
        Params.cos_theta = (float)Math.Cos(Params.theta);
        getDir(Params.theta, Params.phi, out Params.dx, out Params.dy, out Params.dz);
    }
}
public float Phi {
    get => Params.phi;
    set {
        Params.phi = value % (2.0f * 3.141592f);
        Params.sin_phi = (float)Math.Sin(Params.phi);
        Params.cos_phi = (float)Math.Cos(Params.phi);
        Params.sin_phi_hack = (float)Math.Sin(Params.phi - 3.1415926f / 2.0f);
        Params.cos_phi_hack = (float)Math.Cos(Params.phi - 3.1415926f / 2.0f);
        getDir(Params.theta, Params.phi, out Params.dx, out Params.dy, out Params.dz);
    }
}
public (float x,float y,float z) Pos { get => (Params.ox, Params.oy, Params.oz); set  { Params.ox = value.x; Params.oy = value.y; Params.oz = value.z; } }

public float FocalLength { get => Params.focallength; set => Params.focallength = value; }
public float FocusDistance { get => Params.focusdistance; set => Params.focusdistance = value; }
public float DepthOfField { get => Params.dof_effect; set => Params.dof_effect = value; }

public void SetDirection(float x, float y, float z)
{
    Phi = (float)Math.Atan(y / x);
    Theta = (float)Math.Acos(z);
}

public Camera()
{
    Phi = Params.phi;
    Theta = Params.theta;
}
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

public (float dx, float dy, float dz) GetDirection()
{
    getDir(Theta, Phi, out float dx, out float dy, out float dz);
    return (dx,dy,dz);
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
    getDir(3.1415926f / 2.0f, Params.phi - 3.1415926f / 2.0f, out rx, out ry, out rz);//???
    //Normalize(ref rx, ref ry, ref rz);
}

private void getUp(out float dx, out float dy, out float dz)
{
    getDir(Params.theta - 3.1415926f / 2.0f, Params.phi, out dx, out dy, out dz);
    //Normalize(ref dx, ref dy, ref dz);
    /*dx *= -1;
    dy *= -1;
    dz *= -1;*/
    }

    public void Translate(float forward/*z*/, float strafe/*x*/, float height/*y*/)
        {
            getDir(Params.theta, Params.phi, out float fx, out float fy, out float fz);//forward
            getUp(out float ux, out float uy, out float uz);//up
            getRight(out float rx, out float ry, out float rz);//right
            float tx = fx * forward + rx * strafe + ux * height;
            float ty = fy * forward + ry * strafe + uy * height;
            float tz = fz * forward + rz * strafe + uz * height;
            Params.ox += tx;
            Params.oy += ty;
            Params.oz += tz;
        }

    }
}
