using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace Testing
{
    public class TestInitializer : MonoBehaviour
    {
        public string RootDirectoryName;
        public string TestFolderName;

        private IEnumerator Start()
        {
            Tester.Init(new DirectoryInfo(RootDirectoryName), TestFolderName);
            yield return InitializeVulkanPlugin();
        }

        private void Update()
        {
            Tester.Flush();
        }

        private IEnumerator InitializeVulkanPlugin()
        {
            Debug.Log("Beginning GputTimer initialization");
            // wait a couple frames for things to start
            yield return null;
            yield return null;
            Debug.Log("Executing Init");

            while (Application.isPlaying)
            {
                try
                {
                    if (GpuTimer.InitGpuTimer())
                    {
                        yield break;
                    }
                }
                catch
                {
                    Debug.LogWarning($"Exception initializing GpuTimer");
                }

                Debug.LogWarning("Failed to initialize GpuTimer, retrying in 1 second");
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}