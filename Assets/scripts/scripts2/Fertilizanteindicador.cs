// FertilizanteIndicador.cs
// Indicador visual de fertilizante de ANIMA-101
// Mecánica:
//  - Empieza LLENO (carga = 1.0) al entrar al campo
//  - Cada planta consume ~0.34 de carga (3 plantas = 1.0 total)
//  - Después de usar, se RECARGA con el tiempo (cooldown)
//  - Durante la recarga el jugador puede interactuar con otras plantas pero no fertilizar
//
// SETUP EN UNITY:
// 1. Crea un Canvas (Screen Space Overlay) con este script
// 2. Dentro del Canvas:
//    - Un Slider o una Image con Fill (para la barra)
//    - Un GameObject "IconoANIMA" (opcional, parpadea cuando recarga)
//    - Un TMP_Text para el porcentaje (opcional)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FertilizanteIndicador : MonoBehaviour
{
    public static FertilizanteIndicador Instance { get; private set; }

    // ════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ════════════════════════════════════════════════════════════════
    [Header("UI")]
    [SerializeField] private GameObject panelIndicador;     // Panel completo (se activa/desactiva)
    [SerializeField] private Slider sliderCarga;            // Barra de progreso
    [SerializeField] private Image imagenRelleno;           // Fill de la barra (para cambiar color)
    [SerializeField] private TMP_Text textoPorcentaje;      // "85%" (opcional)
    [SerializeField] private TMP_Text textoEstado;          // "Listo" / "Recargando..."
    [SerializeField] private GameObject iconoRecargando;    // Ícono que parpadea al recargar

    [Header("Configuración")]
    [SerializeField] private float tiempoRecarga = 5f;      // Segundos para recarga completa
    [SerializeField] private float cargaInicial = 1f;       // Carga al entrar al campo
    // Colores de la barra según estado
    [SerializeField] private Color colorLleno = new Color(0.2f, 0.85f, 0.35f);
    [SerializeField] private Color colorMedio = new Color(0.9f, 0.85f, 0.2f);
    [SerializeField] private Color colorBajo = new Color(0.9f, 0.3f, 0.2f);
    [SerializeField] private Color colorRecargando = new Color(0.4f, 0.6f, 0.9f);

    // ════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ════════════════════════════════════════════════════════════════
    private float cargaActual = 0f;
    private bool recargando = false;
    private bool activo = false;
    private Coroutine corrutinaRecarga = null;

    // ════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Empieza oculto — LevelDirector2 lo activa cuando el jugador llega al campo
        if (panelIndicador != null) panelIndicador.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!activo) return;
        ActualizarUI();
    }

    // ════════════════════════════════════════════════════════════════
    // API PÚBLICA
    // ════════════════════════════════════════════════════════════════

    // Llamado por LevelDirector2 cuando el jugador llega al campo
    public void Activar()
    {
        activo = true;
        cargaActual = cargaInicial;
        recargando = false;
        if (panelIndicador != null) panelIndicador.SetActive(true);
        Debug.Log("[FertilizanteIndicador] Activado. Carga inicial: " + cargaActual);
    }

    // Llamado por LevelDirector2 al terminar la fase de campo
    public void Desactivar()
    {
        activo = false;
        if (corrutinaRecarga != null) StopCoroutine(corrutinaRecarga);
        if (panelIndicador != null) panelIndicador.SetActive(false);
    }

    // ¿Hay suficiente carga para fertilizar una planta?
    public bool TieneCarga()
    {
        return !recargando && cargaActual > 0.1f; // Margen mínimo
    }

    // Llamado por PlantaFertilizable al fertilizar
    public void ConsumirCarga(float cantidad)
    {
        cargaActual -= cantidad;
        cargaActual = Mathf.Clamp01(cargaActual);

        Debug.Log($"[FertilizanteIndicador] Carga restante: {cargaActual:P0}");

        // Si la carga cayó a 0, iniciar recarga automática
        if (cargaActual <= 0.05f)
        {
            if (corrutinaRecarga != null) StopCoroutine(corrutinaRecarga);
            corrutinaRecarga = StartCoroutine(RecargarCon(tiempoRecarga));
        }
    }

    // ════════════════════════════════════════════════════════════════
    // RECARGA
    // ════════════════════════════════════════════════════════════════
    private IEnumerator RecargarCon(float duracion)
    {
        recargando = true;
        cargaActual = 0f;

        Debug.Log("[FertilizanteIndicador] Recargando...");

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            cargaActual = Mathf.Clamp01(t / duracion);
            yield return null;
        }

        cargaActual = 1f;
        recargando = false;
        Debug.Log("[FertilizanteIndicador] Recarga completa.");
    }

    // ════════════════════════════════════════════════════════════════
    // UI
    // ════════════════════════════════════════════════════════════════
    private void ActualizarUI()
    {
        // Slider
        if (sliderCarga != null)
            sliderCarga.value = cargaActual;

        // Color de la barra según estado
        if (imagenRelleno != null)
        {
            if (recargando)
                imagenRelleno.color = colorRecargando;
            else if (cargaActual > 0.6f)
                imagenRelleno.color = colorLleno;
            else if (cargaActual > 0.3f)
                imagenRelleno.color = colorMedio;
            else
                imagenRelleno.color = colorBajo;
        }

        // Porcentaje
        if (textoPorcentaje != null)
            textoPorcentaje.text = $"{Mathf.RoundToInt(cargaActual * 100)}%";

        // Estado textual
        if (textoEstado != null)
            textoEstado.text = recargando ? "Recargando..." : (cargaActual > 0.1f ? "Listo" : "Sin carga");

        // Ícono de recarga (activo solo mientras recarga)
        if (iconoRecargando != null)
            iconoRecargando.SetActive(recargando);
    }
}