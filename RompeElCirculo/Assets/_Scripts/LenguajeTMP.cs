using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;

public class LenguajeTMP : MonoBehaviour
{
    public TextMeshProUGUI tmpro;
    public LenguageString textoIdioma = new();

    public void OnValidate()
    {
        if (tmpro != null && textoIdioma != null) 
        {
            tmpro.text = textoIdioma.GetValue();
        }
    }
}

[System.Serializable]
public class LenguageString
{
    [TextArea]
    public string ES, EN;
    public bool ignore;

    public string GetValue()
    {
        AppManager s = Object.FindAnyObjectByType<AppManager>();

        if (s != null)
        {
            var a = Object.FindAnyObjectByType<AppManager>().idiomaActual == IDIOMA.ESPAÑOL ? ES : EN;
            return ignore ? ES : a;
        }
        else
        {
            return ES;
        }       
    }
}

public enum IDIOMA
{
    ESPAÑOL, ENGLISH
}
