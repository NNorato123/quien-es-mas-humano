using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

public class CableDragMinigame : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Configuración del Minijuego")]
    public RectTransform[] cables;
    public RectTransform[] terminales;
    public float snapDistance = 50f;
    public float animationDuration = 0.2f;

    [Header("Referencias")]
    public MinigameActivator activator;

    private Vector2[] originalPositions;
    private bool[] cablesConnected;
    private int currentDragIndex = -1;
    private bool isDragging = false;
    private int totalConnectedCables = 0;

    void Start()
    {
        InitializeMinigame();
    }

    void InitializeMinigame()
    {
        // Guardar posiciones originales de los cables
        originalPositions = new Vector2[cables.Length];
        cablesConnected = new bool[cables.Length];

        for (int i = 0; i < cables.Length; i++)
        {
            originalPositions[i] = cables[i].anchoredPosition;
            cablesConnected[i] = false;
        }

        totalConnectedCables = 0;
    }

    void OnEnable()
    {
        // Reinicializar el minijuego cada vez que se active
        if (cables != null && cables.Length > 0)
        {
            InitializeMinigame();
            ResetAllCables();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Encontrar qué cable se está arrastrando
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;

        for (int i = 0; i < cables.Length; i++)
        {
            if (cables[i].gameObject == clickedObject)
            {
                currentDragIndex = i;
                isDragging = true;

                // Si el cable ya estaba conectado, desconectarlo
                if (cablesConnected[i])
                {
                    cablesConnected[i] = false;
                    totalConnectedCables--;
                }

                // Traer el cable al frente
                cables[i].SetAsLastSibling();
                break;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && currentDragIndex >= 0)
        {
            // Convertir posición del mouse a posición local del Canvas
            Vector2 localPointerPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition);

            cables[currentDragIndex].anchoredPosition = localPointerPosition;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDragging && currentDragIndex >= 0)
        {
            CheckForSnap(currentDragIndex);
            isDragging = false;
            currentDragIndex = -1;
        }
    }

    void CheckForSnap(int cableIndex)
    {
        // Verificar si el cable está cerca de su terminal correspondiente
        if (cableIndex < terminales.Length)
        {
            float distance = Vector2.Distance(
                cables[cableIndex].anchoredPosition,
                terminales[cableIndex].anchoredPosition);

            if (distance <= snapDistance)
            {
                // Snap al terminal
                SnapToTerminal(cableIndex);
            }
            else
            {
                // Regresar a posición original
                ReturnToOriginalPosition(cableIndex);
            }
        }
        else
        {
            // Si no hay terminal correspondiente, regresar a posición original
            ReturnToOriginalPosition(cableIndex);
        }
    }

    void SnapToTerminal(int cableIndex)
    {
        // Animar el cable hacia el terminal
        cables[cableIndex].DOAnchorPos(terminales[cableIndex].anchoredPosition, animationDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                // Marcar cable como conectado
                if (!cablesConnected[cableIndex])
                {
                    cablesConnected[cableIndex] = true;
                    totalConnectedCables++;

                    Debug.Log($"Cable {cableIndex + 1} conectado correctamente!");

                    // Verificar si todos los cables están conectados
                    CheckForCompletion();
                }
            });
    }

    void ReturnToOriginalPosition(int cableIndex)
    {
        // Animar el cable de vuelta a su posición original
        cables[cableIndex].DOAnchorPos(originalPositions[cableIndex], animationDuration)
            .SetEase(Ease.OutBounce);
    }

    void CheckForCompletion()
    {
        if (totalConnectedCables >= cables.Length)
        {
            // Todos los cables están conectados
            StartCoroutine(CompleteMinigame());
        }
    }

    IEnumerator CompleteMinigame()
    {
        // Esperar un momento antes de completar
        yield return new WaitForSeconds(0.5f);

        // Notificar al activador que el minijuego se completó
        if (activator != null)
        {
            activator.OnMinigameCompleted();
        }
        else
        {
            Debug.Log("¡Lo hiciste perfecto!");
        }
    }

    void ResetAllCables()
    {
        // Resetear todos los cables a sus posiciones originales
        for (int i = 0; i < cables.Length; i++)
        {
            cables[i].anchoredPosition = originalPositions[i];
            cablesConnected[i] = false;
        }
        totalConnectedCables = 0;
    }

    // Método público para resetear el minijuego desde el exterior
    public void ResetMinigame()
    {
        ResetAllCables();
    }
}