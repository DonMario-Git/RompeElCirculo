using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoFixController : MonoBehaviour
{
    public VideoPlayer videoplayer;
    public RenderTexture renderTexture;
    public RawImage im;
    public bool mutearDeUna;
    private bool reproducioUnaVez;

    public Image imagenTransicion;

    void OnEnable()
    {
        if (videoplayer != null)
        {
            videoplayer.prepareCompleted += OnVideoPrepared;
            videoplayer.started += OnVideoStarted;
        }
        if (im != null)
            im.enabled = false;
    }

    void OnDisable()
    {
        imagenTransicion.color = Color.white;

        if (videoplayer != null)
        {
            videoplayer.prepareCompleted -= OnVideoPrepared;
            videoplayer.started -= OnVideoStarted;
        }
        if (renderTexture != null)
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = active;
        }
        if (im != null)
            im.enabled = false;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // Limpiar la textura antes de reproducir el nuevo video
        if (renderTexture != null)
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = active;
        }
        
        vp.Play();
    }

    private void OnVideoStarted(VideoPlayer vp)
    {
        imagenTransicion.DOKill();
        imagenTransicion.DOFade(0, 0.2f);
        if (im != null)
            im.enabled = true;
        
        if (!reproducioUnaVez || !mutearDeUna)
        {
            // Primera vez: audio activo
            vp.SetDirectAudioMute(0, false);
            reproducioUnaVez = true;
        }
        else
        {
            // Siguiente vez: audio muteado
            vp.SetDirectAudioMute(0, true);
        }
    }
}
