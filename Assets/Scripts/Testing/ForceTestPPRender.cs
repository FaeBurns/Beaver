using System;
using UnityEngine;

namespace Testing
{
    public class ForceTestPPRender : MonoBehaviour
    {
        [SerializeField]
        private PostProcessingEffectScheduler m_scheduler;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            m_scheduler.Render(source, destination);
        }
    }
}
