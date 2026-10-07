using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

public class ComandoSystem : MonoBehaviour
{
    public static ComandoSystem Instance { get; private set; }
    public static event System.Action<string> OnComandoReconocido;
   
    private bool terminalActivated = false;

    [Header("Configuración de Capas")]
    public LayerMask gameplayLayer;
    public LayerMask menuLayer;

    [Header("Configuración de Teclado")]
    [Tooltip("Tecla para abrir/cerrar la terminal")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Configuración de Comandos")]
    private Dictionary<string, System.Action> comandosDisponibles = new Dictionary<string, System.Action>
    {
        {"arriba", () => playercontroller.Instance.DesbloquearMovimientoArriba()},
        {"abajo", () => playercontroller.Instance.DesbloquearMovimientoAbajo()},
        {"izquierda", () => playercontroller.Instance.DesbloquearMovimientoIzquierda()},
        {"derecha", () => playercontroller.Instance.DesbloquearMovimientoDerecha()},
        {"help", () => ComandoSystem.Instance.MostrarAyuda()},
        {"clear", () => ComandoSystem.Instance.LimpiarTerminal()}
    };

    [Header("Referencias UI")]
    public Camera mainCamera;
    public Canvas menuCanvas;
    public GameObject panelComandos;
    public TMP_Text outputText;         // Historial de comandos y respuestas
    public TMP_Text textoEntrada;       // Texto que muestra lo que se está escribiendo
    public ScrollRect scrollRect;       // Referencia al ScrollRect que contiene el outputText

    // Variables para la terminal
    private List<string> historialComandos = new List<string>();
    private List<string> comandosPendientes = new List<string>();
    private bool menuActive = false;
    private int contadorErrores = 0;
    private string promptCMD = "C:\\Users\\root>";
    private string textoIngresado = "";

    // Configuración para el panel - OPT: Convertidos a [SerializeField] para evitar recompilación
    [SerializeField] private int maxLines = 25;           // Máximo de líneas visibles
    [SerializeField] private int advertenciaLineas = 20;  // Líneas antes de mostrar advertencia
    private bool advertenciaMostrada = false;            // Control para mostrar advertencia una sola vez

    // OPT: Constantes internas para evitar magic strings
    private const string CMD_HELP = "help";
    private const string CMD_CLEAR = "clear";
    private const string CMD_ARRIBA = "arriba";
    private const string CMD_ABAJO = "abajo";
    private const string CMD_IZQUIERDA = "izquierda";
    private const string CMD_DERECHA = "derecha";

    // OPT: StringBuilder reutilizable para reducir allocations
    private StringBuilder stringBuilder = new StringBuilder();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        Debug.LogWarning("=" + this.name + " INICIALIZADO =");

        if (menuCanvas != null)
            menuCanvas.enabled = false;
        else
            Debug.LogError("[ComandoSystem] menuCanvas NO ESTÁ ASIGNADO en Inspector");

        if (panelComandos != null)
            Debug.Log("[ComandoSystem] panelComandos asignado ✓");
        else
            Debug.LogError("[ComandoSystem] panelComandos NO ESTÁ ASIGNADO en Inspector");

        if (outputText != null)
            Debug.Log("[ComandoSystem] outputText asignado ✓");
        else
            Debug.LogError("[ComandoSystem] outputText NO ESTÁ ASIGNADO en Inspector");

        if (textoEntrada != null)
            Debug.Log("[ComandoSystem] textoEntrada asignado ✓");
        else
            Debug.LogError("[ComandoSystem] textoEntrada NO ESTÁ ASIGNADO en Inspector");

        // Si no se asignó el ScrollRect, intentar encontrarlo
        if (scrollRect == null && outputText != null)
        {
            scrollRect = outputText.transform.parent.GetComponent<ScrollRect>();
        }

        Debug.LogWarning("[ComandoSystem] PRESIONA: " + toggleKey.ToString());
    }

    void Start()
    {
        // Validar que el PlayerController esté disponible antes de usarlo
        if (playercontroller.Instance == null)
        {
            Debug.LogError("[ComandoSystem] playercontroller.Instance es NULL");
            Debug.LogError("[ComandoSystem] Asegúrate de que el PlayerController esté en la escena");
        }
        else
        {
            // Bloquea todos los movimientos al inicio
            playercontroller.Instance.BloquearTodoMovimiento();
            Debug.Log("[ComandoSystem] Movimientos bloqueados al inicio");
        }

        // OPT: Reemplazar LINQ con bucle manual para evitar enumerables
        comandosPendientes = new List<string>();
        foreach (var key in comandosDisponibles.Keys)
        {
            if (key != CMD_HELP && key != CMD_CLEAR)
            {
                comandosPendientes.Add(key);
            }
        }

        // Inicializar la terminal
        historialComandos.Add("Sistema de Comandos de Movimiento");
        historialComandos.Add("Versión 1.0");
        historialComandos.Add("");
        
        Debug.LogWarning("[ComandoSystem START] Listo para recibir comandos");
    }

