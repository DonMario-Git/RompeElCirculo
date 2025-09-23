using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using TMPro;
using System.IO;

public class CuadroRegisterController : MonoBehaviour
{
    public TMP_InputField inputName;
    public ButtonExtrasController buttonRegisterName;

    public void ConfirmButton()
    {
        if (inputName.text != "")
        {
            if (!buttonRegisterName.button.interactable)
            {
                buttonRegisterName.SetInteractable(true);
            }
        }
        else
        {
            if (buttonRegisterName.button.interactable)
            {
                buttonRegisterName.SetInteractable(false);
            }
        }
    }
}
