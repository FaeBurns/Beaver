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

        [SerializeField]
        private TestExecutor m_testExecutor;

        [SerializeField]
        private bool m_waitForVRSession;

        private IEnumerator Start()
        {
#if UNITY_ANDROID
            RootDirectoryName = Path.Combine(Application.persistentDataPath, RootDirectoryName);
#endif
            Tester.Init(new DirectoryInfo(RootDirectoryName), TestFolderName);
            yield return InitializeVulkanPlugin();

            // wait frame for flush
            // necessary?
            yield return null;

            if (m_testExecutor == null || !m_testExecutor.isActiveAndEnabled)
                yield break;

            yield return m_testExecutor?.ExecuteTests();
        }

        private void Update()
        {
            Tester.Flush();
        }

        private IEnumerator InitializeVulkanPlugin()
        {
            Debug.Log("Beginning GputTimer initialization");
            // wait a couple frames for things to start - may not be needed anymore?
            yield return null;
            yield return null;

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
                    Debug.LogWarning("Exception initializing GpuTimer");
                }

                Debug.LogWarning("Failed to initialize GpuTimer, retrying in 1 second");
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}