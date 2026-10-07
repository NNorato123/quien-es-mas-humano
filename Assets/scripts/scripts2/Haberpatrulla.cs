// HaberPatrulla.cs
// Script exclusivo para Fritz Haber en el Nivel 2.
// Patrulla entre puntos asignados en Inspector (ping-pong).
// En cada punto se detiene un tiempo configurable "haciendo algo" antes de continuar.
// Maneja el Animator directamente por nombre de clip (sin parámetros en el Controller).
//
// SETUP EN UNITY:
// 1. Agrega este script al GameObject "Haber"
// 2. Crea GameObjects vacíos en el lab como puntos de patrulla:
//    PuntoLibros, PuntoEscritorio, PuntoReloj, PuntoQuimica, PuntoPizarra
// 3. Arrastra esos puntos al array "Puntos Patrulla" en Inspector
// 4. Ajusta "Tiempo En Punto" por índice si quieres tiempos distintos en cada punto
// 5. LevelDirector2 llama a Detener() cuando el jugador encuentra a Haber

using System.Collections;
using UnityEngine;

public class HaberPatrulla : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ════════════════════════════════════════════════════════════════
    [Header("Patrulla")]
    [SerializeField] private Transform[] puntosPatrulla;   // Arrastra los puntos aquí
    [SerializeField] private float velocidad = 1.2f;
    [SerializeField] private float tiempoEnPuntoDefault = 2f;  // Segundos quieto en cada punto
    [SerializeField] private float[] tiemposEnPunto;       // Opcional: tiempo específico por punto
                                                            // Si está vacío, usa tiempoEnPuntoDefault

    [Header("Detección")]
    [SerializeField] private float umbralLlegada = 0.08f;  // Qué tan cerca = "llegó"

    // ════════════════════════════════════════════════════════════════
    // PRIVADO
    // ════════════════════════════════════════════════════════════════
    private Animator anim;
    private Rigidbody2D rb;
    private int indiceActual = 0;
    private int direccionPingPong = 1;   // 1 = avanzar índice, -1 = retroceder
    private bool patrullando = false;
    private bool detenido = false;
    private Coroutine corrutinaPatrulla = null;

    // Dirección actual de movimiento (para saber qué clip poner)
    private Vector2 dirActual = Vector2.zero;

    // ════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════
    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length < 2)
        {
            Debug.LogWarning("[HaberPatrulla] Se necesitan al menos 2 puntos de patrulla.");
            return;
        }

        IniciarPatrulla();
    }

    // ════════════════════════════════════════════════════════════════
    // API PÚBLICA — LevelDirector2 llama a estos métodos
    // ════════════════════════════════════════════════════════════════

    /// <summary>Arranca la patrulla. Se llama automáticamente en Start.</summary>
    public void IniciarPatrulla()
    {
        if (patrullando) return;
        patrullando = true;
        detenido = false;
        corrutinaPatrulla = StartCoroutine(CorrutinaPatrulla());
    }

    /// <summary>
    /// Detiene la patrulla y pone el idle mirando hacia el jugador (o hacia abajo por defecto).
    /// Llamar desde LevelDirector2 cuando el jugador encuentra a Haber.
    /// </summary>
    public void Detener(Vector2 posicionJugador = default)
    {
        detenido = true;
        patrullando = false;

        if (corrutinaPatrulla != null)
        {
            StopCoroutine(corrutinaPatrulla);
            corrutinaPatrulla = null;
        }

        // Calcular dirección hacia el jugador para poner el idle correcto
        if (posicionJugador != default)
        {
            Vector2 dir = (posicionJugador - (Vector2)transform.position).normalized;
            PonerIdle(dir);
        }
        else
        {
            PonerIdle(Vector2.down); // Por defecto mira hacia abajo
        }

        Debug.Log("[HaberPatrulla] Patrulla detenida.");
    }

    /// <summary>Reanuda la patrulla si fue detenida.</summary>
    public void Reanudar()
    {
        if (patrullando) return;
        detenido = false;
        IniciarPatrulla();
    }

    // ════════════════════════════════════════════════════════════════
    // CORRUTINA PRINCIPAL
    // ════════════════════════════════════════════════════════════════
    private IEnumerator CorrutinaPatrulla()
    {
        while (!detenido)
        {
            Transform destino = puntosPatrulla[indiceActual];
            if (destino == null) { yield return null; continue; }

            // ── MOVER HACIA EL PUNTO ──────────────────────────────
            while (!detenido &&
                   Vector2.Distance(transform.position, destino.position) > umbralLlegada)
            {
                dirActual = ((Vector2)destino.position - (Vector2)transform.position).normalized;
                PonerAnimacionCaminar(dirActual);

                // Mover
                if (rb != null)
                    rb.MovePosition(rb.position + dirActual * velocidad * Time.fixedDeltaTime);
                else
                    transform.position = Vector2.MoveTowards(
                        transform.position, destino.position, velocidad * Time.deltaTime);

                yield return new WaitForFixedUpdate();
            }

            if (detenido) break;

            // ── LLEGÓ AL PUNTO: idle + esperar ───────────────────
            PonerIdle(dirActual);

            float tiempo = ObtenerTiempoEnPunto(indiceActual);
            yield return new WaitForSeconds(tiempo);

            if (detenido) break;

            // ── AVANZAR AL SIGUIENTE PUNTO (ping-pong) ────────────
            indiceActual += direccionPingPong;

            // Rebotar en los extremos
            if (indiceActual >= puntosPatrulla.Length)
            {
                indiceActual = puntosPatrulla.Length - 2; // Penúltimo
                direccionPingPong = -1;
            }
            else if (indiceActual < 0)
            {
                indiceActual = 1; // Segundo
                direccionPingPong = 1;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // ANIMACIONES — por nombre de clip directo (sin parámetros)
    // ════════════════════════════════════════════════════════════════
    private void PonerAnimacionCaminar(Vector2 dir)
    {
        if (anim == null) return;

        // Determinar dirección dominante
        string clip;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            // Movimiento principalmente horizontal
            clip = dir.x > 0 ? "haber_derecha_Clip" : "haber_izquierda_Clip";
        }
        else
        {
            // Movimiento principalmente vertical
            clip = dir.y > 0 ? "haber_arriba_Clip" : "haber_abajo_Clip";
        }

        // Solo cambiar el clip si es diferente al actual (evita restart constante)
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clip))
            anim.Play(clip);
    }

    private void PonerIdle(Vector2 dir)
    {
        if (anim == null) return;

        string clip;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            clip = dir.x > 0 ? "idle_derecha" : "idle_izquierda";
        else
            clip = dir.y > 0 ? "idle_arriba" : "idle_abajo";

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName(clip))
            anim.Play(clip);
    }

    // ════════════════════════════════════════════════════════════════
    // AUXILIARES
    // ════════════════════════════════════════════════════════════════
    private float ObtenerTiempoEnPunto(int indice)
    {
        if (tiemposEnPunto != null && indice < tiemposEnPunto.Length && tiemposEnPunto[indice] > 0)
            return tiemposEnPunto[indice];
        return tiempoEnPuntoDefault;
    }

    // Dibuja los puntos en el editor para visualizar la ruta
    private void OnDrawGizmosSelected()
    {
        if (puntosPatrulla == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < puntosPatrulla.Length; i++)
        {
            if (puntosPatrulla[i] == null) continue;
            Gizmos.DrawSphere(puntosPatrulla[i].position, 0.15f);
            if (i < puntosPatrulla.Length - 1 && puntosPatrulla[i + 1] != null)
                Gizmos.DrawLine(puntosPatrulla[i].position, puntosPatrulla[i + 1].position);
        }
    }
}