using DG.Tweening;
using UnityEngine;
using UtilidadesLaEME;

public class LogginMenuController : MonoBehaviour
{
    public Pesta�a[] pesta�as;
    public int indicePesta�aActual;
    public bool puedeCambiarPesta�a = true;

    private void Awake()
    {
        AppManager.OnBackPressed += OnBackPress;
    }

    private void OnBackPress()
    {
        if (!gameObject.activeInHierarchy) return;

        if (indicePesta�aActual != 0)
        {
            CambiarPesta�a(0);
        }
        else
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
    {
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        currentActivity.Call("moveTaskToBack", true);
    }
#endif
        }
    }

    private void OnValidate()
    {
        foreach (var item in pesta�as)
        {
            item.rtr.gameObject.Disable();
            item.rtr.localPosition = new Vector3(800, 0, 0);
        }

        pesta�as[indicePesta�aActual].rtr.gameObject.Enable();
        pesta�as[indicePesta�aActual].rtr.localPosition = Vector3.zero;
    }

    public void CambiarPesta�a(int indice)
    {
        if (!puedeCambiarPesta�a) return;
        puedeCambiarPesta�a = false;
        bool estaDerecha = pesta�as[indice].indiceDireccion > pesta�as[indicePesta�aActual].indiceDireccion;

        foreach (var item in pesta�as)
        {
            item.rtr.gameObject.Disable();
            item.rtr.localPosition = new Vector3(800 * (!estaDerecha ? 1 : -1), 0, 0);
        }

        pesta�as[indicePesta�aActual].rtr.gameObject.Enable(); 
        pesta�as[indicePesta�aActual].rtr.localPosition = Vector3.zero;
  
        pesta�as[indice].rtr.SetSiblingIndex(pesta�as[indicePesta�aActual].rtr.childCount - 1);
        pesta�as[indicePesta�aActual].rtr.DOKill();
        pesta�as[indicePesta�aActual].rtr.DOLocalMoveX(160 * (estaDerecha ? 1 : -1), 0.4f).SetEase(Ease.OutExpo);

        pesta�as[indice].rtr.gameObject.Enable();
        pesta�as[indice].rtr.DOKill();
        pesta�as[indice].rtr.DOLocalMoveX(0, 0.4f).SetEase(Ease.OutExpo).OnComplete(() =>
        {
            pesta�as[indicePesta�aActual].rtr.gameObject.Disable();
            indicePesta�aActual = indice;
            puedeCambiarPesta�a = true;
        });    
    }

    [System.Serializable]
    public struct Pesta�a
    {
        public RectTransform rtr;
        public int indiceDireccion;
    }
}
