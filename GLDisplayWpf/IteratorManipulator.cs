//using IFSEngine;
//using IFSEngine.Model;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace GLDisplayWpf
//{
//    //TODO: remove
//    public static class IteratorManipulator
//    {

//        public static EventHandler EditStateChanged;
//        public static EventHandler IteratorChanged;


//        private static Random rand = new Random();

//        private static int editState = 0;
//        public static int EditState {
//            get => editState;
//            set {
//                editState = (value + iterators.Count) % iterators.Count;
//                EditStateChanged?.Invoke(null, EventArgs.Empty);
//            }
//        }



//        private static List<Iterator> iterators = new List<Iterator>() {
//            new Iterator(
//                new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
//                0,
//                1.0f,
//                0.1f,
//                0.0f,
//                1.0f)
//            ,
//            new Iterator(
//                new Affine(0.2f,-1.0f,0.0f , 0.8f,0.5f,0.0f , 0.1f,0.8f,0.0f , 0.0f,0.0f,1.0f),
//                1,
//                0.5f,
//                0.8f,
//                0.5f,
//                1.0f
//            ),
//            new Iterator(
//                new Affine(0.6f,0.133975f,0.0f , 0.56f,0.0f,0.0f , 0.0f,0.4f,0.0f , 0.0f,0.0f,1.0f),
//                2,
//                0.25f,
//                0.9f,
//                1.0f,
//                1.0f
//            )
//        };

//        private static float SummWeights;
//        public static int IteratorCount => iterators.Count;

//        private static readonly Iterator finalit = new Iterator(
//            new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
//            0,//linear
//            0.0f,
//            0.0f,
//            1.0f,
//            1.0f
//        );

//        public static Iterator Iterator => iterators[EditState];

//        public static void ModifyIterator(Func<Iterator, Iterator> func, object sender, EventArgs e)
//        {
//            iterators[EditState] = func(iterators[EditState]);
//            IteratorChanged?.Invoke(sender, EventArgs.Empty);
//        }

//        public static void Randomize(Renderer r)
//        {
//            iterators.Clear();
//            var itnum = rand.Next(5) + 2;

//            for (var ii = 0; ii < itnum; ii++)
//            {
//                var nit = new Iterator();
//                nit.aff.ox = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.oy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.oz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.xx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.xy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.xz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.yx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.yy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.yz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.zx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.zy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.aff.zz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
//                nit.w = (float)rand.NextDouble();
//                nit.cs = (float)(rand.NextDouble() * 2.0f - 1.0f) * 0.1f;
//                nit.ci = (float)rand.NextDouble();
//                nit.op = (float)rand.NextDouble();
//                nit.tfID = rand.Next() % 2;//spherical
//                iterators.Add(nit);
//            }
//            Update(r, true);
//            // for (var s = 0; s < itnum; s++)
//            // {
//            //     var tmpit = iterators[s];
//            //     tmpit.w /= SummWeights;
//            //     iterators[s] = tmpit;
//            // }
//            // r.Camera = new Camera();
//            // r.UpdateParams(iterators, finalit);
//            EditState = 0;
//        }

//        public static void Update(Renderer r, bool updateCamera = false)
//        {
//            var weightedIterators = iterators.ToList();
//            SummWeights = 0.0f;
//            for (var s = 0; s < weightedIterators.Count; s++) { SummWeights += weightedIterators[s].w; }
//            for (var s = 0; s < weightedIterators.Count; s++)
//            {
//                var tmpit = weightedIterators[s];
//                tmpit.w /= SummWeights;
//                weightedIterators[s] = tmpit;
//            }
//            if (updateCamera)
//            {
//                r.Camera = new Camera();
//            }
//            r.UpdateParams(weightedIterators, finalit);
//        }
//    }
//}