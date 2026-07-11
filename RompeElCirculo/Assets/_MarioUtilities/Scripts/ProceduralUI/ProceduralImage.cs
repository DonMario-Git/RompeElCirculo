using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LeTai.TrueShadow;
using LeTai.TrueShadow.PluginInterfaces;

[ExecuteAlways]
[AddComponentMenu("UI/Procedural Image")]
[RequireComponent(typeof(CanvasRenderer))]
public class ProceduralImage : MaskableGraphic, ITrueShadowCustomHashProviderV2
{
    [HideInInspector][SerializeField] private int m_CustomSalt = 0;

    [Range(0, 80)][SerializeField] private float m_RadiusTopLeft = 16f;
    [Range(0, 80)][SerializeField] private float m_RadiusTopRight = 16f;
    [Range(0, 80)][SerializeField] private float m_RadiusBottomLeft = 16f;
    [Range(0, 80)][SerializeField] private float m_RadiusBottomRight = 16f;

    [SerializeField, Range(1, 64)] private int m_Segments = 24;

    public event Action<int> trueShadowCustomHashChanged;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        Vector2 pivot = rectTransform.pivot;

        float width = rect.width;
        float height = rect.height;

        float xMin = -pivot.x * width;
        float yMin = -pivot.y * height;
        float xMax = (1 - pivot.x) * width;
        float yMax = (1 - pivot.y) * height;

        float hw = width * 0.5f;
        float hh = height * 0.5f;

        float rTL = Mathf.Clamp(m_RadiusTopLeft, 0, Mathf.Min(hw, hh));
        float rTR = Mathf.Clamp(m_RadiusTopRight, 0, Mathf.Min(hw, hh));
        float rBL = Mathf.Clamp(m_RadiusBottomLeft, 0, Mathf.Min(hw, hh));
        float rBR = Mathf.Clamp(m_RadiusBottomRight, 0, Mathf.Min(hw, hh));

        List<Vector2> perimeter = new List<Vector2>();

        void AddArc(Vector2 center, float startDeg, float endDeg, bool skipLast, float radius)
        {
            int steps = Mathf.Max(1, m_Segments);
            for (int i = 0; i <= steps; i++)
            {
                if (i == steps && skipLast) break;
                float t = (float)i / steps;
                float a = Mathf.Deg2Rad * Mathf.Lerp(startDeg, endDeg, t);
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                perimeter.Add(p);
            }
        }

        // Esquinas con pivot correcto
        AddArc(new Vector2(xMax - rTR, yMax - rTR), 0f, 90f, true, rTR);
        AddArc(new Vector2(xMin + rTL, yMax - rTL), 90f, 180f, true, rTL);
        AddArc(new Vector2(xMin + rBL, yMin + rBL), 180f, 270f, true, rBL);
        AddArc(new Vector2(xMax - rBR, yMin + rBR), 270f, 360f, false, rBR);

        // Centro
        UIVertex center = UIVertex.simpleVert;
        center.position = Vector3.zero;
        center.color = color;
        center.uv0 = new Vector2(0.5f, 0.5f);
        center.uv1 = center.uv0;
        center.normal = Vector3.back;
        center.tangent = new Vector4(1, 0, 0, -1);
        vh.AddVert(center);

        // Perímetro
        for (int i = 0; i < perimeter.Count; i++)
        {
            Vector2 p = perimeter[i];

            Vector2 uv = new Vector2(
                (p.x - xMin) / width,
                (p.y - yMin) / height
            );

            UIVertex v = UIVertex.simpleVert;
            v.position = p;
            v.color = color;
            v.uv0 = uv;
            v.uv1 = uv;
            v.normal = Vector3.back;
            v.tangent = new Vector4(1, 0, 0, -1);

            vh.AddVert(v);
        }

        // Triángulos
        for (int i = 0; i < perimeter.Count - 1; i++)
        {
            vh.AddTriangle(0, i + 1, i + 2);
        }

        vh.AddTriangle(0, perimeter.Count, 1);
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (m_CustomSalt == 0)
            m_CustomSalt = Guid.NewGuid().GetHashCode();

        UpdateTrueShadowCustomHash();
    }

    int ComputeCustomHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + m_CustomSalt;
            hash = hash * 23 + m_Segments;
            hash = hash * 23 + m_RadiusTopLeft.GetHashCode();
            hash = hash * 23 + m_RadiusTopRight.GetHashCode();
            hash = hash * 23 + m_RadiusBottomLeft.GetHashCode();
            hash = hash * 23 + m_RadiusBottomRight.GetHashCode();
            hash = hash * 23 + color.GetHashCode();

            var rect = rectTransform.rect;
            hash = hash * 23 + rect.width.GetHashCode();
            hash = hash * 23 + rect.height.GetHashCode();

            return hash;
        }
    }

    void UpdateTrueShadowCustomHash()
    {
        int h = ComputeCustomHash();
        trueShadowCustomHashChanged?.Invoke(h);

        var shadows = GetComponents<TrueShadow>();
        foreach (var s in shadows)
        {
            if (s != null)
                s.CustomHash = h;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        m_Segments = Mathf.Max(1, m_Segments);

        SetVerticesDirty();
        SetMaterialDirty();
    }
#endif
}