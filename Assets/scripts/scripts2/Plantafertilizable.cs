// PlantaFertilizable.cs
// Cada planta del campo del Nivel 2
// Mecánica:
//  - El jugador entra al trigger → se muestra el ícono "E"
//  - Presiona E → ANIMA-101 aplica fertilizante (si el indicador tiene carga suficiente)
//  - La planta se "revive" (cambio de sprite o animación)
//  - Notifica al LevelDirector2
//
// SETUP EN UNITY:
// 1. Crea un GameObject "Planta" con:
//    - SpriteRenderer (sprite marchita)
//    - Collider2D (trigger, radio de interacción)
//    - Este script
// 2. Asigna en Inspector:
//    - spriteMarchita: sprite inicial (marrón/seco)
//    - spriteRevivida: sprite final (verde/frondoso)
//    - iconoInteraccion: GameObject con el texto/ícono "E" (hijo del Planta)
//    - Animator (opcional): si tienes animación de "revivir"

using System.Collections;
using UnityEngine;

public class PlantaFertilizable : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ════════════════════════════════════════════════════════════════
    [Header("Visuales")]
    [SerializeField] private Sprite spriteMarchita;     // Estado inicial
    [SerializeField] private Sprite spriteRevivida;     // Estado después de fertilizar
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;         // Opcional: animación de crecimiento

    [Header("UI de interacción")]
    [SerializeField] private GameObject iconoInteraccion; // Ícono "E" flotante sobre la planta

    [Header("Partículas (opcional)")]
    [SerializeField] private ParticleSystem particulasFertilizante; // Efecto verde al fertilizar

    [Header("Configuración")]
    [SerializeField] private float costoFertilizante = 0.34f; // Cuánta carga consume (0.34 × 3 = ~1.0)
    // Si solo tienes 3 plantas y el indicador va de 0 a 1, cada planta consume 1/3

    // ════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ════════════════════════════════════════════════════════════════
    private bool fertilizada = false;
    private bool jugadorEnRango = false;

    // ════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════
    private void Start()
    {
        // Mostrar sprite marchito al inicio
        if (spriteRenderer != null && spriteMarchita != null)
            spriteRenderer.sprite = spriteMarchita;

        if (iconoInteraccion != null)
            iconoInteraccion.SetActive(false);
    }

    private void Update()
    {
        if (fertilizada) return;
        if (!jugadorEnRango) return;

        // ¿El LevelDirector2 permite fertilizar?
        if (LevelDirector2.Instance == null || !LevelDirector2.Instance.PuedeFertilizar) return;

        // ¿El indicador tiene carga suficiente?
        FertilizanteIndicador indicador = FertilizanteIndicador.Instance;
        if (indicador == null || !indicador.TieneCarga()) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(Fertilizar());
        }
    }

    // ════════════════════════════════════════════════════════════════
    // TRIGGERS
    // ════════════════════════════════════════════════════════════════
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (fertilizada) return;
        if (!collision.CompareTag("Player")) return;

        jugadorEnRango = true;

        // Solo mostrar ícono E si el jugador puede fertilizar
        if (LevelDirector2.Instance != null && LevelDirector2.Instance.PuedeFertilizar)
            if (iconoInteraccion != null) iconoInteraccion.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        jugadorEnRango = false;
        if (iconoInteraccion != null) iconoInteraccion.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════
    // FERTILIZACIÓN
    // ════════════════════════════════════════════════════════════════
    private IEnumerator Fertilizar()
    {
        fertilizada = true;
        if (iconoInteraccion != null) iconoInteraccion.SetActive(false);

        Debug.Log($"[PlantaFertilizable] Fertilizando: {gameObject.name}");

        // Consumir carga del indicador
        FertilizanteIndicador.Instance?.ConsumirCarga(costoFertilizante);

        // Efecto de partículas (si existe)
        if (particulasFertilizante != null)
            particulasFertilizante.Play();

        // Breve pausa para el efecto
        yield return new WaitForSeconds(0.3f);

        // Cambiar a sprite revivido
        if (spriteRenderer != null && spriteRevivida != null)
            spriteRenderer.sprite = spriteRevivida;

        // Disparar animación si existe
        if (animator != null)
            animator.SetTrigger("Revivir");

        // Escalar brevemente para dar feedback visual
        transform.localScale = Vector3.one * 0.8f;
        float t = 0f;
        while (t < 0.4f)
        {
            transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t / 0.4f);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one;

        // Notificar al LevelDirector2
        LevelDirector2.Instance?.NotificarPlantaFertilizada();
    }
}