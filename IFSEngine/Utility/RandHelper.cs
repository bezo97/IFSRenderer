using System;

namespace IFSEngine.Utility;

//global random state to avoid getting same values
public static class RandHelper
{
    private static readonly Random _random = new();
    private static readonly object _syncLock = new();
    public static int Next(int max = int.MaxValue)
    {
        lock (_syncLock)
        { // synchronize
            return _random.Next(max);
        }
    }

    public static double NextDouble()
    {
        lock (_syncLock)
        { // synchronize
            return _random.NextDouble();
        }
    }

}
