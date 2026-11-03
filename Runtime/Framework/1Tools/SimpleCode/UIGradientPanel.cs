using UnityEngine;
using UnityEngine.EventSystems; // Unity6 ugui2.0 中 UIBehaviour 在此命名空间
using UnityEngine.UI;

/// <summary>
/// 渐变面板组件：配合 UI/XCGradientPanel shader 使用。
/// 通过 IMaterialModifier 把 RectTransform 的像素尺寸同步进 _RectSize，
/// 供 shader 做像素级圆角/描边/高光计算，尺寸变化时自动刷新。
/// </summary>
[RequireComponent(typeof(Graphic))]
public class UIGradientPanel : UIBehaviour, IMaterialModifier
{
    private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");

    private Graphic m_CachedGraphic;
    private Material m_Modified;

    /// <summary>延迟获取同物体上的 Graphic（RequireComponent 保证存在）</summary>
    private Graphic TargetGraphic
    {
        get
        {
            if (m_CachedGraphic == null) m_CachedGraphic = GetComponent<Graphic>();
            return m_CachedGraphic;
        }
    }

    /// <summary>基于原材质生成私有实例并写入当前像素尺寸，多面板可共用同一材质</summary>
    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (m_Modified == null)
        {
            m_Modified = new Material(baseMaterial);
            m_Modified.hideFlags = HideFlags.HideAndDontSave;
        }
        m_Modified.CopyPropertiesFromMaterial(baseMaterial);
        m_Modified.SetVector(RectSizeId, ((RectTransform)transform).rect.size);
        return m_Modified;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        TargetGraphic.SetMaterialDirty();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        TargetGraphic.SetMaterialDirty();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (m_Modified != null) Destroy(m_Modified);
    }
}
