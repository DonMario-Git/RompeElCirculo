using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace LonidaMinigames.FreeDrawing
{
    public class LienzoDibujoController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public Image targetDrawImage;
        private Color selectedColor = Color.black;

        public bool debugMode = false;
        private bool isPointerDown = false;

        [Range(1, 64)]
        public int brushSize = 5;
        public TextMeshProUGUI targetSizeText;

        // Enum para herramientas
        public enum ToolType { Brush, Eraser }
        public ToolType currentTool = ToolType.Brush;

        // Para interpolación
        private Vector2Int? lastDrawnPixel = null;

        private void OnEnable()
        {
            if (debugMode) Debug.Log("[LienzoDibujoController] Awake: Inicializando textura y sprite.");
            Texture2D tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            targetDrawImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            FillAll();
            tex.Apply();
            if (debugMode) Debug.Log("[LienzoDibujoController] Awake: Textura y sprite inicializados.");
        }

        public void ChangeMainColor(Color color)
        {
            selectedColor = color;
        }

        public void ChangeSizeBrush(float size)
        {
            brushSize = (int)size;
            targetSizeText.text = size.ToString();
        }

        public void FillAll()
        {
            Texture2D tex = targetDrawImage.sprite.texture;
            Color[] fillColorArray = tex.GetPixels();
            for (int i = 0; i < fillColorArray.Length; ++i)
                fillColorArray[i] = Color.white;
            tex.SetPixels(fillColorArray);
            tex.Apply();
            if (debugMode) Debug.Log("[LienzoDibujoController] FillAll: Llenado completo optimizado.");
        }

        // Brocha circular con antialiasing y herramienta borrador optimizada
        public void DrawCall(int x, int y)
        {
            Texture2D tex = targetDrawImage.sprite.texture;
            int radius = brushSize / 2;
            Color colorToUse = currentTool == ToolType.Eraser ? Color.white : selectedColor;

            int minX = Mathf.Max(x - radius, 0);
            int maxX = Mathf.Min(x + radius, tex.width - 1);
            int minY = Mathf.Max(y - radius, 0);
            int maxY = Mathf.Min(y + radius, tex.height - 1);

            Color[] pixels = tex.GetPixels(minX, minY, maxX - minX + 1, maxY - minY + 1);

            int w = maxX - minX + 1;
            int h = maxY - minY + 1;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius + 0.5f)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (px < minX || px > maxX || py < minY || py > maxY)
                            continue;

                        int localX = px - minX;
                        int localY = py - minY;
                        int idx = localY * w + localX;

                        // Antialiasing: suaviza el borde del círculo
                        if (dist > radius - 1f)
                        {
                            float t = Mathf.Clamp01(radius + 0.5f - dist);
                            Color baseColor = pixels[idx];
                            Color blended = Color.Lerp(baseColor, colorToUse, t);
                            pixels[idx] = blended;
                        }
                        else
                        {
                            pixels[idx] = colorToUse;
                        }
                    }
                }
            }
            tex.SetPixels(minX, minY, w, h, pixels);
            tex.Apply();
            if (debugMode) Debug.Log($"[LienzoDibujoController] DrawCall: Dibuja en pixel ({x}, {y}) optimizado.");
        }

        // Utilidad para convertir la posición del mouse a coordenadas de textura
        private bool TryGetTextureCoords(PointerEventData eventData, out int px, out int py)
        {
            px = py = 0;
            Vector2 localCursor;
            RectTransform rectTransform = targetDrawImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
            {
                if (debugMode) Debug.LogWarning("[LienzoDibujoController] TryGetTextureCoords: No se pudo convertir la posición del click.");
                return false;
            }

            Rect rect = rectTransform.rect;
            float normX = (localCursor.x - rect.x) / rect.width;
            float normY = (localCursor.y - rect.y) / rect.height;

            if (normX < 0f || normX > 1f || normY < 0f || normY > 1f)
            {
                if (debugMode) Debug.LogWarning($"[LienzoDibujoController] TryGetTextureCoords: Cursor fuera del canvas (normX: {normX}, normY: {normY}).");
                return false;
            }

            Texture2D tex = targetDrawImage.sprite.texture;
            px = Mathf.Clamp(Mathf.FloorToInt(normX * tex.width), 0, tex.width - 1);
            py = Mathf.Clamp(Mathf.FloorToInt(normY * tex.height), 0, tex.height - 1);

            if (debugMode)
            {
                Debug.Log($"[LienzoDibujoController] TryGetTextureCoords: Click en pantalla {eventData.position}, local {localCursor}, normalizado ({normX:F2}, {normY:F2}), pixel ({px}, {py})");
            }
            return true;
        }

        // Dibuja al hacer click (único)
        public void OnPointerClick(PointerEventData eventData)
        {
            int px, py;
            if (TryGetTextureCoords(eventData, out px, out py))
            {
                DrawCall(px, py);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            if (debugMode) Debug.Log("[LienzoDibujoController] OnPointerDown: Mouse presionado.");
            int px, py;
            if (TryGetTextureCoords(eventData, out px, out py))
            {
                DrawCall(px, py);
                lastDrawnPixel = new Vector2Int(px, py); // Guardar para interpolación
            }
            else
            {
                lastDrawnPixel = null;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            lastDrawnPixel = null;
            if (debugMode) Debug.Log("[LienzoDibujoController] OnPointerUp: Mouse soltado.");
        }

        // Interpolación de dibujo
        public void OnDrag(PointerEventData eventData)
        {
            if (!isPointerDown) return;
            int px, py;
            if (TryGetTextureCoords(eventData, out px, out py))
            {
                if (lastDrawnPixel.HasValue)
                {
                    Vector2Int last = lastDrawnPixel.Value;
                    InterpolatedDraw(last.x, last.y, px, py);
                }
                else
                {
                    DrawCall(px, py);
                }
                lastDrawnPixel = new Vector2Int(px, py);
            }
        }

        // Interpolación de puntos entre dos píxeles
        private void InterpolatedDraw(int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawCall(x0, y0);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
    }
}