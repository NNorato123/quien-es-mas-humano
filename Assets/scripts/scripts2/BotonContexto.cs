// BotonContexto.cs
// Botones del laboratorio de Haber (fuego, reactor, reloj, química, pizarra)
// El jugador se acerca y presiona E → Haber dice un texto explicativo
// Cada botón cuenta UNA SOLA VEZ hacia el objetivo "explorar_lab"
// aunque el jugador pueda volver a presionar E para releer el texto
//
// SETUP EN UNITY:
// 1. Añade este script + Collider2D (trigger) a cada GameObject de equipo
// 2. Asigna en Inspector:
//    - nombreEquipo: "Fuego", "Reactor", "Reloj", "Quimica", "Pizarra"
//    - textosHaber: los diálogos que Haber dice al activar este botón
//    - iconoInteraccion: el GameObject con ícono "E" flotante (opcional)
//    - soloUnaVez: si true, el ícono E desaparece tras la primera interacción
// 3. Arrastra todos los BotonContexto[] en el Inspector de LevelDirector2

using UnityEngine;

public class BotonContexto : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ════════════════════════════════════════════════════════════════
    [Header("Contenido")]
    [SerializeField] private string nombreEquipo = "Equipo de laboratorio";
    [TextArea(2, 5)]
    [SerializeField] private string[] textosHaber;

    [Header("UI")]
    [SerializeField] private GameObject iconoInteraccion;

    [Header("Uso único")]
    [Tooltip("Si true, el ícono E desaparece tras la primera interacción")]
    [SerializeField] private bool soloUnaVez = true; // Por defecto true — cada equipo se revisa una vez

    // ════════════════════════════════════════════════════════════════
    // ESTADO
    // ════════════════════════════════════════════════════════════════
    private bool jugadorEnRango = false;
    private bool yaContadoEnObjetivo = false; // Cuenta solo UNA vez hacia el objetivo
    private bool yaUsadoVisual = false;        // Controla si el ícono desaparece

    // ════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════
    private void Start()
    {
        if (iconoInteraccion != null) iconoInteraccion.SetActive(false);
    }

    private void Update()
    {
        if (!enabled) return;
        if (!jugadorEnRango) return;
        if (textosHaber == null || textosHaber.Length == 0) return;

        // Si soloUnaVez y ya se usó visualmente, no hacer nada
        if (soloUnaVez && yaUsadoVisual) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"[BotonContexto] Activado: {nombreEquipo}");

            // Mostrar diálogo de Haber
            LevelDirector2.Instance?.MostrarTextoContextoHaber(textosHaber);

            // Contar hacia el objetivo UNA SOLA VEZ por botón
            if (!yaContadoEnObjetivo)
            {
                yaContadoEnObjetivo = true;
                ObjectiveManager.Instance?.CheckObjectiveCompletion("explorar_lab", nombreEquipo);
                Debug.Log($"[BotonContexto] Objetivo registrado: {nombreEquipo}");
            }

            // Control visual de uso único
            if (soloUnaVez)
            {
                yaUsadoVisual = true;
                if (iconoInteraccion != null) iconoInteraccion.SetActive(false);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // TRIGGERS
    // ════════════════════════════════════════════════════════════════
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // No mostrar ícono si ya se usó y es de uso único
        if (soloUnaVez && yaUsadoVisual) return;

        jugadorEnRango = true;
        if (iconoInteraccion != null) iconoInteraccion.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        jugadorEnRango = false;
        if (iconoInteraccion != null) iconoInteraccion.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════
    // API PÚBLICA
    // ════════════════════════════════════════════════════════════════

    /// <summary>Resetea el botón para que pueda volver a contar (útil si se reinicia el nivel)</summary>
    public void Resetear()
    {
        yaContadoEnObjetivo = false;
        yaUsadoVisual = false;
        if (iconoInteraccion != null) iconoInteraccion.SetActive(false);
    }

    public bool YaRevisado => yaContadoEnObjetivo;
}