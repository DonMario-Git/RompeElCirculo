using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LeTai.TrueShadow;
using LeTai.TrueShadow.PluginInterfaces;

[ExecuteAlways]
[AddComponentMenu("UI/Procedural Image")]
public class ProceduralImage : MaskableGraphic, ITrueShadowCustomHashProviderV2
{
    [SerializeField] private float m_Radius = 16f; // kept for backward compatibility but not used as default
    [SerializeField] private bool m_RaycastTargetLocal = true;
    [SerializeField] private bool m_MaskableLocal = true;
    [HideInInspector]
    [SerializeField] private int m_CustomSalt = 0;
    // Per-corner radii. Always active and default to m_Radius.
    [Range(0, 80)][SerializeField] private float m_RadiusTopLeft = 16f;
    [Range(0, 80)][SerializeField] private float m_RadiusTopRight = 16f;
    [Range(0, 80)][SerializeField] private float m_RadiusBottomLeft = 16f;
    [Range(0, 80)][SerializeField] private float m_RadiusBottomRight = 16f;
    [SerializeField, Range(1, 64)] private int m_Segments = 24;

    public float Radius
    {
        get => m_Radius;
        set
        {
            m_Radius = value;
            SetVerticesDirty();
            SetMaterialDirty();
        }
    }

    public override void SetVerticesDirty()
    {
        base.SetVerticesDirty();
        UpdateTrueShadowCustomHash();
    }

    public override void SetMaterialDirty()
    {
        base.SetMaterialDirty();
        UpdateTrueShadowCustomHash();
    }

    public int Segments
    {
        get => m_Segments;
        set
        {
            m_Segments = Mathf.Max(1, value);
            SetVerticesDirty();
        }
    }

    public override bool raycastTarget
    {
        get => m_RaycastTargetLocal;
        set
        {
            if (m_RaycastTargetLocal == value) return;
            m_RaycastTargetLocal = value;
            base.raycastTarget = value;
            SetVerticesDirty();
        }
    }

    public bool Maskable
    {
        get => base.maskable;
        set
        {
            if (base.maskable == value) return;
            base.maskable = value;
            m_MaskableLocal = value;
            SetVerticesDirty();
        }
    }

    // mainTexture left as base

    // Compute a simple custom hash representing the current shape/content so TrueShadow detects changes
    int ComputeCustomHash()
    {
        unchecked
        {
            int hash = 17;
            // include per-instance salt so identical components won't collide
            hash = hash * 23 + m_CustomSalt;
            hash = hash * 23 + m_Segments;
            hash = hash * 23 + m_RadiusTopLeft.GetHashCode();
            hash = hash * 23 + m_RadiusTopRight.GetHashCode();
            hash = hash * 23 + m_RadiusBottomLeft.GetHashCode();
            hash = hash * 23 + m_RadiusBottomRight.GetHashCode();
            hash = hash * 23 + color.GetHashCode();
            // sprite support removed
            // include rect size
            var rect = rectTransform != null ? rectTransform.rect : new Rect();
            hash = hash * 23 + rect.width.GetHashCode();
            hash = hash * 23 + rect.height.GetHashCode();
            return hash;
        }
    }

    void UpdateTrueShadowCustomHash()
    {
        try
        {
            int h = ComputeCustomHash();
            // Notify TrueShadow via the v2 provider event if anyone subscribed
            trueShadowCustomHashChanged?.Invoke(h);

            // Backwards-compat: also set CustomHash directly on any TrueShadow components on this GameObject
            var shadows = GetComponents<TrueShadow>();
            if (shadows != null)
            {
                foreach (var s in shadows)
                {
                    if (s != null)
                        s.CustomHash = h;
                }
            }
        }
        catch { }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        float hw = rect.width * 0.5f;
        float hh = rect.height * 0.5f;

        float radius = Mathf.Max(0f, m_Radius);
        radius = Mathf.Min(radius, Mathf.Min(hw, hh));

        float rTL = m_RadiusTopLeft;
        float rTR = m_RadiusTopRight;
        float rBL = m_RadiusBottomLeft;
        float rBR = m_RadiusBottomRight;

        // Clamp per-corner radii so they don't exceed half-dimensions
        rTL = Mathf.Clamp(rTL, 0f, Mathf.Min(hw, hh));
        rTR = Mathf.Clamp(rTR, 0f, Mathf.Min(hw, hh));
        rBL = Mathf.Clamp(rBL, 0f, Mathf.Min(hw, hh));
        rBR = Mathf.Clamp(rBR, 0f, Mathf.Min(hw, hh));

        // Build perimeter points (clockwise)
        List<Vector2> perimeter = new List<Vector2>();

        // Convenience to add arc points for a corner
        void AddArc(Vector2 center, float startDeg, float endDeg, bool skipLast, float arcRadius)
        {
            int steps = Mathf.Max(1, m_Segments);
            for (int i = 0; i <= steps; i++)
            {
                if (i == steps && skipLast) break;
                float t = (float)i / steps;
                float a = Mathf.Deg2Rad * Mathf.Lerp(startDeg, endDeg, t);
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * arcRadius;
                perimeter.Add(p);
            }
        }

        // Top-right corner (0 -> 90)
        AddArc(new Vector2(hw - rTR, hh - rTR), 0f, 90f, true, rTR);
        // Top-left corner (90 -> 180)
        AddArc(new Vector2(-hw + rTL, hh - rTL), 90f, 180f, true, rTL);
        // Bottom-left corner (180 -> 270)
        AddArc(new Vector2(-hw + rBL, -hh + rBL), 180f, 270f, true, rBL);
        // Bottom-right corner (270 -> 360)
        AddArc(new Vector2(hw - rBR, -hh + rBR), 270f, 360f, false, rBR);

        // Prepare sprite UV mapping if sprite assigned
        // No sprite support: use simple normalized UVs based on rect

        if (perimeter.Count < 3)
        {
            // fallback to simple rect
            Vector2[] pts = new Vector2[4]
            {
                new Vector2(-hw, -hh),
                new Vector2(-hw, hh),
                new Vector2(hw, hh),
                new Vector2(hw, -hh)
            };

            // center
            var col = color;
            UIVertex cv = UIVertex.simpleVert;
            cv.position = Vector3.zero;
            cv.color = col;
            Vector2 centerNorm = new Vector2(0.5f, 0.5f);
            cv.uv0 = centerNorm;
            // fill secondary uv with normalized local position (0..1), and normals/tangent for shadow systems
            cv.uv1 = centerNorm;
            cv.normal = Vector3.back;
            cv.tangent = new Vector4(1f, 0f, 0f, -1f);
            vh.AddVert(cv);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = pts[i];
            Vector2 uv = new Vector2((p.x - rect.xMin) / rect.width, (p.y - rect.yMin) / rect.height);
                UIVertex v = UIVertex.simpleVert;
                v.position = p;
                v.color = col;
                v.uv0 = uv;
                v.uv1 = uv;
                v.normal = Vector3.back;
                v.tangent = new Vector4(1f, 0f, 0f, -1f);
                vh.AddVert(v);
            }

            for (int i = 1; i <= 4; i++)
            {
                int next = i == 4 ? 1 : i + 1;
                vh.AddTriangle(0, i, next);
            }

            return;
        }

