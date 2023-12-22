using System;
using System.Collections.Generic;
using System.Linq;

namespace IFSEngine.Utility;

/// <summary>
/// Efficient algorithm for sampling from a discrete probability distribution.
/// In O(1)
/// </summary>
/// <remarks>
/// Implementation based on <a href="https://en.wikipedia.org/wiki/Alias_method"></a>
/// </remarks>
internal static class AliasMethod
{
    /// <summary>
    /// Table generation for the Alias method.
    /// </summary>
    /// <param name="p">A discrete probability distribution.</param>
    /// <returns>The probability (u) and alias (k) table.</returns>
    public static List<(double u, int k)> GenerateAliasTable(List<double> p)
    {
        int n = p.Count;
        List<int> K = Enumerable.Repeat(0, n).ToList();//alias table
                                                       //Groups:
                                                       //1 - overfull (Ui > 1)
                                                       //-1 - underfull (Ui < 1)
                                                       //0 - exactly full (Ui = 1 or Ki is initialized)
        List<(double v, int group)> U = p.Select(pi => (n * pi, Math.Sign(n * pi - 1))).ToList();//probability table
        for (int i = 0; i < n; i++)
            if (U[i].v == 1)
                K[i] = i;//not important
        while (U.Exists(u => u.group != 0))
        {
            int i = U.FindIndex(u => u.group == 1);//overfull
            int j = U.FindIndex(u => u.group == -1);//underfull
                                                    //handle numerical instability
            if (i == -1)
                U[j] = (1, 0);
            if (j == -1)
                U[i] = (1, 0);
            if (i == -1 || j == -1)
                continue;
            K[j] = i;
            var newui = U[i].v + U[j].v - 1;
            U[j] = (U[j].v, 0);
            U[i] = (newui, Math.Sign(newui - 1));
        }
        return Enumerable.Zip(U, K, (u, k) => (u.v, k)).ToList();
    }
}
