using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace ISILab.Commons.Utility
{
    public static class SafeRandom
    {
        private static readonly ThreadLocal<System.Random> _rand =
            new(() => new System.Random());
        private static System.Random Rand => _rand.Value;

        public static float Value()
        {
            return Range(0.0f, 1.0f);
        }
        public static float Range(float min, float max)
        {
            return UnityThread.IsMainThread
                ? UnityEngine.Random.Range(min, max)
                : (float)Rand.NextDouble() * (max - min) + min;
        }
        public static int Range(int min, int max)
        {
            return UnityThread.IsMainThread
                ? UnityEngine.Random.Range(min, max)
                : Rand.Next(min, max);
        }
    }

    public static class UnityThread
    {
        public static int MainThreadId { get; private set; }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
        #endif

        public static bool IsMainThread =>
            Thread.CurrentThread.ManagedThreadId == MainThreadId;
    }
}