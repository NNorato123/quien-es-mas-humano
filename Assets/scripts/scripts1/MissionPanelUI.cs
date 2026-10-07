// MissionPanelUI.cs (mejorado con DOTween)
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class MissionPanelUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform contentParent;  // ScrollView Content
    [SerializeField] private GameObject missionRowPrefab; // Prefab con TMP_Text

    [Header("Colores")]
    [SerializeField] private Color colorPendiente = Color.gray;
    [SerializeField] private Color colorEnCurso = Color.yellow;
    [SerializeField] private Color colorCompletado = Color.green;

    [Header("Animaciones")]
    [SerializeField] private float delayEntreFilas = 0.1f;
    [SerializeField] private float duracionEntrada = 0.4f;

    private const string ICON_PENDIENTE = "○";
    private const string ICON_EN_CURSO = "▶";
    private const string ICON_COMPLETADO = "✔";

    public void Refresh()
    {
        // OPT: Limpiar todas las filas existentes (sin LINQ)
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }

        // OPT: Iterar sin LINQ
        IReadOnlyList<Objective> objectives = ObjectiveManager.Instance.AllObjectives;
        int objectiveCount = objectives.Count;

        // OPT: Crear todas las filas con animación escalonada
        for (int i = 0; i < objectiveCount; i++)
        {
            Objective obj = objectives[i];
            float delay = i * delayEntreFilas;
            DOVirtual.DelayedCall(delay, () => {
                InstanciarFila(obj);
            });
        }
    }

    private void InstanciarFila(Objective objective)
    {
        // Instanciar prefab
        GameObject instancia = Instantiate(missionRowPrefab, contentParent);
        RectTransform rectTransform = instancia.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = instancia.GetComponent<CanvasGroup>();

        // OPT: Crear CanvasGroup si no existe para animaciones suaves
        if (canvasGroup == null)
        {
            canvasGroup = instancia.AddComponent<CanvasGroup>();
        }

        TMP_Text textComponent = instancia.GetComponentInChildren<TMP_Text>();

        if (textComponent == null)
        {
            Debug.LogError("MissionRowPrefab no contiene un componente TMP_Text");
            return;
        }

        // Determinar icono y color según estado
        string icono = "";
        Color color = colorPendiente;

        switch (objective.Estado)
        {
            case Objective.EstadoType.Pendiente:
                icono = ICON_PENDIENTE;
                color = colorPendiente;
                break;
            case Objective.EstadoType.EnCurso:
                icono = ICON_EN_CURSO;
                color = colorEnCurso;
                break;
            case Objective.EstadoType.Completado:
                icono = ICON_COMPLETADO;
                color = colorCompletado;
                break;
        }

        // Construir texto con progreso si aplica
        string progressText = objective.GetProgressText();
        string textoFinal = $"{icono}  {objective.description}";

        if (!string.IsNullOrEmpty(progressText))
        {
            textoFinal += $"  [{progressText}]";
        }

        textComponent.text = textoFinal;
        textComponent.color = color;

        // Animar entrada de fila
        AnimarFilaEntrante(instancia, rectTransform, canvasGroup);
    }

    private void AnimarFilaEntrante(GameObject fila, RectTransform rectTransform, CanvasGroup canvasGroup)
    {
        // OPT: Estado inicial (invisible y desplazada)
        canvasGroup.alpha = 0f;
        Vector2 posOriginal = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = posOriginal + new Vector2(-20f, 0f);
        rectTransform.localScale = new Vector3(0.9f, 0.9f, 1f);

        // Animar entrada
        Sequence seqFila = DOTween.Sequence();

        // Fade-in
        seqFila.Append(canvasGroup.DOFade(1f, duracionEntrada)
            .SetEase(Ease.InQuad));

        // Slide-in desde izquierda
        seqFila.Join(rectTransform.DOAnchorPos(posOriginal, duracionEntrada)
            .SetEase(Ease.OutCubic));

        // Escala de pequeño a normal
        seqFila.Join(rectTransform.DOScale(1f, duracionEntrada)
            .SetEase(Ease.OutBack));

        // Pequeño bounce al final
        seqFila.Append(rectTransform.DOScale(1.02f, 0.1f).SetLoops(2, LoopType.Yoyo));
    }
}
