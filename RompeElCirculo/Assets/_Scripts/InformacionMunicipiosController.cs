using NaughtyAttributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UtilidadesLaEME;

public class InformacionMunicipiosController : Singleton<InformacionMunicipiosController>
{
    public InformacionMunicipios[] informacionMunicipios;
    public TextAsset tsvAsset;

    [Button]
    public void LoadAssignedTsv()
    {
        if (tsvAsset == null) return;

        try
        {
            informacionMunicipios = ParseTsv(tsvAsset.text);

            foreach (var item in informacionMunicipios)
            {
                item.nombre = item.nombre.Trim().ToUpper().CorregirAcentos();
                item.nombreComisario = item.nombreComisario.Trim().ToUpper().CorregirAcentos();
                item.correoElectronico = item.correoElectronico.Trim().CorregirAcentos();

                item.nombreInspector = item.nombreInspector.Trim().ToUpper().CorregirAcentos();
                item.numeroInspeccionPolicia = item.numeroInspeccionPolicia.Trim().Replace(" ", "");
                item.correoInspector = item.correoInspector.Trim();
                item.direccionInspeccionPolicia = item.direccionInspeccionPolicia.Trim().CorregirAcentos();

                item.nombrePersonero = item.nombrePersonero.Trim().ToUpper().CorregirAcentos();
                item.telefonoContactoPersoneria = item.telefonoContactoPersoneria.Trim().Replace(" ", "");
                item.correoPersonero = item.correoPersonero.Trim();
                item.direccionPersoneria = item.direccionPersoneria.Trim().CorregirAcentos();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al convertir: {ex}");
        }
    }

    [Button]
    public void OrdenarElementos()
    {
        InformacionMunicipios.OrdenarMunicipios(informacionMunicipios);
    }

    #region Conversor TSV a InformacionMunicipios

    // Nombres de columna esperados, en el mismo orden que los campos del modelo.
    static readonly string[] Campos =
    {
        "nombre", "nombreComisario", "correoElectronico",
        "nombreInspector", "numeroInspeccionPolicia", "correoInspector", "direccionInspeccionPolicia",
        "nombrePersonero", "telefonoContactoPersoneria", "correoPersonero", "direccionPersoneria"
    };

    static InformacionMunicipios[] ParseTsv(string tsvText)
    {
        if (string.IsNullOrEmpty(tsvText))
            return Array.Empty<InformacionMunicipios>();

        // Split manual sin LINQ, evitando allocaciones extra de Where().ToArray().
        string[] rawLines = tsvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        string[] headerParts = null;
        Dictionary<string, int> headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        List<InformacionMunicipios> resultado = new List<InformacionMunicipios>(rawLines.Length);

        // Índices de columna resueltos UNA sola vez, no por cada fila.
        int[] indices = null;

        for (int li = 0; li < rawLines.Length; li++)
        {
            string line = rawLines[li];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split('\t');

            if (headerParts == null)
            {
                headerParts = parts;
                for (int i = 0; i < headerParts.Length; i++)
                {
                    string key = headerParts[i].Trim();
                    if (!headerIndex.ContainsKey(key))
                        headerIndex[key] = i;
                }

                // Resolver el índice de columna de cada campo una única vez.
                indices = new int[Campos.Length];
                for (int c = 0; c < Campos.Length; c++)
                {
                    indices[c] = headerIndex.TryGetValue(Campos[c], out int idx) ? idx : -1;
                }

                continue; // esta línea era el header, no un dato
            }

            InformacionMunicipios info = new InformacionMunicipios
            {
                nombre = GetValor(parts, indices[0]),
                nombreComisario = GetValor(parts, indices[1]),
                correoElectronico = GetValor(parts, indices[2]),

                nombreInspector = GetValor(parts, indices[3]),
                numeroInspeccionPolicia = GetValor(parts, indices[4]),
                correoInspector = GetValor(parts, indices[5]),
                direccionInspeccionPolicia = GetValor(parts, indices[6]),

                nombrePersonero = GetValor(parts, indices[7]),
                telefonoContactoPersoneria = GetValor(parts, indices[8]),
                correoPersonero = GetValor(parts, indices[9]),
                direccionPersoneria = GetValor(parts, indices[10]),
            };

            resultado.Add(info);
        }

        return resultado.ToArray();
    }

    static string GetValor(string[] parts, int indice)
    {
        if (indice < 0 || indice >= parts.Length)
            return string.Empty;
        return parts[indice]?.Trim() ?? string.Empty;
    }

    #endregion
}

[System.Serializable]
public class InformacionMunicipios
{
    public string nombre;
    public string nombreComisario;
    public string correoElectronico;

    public string nombreInspector;
    public string numeroInspeccionPolicia;
    public string correoInspector;
    public string direccionInspeccionPolicia;

    public string nombrePersonero;
    public string telefonoContactoPersoneria;
    public string correoPersonero;
    public string direccionPersoneria;

    public static void OrdenarMunicipios(InformacionMunicipios[] municipios)
    {
        Array.Sort(municipios, (a, b) =>
        {
            bool aEsCucuta = EsCucuta(a.nombre);
            bool bEsCucuta = EsCucuta(b.nombre);

            // CÚCUTA siempre primero
            if (aEsCucuta && !bEsCucuta)
                return -1;

            if (!aEsCucuta && bEsCucuta)
                return 1;

            // Orden alfabético para el resto
            return string.Compare(
                a.nombre,
                b.nombre,
                CultureInfo.CurrentCulture,
                CompareOptions.IgnoreCase);
        });
    }

    private static bool EsCucuta(string nombre)
    {
        return string.Equals(
            nombre?.Trim(),
            "CÚCUTA",
            StringComparison.CurrentCultureIgnoreCase);
    }
}