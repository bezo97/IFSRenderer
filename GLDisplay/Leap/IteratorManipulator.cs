using IFSEngine;
using Leap;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GLDisplay.Leap
{
    public static class IteratorManipulator
    {
        public static float MaxTrans { get; private set; } = 0.7f;
        public static float MaxRotate { get; private set; } = 3.1415926f / 3.0f;
        //private static LeapQuaternion rightQuaternion;
        private static LeapQuaternion leftQuaternion;
        //private static Vector rightPalmPosition;
        private static Vector leftPalmPosition;

        //private static bool rightGrabbed;
        private static bool leftGrabbed;

        private static Random rand = new Random();

        private static int editState = 0;
        public static int EditState {
            get => editState;
            private set => editState = (value + iterators.Count) % iterators.Count;
        }

        private static List<Iterator> iterators = new List<Iterator>() {
            new Iterator(
                new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                0,
                1.0f,
                0.1f,
                0.0f,
                1.0f)
            ,
            new Iterator(
                new Affine(0.2f,-1.0f,0.0f , 0.8f,0.5f,0.0f , 0.1f,0.8f,0.0f , 0.0f,0.0f,1.0f),
                1,
                0.5f,
                0.8f,
                0.5f,
                1.0f
            ),
            new Iterator(
                new Affine(0.6f,0.133975f,0.0f , 0.56f,0.0f,0.0f , 0.0f,0.4f,0.0f , 0.0f,0.0f,1.0f),
                2,
                0.25f,
                0.9f,
                1.0f,
                1.0f
            )
        };

        private static float SummWeights;
        public static int IteratorCount => iterators.Count;


        private static Iterator finalit = new Iterator(
            new Affine(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
            0,//linear
            0.0f,
            0.0f,
            1.0f,
            1.0f
        );

        private static Iterator Current {
            get => iterators[EditState];
            set => iterators[EditState] = value;
        }

        private static void ModifyCurrent(Func<Iterator, Iterator> func)
        {
            Current = func(Current);
        }

        private static FixedSizedVectorQueue leftPalmNormalQueue = new FixedSizedVectorQueue();

        static IteratorManipulator()
        {
            Swipe.RightSwiped += (s, e) =>
            {
                EditState++;
                if (EditState >= iterators.Count)
                {
                    EditState = 0;
                }
            };
            Swipe.LeftSwiped += (s, e) =>
            {
                EditState--;
                if (EditState <= 0)
                {
                    EditState = iterators.Count - 1;
                }
            };
        }

        public static void Randomize(Renderer r)
        {
            iterators.Clear();
            SummWeights = 0.0f;
            var itnum = rand.Next(5) + 2;

            for (var ii = 0; ii < itnum; ii++)
            {
                var nit = new Iterator();
                nit.aff.ox = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.oy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.oz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.xx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.xy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.xz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.yx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.yy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.yz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.zx = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.zy = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.aff.zz = ((float)rand.NextDouble() * 2 - 1) * 1.5f;
                nit.w = (float)rand.NextDouble();
                nit.cs = (float)(rand.NextDouble() * 2.0f - 1.0f) * 0.1f;
                nit.ci = (float)rand.NextDouble();
                nit.op = (float)rand.NextDouble();
                nit.tfID = rand.Next() % 2;//spherical
                iterators.Add(nit);

                SummWeights += nit.w;
            }
            Update(r, true);
            // for (var s = 0; s < itnum; s++)
            // {
            //     var tmpit = iterators[s];
            //     tmpit.w /= SummWeights;
            //     iterators[s] = tmpit;
            // }
            // r.Camera = new Camera();
            // r.UpdateParams(iterators, finalit);
            EditState = 0;
        }

        public static void Update(Renderer r, bool updateCamera = false)
        {
            var waightedIterators = iterators.ToList();
            for (var s = 0; s < waightedIterators.Count; s++)
            {
                var tmpit = waightedIterators[s];
                tmpit.w /= SummWeights;
                waightedIterators[s] = tmpit;
            }
            if (updateCamera)
            {
                r.Camera = new Camera();
            }
            r.UpdateParams(waightedIterators, finalit);
        }

        public static void UpdateIterator(Hand left, Renderer r)
        {
            bool updateNeeded = false;
            if (!left.Fingers.Any(f => f.IsExtended) && left.Confidence >= 0.7)
            {
                if (!leftGrabbed)
                {
                    leftGrabbed = true;
                    leftPalmPosition = left.PalmPosition;
                    leftQuaternion = left.Rotation.Inverse();
                }

                (float roll, float pitch, float yaw) = left.Rotation.Multiply(leftQuaternion).GetRotations();
                if (Math.Abs(pitch) < MaxRotate && Math.Abs(roll) < MaxRotate && Math.Abs(yaw) < MaxRotate)
                {
                    ModifyCurrent(c => {
                        // TODO
                        return c;
                    });
                }

                var v = (left.PalmPosition - leftPalmPosition) / 100.0f;
                if (Math.Abs(v.z) < MaxTrans && Math.Abs(v.z) < MaxTrans && Math.Abs(v.z) < MaxTrans)
                {
                    ModifyCurrent(c => {
                        // TODO
                        return c;
                    });
                }

                updateNeeded = true;
            }
            else if (leftGrabbed && left.Fingers.Any(f => f.IsExtended))
            {
                leftGrabbed = false;
                // TODO
            }
            if (left.Fingers.All(f => f.IsExtended) && left.Confidence >= 0.7)
            {
                Vector felenk = new Vector(0, 0, -1);
                if (Math.Abs(leftPalmNormalQueue.NextAvg(left.PalmNormal).Dot(felenk)) > 0.7)
                {
                    float handDistance = 1.0f - (left.PalmPosition.z + 360.0f) / 360.0f;

                    // TODO ModifyCurrent(c => { c.w = handDistance; return c; });
                    updateNeeded = true;
                }
            }
            if (updateNeeded)
            {
                Update(r);
            }
        }
    }
}
