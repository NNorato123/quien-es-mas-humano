// LevelDirector2.cs
// Orquestador del Nivel 2: Laboratorio Haber + Campo de plantas
// IMPORTANTE: NO DontDestroyOnLoad — vive solo en la escena Nivel2

using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class LevelDirector2 : MonoBehaviour
{
    public enum LevelPhase2
    {
        Entrada, BuscarHaber, DialogoHaber, Minijuego,
        DecisionMoral, DialogoCierre, TransicionCampo, CampoPlantas, CierreNivel
    }

    public static LevelDirector2 Instance { get; private set; }

    [Header("NPCs")]
    [SerializeField] private GameObject haberNPC;
    [SerializeField] private GameObject boschNPC;
    [SerializeField] private playercontroller playerRef;

    [Header("Camara")]
    [SerializeField] private FollowCamera camaraSeguimiento;
    [SerializeField] private Transform ancoraCamaraInicio;
    [SerializeField] private Transform ancoraCamaraHaber;
    [SerializeField] private Transform ancoraCamaraBosch;
    [SerializeField] private Transform ancoraCamaraCampo;
    [SerializeField] private Transform ancoraCamaraPortal;

    [Header("Minijuego sintesis")]
    [SerializeField] private GameObject canvasMinijuego;
    [SerializeField] private SintesisMinijuego sintesisMinijuego;

    [Header("Botones de contexto (laboratorio)")]
    [SerializeField] private BotonContexto[] botonesLaboratorio;

    [Header("Campo de plantas")]
    [SerializeField] private PlantaFertilizable[] plantasCampo;
    [SerializeField] private GameObject portalNivel3;

    [Header("Indicador de fertilizante")]
    [SerializeField] private FertilizanteIndicador indicadorFertilizante;

    [Header("Timelines")]
    [SerializeField] private PlayableDirector timelineEntrada;
    [SerializeField] private PlayableDirector timelineBosch_Si;
    [SerializeField] private PlayableDirector timelineBosch_No;
    [SerializeField] private PlayableDirector timelineTransicionCampo;
    [SerializeField] private PlayableDirector timelinePortal;

    [Header("Fade de entrada (azul)")]
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private float duracionFadeEntrada = 2f;

    [Header("Configuracion")]
    [SerializeField] private float duracionTransicionSinTimeline = 3f;
    [SerializeField] private string nombreEscenaNivel3 = "Nivel3";
    [SerializeField] private int plantasRequeridas = 3;
    [SerializeField] private float distanciaHaberParaDialogo = 2f;

    private LevelPhase2 faseActual;
    private Dialogo dialogoHaber;
    private Dialogo dialogoBosch;
    private bool decisionMoralRegistrada = false;
    private bool compartioFormula = false;
    private int plantasFertilizadas = 0;

    public bool PuedeFertilizar { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (haberNPC != null)
            dialogoHaber = haberNPC.GetComponent<Dialogo>();
        else
            Debug.LogError("[LevelDirector2] haberNPC no asignado en Inspector");

        if (boschNPC != null)
            dialogoBosch = boschNPC.GetComponent<Dialogo>();

        if (playerRef == null)
            playerRef = FindObjectOfType<playercontroller>();

        if (camaraSeguimiento != null && ancoraCamaraInicio != null)
            camaraSeguimiento.SetTarget(ancoraCamaraInicio);

        if (portalNivel3 != null) portalNivel3.SetActive(false);
        if (canvasMinijuego != null) canvasMinijuego.SetActive(false);
        if (boschNPC != null) boschNPC.SetActive(false);

        HaberPatrulla patrulla = haberNPC?.GetComponent<HaberPatrulla>();
        if (patrulla != null) patrulla.enabled = false;

        Debug.Log("[LevelDirector2] Iniciando Nivel 2");
        BloqueaInputTotal();
        StartCoroutine(IniciarEntrada());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (faseActual == LevelPhase2.DecisionMoral && !decisionMoralRegistrada)
            ProcesarInputDecisionMoral();

        if (faseActual == LevelPhase2.BuscarHaber && haberNPC != null && playerRef != null)
        {
            float dist = Vector2.Distance(playerRef.transform.position, haberNPC.transform.position);
            if (dist < distanciaHaberParaDialogo && Input.GetKeyDown(KeyCode.E))
            {
                faseActual = LevelPhase2.DialogoHaber;
                StopAllCoroutines();
                haberNPC.GetComponent<HaberPatrulla>()?.Detener(playerRef.transform.position);
                StartCoroutine(IniciarDialogoHaber());
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    // FASES
    // ════════════════════════════════════════════════════════════════

    private IEnumerator IniciarEntrada()
    {
        faseActual = LevelPhase2.Entrada;
        Debug.Log("[LevelDirector2] FASE: Entrada");

        if (screenFader != null)
        {
            yield return screenFader.FadeIn(new Color(0.05f, 0.1f, 0.4f), 0.1f);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return screenFader.FadeOut(duracionFadeEntrada);
        }

        if (timelineEntrada != null)
            yield return EsperarTimeline(timelineEntrada);

        HaberPatrulla patrulla = haberNPC?.GetComponent<HaberPatrulla>();
        if (patrulla != null)
        {
            patrulla.enabled = true;
            patrulla.IniciarPatrulla();
            Debug.Log("[LevelDirector2] HaberPatrulla activada.");
        }

        StartCoroutine(IniciarBuscarHaber());
    }

    private IEnumerator IniciarBuscarHaber()
    {
        faseActual = LevelPhase2.BuscarHaber;
        Debug.Log("[LevelDirector2] FASE: Buscar Haber");

        if (camaraSeguimiento != null && playerRef != null)
            camaraSeguimiento.SetTarget(playerRef.transform);

        DesbloqueaInputTotal();
        HabilitarBotonesContexto(false);
        AgregarObjetivo("Encuentra a Fritz Haber en el laboratorio", "encontrar_haber");

        yield return null;
    }

    private IEnumerator IniciarDialogoHaber()
    {
        faseActual = LevelPhase2.DialogoHaber;
        Debug.Log("[LevelDirector2] FASE: Dialogo Haber");

        BloqueaInputTotal();
        ObjectiveManager.Instance?.CheckObjectiveCompletion("encontrar_haber");

        if (camaraSeguimiento != null && ancoraCamaraHaber != null)
            camaraSeguimiento.SetTarget(ancoraCamaraHaber);
        yield return EsperarCamaraEstable();

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "¡Por fin! Llevas diez minutos de retraso.",
            "En ciencia, diez minutos pueden ser la diferencia entre el exito y empezar desde cero.",
            "Soy el profesor Haber. Y tu eres mi nuevo asistente, supongo.",
            "No importa. Hay trabajo que hacer antes de que llegue nuestra visita de esta tarde.",
            "Revisa cada equipo del laboratorio. El quemador, el reactor, el reloj, la mesa de quimica y la pizarra.",
            "Quiero saber que todo esta en orden. Presiona E en cada uno."
        });
        yield return EsperarTerminoDialogo();

        if (camaraSeguimiento != null && playerRef != null)
            camaraSeguimiento.SetTarget(playerRef.transform);

        HabilitarBotonesContexto(true);
        DesbloqueaInputTotal();
        AgregarObjetivo("Revisa todos los equipos del laboratorio (presiona E en cada uno)", "explorar_lab", "", 5);

        yield return EsperarObjetivo("explorar_lab");

        HabilitarBotonesContexto(false);
        BloqueaInputTotal();

        if (camaraSeguimiento != null && ancoraCamaraHaber != null)
            camaraSeguimiento.SetTarget(ancoraCamaraHaber);
        yield return EsperarCamaraEstable();

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "Bien. Ya tienes una idea del proceso. Ahora ven, necesito que me ayudes con la sintesis.",
            "Hoy vamos a demostrar que esto funciona.",
            "Coloca el nitrogeno y el hidrogeno en los slots. La proporcion es exacta: un volumen de N2 por cada tres de H2."
        });
        yield return EsperarTerminoDialogo();

        StartCoroutine(IniciarMinijuego());
    }

    private IEnumerator IniciarMinijuego()
    {
        faseActual = LevelPhase2.Minijuego;
        Debug.Log("[LevelDirector2] FASE: Minijuego sintesis");

        AgregarObjetivo("Sintetiza amoniaco: coloca N2 y 3xH2 en los slots", "sintesis_completa");

        if (canvasMinijuego != null) canvasMinijuego.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        yield return EsperarObjetivo("sintesis_completa");

        if (canvasMinijuego != null) canvasMinijuego.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        StartCoroutine(IniciarDecisionMoral());
    }

    private IEnumerator IniciarDecisionMoral()
    {
        faseActual = LevelPhase2.DecisionMoral;
        Debug.Log("[LevelDirector2] FASE: Decision Moral");

        BloqueaMovimientoJugador();

        if (camaraSeguimiento != null && ancoraCamaraHaber != null)
            camaraSeguimiento.SetTarget(ancoraCamaraHaber);
        yield return EsperarCamaraEstable();

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "Perfecto. ¿Lo ves? Casi un gramo de amoniaco puro.",
            "En ese tubo pequeño esta el futuro de la agricultura mundial.",
            "Pero antes de que llegue Bosch... el Ministerio de Guerra lleva semanas contactandome.",
            "Alemania depende del salitre de Chile para sus reservas militares.",
            "Si hay un bloqueo naval, se quedan sin explosivos en meses.",
            "Con esta formula... eso cambiaria.",
            "¿Tu que harias? Presiona 1 para compartir la formula con el ejercito, 2 para negarte."
        });
        yield return EsperarTerminoDialogo();

        while (!decisionMoralRegistrada)
            yield return new WaitForSeconds(0.1f);

        StartCoroutine(IniciarCinematicaSegunDecision());
    }

    private IEnumerator IniciarCinematicaSegunDecision()
    {
        faseActual = LevelPhase2.DialogoCierre;
        Debug.Log($"[LevelDirector2] FASE: Cinematica segun decision — compartio: {compartioFormula}");

        BloqueaInputTotal();

        if (boschNPC != null)
        {
            boschNPC.SetActive(true);
            dialogoBosch = boschNPC.GetComponent<Dialogo>();
        }

        if (camaraSeguimiento != null && ancoraCamaraBosch != null)
            camaraSeguimiento.SetTarget(ancoraCamaraBosch);
        yield return EsperarCamaraEstable();

        if (compartioFormula)
        {
            if (timelineBosch_Si != null)
                yield return EsperarTimeline(timelineBosch_Si);
            else
                yield return StartCoroutine(FallbackRama1());
        }
        else
        {
            if (timelineBosch_No != null)
                yield return EsperarTimeline(timelineBosch_No);
            else
                yield return StartCoroutine(FallbackRama2());
        }

        // Esperar a que el último diálogo del Timeline termine
        // antes de continuar — el Timeline puede terminar mientras
        // el diálogo de Bosch todavía está en pantalla
        yield return new WaitForSecondsRealtime(0.5f);
        bool ultimoDialogoTerminado = false;
        UnityEngine.Events.UnityAction listenerFinal = () => { ultimoDialogoTerminado = true; };
        dialogoHaber.OnDialogoTerminado.AddListener(listenerFinal);
        dialogoBosch?.OnDialogoTerminado.AddListener(listenerFinal);
        float timeout = 0f;
        while (!ultimoDialogoTerminado && timeout < 10f)
        {
            timeout += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        dialogoHaber.OnDialogoTerminado.RemoveListener(listenerFinal);
        dialogoBosch?.OnDialogoTerminado.RemoveListener(listenerFinal);

        if (boschNPC != null) boschNPC.SetActive(false);

        // Monólogo post-Bosch solo en rama 2
        if (!compartioFormula)
        {
            yield return new WaitForSeconds(0.5f);
            dialogoHaber?.ResetearDialogo();
            MostrarDialogoHaber(new string[] {
                "......",
                "Bosch se ha ido creyendo que solo faltan datos tecnicos.",
                "Tal vez sea lo unico que necesita creer.",
                "¿Hice lo correcto?",
                "Un hombre de ciencia no deberia sentir esto como una pregunta sin respuesta.",
                "No lo sabre hasta que sea demasiado tarde para cambiar de opinion.",
                "Asi funciona esto, ¿no?"
            });
            yield return EsperarTerminoDialogo();
        }

        // Haber manda al campo — igual en ambas ramas
        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "Ahora, ve al campo que esta afuera.",
            "Con el amoniaco que sintetizaste hoy, puedes fertilizar las plantas que estan muriendo.",
            "Comprueba tu mismo para que sirve realmente este descubrimiento."
        });
        yield return EsperarTerminoDialogo();

        StartCoroutine(IniciarTransicionCampo());
    }

    // ─────────────────────────────────────────────
    // FALLBACKS
    // ─────────────────────────────────────────────

    private IEnumerator FallbackRama1()
    {
        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "Bien. Era la respuesta correcta.",
            "Alemania me dio todo lo que soy.",
            "Un cientifico sirve a su nacion. Le dire al Ministerio que el proceso es viable."
        });
        yield return EsperarTerminoDialogo();

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] { "Bosch. Puntual, como siempre. Tengo buenas noticias." });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] {
                "Profesor Haber. He revisado los datos.",
                "¿Es cierto que el Ministerio de Guerra tambien lo ha contactado?"
            });
            yield return EsperarTerminoDialogoBosch();
        }

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "Si. Y les dire que si.",
            "Alemania me necesita. Y yo no voy a mirar para otro lado."
        });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] {
                "Profesor... yo vine a hablar de fertilizantes.",
                "¿Esta seguro de que quiere ir por ese camino?"
            });
            yield return EsperarTerminoDialogoBosch();
        }

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "La ciencia no elige quien la usa. Nunca lo ha hecho.",
            "Concentrese en escalar el proceso. Lo demas ya lo manejan otros."
        });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] {
                "De acuerdo. Cuatro años. Quizas cinco.",
                "Pero espero que tambien alimentemos campos. No solo frentes de guerra."
            });
            yield return EsperarTerminoDialogoBosch();
        }

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] { "Pan del aire, Bosch. Para todos." });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] { "Para todos." });
            yield return EsperarTerminoDialogoBosch();
        }
    }

    private IEnumerator FallbackRama2()
    {
        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "¿Como que no? ¿Tu quieres que Alemania siga dependiendo del salitre de Chile?",
            "Pero...... esta formula tambien podria crear explosivos.",
            "¿O si?",
            "No. No se la voy a dar. Le dire al Ministerio que el proceso no esta listo."
        });
        yield return EsperarTerminoDialogo();

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] { "Bosch. Adelante." });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] {
                "Profesor Haber. He revisado los datos.",
                "El Ministerio de Guerra tambien me contacto esta semana. ¿Usted ya hablo con ellos?"
            });
            yield return EsperarTerminoDialogoBosch();
        }

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "El proceso no esta listo para aplicaciones militares.",
            "Necesito mas tiempo. Concentremonos en la agricultura."
        });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] {
                "Profesor, los numeros son claros. En algun momento vamos a tener que darles una respuesta."
            });
            yield return EsperarTerminoDialogoBosch();
        }

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] {
            "Cuando el proceso escale industrialmente... las aplicaciones seran decision de otros.",
            "Yo soy cientifico, no militar.",
            "......eso ya no esta en mis manos."
        });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] {
                "De acuerdo. Cuatro años. Quizas cinco.",
                "Espero que cuando llegue ese momento, todavia piense lo mismo."
            });
            yield return EsperarTerminoDialogoBosch();
        }

        dialogoHaber?.ResetearDialogo();
        MostrarDialogoHaber(new string[] { "Pan del aire, Bosch." });
        yield return EsperarTerminoDialogo();

        if (dialogoBosch != null)
        {
            dialogoBosch.ResetearDialogo();
            dialogoBosch.IniciarDialogoPrograma(new string[] { "Pan del aire." });
            yield return EsperarTerminoDialogoBosch();
        }
    }

    private IEnumerator IniciarTransicionCampo()
    {
        faseActual = LevelPhase2.TransicionCampo;
        Debug.Log("[LevelDirector2] FASE: Transicion al campo");

        BloqueaInputTotal();

        if (camaraSeguimiento != null && ancoraCamaraCampo != null)
            camaraSeguimiento.SetTarget(ancoraCamaraCampo);

        if (timelineTransicionCampo != null)
            yield return EsperarTimeline(timelineTransicionCampo);
        else
            yield return new WaitForSeconds(duracionTransicionSinTimeline);

        StartCoroutine(IniciarCampoPlantas());
    }

    private IEnumerator IniciarCampoPlantas()
    {
        faseActual = LevelPhase2.CampoPlantas;
        Debug.Log("[LevelDirector2] FASE: Campo de plantas");

        if (camaraSeguimiento != null && playerRef != null)
            camaraSeguimiento.SetTarget(playerRef.transform);

        DesbloqueaInputTotal();

        if (indicadorFertilizante != null)
            indicadorFertilizante.Activar();

        plantasFertilizadas = 0;
        AgregarObjetivo($"Fertiliza {plantasRequeridas} plantas con E", "planta_fertilizada", "", plantasRequeridas);
        PuedeFertilizar = true;

        yield return EsperarObjetivo("planta_fertilizada");

        PuedeFertilizar = false;
        if (indicadorFertilizante != null)
            indicadorFertilizante.Desactivar();

        StartCoroutine(IniciarCierreNivel());
    }

    private IEnumerator IniciarCierreNivel()
    {
        faseActual = LevelPhase2.CierreNivel;
        Debug.Log("[LevelDirector2] FASE: Cierre — portal");

        BloqueaInputTotal();

        if (camaraSeguimiento != null && ancoraCamaraPortal != null)
            camaraSeguimiento.SetTarget(ancoraCamaraPortal);
        yield return EsperarCamaraEstable();

        if (timelinePortal != null)
            yield return EsperarTimeline(timelinePortal);
        else
            yield return new WaitForSeconds(2f);

        if (portalNivel3 != null) portalNivel3.SetActive(true);

        DesbloqueaInputTotal();

        if (camaraSeguimiento != null && playerRef != null)
            camaraSeguimiento.SetTarget(playerRef.transform);

        AgregarObjetivo("Entra al portal para continuar", "portal_activado");
        yield return null;
    }

    // ════════════════════════════════════════════════════════════════
    // MÉTODOS PÚBLICOS
    // ════════════════════════════════════════════════════════════════

    public void NotificarPlantaFertilizada()
    {
        plantasFertilizadas++;
        Debug.Log($"[LevelDirector2] Plantas fertilizadas: {plantasFertilizadas}/{plantasRequeridas}");
        ObjectiveManager.Instance?.CheckObjectiveCompletion("planta_fertilizada");
    }

    public void NotificarSintesisCompleta()
    {
        Debug.Log("[LevelDirector2] Sintesis completada.");
        ObjectiveManager.Instance?.CheckObjectiveCompletion("sintesis_completa");
    }

    public void MostrarTextoContextoHaber(string[] lineas)
    {
        if (faseActual != LevelPhase2.DialogoHaber) return;
        if (dialogoHaber == null) return;
        dialogoHaber.ResetearDialogo();
        dialogoHaber.IniciarDialogoPrograma(lineas);
    }

    // ✅ CORREGIDO: fade azul antes de cargar el Nivel 3
    public void CargarNivel3()
    {
        Debug.Log("[LevelDirector2] Cargando Nivel 3...");
        ObjectiveManager.Instance?.CheckObjectiveCompletion("portal_activado");
        StartCoroutine(FadeYCargarNivel3());
    }

    private IEnumerator FadeYCargarNivel3()
    {
        if (screenFader != null)
            yield return screenFader.FadeIn(new Color(0.05f, 0.1f, 0.4f), 1.5f);
        else
            yield return new WaitForSeconds(1.5f);

        SceneManager.LoadScene(nombreEscenaNivel3);
    }

    // ════════════════════════════════════════════════════════════════
    // AUXILIARES
    // ════════════════════════════════════════════════════════════════

    private IEnumerator EsperarTerminoDialogo()
    {
        if (dialogoHaber == null) yield break;
        bool terminado = false;
        UnityEngine.Events.UnityAction listener = () => { terminado = true; };
        dialogoHaber.OnDialogoTerminado.AddListener(listener);
        while (!terminado) yield return new WaitForSeconds(0.1f);
        dialogoHaber.OnDialogoTerminado.RemoveListener(listener);
    }

    private IEnumerator EsperarTerminoDialogoBosch()
    {
        if (dialogoBosch == null) yield break;
        bool terminado = false;
        UnityEngine.Events.UnityAction listener = () => { terminado = true; };
        dialogoBosch.OnDialogoTerminado.AddListener(listener);
        while (!terminado) yield return new WaitForSeconds(0.1f);
        dialogoBosch.OnDialogoTerminado.RemoveListener(listener);
    }

    private IEnumerator EsperarTimeline(PlayableDirector pd)
    {
        if (pd == null) yield break;
        pd.Stop();
        yield return null;

        bool finalizado = false;
        void OnStopped(PlayableDirector d) => finalizado = true;
        pd.stopped += OnStopped;
        pd.Play();

        while (!finalizado) yield return null;
        pd.stopped -= OnStopped;
        Debug.Log($"[LevelDirector2] Timeline terminado: {pd.name}");
    }

    private IEnumerator EsperarCamaraEstable()
    {
        if (camaraSeguimiento == null) yield break;
        while (!camaraSeguimiento.EstaEstable)
            yield return null;
    }

    private IEnumerator EsperarObjetivo(string actionType)
    {
        Objective objetivo = null;
        while (objetivo == null)
        {
            var todos = ObjectiveManager.Instance?.AllObjectives;
            if (todos != null)
                foreach (var obj in todos)
                    if (obj.requiredActionType == actionType &&
                        obj.Estado != Objective.EstadoType.Completado)
                    { objetivo = obj; break; }
            if (objetivo == null) yield return new WaitForSeconds(0.2f);
        }
        while (objetivo.Estado != Objective.EstadoType.Completado)
            yield return new WaitForSeconds(0.2f);
        Debug.Log($"[LevelDirector2] Objetivo '{actionType}' completado.");
    }

    private void ProcesarInputDecisionMoral()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            compartioFormula = true;
            decisionMoralRegistrada = true;
            Debug.Log("[LevelDirector2] Decision: SI compartio.");
            DesbloqueaInputTotal();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            compartioFormula = false;
            decisionMoralRegistrada = true;
            Debug.Log("[LevelDirector2] Decision: NO compartio.");
            DesbloqueaInputTotal();
        }
    }

    private void MostrarDialogoHaber(string[] lineas)
    {
        if (dialogoHaber != null)
            dialogoHaber.IniciarDialogoPrograma(lineas);
        else
            Debug.LogWarning("[LevelDirector2] dialogoHaber es null");
    }

    private void AgregarObjetivo(string descripcion, string actionType = "",
                                  string actionDetail = "", int count = 1)
    {
        ObjectiveManager.Instance?.AddObjective(
            new Objective(descripcion, actionType, actionDetail, count));
        Debug.Log($"[LevelDirector2] Objetivo: \"{descripcion}\"");
    }

    private void HabilitarBotonesContexto(bool estado)
    {
        if (botonesLaboratorio == null) return;
        foreach (var b in botonesLaboratorio)
            if (b != null) b.enabled = estado;
    }

    private void BloqueaInputTotal()
    {
        if (playerRef != null) playerRef.enabled = false;
        ComandoSystem cs = FindObjectOfType<ComandoSystem>();
        if (cs != null) cs.enabled = false;
        PauseMenuController pm = FindObjectOfType<PauseMenuController>();
        if (pm != null) pm.enabled = false;
        Debug.Log("[LevelDirector2] Input BLOQUEADO");
    }

    private void DesbloqueaInputTotal()
    {
        if (playerRef != null) playerRef.enabled = true;
        ComandoSystem cs = FindObjectOfType<ComandoSystem>();
        if (cs != null) cs.enabled = true;
        PauseMenuController pm = FindObjectOfType<PauseMenuController>();
        if (pm != null) pm.enabled = true;
        Debug.Log("[LevelDirector2] Input DESBLOQUEADO");
    }

    private void BloqueaMovimientoJugador()
    {
        if (playerRef != null) playerRef.enabled = false;
    }

    public LevelPhase2 ObtenerFase() => faseActual;
    public bool CompartioFormula() => compartioFormula;
    public int PlantasFertilizadas() => plantasFertilizadas;
}