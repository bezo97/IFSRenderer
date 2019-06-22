//using Leap;
//using System;

//namespace GLDisplay
//{
//    public static class Extensions
//    {
//        public static LeapQuaternion Inverse(this LeapQuaternion q)
//        {
//            var c = q.Conjugate();
//            var sum = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
//            return new LeapQuaternion(c.x / sum, c.y / sum, c.z / sum, c.w / sum);
//        }

//        public static LeapQuaternion Conjugate(this LeapQuaternion q)
//        {
//            return new LeapQuaternion(q.x, -q.y, -q.z, -q.w);
//        }

//        public static (float roll, float pitch, float yaw) GetRotations(this LeapQuaternion q)
//        {
//            float roll, pitch, yaw;

//            float sinr_cosp = +2.0f * (q.w * q.x + q.y * q.z);
//            float cosr_cosp = +1.0f - 2.0f * (q.x * q.x + q.y * q.y);
//            roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);


//            float CopySign(float cx, float cy)
//            {
//                if ((cx < 0 && cy > 0) || (cx > 0 && cy < 0))
//                    return -cx;
//                return cx;
//            }

//            // pitch (y-axis rotation)
//            float sinp = +2.0f * (q.w * q.y - q.z * q.x);
//            if (Math.Abs(sinp) >= 1)
//                pitch = CopySign(3.141592f / 2, sinp); // use 90 degrees if out of range
//            else
//                pitch = (float)Math.Asin(sinp);

//            // yaw (z-axis rotation)
//            float siny_cosp = +2.0f * (q.w * q.z + q.x * q.y);
//            float cosy_cosp = +1.0f - 2.0f * (q.y * q.y + q.z * q.z);
//            yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

//            return (roll, pitch, yaw);
//        }
//    }
//}