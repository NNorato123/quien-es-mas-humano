// SintesisMinijuego.cs
// Minijuego de síntesis de amoníaco para el Nivel 2
// Mecánica: 4 slots (1x N₂ y 3x H₂). El jugador arrastra moléculas hacia los slots correctos.
// Cuando todos los slots están llenos correctamente → notifica a LevelDirector2
//
// SETUP EN UNITY:
// 1. Crea un Canvas (WorldSpace o Screen Space Overlay) y llámalo "CanvasMinijuego"
// 2. Dentro del Canvas crea:
//    - 4 slots (GameObjects con Image + SlotSintesis tag): uno tipo "N2", tres tipo "H2"
//    - Moléculas arrastrables: Image con EventTrigger o este script
//    - Un panel de resultado (TextMeshProUGUI para mostrar "2NH₃")
// 3. Asigna todo en el Inspector de este script

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class SintesisMinijuego : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // TIPOS DE MOLÉCULA
    // ════════════════════════════════════════════════════════════════
    public enum TipoMolecula { N2, H2 }

    [System.Serializable]
    public class SlotSintesis
    {
        public RectTransform rect;      // Posición visual del slot
        public TipoMolecula tipo;       // Qué molécula acepta
        public Image imagenSlot;        // Para cambiar color al llenarse
        [HideInInspector] public bool lleno = false;
    }

    [System.Serializable]
    public class MoleculaArrastrable
    {
        public RectTransform rect;
        public TipoMolecula tipo;
        public TMP_Text etiqueta;       // Muestra "N₂" o "H₂"
        [HideInInspector] public Vector2 posicionOriginal;
        [HideInInspector] public bool colocada = false;
    }

    // ════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ════════════════════════════════════════════════════════════════
    [Header("Slots (1x N₂ + 3x H₂)")]
    [SerializeField] private SlotSintesis[] slots;          // 4 elementos: [0]=N2, [1][2][3]=H2

    [Header("Moléculas arrastrables")]
    [SerializeField] private MoleculaArrastrable[] moleculas; // El jugador las arrastra

    [Header("UI de resultado")]
    [SerializeField] private GameObject panelResultado;         // Panel "¡Síntesis completa!"
    [SerializeField] private TMP_Text textoResultado;           // "N₂ + 3H₂ → 2NH₃"
    [SerializeField] private TMP_Text textoInstruccion;         // Instrucción inicial

    [Header("Configuración")]
    [SerializeField] private float snapDistance = 80f;          // Distancia para hacer snap al slot
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private float tiempoFeedbackError = 3f;    // Segundos que dura el rojo al fallar
    [SerializeField] private Color colorSlotVacio = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    [SerializeField] private Color colorSlotLleno = new Color(0.3f, 0.9f, 0.4f, 0.8f);
    [SerializeField] private Color colorSlotError = new Color(0.9f, 0.3f, 0.3f, 0.8f);

    // ════════════════════════════════════════════════════════════════
    // ESTADO INTERNO
    // ════════════════════════════════════════════════════════════════
    private int sllotsLlenos = 0;
    private bool minijuegoCompletado = false;
    private MoleculaArrastrable moleculaArrastrando = null;
    private Vector2 offsetArrastre;
    private Canvas canvasRef;

    // ════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════
    private void Awake()
    {
        canvasRef = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        Reiniciar();
    }

    private void Reiniciar()
    {
        sllotsLlenos = 0;
        minijuegoCompletado = false;
        moleculaArrastrando = null;

        // Resetear slots
        if (slots != null)
            foreach (var slot in slots)
            {
                slot.lleno = false;
                if (slot.imagenSlot != null)
                    slot.imagenSlot.color = colorSlotVacio;
            }

        // Resetear moléculas a posición original
        if (moleculas != null)
            foreach (var mol in moleculas)
            {
                mol.colocada = false;
                if (mol.posicionOriginal == Vector2.zero)
                    mol.posicionOriginal = mol.rect.anchoredPosition;
                else
                    mol.rect.anchoredPosition = mol.posicionOriginal;

                // Etiquetar la molécula visualmente
                if (mol.etiqueta != null)
                    mol.etiqueta.text = mol.tipo == TipoMolecula.N2 ? "N₂" : "H₂";
            }

        if (panelResultado != null) panelResultado.SetActive(false);
        if (textoInstruccion != null)
            textoInstruccion.text = "Arrastra N₂ y 3 veces H₂ a sus slots para sintetizar NH₃";
    }

    // ════════════════════════════════════════════════════════════════
    // INPUT — Update maneja el drag & drop manualmente
    // (compatible con EventSystem pero sin depender de IPointerDownHandler global)
    // ════════════════════════════════════════════════════════════════
    private void Update()
    {
        if (minijuegoCompletado) return;

        // Inicio del arrastre
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            foreach (var mol in moleculas)
            {
                if (mol.colocada) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(mol.rect, mousePos, canvasRef.worldCamera))
                {
                    moleculaArrastrando = mol;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        mol.rect.parent as RectTransform, mousePos,
                        canvasRef.worldCamera, out Vector2 localPos);
                    offsetArrastre = mol.rect.anchoredPosition - localPos;
                    mol.rect.SetAsLastSibling(); // Traer al frente
                    break;
                }
            }
        }

        // Durante el arrastre
        if (Input.GetMouseButton(0) && moleculaArrastrando != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                moleculaArrastrando.rect.parent as RectTransform,
                Input.mousePosition, canvasRef.worldCamera, out Vector2 localPos);
            moleculaArrastrando.rect.anchoredPosition = localPos + offsetArrastre;
        }

        // Fin del arrastre — intentar snap
        if (Input.GetMouseButtonUp(0) && moleculaArrastrando != null)
        {
            IntentarSnap(moleculaArrastrando);
            moleculaArrastrando = null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // LÓGICA DE SNAP
    // ════════════════════════════════════════════════════════════════
    private void IntentarSnap(MoleculaArrastrable mol)
    {
        // ── Buscar el slot correcto más cercano (mismo tipo, vacío) ──
        float menorDistanciaCorrecta = float.MaxValue;
        SlotSintesis slotMasCercano = null;

        foreach (var slot in slots)
        {
            if (slot.lleno) continue;           // Slot ya ocupado
            if (slot.tipo != mol.tipo) continue; // Tipo incorrecto

            float distancia = Vector2.Distance(mol.rect.anchoredPosition, slot.rect.anchoredPosition);
            if (distancia < menorDistanciaCorrecta)
            {
                menorDistanciaCorrecta = distancia;
                slotMasCercano = slot;
            }
        }

        if (slotMasCercano != null && menorDistanciaCorrecta <= snapDistance)
        {
            // ✅ Snap exitoso
            mol.rect.DOAnchorPos(slotMasCercano.rect.anchoredPosition, animDuration)
                .SetEase(Ease.OutBack);

            slotMasCercano.lleno = true;
            mol.colocada = true;

            if (slotMasCercano.imagenSlot != null)
                slotMasCercano.imagenSlot.color = colorSlotLleno;

            // La molécula también se queda en verde permanente
            Image imgMolOK = mol.rect.GetComponent<Image>();
            if (imgMolOK != null) imgMolOK.color = colorSlotLleno;

            sllotsLlenos++;
            Debug.Log($"[SintesisMinijuego] Slot llenado. Total: {sllotsLlenos}/4");

            if (sllotsLlenos >= 4)
                StartCoroutine(CompletarSintesis());
        }
        else
        {
            // ❌ Falló — buscar el slot más cercano en general (sin importar tipo)
            // para marcarlo en rojo junto con la molécula
            float menorDistanciaGeneral = float.MaxValue;
            SlotSintesis slotIntentado = null;

            foreach (var slot in slots)
            {
                if (slot.lleno) continue; // no marcar en rojo un slot que ya está lleno/verde
                float distancia = Vector2.Distance(mol.rect.anchoredPosition, slot.rect.anchoredPosition);
                if (distancia < menorDistanciaGeneral)
                {
                    menorDistanciaGeneral = distancia;
                    slotIntentado = slot;
                }
            }

            // Solo lo consideramos "intento fallido sobre un slot" si estaba razonablemente cerca
            bool huboIntentoSobreSlot = slotIntentado != null && menorDistanciaGeneral <= snapDistance;

            StartCoroutine(FeedbackError(mol, huboIntentoSobreSlot ? slotIntentado : null));
            mol.rect.DOAnchorPos(mol.posicionOriginal, animDuration).SetEase(Ease.OutBounce);
        }
    }

    private IEnumerator FeedbackError(MoleculaArrastrable mol, SlotSintesis slotError)
    {
        // Molécula en rojo
        Image imgMol = mol.rect.GetComponent<Image>();
        Color originalMol = default;
        bool tieneImgMol = imgMol != null;
        if (tieneImgMol)
        {
            originalMol = imgMol.color;
            imgMol.color = colorSlotError;
        }

        // Slot en rojo (si hubo un intento sobre uno)
        if (slotError != null && slotError.imagenSlot != null)
            slotError.imagenSlot.color = colorSlotError;

        yield return new WaitForSecondsRealtime(tiempoFeedbackError);

        // Restaurar molécula a su color original
        if (tieneImgMol) imgMol.color = originalMol;

        // Restaurar slot solo si sigue vacío (por si en esos 3s el jugador lo llenó bien)
        if (slotError != null && slotError.imagenSlot != null && !slotError.lleno)
            slotError.imagenSlot.color = colorSlotVacio;
    }

    // ════════════════════════════════════════════════════════════════
    // COMPLETADO
    // ════════════════════════════════════════════════════════════════
    private IEnumerator CompletarSintesis()
    {
        minijuegoCompletado = true;
        Debug.Log("[SintesisMinijuego] ¡Síntesis completa! N₂ + 3H₂ → 2NH₃");

        yield return new WaitForSecondsRealtime(0.5f);

        // Mostrar panel de resultado
        if (panelResultado != null)
        {
            panelResultado.SetActive(true);
            if (textoResultado != null)
                textoResultado.text = "N₂ + 3H₂ → 2NH₃\n¡Amoníaco sintetizado!";
        }

        yield return new WaitForSecondsRealtime(2f);

        // Notificar al LevelDirector2
        LevelDirector2.Instance?.NotificarSintesisCompleta();
    }
}