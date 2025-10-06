using DG.Tweening;
using UnityEngine;
using UtilidadesLaEME;

public class LogginMenuController : MonoBehaviour
{
    public static LogginMenuController singleton;
    public Pestaña[] pestañas;
    public int indicePestañaActual;
    public bool puedeCambiarPestaña = true;

    private void Awake()
    {
        AppManager.OnBackPressed += OnBackPress;
        singleton = this;
    }

    private void OnBackPress()
    {
        if (!gameObject.activeInHierarchy) return;

        if (indicePestañaActual != 0)
        {
            CambiarPestaña(0);
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
        foreach (var item in pestañas)
        {
            item.rtr.gameObject.DesactivarObjeto();
            item.rtr.localPosition = new Vector3(800, 0, 0);
        }

        pestañas[indicePestañaActual].rtr.gameObject.ActivarObjeto();
        pestañas[indicePestañaActual].rtr.localPosition = Vector3.zero;
    }

    public void CambiarPestaña(int indice)
    {
        if (!puedeCambiarPestaña) return;
        puedeCambiarPestaña = false;
        bool estaDerecha = pestañas[indice].indiceDireccion > pestañas[indicePestañaActual].indiceDireccion;

        foreach (var item in pestañas)
        {
            item.rtr.gameObject.DesactivarObjeto();
            item.rtr.localPosition = new Vector3(800 * (!estaDerecha ? 1 : -1), 0, 0);
        }

        pestañas[indicePestañaActual].rtr.gameObject.ActivarObjeto(); 
        pestañas[indicePestañaActual].rtr.localPosition = Vector3.zero;
  
        pestañas[indice].rtr.SetSiblingIndex(pestañas[indicePestañaActual].rtr.childCount - 1);
        pestañas[indicePestañaActual].rtr.DOKill();
        pestañas[indicePestañaActual].rtr.DOLocalMoveX(160 * (estaDerecha ? 1 : -1), 0.4f).SetEase(Ease.OutExpo);

        pestañas[indice].rtr.gameObject.ActivarObjeto();
        pestañas[indice].rtr.DOKill();
        pestañas[indice].rtr.DOLocalMoveX(0, 0.4f).SetEase(Ease.OutExpo).OnComplete(() =>
        {
            pestañas[indicePestañaActual].rtr.gameObject.DesactivarObjeto();
            indicePestañaActual = indice;
            puedeCambiarPestaña = true;
        });    
    }

    [System.Serializable]
    public struct Pestaña
    {
        public RectTransform rtr;
        public int indiceDireccion;
    }
}
