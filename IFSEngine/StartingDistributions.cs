using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFSEngine
{
    /// <summary>
    /// Helper class for generating common volume distributions to start IFS
    /// </summary>
    public static class StartingDistributions
    {
        /// <summary>
        /// Generates a uniform distibution of points in a unit cube centered in origo.
        /// </summary>
        /// <param name="p_num">Number of points to generate</param>
        /// <returns>Generated distribution</returns>
        public static float[] UniformUnitCube(int p_num)
        {
            float[] distr = new float[p_num * 4];
            Random r = new Random();
            for (int i = 0; i < p_num; i++)
            {
                distr[i + 0] = (float)r.NextDouble() * 1.0f - 0.5f;
                distr[i + 1] = (float)r.NextDouble() * 1.0f - 0.5f;
                distr[i + 2] = (float)r.NextDouble() * 1.0f - 0.5f;
                distr[i + 3] = 0.0f;//leave 0
            }
            return distr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] Origo(int p_num)
        {
            float[] distr = new float[p_num * 4];
            for (int i = 0; i < p_num; i++)
            {
                distr[i + 0] = 0.0f;
                distr[i + 1] = 0.0f;
                distr[i + 2] = 0.0f;
                distr[i + 3] = 0.0f;//leave 0
            }
            return distr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] ZUniformAxis(int p_num)
        {
            float[] distr = new float[p_num * 4];
            Random r = new Random();
            for (int i = 0; i < p_num; i++)
            {
                distr[i + 0] = 0.0f;
                distr[i + 1] = (float)r.NextDouble() * 1.0f - 0.5f;
                distr[i + 2] = 0.0f;
                distr[i + 3] = 0.0f;//leave 0
            }
            return distr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] ZUniformUnitSquare(int p_num)
        {
            float[] distr = new float[p_num * 4];
            Random r = new Random();
            for (int i = 0; i < p_num; i++)
            {
                distr[i + 0] = (float)r.NextDouble() * 1.0f - 0.5f;
                distr[i + 1] = 0.0f;
                distr[i + 2] = (float)r.NextDouble() * 1.0f - 0.5f;
                distr[i + 3] = 0.0f;//leave 0
            }
            return distr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] ZUniformUnitCircle(int p_num)
        {
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] Diamond(int p_num)
        {
            float[] distr = new float[p_num * 4];
            Random r = new Random();
            for (int i = 0; i < p_num; i++)
            {
                //3 random szam, osszeguk 1
                float r1 = (float)r.NextDouble();
                float maradektartomany = 1 - r1;
                float r2 = (float)r.NextDouble() * maradektartomany;
                float r3 = 1 - r1 - r2;

                //randomra elojelt valt
                r1 *= r.Next(2) == 0 ? 1 : -1;
                r2 *= r.Next(2) == 0 ? 1 : -1;
                r3 *= r.Next(2) == 0 ? 1 : -1;

                //tengelyeknek kiosztani a szamokat
                float[] randomok = new float[] { r1, r2, r3 };
                var shuffled = randomok.ToList().OrderBy(item => r.Next()).ToList();

                distr[i + 0] = shuffled[0];
                distr[i + 1] = shuffled[1];
                distr[i + 2] = shuffled[2];
                distr[i + 3] = 0.0f;//leave 0
            }
            return distr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] UniformUnitSphere(int p_num)
        {
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] UnitSphereEdge(int p_num)
        {
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] GaussianSphere(int p_num)
        {
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] ZSquare(int p_num)
        {
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] UnitGrid(int p_num)
        {
            float[] distr = new float[p_num * 4];
            Random r = new Random();
            int cnt=0;
            float d = 1.01f / (float)Math.Floor(Math.Pow(p_num, 1.0 / 3.0));//
            for (float x = -0.5f; x < 0.5f; x += d)
                for (float y = -0.5f; y < 0.5f; y += d)
                    for (float z = -0.5f; z < 0.5f; z += d)
                    {
                        distr[cnt + 0] = x;
                        distr[cnt + 1] = y;
                        distr[cnt + 2] = z;
                        distr[cnt + 3] = 0.0f;//leave 0
                        cnt += 4;
                    }
            return distr;
        }


        //TODO: implement more distribution ideas:
        //non-Unit sized shapes..
        //non-uniform distributions..


    }
}
