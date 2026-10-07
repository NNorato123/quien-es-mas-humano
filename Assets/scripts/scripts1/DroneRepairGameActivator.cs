using UnityEngine;
using UnityEngine.UI;

public class MinigameActivator : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject player;
    public Collider2D triggerZone;
    public GameObject miniGameCanvas;

    [Header("UI Feedback (Opcional)")]
    public Text interactionText; // Para mostrar "Presiona E para interactuar"

    private bool playerInZone = false;
    private bool minigameActive = false;

    void Start()
    {
        // Asegurar que el Canvas est� desactivado al inicio
        if (miniGameCanvas != null)
        {
            miniGameCanvas.SetActive(false);
        }

        // Ocultar texto de interacci�n si existe
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

        void Update()
    {
        if (!playerInZone || minigameActive) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (LevelDirector.Instance == null)
            {
                ActivateMinigame();
                return;
            }

            if (!LevelDirector.Instance.DecisionTomada) return;

            if (!LevelDirector.Instance.ObtenerReparoMaquina())
            {
                Debug.Log("[MinigameActivator] Dron bloqueado — decisión: IGNORAR");
                return;
            }

            
            ActivateMinigame();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == player)
        {
            playerInZone = true;

            if (interactionText != null)
            {
                bool mostrar = LevelDirector.Instance == null ||
                            !LevelDirector.Instance.DecisionTomada ||
                            LevelDirector.Instance.ObtenerReparoMaquina();
                interactionText.gameObject.SetActive(mostrar);
            }

            Debug.Log("[MinigameActivator] Jugador en zona del dron.");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Verificar si el objeto que sali� es el jugador
        if (other.gameObject == player)
        {
            playerInZone = false;

            // Ocultar texto de interacci�n si existe
            if (interactionText != null)
            {
                interactionText.gameObject.SetActive(false);
            }

            // Si el minijuego est� activo y el jugador sale de la zona, desactivarlo
            if (minigameActive)
            {
                DeactivateMinigame();
            }
        }
    }

    void ActivateMinigame()
    {
        if (miniGameCanvas != null)
        {
            miniGameCanvas.SetActive(true);
            minigameActive = true;

            // Ocultar texto de interacci�n
            if (interactionText != null)
            {
                interactionText.gameObject.SetActive(false);
            }

            // FORZAR que el cursor sea visible y desbloqueado
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("Minijuego activado");
        }
    }

    public void DeactivateMinigame()
    {
        if (miniGameCanvas != null)
        {
            miniGameCanvas.SetActive(false);
            minigameActive = false;

            // Puedes volver a ocultar el cursor si tu juego lo requiere
            // Cursor.visible = false;
            // Cursor.lockState = CursorLockMode.Locked;

            Debug.Log("Minijuego desactivado");
        }
    }

    // M�todo p�blico para que el minijuego notifique cuando se complete
    public void OnMinigameCompleted()
    {
        Debug.Log("¡Lo hiciste perfecto!");
        
        
        ObjectiveManager.Instance?.CheckObjectiveCompletion("reparar_dron");
        
        DeactivateMinigame();
    }
}