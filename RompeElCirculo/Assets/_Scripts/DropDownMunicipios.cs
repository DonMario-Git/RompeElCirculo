using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropDownMunicipios : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private void Awake()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null) return;

        dropdown.options.Clear();

        List<TMP_Dropdown.OptionData> opciones = new();

        if (InformacionMunicipiosController.singleton.informacionMunicipios == null) return;
        if (InformacionMunicipiosController.singleton.informacionMunicipios.Length == 0) return;

        foreach (var item in InformacionMunicipiosController.singleton.informacionMunicipios)
        {
            opciones.Add(new TMP_Dropdown.OptionData(item.nombre));
        }

        dropdown.options = opciones;

        if (AppManager.UserData != null)
        {
            dropdown.value = AppManager.UserData.municipioID;
        }
    }

    private void OnValidate()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null) return;

        dropdown.options.Clear();

        List<TMP_Dropdown.OptionData> opciones = new();

        if (InformacionMunicipiosController.singleton.informacionMunicipios == null) return;
        if (InformacionMunicipiosController.singleton.informacionMunicipios.Length == 0) return;

        foreach (var item in InformacionMunicipiosController.singleton.informacionMunicipios)
        {
            opciones.Add(new TMP_Dropdown.OptionData(item.nombre));
        }

        dropdown.options = opciones;
    }
}
