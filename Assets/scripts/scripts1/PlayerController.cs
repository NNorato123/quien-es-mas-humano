using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class playercontroller : MonoBehaviour
{

    
    public UnityEvent<string> OnMovementUnlocked;
    // Variables de movimiento
    private bool movimientoDesbloqueado;
    public float speed = 1f;

    [Header("DEBUG — Desactivar antes de build final")]
    [Tooltip("Si está activo, ignora el progreso guardado en PlayerPrefs al iniciar (útil para testear el nivel directamente sin pasar por el menú)")]
    [SerializeField] private bool ignorarProgresoGuardado = false;

    // Referencias
    private Rigidbody2D rb;
    private Animator animator;

    // Variables para control de dirección
    private Vector2 movement;
    private Vector2 ultimoMovimiento = new Vector2(0, -1); // Por defecto mirando hacia abajo

    // Variables para detectar cambios instantáneos
    private bool estaMoviendose = false;
    private bool estabaMoViendonseAntes = false;

    // OPTIMIZACIÓN: Flags booleanos para direcciones desbloqueadas (O(0) en lugar de Contains O(1))
    private bool movimientoArribaDesbloqueado = false;
    private bool movimientoAbajoDesbloqueado = false;
    private bool movimientoIzquierdaDesbloqueado = false;
    private bool movimientoDerechaDesbloqueado = false;

    // Parámetros del Animator
    private readonly int horizontalParam = Animator.StringToHash("Horizontal");
    private readonly int verticalParam = Animator.StringToHash("Vertical");
    private readonly int isMovingParam = Animator.StringToHash("IsMoving");
    private readonly int ultimoHorizontalParam = Animator.StringToHash("UltimoHorizontal");
    private readonly int ultimoVerticalParam = Animator.StringToHash("UltimoVertical");

    // Enumerado para mantener un seguimiento preciso del estado actual
    private enum EstadoAnimacion
    {
        IdleAbajo,
        IdleArriba,
        IdleIzquierda,
        IdleDerecha,
        CaminarAbajo,
        CaminarArriba,
        CaminarIzquierda,
        CaminarDerecha
    }

    private EstadoAnimacion estadoActual = EstadoAnimacion.IdleAbajo;

    // Nombres de los triggers para cambios directos de estado
    private readonly string[] nombresTriggers = new string[]
    {
        "ActivarIdleAbajo",
        "ActivarIdleArriba",
        "ActivarIdleIzquierda",
        "ActivarIdleDerecha",
        "ActivarCaminarAbajo",
        "ActivarCaminarArriba",
        "ActivarCaminarIzquierda",
        "ActivarCaminarDerecha"
    };

    // Almacenar los hash de los triggers para acceso rápido
    private int[] triggerHashes;

    // Estados de dirección
    public enum Direccion
    {
        Arriba,
        Abajo,
        Izquierda,
        Derecha
    }

    private Direccion direccionActual = Direccion.Abajo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        triggerHashes = new int[nombresTriggers.Length];
        for (int i = 0; i < nombresTriggers.Length; i++)
        {
            triggerHashes[i] = Animator.StringToHash(nombresTriggers[i]);
        }

        CambiarEstadoDirectamente(EstadoAnimacion.IdleAbajo);

        movimientoHabilitado = habilidadesDesbloqueadas.Count > 0;
    }

    void Update()
    {
        // Verificar primero si se permite algún tipo de movimiento
        if (!movimientoHabilitado) return;

        // Resetear el movimiento
        movement = Vector2.zero;

        // OPTIMIZACIÓN: Usar flags booleanos en lugar de Contains()
        if (movimientoDerechaDesbloqueado && (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)))
        {
            movement.x = 1;
        }
        if (movimientoIzquierdaDesbloqueado && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)))
        {
            movement.x = -1;
        }
        if (movimientoArribaDesbloqueado && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)))
        {
            movement.y = 1;
        }
        if (movimientoAbajoDesbloqueado && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)))
        {
            movement.y = -1;
        }

        // Guardamos el estado anterior
        estabaMoViendonseAntes = estaMoviendose;

        // Determinamos si está moviéndose
        estaMoviendose = movement.x != 0 || movement.y != 0;

        // Si estamos moviéndonos, actualizamos la última dirección
        if (estaMoviendose)
        {
            ultimoMovimiento = movement.normalized;
            ActualizarDireccionActual();
        }

        // Detectamos cambios en el estado de movimiento
        bool cambioDeEstado = estaMoviendose != estabaMoViendonseAntes;

        // Actualizamos el estado de animación basado en los cambios
        ActualizarEstadoAnimacion(cambioDeEstado);
    }

    void FixedUpdate()
    {
        // Movemos el personaje (físicas)
        if (estaMoviendose)
        {
            rb.MovePosition(rb.position + movement.normalized * speed * Time.fixedDeltaTime);
        }
    }

    void ActualizarDireccionActual()
    {
        // Priorizar el movimiento horizontal sobre el vertical si ambos están presentes
        if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
        {
            direccionActual = movement.x > 0 ? Direccion.Derecha : Direccion.Izquierda;
        }
        else
        {
            direccionActual = movement.y > 0 ? Direccion.Arriba : Direccion.Abajo;
        }
    }

    void ActualizarEstadoAnimacion(bool cambioForzado)
    {
        EstadoAnimacion nuevoEstado = estadoActual;

        // Determinamos el nuevo estado basado en la dirección y si está en movimiento
        if (estaMoviendose)
        {
            switch (direccionActual)
            {
                case Direccion.Arriba:
                    nuevoEstado = EstadoAnimacion.CaminarArriba;
                    break;
                case Direccion.Abajo:
                    nuevoEstado = EstadoAnimacion.CaminarAbajo;
                    break;
                case Direccion.Izquierda:
                    nuevoEstado = EstadoAnimacion.CaminarIzquierda;
                    break;
                case Direccion.Derecha:
                    nuevoEstado = EstadoAnimacion.CaminarDerecha;
                    break;
            }
        }
        else
        {
            // Si no está en movimiento, elegimos el idle correspondiente a la última dirección
            switch (direccionActual)
            {
                case Direccion.Arriba:
                    nuevoEstado = EstadoAnimacion.IdleArriba;
                    break;
                case Direccion.Abajo:
                    nuevoEstado = EstadoAnimacion.IdleAbajo;
                    break;
                case Direccion.Izquierda:
                    nuevoEstado = EstadoAnimacion.IdleIzquierda;
                    break;
                case Direccion.Derecha:
                    nuevoEstado = EstadoAnimacion.IdleDerecha;
                    break;
            }
        }

        // Si hay un cambio de estado o se fuerza el cambio, actualizamos inmediatamente
        if (nuevoEstado != estadoActual || cambioForzado)
        {
            CambiarEstadoDirectamente(nuevoEstado);
        }

        // Actualizamos también los parámetros estándar para mayor compatibilidad
        ActualizarParametrosGenerales();
    }

    void CambiarEstadoDirectamente(EstadoAnimacion nuevoEstado)
    {
        // Reseteamos todos los triggers primero
        foreach (int trigger in triggerHashes)
        {
            animator.ResetTrigger(trigger);
        }

        // Activamos el trigger correspondiente al nuevo estado
        animator.SetTrigger(triggerHashes[(int)nuevoEstado]);

        // Actualizamos el estado actual
        estadoActual = nuevoEstado;
    }

    void ActualizarParametrosGenerales()
    {
        // Actualizamos los parámetros generales para mantener compatibilidad
        animator.SetFloat(horizontalParam, movement.x);
        animator.SetFloat(verticalParam, movement.y);
        animator.SetFloat(ultimoHorizontalParam, ultimoMovimiento.x);
        animator.SetFloat(ultimoVerticalParam, ultimoMovimiento.y);
        animator.SetBool(isMovingParam, estaMoviendose);
    }

    // Método para obtener la dirección actual del jugador (útil para otros scripts)
    public Direccion ObtenerDireccionActual()
    {
        return direccionActual;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movimientoDesbloqueado = enabled;
        // Resetear movimiento cuando se deshabilita
        if (!enabled)
        {
            movement = Vector2.zero;
            estaMoviendose = false;
        }
    }

    public static playercontroller Instance { get; private set; }

    public HashSet<string> habilidadesDesbloqueadas = new HashSet<string>();

    // Variables de control de movimiento
    private bool movimientoHabilitado = false;

    private void Awake()
    {
        Instance = this;
        habilidadesDesbloqueadas = new HashSet<string>();

        // ── PERSISTENCIA ENTRE NIVELES ──────────────────────────────
        // Si el jugador ya desbloqueó habilidades en un nivel anterior,
        // las recuperamos de PlayerPrefs y las aplicamos inmediatamente.
        if (!ignorarProgresoGuardado)
            CargarHabilidadesGuardadas();
        else
            Debug.LogWarning("[playercontroller] DEBUG: ignorando progreso guardado en PlayerPrefs.");
    }

    public void BloquearTodoMovimiento()
    {
        movimientoHabilitado = false;
    }

    public void HabilitarTodoMovimiento()
    {
        movimientoHabilitado = true;
    }

    // OPTIMIZACIÓN: Método público para desbloquear habilidades mantiendo sincronización de flags
    public void DesbloquearHabilidad(string habilidad)
    {
        habilidadesDesbloqueadas.Add(habilidad);

        if (!enabled) enabled = true;

        // Sincronizar el flag correspondiente
        switch (habilidad)
        {
            case "arriba":    movimientoArribaDesbloqueado    = true; break;
            case "abajo":     movimientoAbajoDesbloqueado     = true; break;
            case "izquierda": movimientoIzquierdaDesbloqueado = true; break;
            case "derecha":   movimientoDerechaDesbloqueado   = true; break;
        }

        movimientoHabilitado = true;

        // ── PERSISTENCIA ─────────────────────────────────────────────
        // Guardar en PlayerPrefs para que sobreviva el cambio de escena
        GuardarHabilidad(habilidad);
    }

    // Métodos de desbloqueo individuales (mantienen compatibilidad)
    public void DesbloquearMovimientoArriba()
    {
        DesbloquearHabilidad("arriba");
        Debug.Log("Movimiento hacia arriba desbloqueado");
        ObjectiveManager.Instance.CheckObjectiveCompletion("movement_unlock");
    }

    public void DesbloquearMovimientoAbajo()
    {
        DesbloquearHabilidad("abajo");
        Debug.Log("Movimiento hacia abajo desbloqueado");
        ObjectiveManager.Instance.CheckObjectiveCompletion("movement_unlock");
    }

    public void DesbloquearMovimientoIzquierda()
    {
        DesbloquearHabilidad("izquierda");
        Debug.Log("Movimiento hacia izquierda desbloqueado");
        ObjectiveManager.Instance.CheckObjectiveCompletion("movement_unlock");
    }

    public void DesbloquearMovimientoDerecha()
    {
        DesbloquearHabilidad("derecha");
        Debug.Log("Movimiento hacia derecha desbloqueado");
        ObjectiveManager.Instance.CheckObjectiveCompletion("movement_unlock");
    }

    // ════════════════════════════════════════════════════════════════
    // PERSISTENCIA DE HABILIDADES ENTRE NIVELES
    // Usamos PlayerPrefs con claves simples: "hab_arriba", "hab_abajo", etc.
    // Se borran automáticamente al resetear el juego (ResetearProgreso).
    // ════════════════════════════════════════════════════════════════

    private static readonly string[] HABILIDADES_POSIBLES = { "arriba", "abajo", "izquierda", "derecha" };

    private void GuardarHabilidad(string habilidad)
    {
        PlayerPrefs.SetInt("hab_" + habilidad, 1);
        PlayerPrefs.Save();
        Debug.Log($"[playercontroller] Habilidad guardada en PlayerPrefs: {habilidad}");
    }

    private void CargarHabilidadesGuardadas()
    {
        foreach (string h in HABILIDADES_POSIBLES)
        {
            if (PlayerPrefs.GetInt("hab_" + h, 0) == 1)
            {
                // Aplicar directamente sin llamar a DesbloquearHabilidad()
                // para evitar recursión y no disparar CheckObjectiveCompletion en niveles anteriores
                habilidadesDesbloqueadas.Add(h);
                switch (h)
                {
                    case "arriba":    movimientoArribaDesbloqueado    = true; break;
                    case "abajo":     movimientoAbajoDesbloqueado     = true; break;
                    case "izquierda": movimientoIzquierdaDesbloqueado = true; break;
                    case "derecha":   movimientoDerechaDesbloqueado   = true; break;
                }
                Debug.Log($"[playercontroller] Habilidad restaurada: {h}");
            }
        }

        // Si ya tiene habilidades cargadas, habilitar el movimiento
        if (habilidadesDesbloqueadas.Count > 0)
        {
            movimientoHabilitado = true;
            Debug.Log($"[playercontroller] {habilidadesDesbloqueadas.Count} habilidades restauradas desde niveles anteriores.");
        }
    }

    /// <summary>
    /// Llama esto desde el menú principal o al iniciar una nueva partida
    /// para borrar el progreso guardado y empezar desde cero.
    /// </summary>
    public static void ResetearProgreso()
    {
        foreach (string h in HABILIDADES_POSIBLES)
            PlayerPrefs.DeleteKey("hab_" + h);
        PlayerPrefs.Save();
        Debug.Log("[playercontroller] Progreso reseteado.");
    }
}