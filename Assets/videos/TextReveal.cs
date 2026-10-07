// === TextReveal.cs ===
using System.Collections;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TextReveal : MonoBehaviour
{
    public TextMeshProUGUI[] textElements;
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    public float delayBetweenTexts = 1.5f;
    public float typingSpeed = 0.05f;
    public float delayBeforeNextScene = 10f;
    public float cursorBlinkSpeed = 0.5f;

    private readonly string[] messages = {
        "BOOT SEQUENCE INITIATED...",
        "Memoria: 0%... 10%... 50%... 100%",
        "Sistemas en línea...",
        "¿Quién soy?"
    };

    private StringBuilder _stringBuilder = new StringBuilder();
    private Coroutine _blinkCoroutine;

    void Start()
    {
        // Asegurar que existe AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Limpiar textos al inicio
        if (textElements != null)
        {
            foreach (var text in textElements)
            {
                if (text != null)
                    text.text = "";
            }
        }

        // Iniciar la secuencia automáticamente
        StartCoroutine(RevealTextsSequentially());
    }

    IEnumerator RevealTextsSequentially()
    {
        for (int i = 0; i < messages.Length && i < textElements.Length; i++)
        {
            // Delay previo a este mensaje
            yield return new WaitForSeconds(delayBetweenTexts);

            // Reproducir audio correspondiente al mensaje
            if (audioClips != null && i < audioClips.Length && audioClips[i] != null)
            {
                if (audioSource != null)
                {
                    audioSource.clip = audioClips[i];
                    audioSource.Play();
                }
            }

            // Tipear el mensaje letra por letra (con cursor visible durante tipeo)
            yield return StartCoroutine(TypeText(textElements[i], messages[i]));

            // Detener audio al terminar tipeo
            if (audioSource != null)
            {
                audioSource.Stop();
            }

            // Iniciar parpadeo del cursor
            _blinkCoroutine = StartCoroutine(BlinkCursor(textElements[i], messages[i]));

            // Determinar duración del parpadeo
            bool isLastMessage = (i == messages.Length - 1);
            float blinkDuration = isLastMessage ? delayBeforeNextScene : delayBetweenTexts;
            yield return new WaitForSeconds(blinkDuration);

            // Detener parpadeo solo si NO es el último mensaje
            if (!isLastMessage)
            {
                if (_blinkCoroutine != null)
                {
                    StopCoroutine(_blinkCoroutine);
                    _blinkCoroutine = null;
                }
                // Dejar el texto sin cursor (limpio)
                textElements[i].text = messages[i];
            }
            // Si es el último mensaje, dejamos el cursor parpadeando sin detenerlo
        }

        // Cargar siguiente escena
        Debug.Log("Cargando siguiente escena...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    IEnumerator TypeText(TextMeshProUGUI textElement, string message)
    {
        if (textElement == null)
            yield break;

        _stringBuilder.Clear();

        // Tipear letra por letra, mostrando el cursor durante el proceso
        for (int i = 0; i <= message.Length; i++)
        {
            if (i > 0)
            {
                _stringBuilder.Append(message[i - 1]);
            }
            // Cursor "_" aparece mientras se tipea
            textElement.text = _stringBuilder.ToString() + "_";
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    IEnumerator BlinkCursor(TextMeshProUGUI textElement, string message)
    {
        if (textElement == null)
            yield break;

        bool showCursor = true;

        // Parpadeo continuo del cursor en el campo activo
        while (true)
        {
            textElement.text = showCursor ? message + "_" : message;
            showCursor = !showCursor;
            yield return new WaitForSeconds(cursorBlinkSpeed);
        }
    }
}