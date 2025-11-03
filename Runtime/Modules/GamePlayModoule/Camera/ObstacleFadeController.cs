using UnityEngine;
using System.Collections.Generic;

public class MaterialTransparent
{
    private enum SurfaceType
    {
        Opaque,
        Transparent
    }

    private enum BlendMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }
    
    // 缓存材质状态以避免重复设置
    private static Dictionary<Material, bool> _materialTransparentStates = new Dictionary<Material, bool>();

    public static void SetMaterialTransparent(bool transparent, Material material, float alpha = 1)
    {
        // 检查材质状态是否已经正确设置
        if (_materialTransparentStates.ContainsKey(material) && _materialTransparentStates[material] == transparent)
        {
            // 如果需要透明且已经透明，只需更新alpha值
            if (transparent)
            {
                Color color = material.color;
                if (Mathf.Abs(color.a - alpha) > 0.001f)
                {
                    color.a = alpha;
                    material.color = color;
                }
            }
            return;
        }
        
        if (transparent)
        {
            material.SetFloat("_Surface", (float)SurfaceType.Transparent);
            material.SetFloat("_Blend", (float)BlendMode.Alpha);
        }
        else
        {
            material.SetFloat("_Surface", (float)SurfaceType.Opaque);
        }

        SetupMaterialBlendMode(material);

        Color newColor = material.color;
        newColor.a = alpha;
        material.color = newColor;
        
        // 更新状态缓存
        _materialTransparentStates[material] = transparent;
    }

    private static void SetupMaterialBlendMode(Material material)
    {
        if (material == null)
        {
            return;
        }

        bool alphaClip = material.GetFloat("_AlphaClip") == 1;
        if (alphaClip)
        {
            material.EnableKeyword("_ALPHATEST_ON");
        }
        else
        {
            material.DisableKeyword("_ALPHATEST_ON");
        }

        SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
        if (surfaceType == 0)
        {
            material.SetOverrideTag("RenderType", "");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            material.SetShaderPassEnabled("ShadowCaster", true);
        }
        else
        {
            BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");
            switch (blendMode)
            {
                case BlendMode.Alpha:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    break;
                case BlendMode.Premultiply:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    break;
                case BlendMode.Additive:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    break;
                case BlendMode.Multiply:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                    break;
            }
        }
    }
}