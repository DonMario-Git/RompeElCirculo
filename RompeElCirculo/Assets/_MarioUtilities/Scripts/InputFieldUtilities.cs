using TMPro;
using UnityEngine;
using UtilidadesLaEME;

[RequireComponent(typeof(TMP_InputField))]
[ExecuteAlways]
public class InputFieldUtilities : AsterizcoObligatorio, ICampoObligatorioComprobacion
{
    public TMP_InputField inputField;
    public bool borrarTextoAlHabilitar = true;

    private void OnValidate()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }

        ToggleObligatorio();
    }

    public void ToggleObligatorio()
    {
        obligatorio_TMP.gameObject.SetActive(!EstaContestado() && campoObligatorio);
    }

    private void OnEnable()
    {
        if (borrarTextoAlHabilitar)
        {
            inputField.text = string.Empty;
        }

        ToggleObligatorio();
    }

    public bool EstaContestado()
    {
        contestado = !string.IsNullOrEmpty(inputField.text.TrimEdges());
        return contestado;
    }
}
