using UnityEngine;
using UnityEngine.Video;

public class VideoTransition : MonoBehaviour
{
    public VideoPlayer introVideo;

    void Start()
    {
        if (introVideo == null)
        {
            Debug.LogError("VideoTransition: introVideo no está asignado en el Inspector");
            return;
        }

        // Si Play On Awake no está configurado, reproducir aquí
        if (!introVideo.isPlaying)
        {
            introVideo.Play();
        }
    }
}