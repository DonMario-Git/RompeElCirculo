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

public class PointJoinMinigameManager : MonoBehaviour
{
    public DraggableInPlaceController[] places;

    [Readonly]public bool isComplete;

    private void OnDisable()
    {
        isComplete = false;
    }

    public void Confirm()
    {
        if (!isComplete)
        {
            bool incomplete = false;

            foreach (var item in places)
            {
                if (!item.isComplete) incomplete = true;
            }

            if (incomplete) return;

            isComplete = true;
            FinalScreenController.instance.Evaluate(true);
        }    
    }
}
