using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using TMPro;
using System;
using UtilidadesLaEME;

public class WordInputControllerr : MonoBehaviour
{
    public LenguageString correctWorld;

    public TMP_InputField inputField;

    public UnityEvent OnCorrect;
    public UnityEvent OnIncorrect;
    public AudioEffect audioOnComplete;
    public bool IsCorrect;

    public void Compare()
    {
        if (inputField.text.TrimEdges().Equals(correctWorld.GetValue().TrimEdges(), StringComparison.OrdinalIgnoreCase))
        {
            IsCorrect = true;
            if (audioOnComplete.clip != null) AudioPlayer.singleton.PlayAudioEffect(audioOnComplete);
            OnCorrect?.Invoke();
        }
        else
        {
            IsCorrect = false;
            OnIncorrect?.Invoke();
        }
    }
}
