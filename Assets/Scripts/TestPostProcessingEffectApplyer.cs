using System;
using UnityEngine;

public class TestPostProcessingEffectApplyer : MonoBehaviour
{
    private static readonly int s_eyeIndex = Shader.PropertyToID("_eyeIndex");
    private Camera m_camera;
    private PostProcessingEffectScheduler m_scheduler;

    [SerializeField]
    private Shader m_blitFlatShader;

    [SerializeField]
    private Shader m_blitEyeShader;

    private Material m_blitFlatMaterial;
    private Material m_blitEyeMaterial;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();
        m_scheduler = GetComponent<PostProcessingEffectScheduler>();

        m_blitFlatMaterial = new Material(m_blitFlatShader);
        m_blitEyeMaterial = new Material(m_blitEyeShader);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        m_scheduler.Render(source, destination);
    }
}