using Leap;
using System.Collections.Generic;

namespace GLDisplay
{
    public class FixedSizedVectorQueue
    {
        Queue<Vector> q = new Queue<Vector>();

        public FixedSizedVectorQueue() { Clear(); }

        public int Limit { get; set; } = 20;
        public void Enqueue(Vector obj)
        {
            q.Enqueue(obj);
            while (q.Count > Limit) { q.Dequeue(); };
        }

        public Vector Average()
        {
            float mult = 1f;
            float dem = 0.95f;
            var sum = new Vector(0, 0, 0);
            float div = 0;
            foreach (var vec in q)
            {
                mult *= dem;
                sum.x += vec.x * mult;
                sum.y += vec.y * mult;
                sum.z += vec.z * mult;
                div += mult;
            }
            return sum / div;
        }

        public Vector NextAvg(Vector f)
        {
            Enqueue(f);
            return Average();
        }

        public void Clear()
        {
            q.Clear();
            for (int i = 0; i < Limit; ++i)
            {
                q.Enqueue(new Vector(0, 0, 0));
            }
        }
    }
}
