using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(TMP_InputField))]
public class InputFieldScrollFix : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScrollRect parentScrollRect;
    public TMP_InputField inputField;

    private void OnValidate()
    {
        if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
    }

    private void Awake()
    {
        if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
        if (inputField == null) inputField = GetComponent<TMP_InputField>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        inputField.enabled = false;
        parentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        parentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        parentScrollRect.OnEndDrag(eventData);
        StartCoroutine(ReenableInputField());
    }

    private IEnumerator ReenableInputField()
    {
        yield return new WaitForEndOfFrame();
        inputField.enabled = true;
    }
}