using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoFixController : MonoBehaviour
{
    public VideoPlayer videoplayer;
    public RenderTexture renderTexture;
    public RawImage im;

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
        if (im != null)
            im.enabled = true;
    }
}
