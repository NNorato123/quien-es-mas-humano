using UnityEngine;
using TMPro;
using DG.Tweening;

public class ObjectiveUI : MonoBehaviour
{
    // OPT: Constantes para magic strings en lugar de hardcodeadas
    private const string PREFIX = "← ";
    private const string SUFFIX = " →";
    private const string COMPLETED_PREFIX = "✔ COMPLETADO: ";
    

    [Header("Referencias")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private CanvasGroup canvasGroup; // Para fade suave

    [Header("Configuración de Animación")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float completedDisplayDuration = 3f;
    [SerializeField] private float notificationDisplayDuration = 5f;

    [Header("Colores")]
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorCompletado = Color.green;

    [Header("Efectos de Escala")]
    [SerializeField] private float scaleEntrante = 0.8f;
    [SerializeField] private float scaleNormal = 1f;

    private Vector2 shownPosition;
    private Vector2 hiddenPosition;

    private void Awake()
    {
        // OPT: Si no hay CanvasGroup, crearlo automáticamente
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        shownPosition = panelRect.anchoredPosition;
        hiddenPosition = shownPosition + new Vector2(panelRect.rect.width, 0);
        panelRect.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
    }

    public void ShowObjective(Objective objective, System.Action onComplete)
    
    {
        objectiveText.text = $"{PREFIX}{objective.description}{SUFFIX}";
        objectiveText.color = colorNormal;

        // Entra, se queda 5s, sale sola — como notificación toast
        panelRect.DOKill();
        canvasGroup.DOKill();

        panelRect.anchoredPosition = hiddenPosition;
        panelRect.localScale = new Vector3(scaleEntrante, scaleEntrante, 1f);
        canvasGroup.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        // Entrada
        seq.Join(panelRect.DOAnchorPos(shownPosition, animationDuration).SetEase(Ease.OutCubic));
        seq.Join(panelRect.DOScale(scaleNormal, animationDuration).SetEase(Ease.OutBack));
        seq.Join(canvasGroup.DOFade(1f, animationDuration).SetEase(Ease.InQuad));
        seq.Append(panelRect.DOScale(1.05f, 0.1f).SetLoops(2, LoopType.Yoyo));

        // Se queda visible 5 segundos
        seq.AppendInterval(notificationDisplayDuration);

        // Sale sola
        seq.Append(panelRect.DOAnchorPos(hiddenPosition, animationDuration).SetEase(Ease.InCubic));
        seq.Join(canvasGroup.DOFade(0f, animationDuration).SetEase(Ease.OutQuad));
        seq.Join(panelRect.DOScale(scaleEntrante, animationDuration).SetEase(Ease.InBack));

        seq.OnComplete(() => onComplete?.Invoke());
    }



    
    public void ShowCompleted(Objective objective, System.Action onComplete)
    {
        // OPT: Mostrar notificación de completado con efecto de celebración
        objectiveText.text = $"{COMPLETED_PREFIX}{objective.description}";
        objectiveText.color = colorCompletado;

        AnimarCompletado(() => {
            DOVirtual.DelayedCall(completedDisplayDuration, () => {
                AnimarSalida(() => {
                    onComplete?.Invoke();
                });
            });
        });
    }

    private void AnimarEntrantePermanente(System.Action onComplete)
    {
        // OPT: Llamar DOKill() antes de nueva animación para evitar conflictos
        panelRect.DOKill();
        canvasGroup.DOKill();

        // Resetear estado inicial
        panelRect.anchoredPosition = hiddenPosition;
        panelRect.localScale = new Vector3(scaleEntrante, scaleEntrante, 1f);
        canvasGroup.alpha = 0f;

        // Secuencia: Entrada simultánea (movimiento + escala + fade)
        Sequence seqEntrada = DOTween.Sequence();

        // Mover de derecha a izquierda
        seqEntrada.Join(panelRect.DOAnchorPos(shownPosition, animationDuration)
            .SetEase(Ease.OutCubic));

        // Escalar de pequeño a normal
        seqEntrada.Join(panelRect.DOScale(scaleNormal, animationDuration)
            .SetEase(Ease.OutBack));

        // Fade-in
        seqEntrada.Join(canvasGroup.DOFade(1f, animationDuration)
            .SetEase(Ease.InQuad));

        // Pequeño bounce al final
        seqEntrada.Append(panelRect.DOScale(1.05f, 0.1f).SetLoops(2, LoopType.Yoyo));

        seqEntrada.OnComplete(() => onComplete?.Invoke());
    }

    private void AnimarCompletado(System.Action onComplete)
    {
        // OPT: Animación de celebración (pulse + scale)
        panelRect.DOKill();
        canvasGroup.DOKill();

        Sequence seqCompletado = DOTween.Sequence();

        // Pulse effect (crecer y contraer)
        seqCompletado.Append(panelRect.DOScale(1.15f, 0.2f).SetEase(Ease.OutCubic));
        seqCompletado.Append(panelRect.DOScale(1f, 0.2f).SetEase(Ease.InCubic));

        // Pequeño shake horizontal
        Vector2 posOriginal = panelRect.anchoredPosition;
        seqCompletado.Join(panelRect.DOShakeAnchorPos(0.3f, new Vector2(5f, 0), 8, 90f, false, false));

        seqCompletado.OnComplete(() => onComplete?.Invoke());
    }

    private void AnimarSalida(System.Action onComplete)
    {
        // OPT: Salida suave
        panelRect.DOKill();
        canvasGroup.DOKill();

        Sequence seqSalida = DOTween.Sequence();

        // Mover hacia la derecha
        seqSalida.Join(panelRect.DOAnchorPos(hiddenPosition, animationDuration)
            .SetEase(Ease.InCubic));

        // Fade-out
        seqSalida.Join(canvasGroup.DOFade(0f, animationDuration)
            .SetEase(Ease.OutQuad));

        // Escalar mientras sale
        seqSalida.Join(panelRect.DOScale(scaleEntrante, animationDuration)
            .SetEase(Ease.InBack));

        seqSalida.OnComplete(() => {
            onComplete?.Invoke();
        });
    }
}
