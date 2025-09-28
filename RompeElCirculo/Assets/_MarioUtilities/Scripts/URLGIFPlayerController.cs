using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

public class URLGIFPlayerController : MonoBehaviour
{
    public string gifUrl;
    public Image targetImage;
    public float defaultFrameDelay = 0.1f;

    private List<Texture2D> frames = new List<Texture2D>();
    private List<float> delays = new List<float>();

    private IEnumerator Start()
    {
        yield return LoadGifFrames(gifUrl);
        yield return PlayGif();
    }

    private IEnumerator LoadGifFrames(string url)
    {
        // Limpia recursos previos
        foreach (var tex in frames)
        {
            if (tex != null) Destroy(tex);
        }
        frames.Clear();
        delays.Clear();

        byte[] gifData = null;
        using (HttpClient client = new HttpClient())
        {
            Task<byte[]> downloadTask = client.GetByteArrayAsync(url);
            while (!downloadTask.IsCompleted)
                yield return null;
            gifData = downloadTask.Result;
        }

        if (gifData != null)
        {
            ParseGif(gifData);
        }
    }

    // Lector nativo mejorado para GIFs con transparencia y optimización
    private void ParseGif(byte[] gifData)
    {
        // Busca los encabezados de los frames (0x2C) y los separa
        List<byte[]> frameDatas = new List<byte[]>();
        int pos = 0;
        while (pos < gifData.Length)
        {
            if (gifData[pos] == 0x2C)
            {
                int start = pos;
                int end = start + 1;
                while (end < gifData.Length && gifData[end] != 0x2C && gifData[end] != 0x3B)
                    end++;
                int length = end - start;
                byte[] frame = new byte[length];
                System.Array.Copy(gifData, start, frame, 0, length);
                frameDatas.Add(frame);
                pos = end;
            }
            else
            {
                pos++;
            }
        }

        foreach (var frameData in frameDatas)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(frameData, false);

            // Verifica si el frame tiene transparencia
            bool hasAlpha = false;
            var pixels = tex.GetPixels32();
            foreach (var px in pixels)
            {
                if (px.a < 255)
                {
                    hasAlpha = true;
                    break;
                }
            }
            if (hasAlpha)
            {
                tex.alphaIsTransparency = true;
            }

            frames.Add(tex);
            delays.Add(defaultFrameDelay);
        }
    }

    private IEnumerator PlayGif()
    {
        int frame = 0;
        while (true)
        {
            if (frames.Count == 0) yield break;
            targetImage.sprite = Sprite.Create(frames[frame], new Rect(0, 0, frames[frame].width, frames[frame].height), new Vector2(0.5f, 0.5f));
            yield return new WaitForSeconds(delays[frame]);
            frame = (frame + 1) % frames.Count;
        }
    }
}
