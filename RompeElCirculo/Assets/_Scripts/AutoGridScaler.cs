using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class AutoGridScaler : MonoBehaviour
{
    [Header("Límite de la cuadrícula")]
    [SerializeField] private int maxColumns = 5;
    [SerializeField] private int maxRows = 4;

    [Header("Opciones")]
    [SerializeField] private bool actualizarEnRuntime = true;
    [SerializeField] private bool mantenerCeldasCuadradas = true;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    private void Awake()
    {
        Inicializar();
        ActualizarGrid();
    }

    private void OnEnable()
    {
        Inicializar();
        ActualizarGrid();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Inicializar();

        maxColumns = Mathf.Max(1, maxColumns);
        maxRows = Mathf.Max(1, maxRows);

        ActualizarGrid();
    }
#endif

    private void Update()
    {
        if (Application.isPlaying && actualizarEnRuntime)
            ActualizarGrid();
    }

    private void OnRectTransformDimensionsChange()
    {
        ActualizarGrid();
    }

    private void Inicializar()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    public void ActualizarGrid()
    {
        if (grid == null || rectTransform == null)
            return;

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        float availableWidth =
            width
            - grid.padding.left
            - grid.padding.right
            - (maxColumns - 1) * grid.spacing.x;

        float availableHeight =
            height
            - grid.padding.top
            - grid.padding.bottom
            - (maxRows - 1) * grid.spacing.y;

        float cellWidth = availableWidth / maxColumns;
        float cellHeight = availableHeight / maxRows;

        if (mantenerCeldasCuadradas)
        {
            float size = Mathf.Min(cellWidth, cellHeight);
            cellWidth = size;
            cellHeight = size;
        }

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }
}