using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

public class LevelDirector : MonoBehaviour
{
    public enum LevelPhase
    {
        CinematicaCreador, PrimerDialogo, DesbloqueoDirecciones,
        InteraccionLibre, CinematicaDron, DecisionMoral, DialogoCierre, CierreNivel
    }

    public static LevelDirector Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GameObject creadorNPC;
    [SerializeField] private GameObject machineInteractable;
    [SerializeField] private playercontroller playerControllerRef;

    [Header("Timelines")]
    [SerializeField] private PlayableDirector timelineCreadorRodea;
    [SerializeField] private PlayableDirector timelineDronDañado;
    [SerializeField] private PlayableDirector timelineSalaPortales;
    [SerializeField] private PlayableDirector timelineDespedidaFinal;

    [Header("Anclas de Cámara")]
    [SerializeField] private Transform ancoraCamaraInicio;
    [SerializeField] private Transform ancoraCamaraDron;
    [SerializeField] private Transform ancoraCamaraPortales;
    [SerializeField] private Transform ancoraCamaraFinal;

    [Header("Iluminación Cinemática")]
    [SerializeField] private Light2D luzGlobal;
    [SerializeField] private LightFollowPlayer spotLuz;
    [SerializeField] private FollowCamera camaraSeguimiento;
    [SerializeField] private float duracionFadeLuz = 1.5f;

    [Header("Transición de pantalla")]
    [SerializeField] private float duracionFadeDespertar = 3f;

    private LevelPhase faseActual;
    private bool inputBloqueado = true;
    private bool decisionMoralRegistrada = false;
    private bool reparoMaquina = false;
    private bool dronDesbloqueadoParaReparar = false;
    private Dialogo dialogoCreador;
    private string direccionEsperadaActual = "";

    public bool PuedeInteractuarConDron { get; private set; } = false;
    public bool DecisionTomada { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (playerControllerRef == null)
            playerControllerRef = FindObjectOfType<playercontroller>();
        if (creadorNPC != null)
            dialogoCreador = creadorNPC.GetComponent<Dialogo>();

        ComandoSystem.OnComandoReconocido += ManejarComandoDuranteDesbloqueo;

        // La cámara arranca fija en el ancla de inicio, no siguiendo al robot
        if (camaraSeguimiento != null && ancoraCamaraInicio != null)
            camaraSeguimiento.SetTarget(ancoraCamaraInicio);

        Debug.Log("[LevelDirector] Iniciando Nivel 1 (sin Boot, video ya se reprodujo en escena Cinemática)");
        BloqueaInputTotal();
        StartCoroutine(IniciarSecuenciaCompleta());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ComandoSystem.OnComandoReconocido -= ManejarComandoDuranteDesbloqueo;
    }

    private void Update()
    {
        if (faseActual == LevelPhase.DecisionMoral && !decisionMoralRegistrada)
            ProcesarInputDecisionMoral();
    }

    // ════════════════════════════════════════
    // SECUENCIA INICIAL
    // ════════════════════════════════════════

    private IEnumerator IniciarSecuenciaCompleta()
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOutDespertar(duracionFadeDespertar); // ✅ canal correcto

