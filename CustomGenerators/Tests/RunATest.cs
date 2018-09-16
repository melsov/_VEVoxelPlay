using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEditor;

namespace VE.Generators
{
    public class RunATest : MonoBehaviour
    {
        [MenuItem("VE/Test Noise")]
        static void TestNoise()
        {
            int times = 400;
            float avg = 0;
            float size = 20f;
            float max = -9999999f, min = 9999999f;
            for(int i = 0; i < times; ++i)
            {
                float n = SimplexNoise.Noise.CalcPixel2D(i % ((int)size), (int)(i / size), 1f);
                max = Mathf.Max(max, n);
                min = Mathf.Min(min, n);
                avg += n;
            }
            Debug.Log(string.Format("avg {0} . max {1}. min {2}", avg / times, max, min));
        }
    }
}
