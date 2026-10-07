// PauseMenuController.cs (mejorado con DOTween)
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("Referencias Canvas")]
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    [Header("Paneles")]
    [SerializeField] private GameObject panelPrincipal;
    [SerializeField] private GameObject panelObjetivos;
    [SerializeField] private GameObject panelAjustes;
    [SerializeField] private MissionPanelUI missionPanelUI;

    [Header("Animaciones")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float panelSlideDuration = 0.4f;

    private bool isPaused = false;
    private Sequence currentSequence;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        pauseCanvas.enabled = false;

        // OPT: Crear CanvasGroup si no existe
        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup = pauseCanvas.GetComponent<CanvasGroup>();
            if (pauseCanvasGroup == null)
            {
                pauseCanvasGroup = pauseCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        pauseCanvasGroup.alpha = 0f;
    }

    private void Update()
    {
        // OPT: Detectar P para alternar pausa (sin conflictos con otras teclas)
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                ReanudarJuego();
            }
            else
            {
                PausarJuego();
            }
        }
    }

    private void PausarJuego()
    {
        isPaused = true;
        pauseCanvas.enabled = true;
        pauseCanvasGroup.alpha = 0f;  // OPT: Asegurar que comienza invisible antes del fade-in
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Cancelar secuencia anterior si existe
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        currentSequence = DOTween.Sequence();

        // Fade-in del canvas
        currentSequence.Append(pauseCanvasGroup.DOFade(1f, fadeInDuration)
            .SetEase(Ease.InQuad));

        // Mostrar panel principal con animación
        MostrarPanel(panelPrincipal);
        panelObjetivos.SetActive(false);
        panelAjustes.SetActive(false);
    }

    private void ReanudarJuego()
    {
        isPaused = false;

        // Cancelar secuencia
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        currentSequence = DOTween.Sequence();

        // Fade-out
        currentSequence.Append(pauseCanvasGroup.DOFade(0f, fadeInDuration)
            .SetEase(Ease.OutQuad));

        currentSequence.OnComplete(() => {
            pauseCanvas.enabled = false;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        });
    }

    private void MostrarPanel(GameObject panel)
    {
        // OPT: Animar entrada de panel con slide + fade
        CanvasGroup panelCanvasGroup = panel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        }

        panel.SetActive(true);
        RectTransform panelRect = panel.GetComponent<RectTransform>();

        // Resetear estado
        panelCanvasGroup.alpha = 0f;
        Vector2 posOriginal = panelRect.anchoredPosition;
        panelRect.anchoredPosition = posOriginal + new Vector2(-30f, 0f);

        // Animar entrada
        Sequence seqPanel = DOTween.Sequence();
        seqPanel.Append(panelRect.DOAnchorPos(posOriginal, panelSlideDuration)
            .SetEase(Ease.OutCubic));
        seqPanel.Join(panelCanvasGroup.DOFade(1f, panelSlideDuration)
            .SetEase(Ease.InQuad));
    }

    private void OcultarPanel(GameObject panel, System.Action onComplete = null)
    {
        // OPT: Animar salida de panel
        CanvasGroup panelCanvasGroup = panel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Vector2 posOriginal = panelRect.anchoredPosition;

        Sequence seqSalida = DOTween.Sequence();
        seqSalida.Append(panelRect.DOAnchorPos(posOriginal + new Vector2(-30f, 0f), panelSlideDuration)
            .SetEase(Ease.InCubic));
        seqSalida.Join(panelCanvasGroup.DOFade(0f, panelSlideDuration)
            .SetEase(Ease.OutQuad));

        seqSalida.OnComplete(() => {
            panel.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void OnBotonObjetivos()
    {
        OcultarPanel(panelPrincipal, () => {
            MostrarPanel(panelObjetivos);
            missionPanelUI.Refresh();
        });
    }

    public void OnBotonAjustes()
    {
        OcultarPanel(panelPrincipal, () => {
            MostrarPanel(panelAjustes);
        });
    }

    public void OnBotonVolver()
    {
        // OPT: Detectar cuál panel está activo y ocultarlo
        if (panelObjetivos.activeSelf)
        {
            OcultarPanel(panelObjetivos, () => {
                MostrarPanel(panelPrincipal);
            });
        }
        else if (panelAjustes.activeSelf)
        {
            OcultarPanel(panelAjustes, () => {
                MostrarPanel(panelPrincipal);
            });
        }
    }

    public void OnBotonGuardarSalir()
    {
        // OPT: Animar salida antes de cambiar escena
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        currentSequence = DOTween.Sequence();
        currentSequence.Append(pauseCanvasGroup.DOFade(0f, 0.3f));
        currentSequence.OnComplete(() => {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        });
    }

    public void OnBotonSalir()
    {
        // OPT: Animar salida antes de quit
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        currentSequence = DOTween.Sequence();
        currentSequence.Append(pauseCanvasGroup.DOFade(0f, 0.3f));
        currentSequence.OnComplete(() => {
            Time.timeScale = 1f;
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        });
    }
}