        StartCoroutine(IniciarCinematicaCreador());
    }

    // ════════════════════════════════════════
    // FASES
    // ════════════════════════════════════════

    private IEnumerator IniciarCinematicaCreador()
    {
        faseActual = LevelPhase.CinematicaCreador;
        Debug.Log("[LevelDirector] FASE: Cinemática — Creador rodea al robot");

        if (luzGlobal != null) luzGlobal.intensity = 0f;
        if (spotLuz != null && creadorNPC != null) spotLuz.SetTarget(creadorNPC.transform);
        if (camaraSeguimiento != null && creadorNPC != null) camaraSeguimiento.SetTarget(creadorNPC.transform);

        yield return EsperarFinDeTimeline(timelineCreadorRodea);

        if (spotLuz != null && playerControllerRef != null) spotLuz.SetTarget(playerControllerRef.transform);
        if (camaraSeguimiento != null && playerControllerRef != null) camaraSeguimiento.SetTarget(playerControllerRef.transform);
        yield return FadeLuz(luzGlobal, 1f, duracionFadeLuz);

        StartCoroutine(IniciarPrimerDialogo());
    }

    private IEnumerator FadeLuz(Light2D luz, float intensidadObjetivo, float duracion)
    {
        if (luz == null) yield break;
        float inicio = luz.intensity;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            luz.intensity = Mathf.Lerp(inicio, intensidadObjetivo, t / duracion);
            yield return null;
        }
        luz.intensity = intensidadObjetivo;
    }

    private IEnumerator IniciarPrimerDialogo()
    {
        faseActual = LevelPhase.PrimerDialogo;

        dialogoCreador?.ResetearDialogo();
        MostrarDialogoCreador(
            "Tú eres ANIMA-101. Fuiste creado para aprender de las grandes mentes.",
            "Antes de enviarte para que recuperes conocimiento del pasado, debo probarte.....",
            "Camina."
        );
        yield return EsperarTerminoDialogo();

        dialogoCreador?.ResetearDialogo();
        MostrarDialogoCreador(
            "Oh, cierto. Todavía no lo sabes.",
            "Para que puedas caminar debes desbloquear tus movimientos.",
            "Abre la terminal con [T] y escribe: arriba"
        );
        yield return EsperarTerminoDialogo();

        ComandoSystem cs = FindObjectOfType<ComandoSystem>();
        if (cs != null) cs.enabled = true;

        AgregarObjetivo("Abre la terminal y escribe: arriba", "habilidad_arriba");
        StartCoroutine(IniciarDesbloqueoDirecciones());
    }

    private IEnumerator IniciarDesbloqueoDirecciones()
    {
        faseActual = LevelPhase.DesbloqueoDirecciones;
        Debug.Log("[LevelDirector] FASE: Desbloqueo de Direcciones");

        string[] direcciones = { "arriba", "abajo", "derecha", "izquierda" };
        string[][] dialogosDespues = {
            new[] { "Bien..... ahora abajo." },
            new[] { "Bien..... ahora derecha." },
            new[] { "Perfecto. Ahora izquierda." },
            new[] { "Muy bien. Ahora necesito saber si tienes los sensores de visualización y detección de datos activados.",
                     "Interactúa con los objetos que desees." }
        };
        string[] objetivosSiguientes = {
            "Abre la terminal y escribe: abajo",
            "Abre la terminal y escribe: derecha",
            "Abre la terminal y escribe: izquierda",
            ""
        };
        string[] actionTypes = { "habilidad_abajo", "habilidad_derecha", "habilidad_izquierda", "" };

        for (int i = 0; i < 4; i++)
        {
            direccionEsperadaActual = direcciones[i];
            yield return EsperarHabilidad(direcciones[i]);
            direccionEsperadaActual = "";

            Debug.Log($"[LevelDirector] '{direcciones[i]}' desbloqueado");
            ObjectiveManager.Instance?.CheckObjectiveCompletion("habilidad_" + direcciones[i]);

            dialogoCreador?.ResetearDialogo();
            MostrarDialogoCreador(dialogosDespues[i]);
            yield return EsperarTerminoDialogo();

            if (i < 3)
                AgregarObjetivo(objetivosSiguientes[i], actionTypes[i]);
        }

        AgregarObjetivo("Interactúa con 2 objetos del laboratorio", "object_interaction", "", 2);
        Debug.Log("[LevelDirector] Todas las direcciones desbloqueadas.");
        StartCoroutine(IniciarInteraccionLibre());
    }

    private IEnumerator IniciarInteraccionLibre()
    {
        faseActual = LevelPhase.InteraccionLibre;
        Debug.Log("[LevelDirector] FASE: Interacción Libre");

        DesbloqueaInputTotal();
        yield return EsperarObjetivoActual("object_interaction");

        dialogoCreador?.ResetearDialogo();
        MostrarDialogoCreador("Muy bien. Sígueme.");
        yield return EsperarTerminoDialogo();

        StartCoroutine(IniciarCinematicaDron());
    }

    private IEnumerator IniciarCinematicaDron()
    {
        faseActual = LevelPhase.CinematicaDron;
        Debug.Log("[LevelDirector] FASE: Cinemática — Dron dañado");

        BloqueaInputTotal();
        if (camaraSeguimiento != null && ancoraCamaraDron != null)
            camaraSeguimiento.SetTarget(ancoraCamaraDron);

        yield return EsperarFinDeTimeline(timelineDronDañado);

        dialogoCreador?.ResetearDialogo();
        MostrarDialogoCreador("Jummmm..... enviaré a los técnicos a arreglarlo.");
        yield return EsperarTerminoDialogo();

        if (camaraSeguimiento != null && playerControllerRef != null)
            camaraSeguimiento.SetTarget(playerControllerRef.transform);

        DesbloqueaInputTotal();
        PuedeInteractuarConDron = true;

        yield return EsperarEEnDron();
        StartCoroutine(IniciarDecisionMoral());
    }

    private IEnumerator IniciarDecisionMoral()
    {
        faseActual = LevelPhase.DecisionMoral;
        Debug.Log("[LevelDirector] FASE: Decisión Moral");

        BloqueaMovimientoJugador();
        dialogoCreador?.ResetearDialogo();
        MostrarDialogoCreador("Eh? ¿Quieres arreglarlo (1) o no (2)?");

        while (!decisionMoralRegistrada)
            yield return new WaitForSeconds(0.1f);

        Debug.Log($"[LevelDirector] Decisión: {(reparoMaquina ? "REPARAR" : "IGNORAR")}");

        if (reparoMaquina)
        {
            dronDesbloqueadoParaReparar = true;
            dialogoCreador?.ResetearDialogo();
            MostrarDialogoCreador("Wow..... qué interesante..... adelante, inténtalo.");
            yield return EsperarTerminoDialogo();

            AgregarObjetivo("Presiona E para reparar el dron", "reparar_dron");
        }
        else
        {
            dronDesbloqueadoParaReparar = false;
            PuedeInteractuarConDron = false;
            dialogoCreador?.ResetearDialogo();
            MostrarDialogoCreador("Vale. Sígueme.");
            yield return EsperarTerminoDialogo();

            StartCoroutine(IniciarDialogoCierre());
            yield break;
        }

        yield return EsperarObjetivoActual("reparar_dron");
        StartCoroutine(IniciarDialogoCierre());
    }

    private IEnumerator IniciarDialogoCierre()
    {
        faseActual = LevelPhase.DialogoCierre;
        Debug.Log("[LevelDirector] FASE: Diálogo de Cierre");

        if (camaraSeguimiento != null && ancoraCamaraPortales != null)
            camaraSeguimiento.SetTarget(ancoraCamaraPortales);

        dialogoCreador?.ResetearDialogo();

        if (reparoMaquina)
            MostrarDialogoCreador("Vaya, aprendes bastante rápido. Esto es impresionante..... en serio.");
        else
            MostrarDialogoCreador("Interesante. Elegiste ignorarla. Eso también dice mucho.");

        yield return EsperarTerminoDialogo();

        dialogoCreador?.ResetearDialogo();
        MostrarDialogoCreador("Ahora te enviaré a una misión. Aprenderás de Fritz Haber y el proceso Haber-Bosch.");
        yield return EsperarTerminoDialogo();

        StartCoroutine(IniciarCierreNivel());
    }

    private IEnumerator IniciarCierreNivel()
    {
        faseActual = LevelPhase.CierreNivel;
        Debug.Log("[LevelDirector] FASE: Cierre de Nivel — Sala de portales");

        BloqueaInputTotal();
        yield return EsperarFinDeTimeline(timelineSalaPortales); // cinemática 3

        // Oscurece automáticamente al terminar la cinemática
        yield return FadeLuz(luzGlobal, 0f, duracionFadeLuz);

        // Reposiciona la cámara mientras está oscuro (invisible para el jugador)
        if (camaraSeguimiento != null && ancoraCamaraFinal != null)
            camaraSeguimiento.SetTarget(ancoraCamaraFinal);

        // Espera a que termine de deslizarse antes de revelar
        yield return EsperarCamaraEstable();

        yield return FadeLuz(luzGlobal, 1f, duracionFadeLuz);

        // Cinemática 4: despedida, viaje a la época de Fritz Haber, robot se acerca al portal (azul)
        yield return EsperarFinDeTimeline(timelineDespedidaFinal);

        Debug.Log("[LevelDirector] ¡Nivel 1 completado!");
        // Aquí eventualmente: SceneManager.LoadScene("Nivel2") o pantalla de fin de demo
    }

    // ════════════════════════════════════════
    // AUXILIARES
    // ════════════════════════════════════════

    private IEnumerator EsperarFinDeTimeline(PlayableDirector director)
    {
        if (director == null)
        {
            Debug.LogWarning("[LevelDirector] Timeline no asignado en Inspector, se salta la espera.");
            yield break;
        }

        bool terminado = false;
        void OnStopped(PlayableDirector d) => terminado = true;

        director.stopped += OnStopped;
        director.Play();

        while (!terminado) yield return null;

        director.stopped -= OnStopped;
        Debug.Log("[LevelDirector] Timeline terminado.");
    }

    private IEnumerator EsperarCamaraEstable()
    {
        if (camaraSeguimiento == null) yield break;
        while (!camaraSeguimiento.EstaEstable)
            yield return null;
    }

    private void ManejarComandoDuranteDesbloqueo(string comando)
    {
        if (faseActual != LevelPhase.DesbloqueoDirecciones) return;
        if (string.IsNullOrEmpty(direccionEsperadaActual)) return;
        if (comando == direccionEsperadaActual) return; // correcto, el flujo principal ya lo maneja

        dialogoCreador?.ResetearDialogo();
        MostrarDialogoCreador($"Jummmm..... ¿por qué no obedeces? El comando es {direccionEsperadaActual}, no {comando}.");
    }

    private IEnumerator EsperarTerminoDialogo()
    {
        if (dialogoCreador == null) yield break;
        bool terminado = false;
        UnityEngine.Events.UnityAction listener = () => { terminado = true; };
        dialogoCreador.OnDialogoTerminado.AddListener(listener);
        while (!terminado) yield return new WaitForSeconds(0.1f);
        dialogoCreador.OnDialogoTerminado.RemoveListener(listener);
    }

    private IEnumerator EsperarHabilidad(string habilidad)
    {
        while (!playerControllerRef.habilidadesDesbloqueadas.Contains(habilidad))
            yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator EsperarObjetivoActual(string actionType)
    {
        Objective objetivo = null;
        while (objetivo == null)
        {
            var todos = ObjectiveManager.Instance?.AllObjectives;
            if (todos != null)
            {
                foreach (var obj in todos)
                {
                    if (obj.requiredActionType == actionType && obj.Estado != Objective.EstadoType.Completado)
                    {
                        objetivo = obj;
                        break;
                    }
                }
            }
            if (objetivo == null) yield return new WaitForSeconds(0.2f);
        }

        while (objetivo.Estado != Objective.EstadoType.Completado)
            yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator EsperarEEnDron()
    {
        if (machineInteractable == null) yield break;

        bool presionoE = false;
        while (!presionoE)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                float distancia = Vector2.Distance(
                    playerControllerRef.transform.position,
                    machineInteractable.transform.position);
                if (distancia < 2f) presionoE = true;
            }
            yield return null;
        }
    }

    private void ProcesarInputDecisionMoral()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            reparoMaquina = true;
            decisionMoralRegistrada = true;
            DecisionTomada = true;
            DesbloqueaInputTotal();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            reparoMaquina = false;
            decisionMoralRegistrada = true;
            DecisionTomada = true;
            DesbloqueaInputTotal();
        }
    }

    private void MostrarDialogoCreador(params string[] lineas)
    {
        if (dialogoCreador != null)
            dialogoCreador.IniciarDialogoPrograma(lineas);
        else
            Debug.LogWarning("[LevelDirector] dialogoCreador es null");
    }

    private void AgregarObjetivo(string descripcion, string actionType = "", string actionDetail = "", int count = 1)
    {
        ObjectiveManager.Instance?.AddObjective(new Objective(descripcion, actionType, actionDetail, count));
    }

    private void BloqueaInputTotal()
    {
        inputBloqueado = true;
        if (playerControllerRef != null) playerControllerRef.enabled = false;
        ComandoSystem cs = FindObjectOfType<ComandoSystem>();
        if (cs != null) cs.enabled = false;
        PauseMenuController pm = FindObjectOfType<PauseMenuController>();
        if (pm != null) pm.enabled = false;
    }

    private void DesbloqueaInputTotal()
    {
        inputBloqueado = false;
        if (playerControllerRef != null) playerControllerRef.enabled = true;
        ComandoSystem cs = FindObjectOfType<ComandoSystem>();
        if (cs != null) cs.enabled = true;
        PauseMenuController pm = FindObjectOfType<PauseMenuController>();
        if (pm != null) pm.enabled = true;
    }

    private void BloqueaMovimientoJugador()
    {
        if (playerControllerRef != null) playerControllerRef.enabled = false;
    }

    // ════════════════════════════════════════
    // GETTERS PÚBLICOS
    // ════════════════════════════════════════
    public LevelPhase ObtenerFaseActual() => faseActual;
    public bool InputEstaBloqueado() => inputBloqueado;
    public bool ObtenerDecisionMoralRegistrada() => decisionMoralRegistrada;
    public bool ObtenerReparoMaquina() => reparoMaquina;
    public bool DronDesbloqueadoParaReparar() => dronDesbloqueadoParaReparar;
    public string DireccionEsperadaActual => direccionEsperadaActual;
}