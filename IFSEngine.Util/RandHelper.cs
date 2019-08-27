using System;
using System.Collections.Generic;
using System.Text;

namespace IFSEngine.Util
{
    //global random state to avoid getting same values
    public static class RandHelper
    {
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int Next(int max = int.MaxValue)
        {
            lock (syncLock)
            { // synchronize
                return random.Next(max);
            }
        }

        public static double NextDouble()
        {
            lock (syncLock)
            { // synchronize
                return random.NextDouble();
            }
        }

    }
}
