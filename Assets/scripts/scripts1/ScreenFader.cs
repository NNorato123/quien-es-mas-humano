using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Fade genérico (negro / azul — reusable)")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image imagenFade;

    [Header("Efecto Despertar (viñeta — solo una vez al inicio)")]
    [SerializeField] private CanvasGroup canvasGroupDespertar;
    [SerializeField] private Image imagenDespertar;

    private Coroutine fadeActivo = null;
    private Coroutine fadeDespertarActivo = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        if (canvasGroupDespertar != null) canvasGroupDespertar.blocksRaycasts = false;
    }

    // ════════════════════════════════════════
    // CANAL 1: Fade genérico (negro al inicio, azul en el portal)
    // ════════════════════════════════════════

    public IEnumerator FadeIn(Color color, float duracion)
    {
        if (imagenFade != null)
            imagenFade.color = new Color(color.r, color.g, color.b, imagenFade.color.a);
        yield return EjecutarFade(1f, duracion, dejarActivo: true);
    }

    public IEnumerator FadeOut(float duracion)
    {
        yield return EjecutarFade(0f, duracion, dejarActivo: false);
    }

    private IEnumerator EjecutarFade(float alphaObjetivo, float duracion, bool dejarActivo)
    {
        if (fadeActivo != null) { StopCoroutine(fadeActivo); fadeActivo = null; }
        fadeActivo = StartCoroutine(RutinaFade(canvasGroup, imagenFade, alphaObjetivo, duracion, dejarActivo));
        yield return fadeActivo;
    }

    // ════════════════════════════════════════
    // CANAL 2: Efecto Despertar (viñeta, uso único)
    // ════════════════════════════════════════

    /// <summary>Revela la viñeta de "despertar" y la apaga permanentemente al terminar.</summary>
    public IEnumerator FadeOutDespertar(float duracion)
    {
        if (fadeDespertarActivo != null) { StopCoroutine(fadeDespertarActivo); fadeDespertarActivo = null; }
        fadeDespertarActivo = StartCoroutine(RutinaFade(canvasGroupDespertar, imagenDespertar, 0f, duracion, dejarActivo: false));
        yield return fadeDespertarActivo;
    }

    // ════════════════════════════════════════
    // MOTOR COMPARTIDO — ambos canales usan esta misma lógica, pero por separado
    // ════════════════════════════════════════

    private IEnumerator RutinaFade(CanvasGroup grupo, Image imagen, float alphaObjetivo, float duracion, bool dejarActivo)
    {
        if (grupo == null) yield break;

        if (imagen != null) imagen.gameObject.SetActive(true);
        grupo.blocksRaycasts = true;
        yield return null;

        float inicio = grupo.alpha;
        float t = 0f;
        while (t < duracion)
        {
            float deltaSeguro = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            t += deltaSeguro;
            grupo.alpha = Mathf.Lerp(inicio, alphaObjetivo, Mathf.SmoothStep(0f, 1f, t / duracion));
            yield return null;
        }
        grupo.alpha = alphaObjetivo;

        if (!dejarActivo)
        {
            if (imagen != null) imagen.gameObject.SetActive(false); // ✅ apagada de verdad, no solo transparente
            grupo.blocksRaycasts = false;
        }
    }
}