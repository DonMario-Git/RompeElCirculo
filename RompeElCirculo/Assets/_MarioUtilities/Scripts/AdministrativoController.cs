using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UtilidadesLaEME;

public class AdministrativoController : MonoBehaviour
{
    public InputFieldUtilities textoBusqueda;

    public GameObject pantallaNegra;
    public Transform ruedaCarga;

    public TextMeshProUGUI tmp_nombre, tmp_tipoDocumento, tmp_documento, tmp_edad, tmp_sexo, tmp_razon, tmp_fechaReporte, tmp_direccion;
    private string numeroContacto;
    public TextMeshProUGUI contactoButton;

    public Caso casoActual;

    public TMP_Dropdown dd_tipoAvance, dd_estadoCaso;
    public InputFieldUtilities descripcionAvance;

    public ButtonExtrasController btn_gestionar, btn_remitir, btn_archivar, btn_guardar;
    public GameObject objetoOpciones;

    private bool consultaRespondida = false;

    public void Contactar()
    {
        AppManager.singleton.LlamarPorWhatsApp(numeroContacto);
    }

    private void OnEnable()
    {
        ReiniciarTodo();
    }

    private void ReiniciarTodo()
    {
        tmp_nombre.gameObject.DesactivarObjeto();
        tmp_tipoDocumento.gameObject.DesactivarObjeto();
        tmp_documento.gameObject.DesactivarObjeto();
        tmp_edad.gameObject.DesactivarObjeto();
        tmp_sexo.gameObject.DesactivarObjeto();
        tmp_razon.gameObject.DesactivarObjeto();
        tmp_fechaReporte.gameObject.DesactivarObjeto();
        tmp_direccion.gameObject.DesactivarObjeto();
        contactoButton.transform.parent.gameObject.DesactivarObjeto();
        dd_estadoCaso.value = 0;
        dd_tipoAvance.value = 0;
        descripcionAvance.inputField.text = string.Empty;
        objetoOpciones.DesactivarObjeto();
        casoActual = null;
        btn_gestionar.button.interactable = false;
        btn_remitir.button.interactable = false;
        btn_archivar.button.interactable = false;
        btn_guardar.button.interactable = false;
    }

    public void GuardarCaso()
    {
        casoActual.estadoDelCaso = dd_estadoCaso.value;
        casoActual.tipoAvance = dd_tipoAvance.value;
        casoActual.descripcionDeAvance = descripcionAvance.inputField.text.TrimEdges();

        btn_guardar.button.interactable = false;
        FirebaseStorageManager.singleton.EditarReporteCaso(casoActual.ID, casoActual, (error) => {
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning(error);
            }

            btn_guardar.button.interactable = true;
        });
    }

    public void GestionarCaso()
    {
        btn_guardar.button.interactable = true;
        btn_remitir.button.interactable = true;
        btn_gestionar.button.interactable = false;

        descripcionAvance.inputField.text = casoActual.descripcionDeAvance;
        dd_tipoAvance.value = casoActual.tipoAvance;
        dd_estadoCaso.value = casoActual.estadoDelCaso;

        objetoOpciones.ActivarObjeto();
    }

    public void ConsultarCaso()
    {
        if (string.IsNullOrEmpty(textoBusqueda.inputField.text))
        {
            tmp_nombre.gameObject.ActivarObjeto();
            tmp_nombre.color = Color.red;
            tmp_nombre.text = "Introduzca un ID valido";
            return;
        }

        ReiniciarTodo();

        pantallaNegra.ActivarObjeto();
        ruedaCarga.gameObject.ActivarObjeto();
        ruedaCarga.DOKill();
        ruedaCarga.DORotate(new Vector3(0, 0, 360), 1, RotateMode.FastBeyond360)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart);

        consultaRespondida = false;
        float timeout = 5f;
        Invoke(nameof(OnConsultaTimeout), timeout);

        FirebaseStorageManager.singleton.BuscarCasoPorID(textoBusqueda.inputField.text.TrimEdges(), (caso, error) => { 
            if (consultaRespondida) return;
            consultaRespondida = true;
            CancelInvoke(nameof(OnConsultaTimeout));
            Debug.Log("Callback recibido");
            try
            {
                if (string.IsNullOrEmpty(error))
                {
                    if (caso == null)
                    {
                        tmp_nombre.gameObject.ActivarObjeto();
                        tmp_nombre.color = Color.red;
                        tmp_nombre.text = "No se encontró el caso (objeto nulo)";
                    }
                    else
                    {
                        tmp_nombre.color = Color.black;
                        tmp_nombre.text = $"Nombre: {caso.nombreCompleto}";
                        tmp_tipoDocumento.text = $"Tipo de documento: {caso.tipoDocumento}";
                        tmp_documento.text = $"N° documento: {caso.numeroDocumento}";

                        if (caso.fechaNacimiento != "[Anonimo]")
                        {
                            tmp_edad.text = $"Edad: {Utilities.CalcularEdad(Utilities.StringToDateTime(caso.fechaNacimiento), DateTime.Now)}";
                        }
                        else
                        {
                            tmp_edad.text = "[Anonimo]";
                        }

                        tmp_sexo.text = $"Sexo: {caso.sexo}";
                        tmp_razon.text = $"Razón: {caso.hechoAReportar}";
                        tmp_fechaReporte.text = caso.fechaCaso;
                        tmp_direccion.text = caso.direccion;

                        numeroContacto = caso.numeroCelular;
                        contactoButton.text = $"Contactar al: {caso.numeroCelular}";
                        contactoButton.transform.parent.gameObject.SetActive(caso.numeroCelular != "[Anonimo]");

                        tmp_nombre.gameObject.ActivarObjeto();
                        tmp_tipoDocumento.gameObject.ActivarObjeto();
                        tmp_documento.gameObject.ActivarObjeto();
                        tmp_edad.gameObject.ActivarObjeto();
                        tmp_sexo.gameObject.ActivarObjeto();
                        tmp_razon.gameObject.ActivarObjeto();
                        tmp_fechaReporte.gameObject.ActivarObjeto();
                        tmp_direccion.gameObject.ActivarObjeto();    

                        casoActual = caso;

                        btn_gestionar.button.interactable = true;
                    }
                }
                else
                {
                    tmp_nombre.gameObject.ActivarObjeto();
                    tmp_nombre.color = Color.red;
                    tmp_nombre.text = error;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Excepción en callback ConsultarCaso: {ex.Message}\n{ex.StackTrace}");
                tmp_nombre.gameObject.ActivarObjeto();
                tmp_nombre.color = Color.red;
                tmp_nombre.text = "Error inesperado al mostrar el caso";
            }
            Debug.Log("Fin del callback, limpiando carga");
            ruedaCarga.transform.DOKill();
            ruedaCarga.gameObject.DesactivarObjeto();
            pantallaNegra.DesactivarObjeto();
        });
    }

    private void OnConsultaTimeout()
    {
        if (consultaRespondida) return;
        consultaRespondida = true;
        tmp_nombre.gameObject.ActivarObjeto();
        tmp_nombre.color = Color.red;
        tmp_nombre.text = "La consulta tardó demasiado en responder";
        ruedaCarga.transform.DOKill();
        ruedaCarga.gameObject.DesactivarObjeto();
        pantallaNegra.DesactivarObjeto();
    }
}
