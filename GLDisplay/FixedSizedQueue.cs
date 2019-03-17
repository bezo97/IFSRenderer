using System.Collections.Generic;
using System.Linq;

namespace GLDisplay
{
    public class FixedSizedQueue
    {
        Queue<float> q = new Queue<float>();

        public FixedSizedQueue() { Clear(); }

        public int Limit { get; set; } = 20;
        public void Enqueue(float obj)
        {
            q.Enqueue(obj);
            while (q.Count > Limit) { q.Dequeue(); };
        }

        public float Average()
        {
            float mult = 1f;
            float dem = 0.95f;
            float div = 0;
            return q.Select(f => { mult *= dem; div += mult; return f * mult; }).Sum() / div;
        }

        public float NextAvg(float f)
        {
            Enqueue(f);
            return Average();
        }

        public void Clear()
        {
            q.Clear();
            for (int i = 0; i < Limit; ++i)
            {
                q.Enqueue(0);
            }
        }
    }
}
