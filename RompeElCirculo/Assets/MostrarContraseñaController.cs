using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MostrarContraseñaController : MonoBehaviour
{
    public TMP_InputField input;
    public Sprite ojoNormal;   // ojo abierto (mostrar)
    public Sprite ojoTachado;  // ojo con línea (ocultar)
    public Image im;           // imagen del botón que muestra el sprite
    public Button button;

    // Estado interno
    private bool isPasswordShown = false;
    private TMP_InputField.ContentType previousContentType = TMP_InputField.ContentType.Standard;

    void Awake()
    {
        // Intentar auto-asignar referencias si no están en el inspector
        if (input == null) input = GetComponentInChildren<TMP_InputField>();
        if (button == null) button = GetComponent<Button>();
        if (im == null && button != null) im = button.GetComponent<Image>();

        // Guardar tipo previo si no es contraseña
        previousContentType = input != null ? input.contentType : TMP_InputField.ContentType.Standard;

        // Si actualmente está en modo contraseña, marcar estado inicial como oculto
        if (input != null && input.contentType == TMP_InputField.ContentType.Password)
        {
            isPasswordShown = false;
            if (im != null && ojoTachado != null) im.sprite = ojoTachado;
            im.enabled = true;
        }
        else
        {
            isPasswordShown = true;
            if (im != null && ojoNormal != null) im.sprite = ojoNormal;
            im.enabled = false;
        }

        // Añadir listener al botón
        if (button != null)
        {
            button.onClick.RemoveListener(TogglePasswordVisibility);
            button.onClick.AddListener(TogglePasswordVisibility);
        }
    }

    public void TogglePasswordVisibility()
    {
        if (input == null) return;

        // Mantener posición del caret
        int caretPos = input.caretPosition;

        if (!isPasswordShown)
        {
            // Mostrar la contraseña: restaurar tipo previo (o Standard)
            input.contentType = previousContentType != TMP_InputField.ContentType.Password
                ? previousContentType
                : TMP_InputField.ContentType.Standard;

            isPasswordShown = true;
            if (im != null && ojoNormal != null) im.sprite = ojoNormal;
        }
        else
        {
            // Ocultar la contraseña: guardar tipo actual y poner Password
            previousContentType = input.contentType;
            input.contentType = TMP_InputField.ContentType.Password;

            isPasswordShown = false;
            if (im != null && ojoTachado != null) im.sprite = ojoTachado;
        }

        // Forzar actualización y restaurar caret/foco
        input.ForceLabelUpdate();
        input.ActivateInputField();
        input.caretPosition = Mathf.Clamp(caretPos, 0, input.text.Length);
    }
}
