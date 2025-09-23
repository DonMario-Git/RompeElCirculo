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

public class SquearePointsController : MonoBehaviour
{
    public ButtonExtrasController buttonExtras;
    public Image squareDark;
    public Image tick;
    public Image x;  

    public void Win()
    {
        SetActive(true);
        tick.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void Bad()
    {
        SetActive(true);
        x.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void SetActive(bool active)
    {
        squareDark.gameObject.SetActive(active);
        buttonExtras.button.interactable = !active;
    }

    private void OnDisable()
    {
        SetActive(false);
        tick.transform.DOKill();
        x.transform.DOKill();
        tick.transform.localScale = Vector3.zero;
        x.transform.localScale = Vector3.zero;
    }
}
