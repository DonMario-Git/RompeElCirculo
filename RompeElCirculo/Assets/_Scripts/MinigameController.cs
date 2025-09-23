using TMPro;
using UnityEngine;

public class MinigameController : MonoBehaviour
{
    public Sprite spritePortada;
    public TextMeshProUGUI textoTitulo;
    public AudioClip voiceHelpTexts;

    
    public LenguageString l_title, l_content;

    public void EnableHelpButton()
    {
        AppManager.singleton.OpenHelpWindow(true);
    }
}
