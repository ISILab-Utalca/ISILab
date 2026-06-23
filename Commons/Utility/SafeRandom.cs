using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ISILab.Commons.Utility
{
    public static class SafeRandom
    {
        private static System.Random _rand;
        private static System.Random Rand => _rand ??= new();

        public static float Value(bool async = false)
        {
            return Range(0.0f, 1.0f, async);
        }
        public static float Range(float min, float max, bool async = false)
        {
            return async ? (float)Rand.NextDouble() * (max - min) + min : UnityEngine.Random.Range(min, max);
        }
        public static int Range(int min, int max, bool async = false)
        {
            return async ? Rand.Next(min, max) : UnityEngine.Random.Range(min, max);
        }
    }
}