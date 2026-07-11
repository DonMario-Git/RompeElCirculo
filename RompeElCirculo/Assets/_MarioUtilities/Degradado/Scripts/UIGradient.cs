using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    [SerializeField] private Color m_color1 = Color.white;
    [SerializeField] private Color m_color2 = Color.white;
    [SerializeField, Range(-180f, 180f)] private float m_angle = 0f;
    [SerializeField] private bool m_ignoreRatio = true;

    private bool _dirty = true;
    private Vector2 _cachedDir;
    private UIGradientUtils.Matrix2x3 _cachedMatrix;
    private Rect _lastRect;

    public Color color1
    {
        get => m_color1;
        set { m_color1 = value; MarkDirty(); }
    }

    public Color color2
    {
        get => m_color2;
        set { m_color2 = value; MarkDirty(); }
    }

    public float angle
    {
        get => m_angle;
        set { m_angle = value; MarkDirty(); }
    }

    public bool ignoreRatio
    {
        get => m_ignoreRatio;
        set { m_ignoreRatio = value; MarkDirty(); }
    }

    private void MarkDirty()
    {
        _dirty = true;
        if (graphic != null)
            graphic.SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        MarkDirty();
    }
#endif

    public override void ModifyMesh(VertexHelper vh)
    {
        Rect rect = graphic.rectTransform.rect;

        // Recalcular la matriz solo si algo relevante cambió
        if (_dirty || rect != _lastRect)
        {
            Vector2 dir = UIGradientUtils.RotationDir(m_angle);

            if (!m_ignoreRatio)
                dir = UIGradientUtils.CompensateAspectRatio(rect, dir);

            _cachedDir = dir;
            _cachedMatrix = UIGradientUtils.LocalPositionMatrix(rect, dir);
            _lastRect = rect;
            _dirty = false;
        }

        UIVertex vertex = default;
        int count = vh.currentVertCount;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            Vector2 localPosition = _cachedMatrix * vertex.position;
            vertex.color *= Color.Lerp(m_color2, m_color1, localPosition.y);
            vh.SetUIVertex(vertex, i);
        }
    }
}