using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorTablaController : Singleton<EditorTablaController>
{
    public GameObject prefabFila, prefabCampo;

    public List<Fila> filas = new List<Fila>();

    public RectTransform contenedorFilas;

    public Slider porcentajeAnchoSlider, porcentajeAltoSlider;
    public TextMeshProUGUI porcentajeAnchoTexto, porcentajeAltoTexto, nombreTabla;

    [Header("Culling")]
    public ScrollRect scrollRect;
    public RectTransform viewport;
    [Tooltip("Margen extra en píxeles arriba/abajo del viewport antes de ocultar una fila.")]
    public float margenCulling = 300f;
    [Tooltip("Cuántas filas se evalúan por frame durante el culling asíncrono.")]
    public int filasPorFrameCulling = 50;

    private readonly Vector3[] _cornersViewport = new Vector3[4];
    private readonly Vector3[] _cornersFila = new Vector3[4];

    private bool _cullingDirty;
    private Coroutine _cullingCoroutine;

    private void OnEnable()
    {
        porcentajeAnchoSlider.value = 0;
        porcentajeAltoSlider.value = 0;
        ActualizarTamaño();

        if (scrollRect != null)
            scrollRect.onValueChanged.AddListener(OnScrollChanged);

        CargarJSON(AppManager.comisariaArchivoPath);
    }

    public Image iconoEstadoDescarga;
    public Sprite cargando;
    public Sprite error;
    public Sprite correcto;
    public Sprite normal;

    public void GuardarTablaFirebase()
    {
        iconoEstadoDescarga.sprite = cargando;

        iconoEstadoDescarga.transform.DOKill();
        iconoEstadoDescarga.transform.rotation = Quaternion.identity;
        iconoEstadoDescarga.transform.DORotate(new Vector3(0, 0, 360), 1f, RotateMode.FastBeyond360).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
        iconoEstadoDescarga.raycastTarget = false;
        
        AppManager.versionActualInfoMunicipios = UnityEngine.Random.Range(100000, 999999999);

        try
        {
            FirebaseStorageManager.singleton.SaveData("datosMunicipios",  AppManager.singleton.GuardarTablaLocal(), (isError, errorMessage) =>
            {
                if (isError)
                {
                    Debug.LogError($"Error al guardar datos en Firebase: {errorMessage}");
                    iconoEstadoDescarga.transform.DOKill();
                    iconoEstadoDescarga.sprite = error;
                    iconoEstadoDescarga.raycastTarget = true;
                    iconoEstadoDescarga.transform.rotation = Quaternion.identity;
                    return;
                }

                FirebaseStorageManager.singleton.SaveData("numeroVersion", AppManager.versionActualInfoMunicipios, (isError2, errorMessage2) =>
                {
                    iconoEstadoDescarga.raycastTarget = true;
                    iconoEstadoDescarga.transform.DOKill();
                    iconoEstadoDescarga.transform.rotation = Quaternion.identity;

                    if (isError2)
                    {
                        Debug.LogError($"Error al guardar datos en Firebase: {errorMessage2}");
                        iconoEstadoDescarga.sprite = error; 
                    }
                    else
                    {
                        iconoEstadoDescarga.sprite = correcto;
                    }


                }, false); 


            }, false, true);
        }
        catch (Exception)
        {
            iconoEstadoDescarga.transform.DOKill();
            iconoEstadoDescarga.sprite = error;
            iconoEstadoDescarga.raycastTarget = true;
            iconoEstadoDescarga.transform.rotation = Quaternion.identity;
            throw;
        }
    }

    private void OnDisable()
    {
        if (scrollRect != null)
            scrollRect.onValueChanged.RemoveListener(OnScrollChanged);

        if (_cullingCoroutine != null)
        {
            StopCoroutine(_cullingCoroutine);
            _cullingCoroutine = null;
        }

        LimpiarTabla();
    }

    private void OnScrollChanged(Vector2 _)
    {
        SolicitarCulling();
    }

    public void ActualizarTamaño()
    {
        porcentajeAnchoTexto.text = $"Ancho: {porcentajeAnchoSlider.value * 100f:0}%";
        porcentajeAltoTexto.text = $"Alto: {porcentajeAltoSlider.value * 100f:0}%";

        if (filas.Count == 0) return;

        float anchoCampo = (1 + porcentajeAnchoSlider.value) * 198;
        float altoFila = (1 + porcentajeAltoSlider.value) * 41;

        foreach (var item in filas)
        {
            if (item.campos != null)
            {
                for (int i = 0; i < item.campos.Count; i++)
                {
                    if (i == 0)
                    {
                        ((RectTransform)item.campos[i].transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 40);
                        continue;
                    }

                    ((RectTransform)item.campos[i].transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, anchoCampo);
                }
            }

            item.objetoFila.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, altoFila);
        }

        Canvas.ForceUpdateCanvases();
        SolicitarCulling();
    }

    // ---------- Culling asíncrono (sin desactivar GameObjects) ----------

    /// <summary>
    /// Marca que el culling necesita recalcularse y arranca la corrutina si no está corriendo.
    /// No hace el trabajo pesado inmediatamente: se procesa en los próximos frames.
    /// </summary>
    public void SolicitarCulling()
    {
        _cullingDirty = true;

        if (_cullingCoroutine == null && isActiveAndEnabled)
        {
            _cullingCoroutine = StartCoroutine(RutinaCulling());
        }
    }

    private IEnumerator RutinaCulling()
    {
        // Corre mientras haya trabajo pendiente. Cuando ya no queda nada por hacer, termina
        // y libera la referencia, para no dejar una corrutina infinita corriendo en vacío.
        while (_cullingDirty)
        {
            _cullingDirty = false;
            yield return ProcesarCullingEnLotes();
        }

        _cullingCoroutine = null;
    }

    private IEnumerator ProcesarCullingEnLotes()
    {
        if (viewport == null)
        {
            Debug.LogWarning("EditorTablaController: 'viewport' no está asignado en el Inspector, el culling no puede calcularse.");
            yield break;
        }
        if (filas.Count == 0) yield break;

        viewport.GetWorldCorners(_cornersViewport);
        float viewportMinY = _cornersViewport[0].y - margenCulling;
        float viewportMaxY = _cornersViewport[1].y + margenCulling;

        int procesadasEnLote = 0;

        for (int i = 0; i < filas.Count; i++)
        {
            Fila fila = filas[i];
            if (fila.objetoFila == null) continue;

            fila.objetoFila.GetWorldCorners(_cornersFila);
            float filaMinY = _cornersFila[0].y;
            float filaMaxY = _cornersFila[1].y;

            bool visible = filaMaxY >= viewportMinY && filaMinY <= viewportMaxY;

            if (fila.visible != visible)
            {
                fila.visible = visible;
                AplicarVisibilidad(fila, visible);
            }

            procesadasEnLote++;
            if (procesadasEnLote >= filasPorFrameCulling)
            {
                procesadasEnLote = 0;
                yield return null; // cede el frame, continúa en el siguiente
            }
        }
    }

    private void AplicarVisibilidad(Fila fila, bool visible)
    {
        // Primero el estado del InputField (interacción)...
        if (fila.campos != null)
        {
            for (int c = 0; c < fila.campos.Count; c++)
            {
                TMP_InputField campo = fila.campos[c];
                campo.enabled = visible;
                campo.textComponent.canvasRenderer.cull = !visible;
                campo.placeholder.canvasRenderer.cull = !visible;
            }
        }

        // ...y después el cull de gráficos (texto, placeholder, fondo),
        // para que quede como estado final y no lo pise TMP internamente.
        if (fila.graficos != null)
        {
            for (int g = 0; g < fila.graficos.Length; g++)
            {
                Graphic grafico = fila.graficos[g];
                if (grafico != null)
                    grafico.canvasRenderer.cull = !visible;
            }
        }
    }

    private void CachearGraficos(int indiceFila)
    {
        Fila fila = filas[indiceFila];

        // Barrido genérico: cubre fondo (Image), bordes, etc.
        Graphic[] graficosGenericos = fila.objetoFila.GetComponentsInChildren<Graphic>(true);

        // Referencias explícitas a texto y placeholder de cada InputField,
        // que son las que más nos interesa cullear y no queremos dejar al azar
        // del orden de instanciación interna de TMP_InputField.
        List<Graphic> lista = new List<Graphic>(graficosGenericos);

        if (fila.campos != null)
        {
            foreach (TMP_InputField campo in fila.campos)
            {
                if (campo == null) continue;

                if (campo.textComponent != null && !lista.Contains(campo.textComponent))
                    lista.Add(campo.textComponent);

                if (campo.placeholder is Graphic placeholderGraphic && !lista.Contains(placeholderGraphic))
                    lista.Add(placeholderGraphic);
            }
        }

        fila.graficos = lista.ToArray();
    }

    /// <summary>
    /// se instancian de arriba hacia abajo, y se les asigna un nombre de acuerdo a su orden
    /// </summary>
    public void CrearFila()
    {
        if (prefabFila == null)
        {
            Debug.LogError("prefabFila no está asignado en el inspector.");
            return;
        }
        if (contenedorFilas == null)
        {
            Debug.LogError("contenedorFilas no está asignado en el inspector.");
            return;
        }

        GameObject nuevaFila = Instantiate(prefabFila, contenedorFilas);
        nuevaFila.name = "Fila_" + filas.Count;
        filas.Add(new Fila()
        {
            objetoFila = (RectTransform)nuevaFila.transform,
            campos = new List<TMP_InputField>(),
            visible = true
        });
    }

    /// <summary>
    /// se deben instanciar en orden de izquierda a derecha
    /// </summary>
    public void CrearCampo(int indiceFila)
    {
        if (prefabCampo == null)
        {
            Debug.LogError("prefabCampo no está asignado en el inspector.");
            return;
        }
        if (indiceFila < 0 || indiceFila >= filas.Count)
        {
            Debug.LogError($"indiceFila fuera de rango: {indiceFila} (filas.Count = {filas.Count})");
            return;
        }

        Fila fila = filas[indiceFila];
        if (fila.objetoFila == null)
        {
            Debug.LogError($"La fila en el índice {indiceFila} no tiene objetoFila asignado.");
            return;
        }

        GameObject instancia = Instantiate(prefabCampo, fila.objetoFila);
        TMP_InputField nuevoCampo = instancia.GetComponent<TMP_InputField>();
        if (nuevoCampo == null)
        {
            Debug.LogError("prefabCampo no tiene un componente TMP_InputField en su raíz.");
            return;
        }

        nuevoCampo.name = "Campo_" + fila.campos.Count;
        fila.campos.Add(nuevoCampo);
    }

    /// <summary>
    /// Destruye todas las filas instanciadas y vacía la lista de filas.
    /// </summary>
    [Button]
    public void LimpiarTabla()
    {
        if (_cullingCoroutine != null)
        {
            StopCoroutine(_cullingCoroutine);
            _cullingCoroutine = null;
        }
        _cullingDirty = false;

        for (int i = contenedorFilas.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(contenedorFilas.GetChild(i).gameObject);
        }
        filas.Clear();
    }

    // ---------- Conversión a/desde JSON (arreglo de objetos) ----------

    static JToken ParseValorRapido(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return new JValue(string.Empty);

        char c = texto[0];
        bool pareceJson = c == '{' || c == '[' || c == '"' || c == '-' || char.IsDigit(c)
                        || texto == "true" || texto == "false" || texto == "null";

        if (pareceJson)
        {
            try
            {
                return JToken.Parse(texto);
            }
            catch
            {
                return new JValue(texto);
            }
        }

        return new JValue(texto);
    }

    public string ObtenerTablaActualJSON()
    {
        JArray data = new JArray();

        if (filas.Count < 2)
        {
            Debug.LogWarning("Se necesita al menos 1 fila de claves y 1 fila de valores para generar el JSON.");
            return data.ToString(Formatting.Indented);
        }

        List<TMP_InputField> filaClaves = filas[0].campos;

        int totalColumnas = filaClaves.Count;
        string[] claves = new string[totalColumnas];
        for (int c = 0; c < totalColumnas; c++)
        {
            claves[c] = filaClaves[c].text;
        }

        for (int f = 1; f < filas.Count; f++)
        {
            List<TMP_InputField> filaValores = filas[f].campos;
            JObject objeto = new JObject();

            int columnas = Mathf.Min(totalColumnas, filaValores.Count);
            for (int c = 0; c < columnas; c++)
            {
                string clave = claves[c];
                if (string.IsNullOrEmpty(clave)) continue;

                objeto[clave] = ParseValorRapido(filaValores[c].text);
            }

            data.Add(objeto);
        }

        return data.ToString(Formatting.Indented);
    }

    public void CargarDesdeJSON(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("La cadena JSON está vacía o es nula.");
            return;
        }

        JArray data;
        try
        {
            JToken parsed = JToken.Parse(json);
            if (parsed.Type != JTokenType.Array)
            {
                Debug.LogError("El JSON debe ser un arreglo de objetos (ej: [ {...}, {...} ]).");
                return;
            }
            data = (JArray)parsed;
        }
        catch (JsonException e)
        {
            Debug.LogError($"Error al interpretar el JSON: {e.Message}");
            return;
        }

        if (data.Count == 0)
        {
            Debug.LogWarning("El arreglo JSON está vacío, no hay nada que cargar.");
            return;
        }

        if (!(data[0] is JObject primerObjeto))
        {
            Debug.LogError("Los elementos del arreglo deben ser objetos JSON.");
            return;
        }

        string[] claves;
        {
            List<string> lista = new List<string>();
            foreach (var propiedad in primerObjeto.Properties())
            {
                lista.Add(propiedad.Name);
            }
            claves = lista.ToArray();
        }

        bool estabaActivo = contenedorFilas.gameObject.activeSelf;
        contenedorFilas.gameObject.SetActive(false);

        LimpiarTabla();

        CrearFila();
        for (int c = 0; c < claves.Length; c++)
        {
            CrearCampo(0);
            TMP_InputField campoClave = filas[0].campos[c];
            campoClave.text = claves[c];
            campoClave.interactable = false;
        }
        CachearGraficos(0);

        for (int i = 0; i < data.Count; i++)
        {
            if (!(data[i] is JObject objeto))
            {
                Debug.LogWarning($"El elemento en el índice {i} del arreglo no es un objeto, se omite.");
                continue;
            }

            int indiceFila = filas.Count;
            CrearFila();

            List<TMP_InputField> camposFila = filas[indiceFila].campos;

            for (int c = 0; c < claves.Length; c++)
            {
                CrearCampo(indiceFila);

                JToken valor = objeto[claves[c]];
                camposFila[c].text = valor == null
                    ? string.Empty
                    : (valor.Type == JTokenType.Object || valor.Type == JTokenType.Array
                        ? valor.ToString(Formatting.None)
                        : valor.ToString());
            }

            CachearGraficos(indiceFila);
        }

        contenedorFilas.gameObject.SetActive(estabaActivo);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contenedorFilas);

        ActualizarTamaño();
    }

    // ---------- Manejo de archivo ----------

    public void GuardarJSON(string ruta)
    {
        try
        {
            JArray data = new JArray();

            if (filas.Count >= 2)
            {
                List<TMP_InputField> filaClaves = filas[0].campos;
                int totalColumnas = filaClaves.Count;
                string[] claves = new string[totalColumnas];
                for (int c = 0; c < totalColumnas; c++)
                    claves[c] = filaClaves[c].text;

                for (int f = 1; f < filas.Count; f++)
                {
                    List<TMP_InputField> filaValores = filas[f].campos;
                    JObject objeto = new JObject();
                    int columnas = Mathf.Min(totalColumnas, filaValores.Count);
                    for (int c = 0; c < columnas; c++)
                    {
                        if (string.IsNullOrEmpty(claves[c])) continue;
                        objeto[claves[c]] = ParseValorRapido(filaValores[c].text);
                    }
                    data.Add(objeto);
                }
            }

            using (StreamWriter sw = new StreamWriter(ruta, false))
            using (JsonTextWriter writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
            {
                data.WriteTo(writer);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar JSON en '{ruta}': {e.Message}");
        }
    }

    public void CargarJSON(string ruta)
    {
        if (!File.Exists(ruta))
        {
            Debug.LogError($"No se encontró el archivo JSON en '{ruta}'");
            return;
        }

        string json;
        try
        {
            json = File.ReadAllText(ruta);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al leer JSON en '{ruta}': {e.Message}");
            return;
        }

        CargarDesdeJSON(json);
    }
}

[System.Serializable]
public class Fila
{
    public RectTransform objetoFila;
    public List<TMP_InputField> campos;

    [NonSerialized] public Graphic[] graficos;
    [NonSerialized] public bool visible = true;
}

[System.Serializable]
public class TablaData
{
    public string nombre;
}