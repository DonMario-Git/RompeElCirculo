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
using AwesomeAttributes;

public class SelectTheCorrectController : MonoBehaviour
{
    [Readonly]public bool isCorrect;

    public UnityEvent OnCorrect;
    public UnityEvent OnIncorrect;

    public GameObject[] objects;

    public AudioEffect effectOnDown;

    public void SelectIsCorrect(bool correct)
    {
        if (correct)
        {
            isCorrect = true;
            if (effectOnDown != null) AudioPlayer.singleton.PlayAudioEffect(effectOnDown);
            OnCorrect?.Invoke();
        }
        else
        {
            isCorrect = false;
            OnIncorrect?.Invoke();
        }
    }

    private void OnDisable()
    {
        isCorrect = false;
    }
}
