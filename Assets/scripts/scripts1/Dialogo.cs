using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Dialogo : MonoBehaviour
{
    private static HashSet<int> objetosInteractuadosUnicos = new HashSet<int>();
    private static Dialogo instanciaActiva = null; // ✅ candado global compartido
    [SerializeField] private GameObject dialogueMark;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField, TextArea(4, 6)] private string[] dialogueLines;
    [SerializeField] public UnityEvent OnDialogoTerminado;
    [SerializeField] private float duracionLinea = 3f;
    public float typingTime = 0.05f;
    

    private bool isPlayerInRange = false;
    private bool dialogoActivo = false;
    private bool modoAutoAvance = false;

    // Señales que el Update manda a la corrutina maestra
    private bool señalAvanzar = false;
    private bool señalAcelerar = false;

    // UNA sola corrutina maestra que controla todo el flujo
    private Coroutine corrutinaDialogo = null;

    // ─────────────────────────────────────────────
    // UPDATE — solo captura input, NO toma decisiones
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log($"[Dialogo:{gameObject.name}] CLICK detectado. isPlayerInRange={isPlayerInRange} dialogoActivo={dialogoActivo} instanciaActiva={(instanciaActiva == null ? "null" : instanciaActiva.name)}");
        }

        // Solo puede iniciar diálogo interactivo si NADIE más tiene el control
        if (isPlayerInRange && !dialogoActivo && instanciaActiva == null && Input.GetButtonDown("Fire1"))
        {
            IniciarDialogoInteractivo();
            return;
        }

        if (dialogoActivo && Input.GetButtonDown("Fire1"))
        {
            señalAcelerar = true;
            señalAvanzar = true;
        }
    }

    // ─────────────────────────────────────────────
    // TRIGGERS
    // ─────────────────────────────────────────────
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[Dialogo:{gameObject.name}] OnTriggerEnter2D con: {collision.gameObject.name} (tag={collision.gameObject.tag})");
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (dialogueMark != null) dialogueMark.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log($"[Dialogo:{gameObject.name}] OnTriggerExit2D con: {collision.gameObject.name}");
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (dialogueMark != null) dialogueMark.SetActive(false);

            if (dialogoActivo && !modoAutoAvance)
                TerminarDialogoInterno();
        }
    }

    // ─────────────────────────────────────────────
    // INICIO
    // ─────────────────────────────────────────────
    private void IniciarDialogoInteractivo()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;
        if (dialogoActivo) return;

        // ✅ Ahora cuenta al INTERACTUAR (click), no al acercarse
        int myID = gameObject.GetInstanceID();
        if (!objetosInteractuadosUnicos.Contains(myID))
        {
            objetosInteractuadosUnicos.Add(myID);
            ObjectiveManager.Instance?.CheckObjectiveCompletion("object_interaction");
            Debug.Log($"[Dialogo] Objeto único interactuado: {gameObject.name} (total únicos: {objetosInteractuadosUnicos.Count})");
        }

        modoAutoAvance = false;
        IniciarCorrutinaDialogo();
    }

    
    public void IniciarDialogoPrograma(string[] lineas = null)
    {
    if (lineas != null && lineas.Length > 0)
        dialogueLines = lineas;

    if (dialogueLines == null || dialogueLines.Length == 0)
    {
        Debug.LogWarning("[Dialogo] Sin líneas en " + gameObject.name + " — disparando evento igual.");
        OnDialogoTerminado?.Invoke();
        return;
    }

    if (dialogoActivo)
    {
        Debug.LogWarning("[Dialogo] Diálogo ya activo en " + gameObject.name + ". Llama ResetearDialogo() primero.");
        return;
    }

    modoAutoAvance = true;
    IniciarCorrutinaDialogo();
    }

    private void IniciarCorrutinaDialogo()
    {
        if (corrutinaDialogo != null)
        {
            StopCoroutine(corrutinaDialogo);
            corrutinaDialogo = null;
        }

        dialogoActivo = true;
        instanciaActiva = this; // ✅ NUEVO: este Dialogo toma el candado
        señalAvanzar = false;
        señalAcelerar = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueMark != null) dialogueMark.SetActive(false);

        BloqueaInput();

        corrutinaDialogo = StartCoroutine(CorrutinaDialogoCompleto());
    }

    // ─────────────────────────────────────────────
    // CORRUTINA MAESTRA
    // ─────────────────────────────────────────────
    private IEnumerator CorrutinaDialogoCompleto()
    {
        for (int i = 0; i < dialogueLines.Length; i++)
        {
            señalAvanzar = false;
            señalAcelerar = false;

            // --- Typewriter ---
            StringBuilder sb = new StringBuilder();
            bool acelerado = false;

            foreach (char ch in dialogueLines[i])
            {
                if (señalAcelerar)
                {
                    // Mostrar línea completa al instante
                    dialogueText.text = dialogueLines[i];
                    señalAcelerar = false;
                    señalAvanzar = false; // consumir para no avanzar de golpe
                    acelerado = true;
                    break;
                }

                sb.Append(ch);
                dialogueText.text = sb.ToString();
                yield return new WaitForSecondsRealtime(typingTime);
            }

            if (!acelerado)
                dialogueText.text = dialogueLines[i];

            // --- Espera después de que terminó la línea ---
            if (modoAutoAvance)
            {
                float t = 0f;
                while (t < duracionLinea)
                {
                    if (señalAcelerar)
                    {
                        señalAcelerar = false;
                        señalAvanzar = false;
                        break;
                    }
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                // Modo interactivo: esperar click
                señalAvanzar = false;
                while (!señalAvanzar)
                    yield return null;
                señalAvanzar = false;
            }
        }

        // Fin — todas las líneas mostradas
        TerminarDialogoInterno();
    }

    // ─────────────────────────────────────────────
    // TERMINAR
    // ─────────────────────────────────────────────
    private void TerminarDialogoInterno()
    {
        if (corrutinaDialogo != null)
        {
            StopCoroutine(corrutinaDialogo);
            corrutinaDialogo = null;
        }

        dialogoActivo = false;
        modoAutoAvance = false;
        señalAvanzar = false;
        señalAcelerar = false;

        if (instanciaActiva == this) instanciaActiva = null; // ✅ NUEVO: libera el candado

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        DesbloquearInput();

        OnDialogoTerminado?.Invoke();
    }

    /// <summary>
    /// Resetea el estado. Llamar antes de IniciarDialogoPrograma() en cada nuevo diálogo.
    /// </summary>
    public void ResetearDialogo()
    {
        if (corrutinaDialogo != null)
        {
            StopCoroutine(corrutinaDialogo);
            corrutinaDialogo = null;
        }

        dialogoActivo = false;
        modoAutoAvance = false;
        señalAvanzar = false;
        señalAcelerar = false;

        if (instanciaActiva == this) instanciaActiva = null; // ✅ NUEVO

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
    }

    /// <summary>
    /// Termina el diálogo desde fuera (ej: LevelDirector cancela).
    /// </summary>
    public void TerminarDialogo()
    {
        TerminarDialogoInterno();
    }

    // ─────────────────────────────────────────────
    // BLOQUEO DE INPUT
    // ─────────────────────────────────────────────
    private void BloqueaInput()
    {
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        playercontroller pc = playercontroller.Instance;
        if (pc != null) pc.enabled = false;

        ComandoSystem cs = FindObjectOfType<ComandoSystem>();
        if (cs != null)
        {
            cs.CerrarMenuForzado();
            cs.enabled = false;
        }

        PauseMenuController pm = FindObjectOfType<PauseMenuController>();
        if (pm != null) pm.enabled = false;
    }

    private void DesbloquearInput()
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        playercontroller pc = playercontroller.Instance;
        if (pc != null) pc.enabled = true;

        ComandoSystem cs = FindObjectOfType<ComandoSystem>();
        if (cs != null) cs.enabled = true;

        PauseMenuController pm = FindObjectOfType<PauseMenuController>();
        if (pm != null) pm.enabled = true;
    }}