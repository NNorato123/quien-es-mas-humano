using UnityEngine;

/// <summary>
/// Recibe señales del Timeline y dispara diálogos de Haber y Bosch.
///
/// CINEMATICA 1 (timelineEntrada) — Haber solo antes de que lleguemos:
///   Signals: Haber_Intro1 → Haber_Intro2 → Haber_Intro3 → Haber_Intro4
///
/// CINEMATICA 2A (timelineBosch_Si) — Rama 1: Haber comparte la fórmula:
///   Signals: Haber_Si_ReaccionDecision → Haber_Si_BoschLlega → Bosch_Si_Saludo →
///            Haber_Si_Confirma → Bosch_Si_Duda → Haber_Si_Convence →
///            Bosch_Si_Acepta → Haber_Si_PanDelAire → Bosch_Si_PanDelAire
///
/// CINEMATICA 2B (timelineBosch_No) — Rama 2: Haber no comparte:
///   Signals: Haber_No_Indigna → Haber_No_Duda → Haber_No_Racionaliza →
///            Haber_No_Decision → Haber_No_BoschLlega → Bosch_No_Saludo →
///            Haber_No_Corta → Bosch_No_Insiste → Haber_No_SalidaComoda →
///            Bosch_No_Acepta → Haber_No_PanDelAire → Bosch_No_PanDelAire
///
/// CINEMATICA 4 (timelinePortal):
///   Signals: Haber_Portal1 → Haber_Portal2
///
/// SETUP:
/// 1. GameObject vacío "CinematicaSignals2" en la escena Nivel2
/// 2. Asignar dialogoHaber y dialogoBosch en Inspector
/// 3. En cada Signal Track del Timeline, receptor = CinematicaSignals2
/// 4. Nombre del Signal = nombre exacto del método público
/// </summary>
public class CinematicaDialogoReceiver2 : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Dialogo dialogoHaber;
    [SerializeField] private Dialogo dialogoBosch;

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    private void HaberDice(params string[] textos)
    {
        if (dialogoHaber == null) { Debug.LogWarning("[CinematicaDialogoReceiver2] dialogoHaber no asignado."); return; }
        dialogoHaber.ResetearDialogo();
        dialogoHaber.IniciarDialogoPrograma(textos);
    }

    private void BoschDice(params string[] textos)
    {
        if (dialogoBosch == null) { Debug.LogWarning("[CinematicaDialogoReceiver2] dialogoBosch no asignado."); return; }
        dialogoBosch.ResetearDialogo();
        dialogoBosch.IniciarDialogoPrograma(textos);
    }

    // ─────────────────────────────────────────────
    // CINEMATICA 1 — Haber hablando solo
    // ─────────────────────────────────────────────

    public void Haber_Intro1()
    {
        HaberDice("N2 mas 3H2... la presion tiene que ser exacta. Exacta.");
    }

    public void Haber_Intro2()
    {
        HaberDice(
            "Cuatrocientas atmosferas. Cuatrocientas. Ni una menos.",
            "Si el catalizador de hierro falla hoy, volvemos a empezar desde cero."
        );
    }

    public void Haber_Intro3()
    {
        HaberDice(
            "Esta tarde viene Bosch. Carl Bosch, de BASF.",
            "Si lo convenzo hoy, esto deja de ser un experimento y se convierte en industria.",
            "Pero primero necesito que todo funcione. Todo."
        );
    }

    public void Haber_Intro4()
    {
        HaberDice(
            "¿Donde esta ese asistente? Lleva ya... diez minutos.",
            "En ciencia, diez minutos pueden ser la diferencia entre el exito y empezar desde cero.",
            "Mas le vale tener una buena excusa."
        );
    }

    // ─────────────────────────────────────────────
    // CINEMATICA 2A — Rama 1: Haber SÍ comparte
    // Haber entusiasta, patriótico, sin dudas
    // Bosch cauteloso pero termina aceptando
    // ─────────────────────────────────────────────

    public void Haber_Si_ReaccionDecision()
    {
        HaberDice(
            "Bien. Era la respuesta correcta.",
            "Alemania me dio todo lo que soy. Mi laboratorio, mi catedra, mi nombre.",
            "Un cientifico sirve a su nacion. Si este descubrimiento puede protegerla, tengo esa responsabilidad.",
            "Le dire al Ministerio que el proceso es viable. Que pueden contar con el."
        );
    }

    public void Haber_Si_BoschLlega()
    {
        HaberDice("Bosch. Puntual, como siempre. Tengo buenas noticias.");
    }

    public void Bosch_Si_Saludo()
    {
        BoschDice(
            "Profesor Haber. He revisado los datos que me envio.",
            "Las presiones, las temperaturas, el rendimiento del catalizador.",
            "Creo que puede hacerse a escala industrial. Pero antes de hablar de eso...",
            "¿Es cierto que el Ministerio de Guerra tambien lo ha contactado?"
        );
    }

    public void Haber_Si_Confirma()
    {
        HaberDice(
            "Si. Y les dire que si.",
            "El amoniaco puede usarse para producir nitratos. Para municion.",
            "Alemania depende del salitre de Chile. Si hay un bloqueo naval, se quedan sin defensa en meses.",
            "Este proceso cambia eso. Y yo no voy a mirar para otro lado cuando mi pais me necesita."
        );
    }

    public void Bosch_Si_Duda()
    {
        BoschDice(
            "Profesor... yo vine a hablar de fertilizantes. De alimentar personas.",
            "Si esto se convierte en un proyecto militar, BASF va a tener preguntas muy dificiles que responder.",
            "¿Esta seguro de que quiere ir por ese camino?"
        );
    }

    public void Haber_Si_Convence()
    {
        HaberDice(
            "Bosch, la ciencia no elige quien la usa. Nunca lo ha hecho.",
            "Lo que yo puedo elegir es si Alemania tiene acceso a ella o no.",
            "Y mi respuesta es si.",
            "Ademas... primero hay que escalar el proceso industrialmente. Eso nos llevara años.",
            "Concentrese en eso. Lo demas ya lo manejan otros."
        );
    }

    public void Bosch_Si_Acepta()
    {
        BoschDice(
            "De acuerdo. Cuatro años. Quizas cinco.",
            "Pero cuando esto funcione a escala, profesor, espero que tambien alimentemos campos.",
            "No solo frentes de guerra."
        );
    }

    public void Haber_Si_PanDelAire()
    {
        HaberDice("Pan del aire, Bosch. Para todos.");
    }

    public void Bosch_Si_PanDelAire()
    {
        BoschDice("Para todos.");
    }

    // ─────────────────────────────────────────────
    // CINEMATICA 2B — Rama 2: Haber NO comparte
    // Haber indignado → duda → racionaliza → salida cómoda
    // Con Bosch lo desvía, pero termina cediendo a medias
    // ─────────────────────────────────────────────

    public void Haber_No_Indigna()
    {
        HaberDice(
            "¿Como que no?",
            "¿Tu quieres que Alemania siga dependiendo del salitre de Chile para siempre?",
            "¿Que un bloqueo naval nos deje sin defensa en semanas?",
            "Yo no cree esto para eso, pero tampoco soy ingenuo.",
            "El conocimiento no elige como lo usan."
        );
    }

    public void Haber_No_Duda()
    {
        HaberDice(
            "Pero......",
            "Esta formula tambien podria crear explosivos. No solo fertilizantes.",
            "El mismo nitrogeno que alimenta el trigo puede alimentar una bomba.",
            "Es la misma reaccion."
        );
    }

    public void Haber_No_Racionaliza()
    {
        HaberDice(
            "Aunque...... yo no tendria nada que ver con como lo usen una vez que entregue la formula.",
            "¿O si?",
            "......",
            "No. No se la voy a dar. Todavia no."
        );
    }

    public void Haber_No_Decision()
    {
        HaberDice(
            "Le dire al Ministerio que el proceso industrial aun no esta listo.",
            "Que el rendimiento de laboratorio no escala directamente.",
            "No es del todo mentira. Y hoy... tendra que bastar."
        );
    }

    public void Haber_No_BoschLlega()
    {
        HaberDice("Bosch. Adelante.");
    }

    public void Bosch_No_Saludo()
    {
        BoschDice(
            "Profesor Haber. He revisado los datos que me envio.",
            "Creo que puede hacerse a escala industrial.",
            "Aunque debo preguntarle algo antes de continuar.",
            "El Ministerio de Guerra tambien me contacto esta semana.",
            "¿Usted ya hablo con ellos?"
        );
    }

    public void Haber_No_Corta()
    {
        HaberDice(
            "El proceso no esta listo para aplicaciones militares.",
            "El rendimiento de laboratorio no escala directamente a produccion industrial.",
            "Hay demasiadas variables sin resolver. Necesito mas tiempo.",
            "Concentremonos en lo que importa hoy: la agricultura."
        );
    }

    public void Bosch_No_Insiste()
    {
        BoschDice(
            "Profesor, los numeros que me envio son claros.",
            "Cualquier ingeniero con experiencia veria que esto ya es viable.",
            "Si el Ministerio insiste, en algun momento vamos a tener que darles una respuesta."
        );
    }

    public void Haber_No_SalidaComoda()
    {
        HaberDice(
            "Cuando el proceso escale industrialmente... las aplicaciones seran decision de otros.",
            "Yo soy cientifico, no militar.",
            "Mi trabajo es que la reaccion funcione. Lo que hagan con ella despues...",
            "......eso ya no esta en mis manos."
        );
    }

    public void Bosch_No_Acepta()
    {
        BoschDice(
            "De acuerdo. Cuatro años. Quizas cinco para escalar.",
            "Aunque, profesor...",
            "Espero que cuando llegue ese momento, todavia piense lo mismo."
        );
    }

    public void Haber_No_PanDelAire()
    {
        HaberDice("Pan del aire, Bosch.");
    }

    public void Bosch_No_PanDelAire()
    {
        BoschDice("Pan del aire.");
    }

    // ─────────────────────────────────────────────
    // CINEMATICA 4 — Portal al Nivel 3
    // ─────────────────────────────────────────────

    public void Haber_Portal1()
    {
        HaberDice(
            "Lo que sintetizaste hoy alimentara campos enteros.",
            "Eso es lo que este descubrimiento puede hacer."
        );
    }

    public void Haber_Portal2()
    {
        HaberDice(
            "Ese portal te llevara al siguiente capitulo.",
            "Lo que aprendas alli... espero que lo uses mejor de lo que yo use lo mio."
        );
    }
}