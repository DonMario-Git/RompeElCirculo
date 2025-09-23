using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using UnityEditor;
using System;
using UtilidadesLaEME;

public class HelpWindowController : MonoBehaviour
{
    public static HelpWindowController singleton;

    public TextMeshProUGUI TMP_Title, TMP_Content;
    public HelpText[] helpTexts;

    [Space(20)]
    public Image imageFront;
    public bool isOpened;

    public event Action OnPause, OnDespause;

    private void Awake()
    {
        singleton = this;
        gameObject.Disable();
    }

    public void OpenHelpMenu(bool playAudio, MinigameController minigame)
    {
        int indice = 0;

        switch (AppManager.ScreenOrientacion)
        {
            case ScreenOrientation.Portrait:
                indice = 0;
                break;
            case ScreenOrientation.LandscapeLeft:
                indice = 1;
                break;
        }

        if (!isOpened)
        {
            helpTexts[indice].TMP_Title.color = Color.white;
            helpTexts[indice].TMP_Content.color = Color.white;
            helpTexts[indice].TMP_Title.transform.localScale = Vector3.one * 1.1f;
            helpTexts[indice].TMP_Content.transform.localScale = Vector3.one * 1.1f;
            helpTexts[indice].TMP_Title.text = minigame.l_title.GetValue();
            helpTexts[indice].TMP_Content.text = minigame.l_content.GetValue();
            helpTexts[indice].SetActive(true);
            minigame.textoTitulo.text = minigame.l_title.GetValue();
            gameObject.SetActive(true);
            helpTexts[indice].TMP_Content.transform.parent.gameObject.SetActive(true);
            imageFront.DOFade(0.8f, 0.2f);
            helpTexts[indice].TMP_Title.transform.DOScale(1, 0.3f).SetEase(Ease.OutQuart);
            helpTexts[indice].TMP_Content.transform.DOScale(1, 0.3f).SetEase(Ease.OutQuart).SetDelay(0.1f);
            if (playAudio) AudioPlayer.singleton.PlayAudio(3);
            isOpened = true;      
        }

        OnPause?.Invoke();
    }

    public void CloseHelpMenu(bool activarEvento)
    {
        int indice = 0;

        switch (AppManager.ScreenOrientacion)
        {
            case ScreenOrientation.Portrait:
                indice = 0;
                break;
            case ScreenOrientation.LandscapeLeft:
                indice = 1;
                break;
        }

        if (isOpened)
        {
            imageFront.DOFade(0, 0.1f);
            helpTexts[indice].SetActive(false);
            helpTexts[indice].TMP_Title.DOColor(new Color(1, 1, 1, 0), 0.2f);
            helpTexts[indice].TMP_Content.DOColor(new Color(1, 1, 1, 0), 0.2f);
            helpTexts[indice].TMP_Title.transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuart);
            helpTexts[indice].TMP_Content.transform.DOScale(1.1f, 0.22f).SetEase(Ease.OutQuart).SetDelay(0.1f).OnComplete(() => {
                gameObject.SetActive(false);
                helpTexts[indice].TMP_Content.transform.parent.gameObject.SetActive(false);
                if (activarEvento) OnDespause?.Invoke();
            });

            isOpened = false;
        }  
    }

    [System.Serializable]
    public struct HelpText
    {
        public TextMeshProUGUI TMP_Title, TMP_Content;
        public GameObject[] desactivarAlSalir;

        public readonly void SetActive(bool value)
        {
            foreach (var item in desactivarAlSalir)
            {
                item.SetActive(value);
            }
        }
    }
}
