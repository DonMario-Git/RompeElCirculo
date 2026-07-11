using TMPro;
using UnityEngine;

public class InfoComisariaController : MonoBehaviour
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
            info.Direccion_ComisariaFamilia,
            "-Dirección no disponible-"
        );

        ActualizarCampo(
            telefono,
            info.NumeroTelefonico_ComisariaFamilia,
            "-Teléfono no disponible-"
        );

        ActualizarCampo(
            email,
            info.CorreoElectronico_ComisariaFamilia,
            "-Correo no disponible-"
        );
    }

    private void ActualizarCampo(TextMeshProUGUI texto, string valor, string mensajeNoDisponible)
    {
        bool disponible = !string.IsNullOrWhiteSpace(valor);

        texto.text = disponible ? valor : mensajeNoDisponible;

        if (texto.transform.parent.TryGetComponent(out TMP_Dropdown dropdown))
        {
            dropdown.interactable = disponible;
        }
    }

    public void BuscarDireccion()
    {
        AppManager.singleton.AbrirDireccion(textoDireccion.text);
    }

    public void MarcarContacto()
    {
        AppManager.singleton.LlamarPorWhatsApp(telefono.text);
    }

    public void EnviarCorreo()
    {
        AppManager.singleton.RedactarCorreo(email.text);
    }
}