using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using Coffee.UIExtensions;
using AwesomeAttributes;

public class FinalScreenController : MonoBehaviour
{
    public static FinalScreenController instance;
    public Image background;
    public ButtonExtrasController[] buttons;

    public Sprite[] spritesIcons;

    public RandomString textosBien;
    public RandomString textosMal;


    [Readonly] public bool isPassed;

    [Space(20)]
    public FinalScreen[] finalScreen;

    private void Awake()
    {
        instance = this;
    }

    public void Close()
    {
        int indice = 0;

        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                indice = 0;
                break;
            case ScreenOrientation.LandscapeLeft:
                indice = 1;
                break;
        }

        finalScreen[indice].forground.rectTransform.DOScaleY(0, 0.3f).SetEase(Ease.InBack);
        background.DOColor(Color.clear, 0.5f).OnComplete(() =>
        {
            finalScreen[indice].objeto.SetActive(false);
            background.raycastTarget = false;
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Evaluate(true);
        }
    }

    public void Evaluate(bool isPass)
    {
        int indice = 0;

        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                indice = 0;
                break;
            case ScreenOrientation.LandscapeLeft:
                indice = 1;
                break;
        }

        finalScreen[indice].objeto.SetActive(true);

        if (isPass)
        {
            finalScreen[indice].im_icon.sprite = spritesIcons[0];
            finalScreen[indice].tmp_text.text = textosBien.GetRandomValue();
            finalScreen[indice].buttons[0].gameObject.SetActive(true);
            finalScreen[indice].buttons[1].gameObject.SetActive(false);
            AudioPlayer.singleton.PlayAudio(1);
            isPassed = true;
        }
        else
        {
            finalScreen[indice].im_icon.sprite = spritesIcons[1];
            finalScreen[indice].tmp_text.text = textosMal.GetRandomValue();
            finalScreen[indice].buttons[0].gameObject.SetActive(false);
            finalScreen[indice].buttons[1].gameObject.SetActive(true);
            AudioPlayer.singleton.PlayAudio(5);
            isPassed = false;
        }


        finalScreen[indice].confeti.Stop();
        background.raycastTarget = true;
        background.color = Color.clear;
        finalScreen[indice].forground.rectTransform.localScale = new Vector3(1, 0, 1);
        finalScreen[indice].im_icon.rectTransform.localScale = Vector3.zero;
        finalScreen[indice].tmp_text.rectTransform.localScale = Vector3.zero;

        foreach (var item in finalScreen[indice].buttons)
        {
            item.transform.localScale = Vector3.zero;
        }

        background.DOColor(new Color(0, 0, 0, 0.3f), 0.3f);

        finalScreen[indice].forground.rectTransform.DOScaleY(1, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            finalScreen[indice].im_icon.rectTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            finalScreen[indice].tmp_text.rectTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(0.2f);
            if (isPassed) finalScreen[indice].confeti.Play();

            foreach (var item in finalScreen[indice].buttons)
            {
                item.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(0.4f);
            }
        });
    }

    [System.Serializable]
    public struct FinalScreen
    {
        public GameObject objeto;
        public Image forground;
        public Image im_icon;
        public TextMeshProUGUI tmp_text;
        public ButtonExtrasController[] buttons;
        public UIParticle confeti;
    }
}

[System.Serializable]
public class RandomString
{
    [SerializeField] private LenguageString[] strings;

    public string GetRandomValue()
    {
        if (strings.Length > 0)
        {
            return strings[Random.Range(0, strings.Length - 1)].GetValue();
        }
        else
        {
            Debug.LogWarning("The random string is emty");
            return string.Empty;
        }
    }
}
