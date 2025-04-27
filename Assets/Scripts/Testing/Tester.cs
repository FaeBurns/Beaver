using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Testing
{
    public class Tester
    {
        private static readonly ConcurrentQueue<TestReport> s_testResults = new ConcurrentQueue<TestReport>();
        private static readonly ConcurrentQueue<int> s_vkTestIds = new ConcurrentQueue<int>();
        private static TestWriter s_testWriter;

        public static void Init(DirectoryInfo testFolderPath, string testFileName)
        {
            s_testWriter = new TestWriter(testFolderPath, testFileName);

            Application.quitting += () =>
            {
                s_testWriter.Dispose();
            };
        }

        public static void Flush()
        {
            while (s_testResults.TryDequeue(out TestReport report))
            {
                s_testWriter.WriteToFile(report);
            }

            while (s_vkTestIds.TryDequeue(out int vkId))
            {
                ulong nanoseconds = GpuTimer.GetElapsedNanosecondsForId(vkId);
                s_testWriter.WriteToFile(new TestReport(GpuTimer.GetName(vkId), nanoseconds.ToString()));
            }
        }

        public static int BeginTimeMonitor(CommandBuffer buffer, string name)
        {
            int id = GpuTimer.GetId(name);
            GpuTimer.BeginRecordTiming(buffer, id);
            return id;
        }

        public static void EndTimeMonitor(CommandBuffer buffer, int id)
        {
            GpuTimer.EndRecordTiming(buffer, id);
            s_vkTestIds.Enqueue(id);
        }

        public static void WriteToColumn(string column, string value)
        {
            s_testWriter.WriteToFile(new TestReport(column, value));
        }
    }

    public struct TestReport
    {
        public TestReport(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
    }

    public class TestWriter : IDisposable
    {
        private readonly Dictionary<string, StreamWriter> m_writers = new Dictionary<string, StreamWriter>();
        private readonly DirectoryInfo m_testFolder;

        public TestWriter(DirectoryInfo rootFolderPath, string testFolderName)
        {
            if (!rootFolderPath.Exists)
                rootFolderPath.Create();

            int count = 0;

            string GetFullPath()
            {
                if (count == 0)
                    return Path.Combine(rootFolderPath.FullName, testFolderName);
                else
                    return Path.Combine(rootFolderPath.FullName, testFolderName + "_" + count);
            }

            while (Directory.Exists(GetFullPath()))
            {
                count++;
            }

            m_testFolder = new DirectoryInfo(GetFullPath());
            m_testFolder.Create();
            Debug.Log($"Tests writing to {m_testFolder.FullName}");
        }

        public void WriteToFile(TestReport report)
        {
            StreamWriter writer = m_writers.GetValueOrDefault(report.Name);
            if (writer == null)
            {
                writer = new StreamWriter(Path.Combine(m_testFolder.FullName, report.Name + ".csvpart"))
                {
                    AutoFlush = true,
                };
                m_writers[report.Name] = writer;
                writer.WriteLine($"Frame,{report.Name}");
            }

            writer.WriteLine($"{Time.frameCount},{report.Value}");
        }

        public void Dispose()
        {
            foreach (StreamWriter writer in m_writers.Values)
            {
                writer.Flush();
                writer.Dispose();
            }
        }
    }
}