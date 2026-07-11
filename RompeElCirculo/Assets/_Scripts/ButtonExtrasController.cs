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
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using NaughtyAttributes;

[RequireComponent(typeof(Button))]
[ExecuteAlways]
public class ButtonExtrasController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [ReadOnly] public Button button;

    [Space(20)]
    [Header("Animations On Pointer Down")]

    public Vector3 targetscale_down = new Vector3(0.9f, 0.9f, 0.9f);
    public float time_down = 0.2f;
    public Ease ease_down = Ease.OutCubic;
    

    [Space(20)]
    [Header("Animations On Pointer Up")]

    public Vector3 targetscale_Up = Vector3.one;
    public float time_Up = 0.2f;
    public Ease ease_Up = Ease.OutCubic;

    private TweenerCore<Vector3, Vector3, VectorOptions> core;

    [Space(20)]
    [Header("Text Features")]
    public bool textFeatures;
    [ShowIf(nameof(textFeatures))][ReadOnly] public TextMeshProUGUI textButton;
    [ShowIf(nameof(textFeatures))] public Color defautColor, disableColor = new(1, 1, 1, 0.5f);

    [Space(20)]
    public bool desactivarDeUna;
    public bool shadowFeatures;
    public bool firebaseDependenciesFreatures;

    private Shadow shadow;

    [Space(20)]
    [Header("Eventos")]
    public UnityEvent OnPointerDown;
    public UnityEvent<bool> OnSetInteractable;

    private void OnDisable()
    {
        transform.localScale = targetscale_Up;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    private async void OnEnable()
    {
        if (firebaseDependenciesFreatures)
        {
            SetInteractable(false);
            if (button.image != null) button.image.raycastTarget = false;
            if (textButton != null) textButton.raycastTarget = false;

            bool enable = FirebaseStorageManager.singleton.IsInitialized && await FirebaseStorageManager.IsInternetAvailable();
            if (button.image != null) button.image.raycastTarget = enable;
            if (textButton != null) textButton.raycastTarget = enable;
            SetInteractable(enable);

            if (!enable) return;
        }

        if (desactivarDeUna)
        {
            button.image.raycastTarget = true;
            if (textButton != null) textButton.raycastTarget = true;
        }
    }

    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();  
        }

        if (textFeatures)
        {
            if (textButton == null)
            {
                textButton = GetComponentInChildren<TextMeshProUGUI>();

                if (textButton == null)
                {
                    Debug.LogWarning($"No se encontro componente TextMeshProUGUI en '{gameObject.name}'.", gameObject);
                }
                else
                {
                    defautColor = textButton.color;
                }  
            }
            else
            {
                textButton.color = button.interactable ? defautColor : defautColor * disableColor;
            }      
        }
    }

    private void Update()
    {
        if (shadowFeatures)
        {
            if (shadow == null)
            {
                TryGetComponent(out shadow);
            }
            else
            {
                shadow.effectColor = new Color(button.image.color.r * 0.7f, button.image.color.g * 0.7f, button.image.color.b * 0.7f, 1);
            }
        }
        
        if (textFeatures)
        {
            if (textButton != null)
            {
                textButton.color = button.interactable ? defautColor : defautColor * disableColor;
                textButton.raycastTarget = false;
            }
        }
    }

    public void Reset()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (textFeatures && textButton == null)
        {
            textButton = GetComponentInChildren<TextMeshProUGUI>();

            if (textButton == null)
            {
                Debug.LogWarning($"No se encontro componente TextMeshProUGUI en '{gameObject.name}'.");
            }
            else
            {
                defautColor = textButton.color;
            }
        }
    }

    public void SetInteractable(bool value)
    {
        if (button.interactable == value) return;
        button.interactable = value;
        OnSetInteractable?.Invoke(value);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            core.Kill();
            if (time_down > 0) core = transform.DOScale(targetscale_down, time_down).SetEase(ease_down);
            OnPointerDown?.Invoke();
        }      
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
        {
            core.Kill();
            if (time_Up > 0) core = transform.DOScale(targetscale_Up, time_Up).SetEase(ease_Up);
        }      
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (desactivarDeUna)
        {
            button.image.raycastTarget = false;
            if (textButton != null) textButton.raycastTarget = false;
        }
    }
}