        // Add center vertex
        var centerColor = color;
        Vector2 centerUv2 = new Vector2(0.5f, 0.5f);
        UIVertex centerV = UIVertex.simpleVert;
        centerV.position = Vector3.zero;
        centerV.color = centerColor;
        centerV.uv0 = centerUv2;
        centerV.uv1 = centerUv2;
        centerV.normal = Vector3.back;
        centerV.tangent = new Vector4(1f, 0f, 0f, -1f);
        vh.AddVert(centerV);

        // Add perimeter vertices
        for (int i = 0; i < perimeter.Count; i++)
        {
            Vector2 p = perimeter[i];
            Vector2 uv = new Vector2((p.x - rect.xMin) / rect.width, (p.y - rect.yMin) / rect.height);
            UIVertex v = UIVertex.simpleVert;
            v.position = new Vector3(p.x, p.y, 0f);
            v.color = color;
            v.uv0 = uv;
            v.uv1 = uv;
            v.normal = Vector3.back;
            v.tangent = new Vector4(1f, 0f, 0f, -1f);
            vh.AddVert(v);
        }

        // Triangles (fan from center at index 0)
        int perStart = 1;
        int perCount = perimeter.Count;
        for (int i = 0; i < perCount - 1; i++)
        {
            vh.AddTriangle(0, perStart + i, perStart + i + 1);
        }
        // close
        vh.AddTriangle(0, perStart + perCount - 1, perStart + 0);
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
        {
            m_CustomSalt = Guid.NewGuid().GetHashCode();
        }
        UpdateTrueShadowCustomHash();
    }


    // ITrueShadowCustomHashProviderV2 implementation
    public event Action<int> trueShadowCustomHashChanged;

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        base.OnValidate();
        m_Segments = Mathf.Max(1, m_Segments);
        m_Radius = Mathf.Max(0f, m_Radius);
        m_RadiusTopLeft = Mathf.Max(0f, m_RadiusTopLeft);
        m_RadiusTopRight = Mathf.Max(0f, m_RadiusTopRight);
        m_RadiusBottomLeft = Mathf.Max(0f, m_RadiusBottomLeft);
        m_RadiusBottomRight = Mathf.Max(0f, m_RadiusBottomRight);
        // ensure base properties reflect serialized fields
        base.raycastTarget = m_RaycastTargetLocal;
        base.maskable = m_MaskableLocal;
        SetVerticesDirty();
        SetMaterialDirty();
    }

#endif

    // Precise raycast for rounded corners
    public override bool Raycast(Vector2 sp, Camera eventCamera)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out local))
            return false;

        Rect rect = rectTransform.rect;
        float hw = rect.width * 0.5f;
        float hh = rect.height * 0.5f;
        // choose corner-specific radius based on which quadrant the point is in
        float signX = local.x >= 0f ? 1f : -1f;
        float signY = local.y >= 0f ? 1f : -1f;

        float cornerRadius = m_Radius;
        if (signX > 0f && signY > 0f) cornerRadius = m_RadiusTopRight; // top-right
        if (signX < 0f && signY > 0f) cornerRadius = m_RadiusTopLeft;  // top-left
        if (signX < 0f && signY < 0f) cornerRadius = m_RadiusBottomLeft; // bottom-left
        if (signX > 0f && signY < 0f) cornerRadius = m_RadiusBottomRight; // bottom-right

        cornerRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(hw, hh));

        Vector2 cornerCenter = new Vector2(signX * (hw - cornerRadius), signY * (hh - cornerRadius));
        Vector2 d = local - cornerCenter;

        // if point is inside the central rect area for that corner, it's inside
        if (Mathf.Abs(local.x) <= hw - cornerRadius || Mathf.Abs(local.y) <= hh - cornerRadius)
            return true;

        return (d.x * d.x + d.y * d.y) <= (cornerRadius * cornerRadius + 0.0001f);
    }
}
