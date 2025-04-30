using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Testing;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class PostProcessingEffectSource : MonoBehaviour
{
    private Material m_material;

    [FormerlySerializedAs("UseDestinationTextureDescriptor")]
    public bool UseFullResolutionTarget;

    [SerializeField]
    private Shader m_shader;

    [SerializeField]
    private int m_shaderPasses = 1;

    [FormerlySerializedAs("m_defaultParameters")]
    [SerializeField]
    public PostProcessingEffectParameter[] Parameters = Array.Empty<PostProcessingEffectParameter>();

    [SerializeField]
    private string m_logName = "unnamed";

    private void OnEnable()
    {
        m_material = new Material(m_shader);
    }

    public void Render(CommandBuffer buffer, RenderTexture source, RenderTexture destination)
    {
        ApplyCurrentParameters();
        int monitor = Tester.BeginTimeMonitor(buffer, $"PP_{m_logName}");
        Stack<RenderTexture> temporaryTextures = new Stack<RenderTexture>();
        temporaryTextures.Push(source);
        for (int i = 0; i < m_shaderPasses; i++)
        {
            RenderTexture target = RenderTexture.GetTemporary(source.descriptor);
            buffer.Blit(temporaryTextures.Peek(), target, m_material, i); // i = pass

            temporaryTextures.Push(target);
        }

        buffer.Blit(temporaryTextures.Peek(), destination);

        // don't release the last one as that's the source
        while (temporaryTextures.Count > 1)
        {
            RenderTexture.ReleaseTemporary(temporaryTextures.Pop());
        }

        Tester.EndTimeMonitor(buffer, monitor);
    }

    private void ApplyCurrentParameters()
    {
        foreach (PostProcessingEffectParameter defaultParameter in Parameters)
        {
            ApplyParameter(defaultParameter);
        }
    }

    private void ApplyParameter(PostProcessingEffectParameter parameter)
    {
        switch (parameter.Type)
        {
            case EffectParameterType.FLAG:
                if (parameter.FlagValue)
                    m_material.EnableKeyword(parameter.Name);
                else
                    m_material.DisableKeyword(parameter.Name);
                break;
            case EffectParameterType.INT:
                m_material.SetInt(parameter.Name, parameter.IntValue);
                break;
            case EffectParameterType.FLOAT:
                m_material.SetFloat(parameter.Name, parameter.FloatValue);
                break;
            case EffectParameterType.COLOR:
                m_material.SetColor(parameter.Name, parameter.ColorValue);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}