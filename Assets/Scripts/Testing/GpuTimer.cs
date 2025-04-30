using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace Testing
{
    public static class GpuTimer
    {
        private static readonly Dictionary<string, int> s_nameToIdMap = new Dictionary<string, int>();
        private static readonly Dictionary<int, string> s_idToNameMap = new Dictionary<int, string>();
        private static int s_nextId = 0;

        #if UNITY_EDITOR
        private const string DLL = "gpu_timer";
        #elif UNITY_ANDROID
        // android packages into one file so it needs to be internalized
        private const string DLL = "gpu_timer";
        #endif

        [DllImport(DLL)] public static extern bool InitGpuTimer();
        [DllImport(DLL)] public static extern ulong GetElapsedNanosecondsForId(int id);
        [DllImport(DLL)] public static extern IntPtr GetRenderEventFunc();

        public static int GetId(string name)
        {
            if (s_nameToIdMap.TryGetValue(name, out int result))
            {
                return result;
            }
            else
            {
                s_nameToIdMap.Add(name, s_nextId++);
                s_idToNameMap[s_nameToIdMap[name]] = name;
                return s_nameToIdMap[name];
            }
        }

        public static string GetName(int id) => s_idToNameMap[id];

        public static void BeginRecordTiming(CommandBuffer buffer, int id)
        {
            buffer.IssuePluginEventAndData(GetRenderEventFunc(), 1, (IntPtr)id);
        }

        public static void EndRecordTiming(CommandBuffer buffer, int id)
        {
            buffer.IssuePluginEventAndData(GetRenderEventFunc(), 2, (IntPtr)id);
        }
    }
}