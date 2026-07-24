using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoSaludReemplazoInformacion : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Dropdown dropdownMunicipios;
    [SerializeField] private TextMeshProUGUI textoDireccion;
    [SerializeField] private TextMeshProUGUI telefono;
    [SerializeField] private TextMeshProUGUI email;

    private void OnEnable()
    {
        if (AppManager.UserData == null || AppManager.informacionMunicipios == null)
            return;

        dropdownMunicipios.value = AppManager.UserData.municipioID;
        ActualizarConID(AppManager.UserData.municipioID);
    }

    public void ActualizarConID(int id)
    {
        if (AppManager.informacionMunicipios == null)
            return;

        var info = AppManager.informacionMunicipios[id];

        ActualizarCampo(
            textoDireccion,
            info.Direccion_Salud,
            "-Dirección no disponible-"
        );

        ActualizarCampo(
            telefono,
            info.NumeroTelefono_Salud,
            "-Teléfono no disponible-"
        );

        ActualizarCampo(
            email,
            info.CorreoElectronico_Salud,
            "-Correo no disponible-"
        );
    }

    private void ActualizarCampo(TextMeshProUGUI texto, string valor, string mensajeNoDisponible)
    {
        bool disponible = !string.IsNullOrWhiteSpace(valor);

        texto.text = disponible ? valor : mensajeNoDisponible;

        if (texto.transform.parent.TryGetComponent(out Button button))
        {
            button.interactable = disponible;
        }
    }

    public void BuscarDireccion()
    {
        AppManager.singleton.AbrirDireccion(textoDireccion.text);
    }

    public void MarcarContacto()
    {
        AppManager.singleton.LlamarPorTelefono(telefono.text);
    }

    public void EnviarCorreo()
    {
        AppManager.singleton.RedactarCorreo(email.text);
    }
}
