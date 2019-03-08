using System;
using System.Collections.Generic;
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
                distr[i + 0] = (float)r.NextDouble() * 1.0f - 1.0f;
                distr[i + 1] = (float)r.NextDouble() * 1.0f - 1.0f;
                distr[i + 2] = (float)r.NextDouble() * 1.0f - 1.0f;
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
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] ZUniformAxis(int p_num)
        {
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_num"></param>
        /// <returns></returns>
        public static float[] ZUniformUnitSquare(int p_num)
        {
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
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
            //TODO: implement, document, write unit tests
            throw new NotImplementedException();
        }


        //TODO: implement more distribution ideas:
        //non-Unit sized shapes..
        //non-uniform distributions..


    }
}
