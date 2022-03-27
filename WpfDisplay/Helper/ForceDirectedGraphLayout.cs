using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WpfDisplay.Helper;

/// <summary>
/// Naive implementation of Force-Directed Graph Layout.
/// <a href="https://www2.eecs.berkeley.edu/Pubs/TechRpts/2013/EECS-2013-176.pdf">Algorithm description</a>.
/// <a href="http://yifanhu.net/PUB/graph_draw.pdf">Original paper</a>.
/// "Intuitively, every vertex wants to keep its neighbors close while pushing all other vertices away."
/// O(V^2), but O(V) in practice
/// </summary>
public static class ForceDirectedGraphLayout
{
    public record Graph(List<Vector2> Vertices, List<(int, int)> Edges);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="g"></param>
    /// <param name="K">Optimal distance, aka. natural spring length.</param>
    /// <param name="C">Relative strength of the attractive and repulsive forces.</param>
    /// <param name="tol">Termination tolerance. > 0</param>
    /// <returns></returns>
    public static List<Vector2> GenerateLayout(Graph g, double K, double C, double tol)
    {
        //TODO: Kmult
        K *= C * Math.Sqrt(500 * 500 / g.Vertices.Count);

        List<Vector2> x = g.Vertices.ToList();
        double step = 50;//initial step length. say 1/10 of canvas width
        bool converged = false;
        while (!converged)//TODO: max 600
        {//iteratively minimize the system energy
            var x0 = x.ToList();
            for (int i = 0; i < g.Vertices.Count; i++)
            {
                Vector2 f = new Vector2(0, 0);
                foreach (var e in g.Edges.Where(e => e.Item1 == i))
                {
                    var v1 = g.Vertices[e.Item1];
                    var v2 = g.Vertices[e.Item2];
                    float af = (float)(CalculateAttractiveForce((v1, v2), K) / Vector2.Distance(v1, v2));
                    f += Vector2.Multiply(af, v2 - v1);
                }
                for (int j = 0; j < g.Vertices.Count; j++)
                {
                    if (i == j)
                        continue;
                    var v1 = g.Vertices[i];
                    var v2 = g.Vertices[j];
                    float rf = (float)(CalculateRepulsiveForce(v1, v2, C, K) / Vector2.Distance(v1, v2));
                    f += Vector2.Multiply(rf, v2 - v1);
                }
                x[i] += Vector2.Multiply(f, (float)step / f.Length());
            }
            step *= 0.9;
            if (x.Zip(x0).Select(p => Vector2.Distance(p.First, p.Second)).Sum() < K * tol)
                converged = true;
        }
        return x;
    }

    // i =/= j
    private static double CalculateRepulsiveForce(Vector2 i, Vector2 j, double C, double K)
        => -C * K * K / Vector2.Distance(i, j);

    private static double CalculateAttractiveForce((Vector2 i, Vector2 j) edge, double K)
        => Math.Pow(Vector2.Distance(edge.i, edge.j), 2) / K;

}
