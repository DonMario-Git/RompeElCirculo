using UnityEngine;

public class ConfiguracionController : MonoBehaviour
{
    public DropDownMunicipios dropDownMunicipiosDefault;

    private void OnEnable()
    {
        dropDownMunicipiosDefault.dropdown.value = AppManager.UserData.municipioID;
    }

    public void CambiarMunicipioYGuardar()
    {
        AppManager.UserData.municipioID = dropDownMunicipiosDefault.dropdown.value;
        AppManager.UserData.municipio = dropDownMunicipiosDefault.dropdown.options[dropDownMunicipiosDefault.dropdown.value].text;
        AppManager.singleton.GuardarDatosDisco();
    }
}