    void Update()
    {
        // Detectar presión de la tecla de toggle
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.LogWarning("[ComandoSystem] ✓ TECLA PRESIONADA: " + toggleKey.ToString());
            ToggleMenu();
        }

        if (!menuActive) return;

        ProcesarEntradaTexto();
    }

    private void ToggleMenu()
    {
        Debug.LogWarning("[ComandoSystem TOGGLE] menuActive actual: " + menuActive + " → " + !menuActive);
        
        menuActive = !menuActive;
        
        if (menuCanvas != null)
            menuCanvas.enabled = menuActive;
        else
            Debug.LogError("[ComandoSystem] menuCanvas es NULL en ToggleMenu");
            
        if (panelComandos != null)
            panelComandos.SetActive(menuActive);
        else
            Debug.LogError("[ComandoSystem] panelComandos es NULL en ToggleMenu");

        if (menuActive)
        {
            Debug.LogWarning("[ComandoSystem] ═══ TERMINAL ABIERTA ═══");
            
            if (!terminalActivated)
            {
                terminalActivated = true;
                if (ObjectiveManager.Instance != null)
                {
                    ObjectiveManager.Instance.CheckObjectiveCompletion("key_press", "Tab");
                    Debug.Log("[ComandoSystem] Objetivo completado: key_press");
                }
                else
                {
                    Debug.LogError("[ComandoSystem] ObjectiveManager.Instance es NULL");
                }
            }

            if (mainCamera != null)
                mainCamera.cullingMask = menuLayer;
            else
                Debug.LogError("[ComandoSystem] mainCamera es NULL");

            // Limpiar el texto de entrada
            textoIngresado = "";
            if (textoEntrada != null)
                textoEntrada.text = promptCMD + " ";
            else
                Debug.LogError("[ComandoSystem] textoEntrada es NULL");

            ActualizarTextoOutput();
        }
        else
        {
            Debug.LogWarning("[ComandoSystem] ═══ TERMINAL CERRADA ═══");
            // Mostrar todas las capas
            if (mainCamera != null)
                mainCamera.cullingMask = -1;
        }
    }

        public void CerrarMenuForzado()
            {
                if (!menuActive) return;

                menuActive = false;

                if (menuCanvas != null) menuCanvas.enabled = false;
                if (panelComandos != null) panelComandos.SetActive(false);
                if (mainCamera != null) mainCamera.cullingMask = -1;

                Debug.LogWarning("[ComandoSystem] Menú cerrado forzosamente (CerrarMenuForzado)");
            }

    private void ProcesarEntradaTexto()
    {
        foreach (char c in Input.inputString)
        {
            if (c == '\b') // Backspace
            {
                if (textoIngresado.Length > 0)
                    textoIngresado = textoIngresado.Substring(0, textoIngresado.Length - 1);
            }
            else if (c == '\n' || c == '\r') // Enter
            {
                ProcesarComando();
            }
            else if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_') // Permitir letras, números y algunos caracteres especiales
            {
                textoIngresado += c;
            }
        }

        textoEntrada.text = promptCMD + " " + textoIngresado;
    }

    private void ProcesarComando()
    {
        if (string.IsNullOrEmpty(textoIngresado))
        {
            // Si no hay texto, agregar un nuevo prompt
            historialComandos.Add(promptCMD);
            ActualizarTextoOutput();
            textoIngresado = "";
            textoEntrada.text = promptCMD + " ";
            return;
        }

        string comando = textoIngresado.ToLower().Trim();

        // Agregar el comando al historial con el prompt
        historialComandos.Add($"{promptCMD} {comando}");

        if (comandosDisponibles.ContainsKey(comando))
        {
            bool esComandoDireccion = comando == CMD_ARRIBA || comando == CMD_ABAJO ||
                                    comando == CMD_IZQUIERDA || comando == CMD_DERECHA;

            // ✅ Si es un comando de dirección y LevelDirector espera OTRA dirección específica, bloquear
            if (esComandoDireccion && LevelDirector.Instance != null)
            {
                string esperado = LevelDirector.Instance.DireccionEsperadaActual;
                if (!string.IsNullOrEmpty(esperado) && comando != esperado)
                {
                    historialComandos.Add($"'{comando}' no es el comando esperado ahora.");
                    OnComandoReconocido?.Invoke(comando); // dispara igual para que el Creador reaccione/regañe

                    VerificarLimiteLineas();
                    historialComandos.Add(promptCMD);
                    textoIngresado = "";
                    textoEntrada.text = promptCMD + " ";
                    ActualizarTextoOutput();
                    return; // ⛔ NO se ejecuta comandosDisponibles[comando]
                }
            }

            // Resetear contador de errores
            contadorErrores = 0;

            // Ejecutar el comando
            comandosDisponibles[comando]?.Invoke();

            if (comando != CMD_HELP && comando != CMD_CLEAR)
            {
                OnComandoReconocido?.Invoke(comando);
            }

            // Si es un comando de movimiento, marcarlo como desbloqueado
            if (comando != CMD_HELP && comando != CMD_CLEAR)
            {
                if (comandosPendientes.Contains(comando))
                {
                    comandosPendientes.Remove(comando);
                    playercontroller.Instance.DesbloquearHabilidad(comando);
                    historialComandos.Add($"Comando '{comando}' reconocido.");
                    historialComandos.Add($"Movimiento {comando} desbloqueado.");
                }
                else
                {
                    historialComandos.Add($"El comando '{comando}' ya está desbloqueado.");
                }
            }

            if (comando != CMD_HELP && comando != CMD_CLEAR)
            {
                ObjectiveManager.Instance.CheckObjectiveCompletion("movement_unlock");
            }
        }
        else
        {
            // OPT: Usar interpolación en lugar de concatenación con +
            historialComandos.Add($"'{comando}' no es un comando reconocido.");
            contadorErrores++;

            // Después de 3 errores, sugerir help
            if (contadorErrores >= 3)
            {
                historialComandos.Add("Has tenido varios errores consecutivos.");
                historialComandos.Add("Comando recomendado: help");
                contadorErrores = 0; // Resetear el contador
            }
        }

        // Verificar si todos los comandos han sido desbloqueados
        if (comandosPendientes.Count == 0 && !historialComandos.Contains("¡Todos los movimientos han sido desbloqueados!"))
        {
            historialComandos.Add("¡Todos los movimientos han sido desbloqueados!");
        }

        // Verificar si el panel está lleno y mostrar advertencia
        VerificarLimiteLineas();

        // Agregar un nuevo prompt para el siguiente comando
        historialComandos.Add(promptCMD);

        textoIngresado = "";
        textoEntrada.text = promptCMD + " ";
        ActualizarTextoOutput();
    }

    private void VerificarLimiteLineas()
    {
        if (historialComandos.Count >= advertenciaLineas && !advertenciaMostrada)
        {
            historialComandos.Add("AVISO: El panel se está llenando.");
            historialComandos.Add("Usa el comando 'clear' para limpiar la terminal.");
            advertenciaMostrada = true;
        }

        // OPTIMIZACIÓN: Usar RemoveRange en lugar de Skip().ToList() para evitar allocation
        if (historialComandos.Count > maxLines)
        {
            int linesToRemove = historialComandos.Count - maxLines;
            historialComandos.RemoveRange(0, linesToRemove);
        }
    }

    public void MostrarAyuda()
    {
        historialComandos.Add("Comandos disponibles:");
        historialComandos.Add("  help     - Ver ayuda");
        historialComandos.Add("  clear    - Limpiar terminal");
        historialComandos.Add("  arriba   - Mov. arriba");
        historialComandos.Add("  abajo    - Mov. abajo");
        historialComandos.Add("  izquierda - Mov. izquierda");
        historialComandos.Add("  derecha  - Mov. derecha");

        historialComandos.Add("");
        historialComandos.Add("Estado de movimientos:");
        
        // OPT: Reemplazar LINQ con bucle manual para evitar enumerables
        foreach (var cmd in comandosDisponibles.Keys)
        {
            if (cmd != CMD_HELP && cmd != CMD_CLEAR)
            {
                string estado = playercontroller.Instance.habilidadesDesbloqueadas.Contains(cmd) ? "OK" : "NO";
                historialComandos.Add($"  {cmd}: {estado}");
            }
        }
    }

    public void LimpiarTerminal()
    {
        historialComandos.Clear();
        historialComandos.Add("Terminal limpiada.");
        historialComandos.Add(promptCMD);

        // Resetear la advertencia
        advertenciaMostrada = false;
    }

    private void ActualizarTextoOutput()
    {
        // OPT: Usar StringBuilder en lugar de string.Join() para evitar allocation en cada frame
        stringBuilder.Clear();
        for (int i = 0; i < historialComandos.Count; i++)
        {
            if (i > 0) stringBuilder.Append("\n");
            stringBuilder.Append(historialComandos[i]);
        }
        outputText.text = stringBuilder.ToString();

        // Hacer scroll hacia abajo para mostrar el último comando
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
        else if (outputText.rectTransform.parent is RectTransform parentRect && parentRect.TryGetComponent<ScrollRect>(out var scroll))
        {
            scroll.verticalNormalizedPosition = 0;
        }
    }
}