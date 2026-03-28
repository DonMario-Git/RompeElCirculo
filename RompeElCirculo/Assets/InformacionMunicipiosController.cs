using AwesomeAttributes;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UtilidadesLaEME;

public class InformacionMunicipiosController : Singleton<InformacionMunicipiosController>
{
    public InformacionMunicipios[] informacionMunicipios;

    [Button(nameof(LoadAssignedTsv))]
    public TextAsset tsvAsset; 

    public void LoadAssignedTsv()
    {
        if (tsvAsset == null) return;

        try
        {
            informacionMunicipios = ParseTsv(tsvAsset.text).ToArray();

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

    #region Conversor TSV a InformacionMunicipios

    static IEnumerable<InformacionMunicipios> ParseTsv(string tsvText)
    {
        if (string.IsNullOrEmpty(tsvText))
            yield break;

        var lines = tsvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToArray();

        if (lines.Length == 0)
            yield break;

        string[] headerParts = SplitLine(lines[0]);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerParts.Length; i++)
        {
            var key = headerParts[i].Trim();
            if (!headerIndex.ContainsKey(key))
                headerIndex[key] = i;
        }

        for (int li = 1; li < lines.Length; li++)
        {
            var parts = SplitLine(lines[li]);
            InformacionMunicipios info = new InformacionMunicipios();

            string Get(string key)
            {
                if (headerIndex.TryGetValue(key, out var idx) && idx >= 0 && idx < parts.Length)
                    return parts[idx]?.Trim() ?? string.Empty;
                // try lowercase header match if exact not found
                var match = headerIndex.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (match != null && headerIndex.TryGetValue(match, out idx) && idx >= 0 && idx < parts.Length)
                    return parts[idx]?.Trim() ?? string.Empty;
                return string.Empty;
            }

            info.nombre = Get("nombre");
            info.nombreComisario = Get("nombreComisario");
            info.correoElectronico = Get("correoElectronico");

            info.nombreInspector = Get("nombreInspector");
            info.numeroInspeccionPolicia = Get("numeroInspeccionPolicia");
            info.correoInspector = Get("correoInspector");
            info.direccionInspeccionPolicia = Get("direccionInspeccionPolicia");

            info.nombrePersonero = Get("nombrePersonero");
            info.telefonoContactoPersoneria = Get("telefonoContactoPersoneria");
            info.correoPersonero = Get("correoPersonero");
            info.direccionPersoneria = Get("direccionPersoneria");

            yield return info;
        }
    }

    static string[] SplitLine(string line)
    {
        return line.Split(new[] { '\t' }, StringSplitOptions.None);
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
}
