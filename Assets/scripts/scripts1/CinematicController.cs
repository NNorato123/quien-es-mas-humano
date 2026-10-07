// === Cinematica.cs ===
using UnityEngine;

public class CinematicController : MonoBehaviour
{
    [System.Serializable]
    public class CinematicAction
    {
        public enum ActionType
        {
            ChangeDirection,
            MoveToPosition,
            Wait,
            PlayAnimation,
            EnableMovement,
            DisableMovement
        }

        public ActionType actionType;
        public Vector2 direction;
        public Vector2 targetPosition;
        public float duration;
        public string animationName;
        public bool loopAnimation;
    }

    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private playercontroller playerController;

    [Header("Configuración de Cinemática")]
    [SerializeField] private CinematicAction[] cinematicSequence;
    [SerializeField] private bool playOnStart = false;

    private bool isPlayingCinematic = false;
    private int currentActionIndex = 0;
    private float actionTimer = 0f;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerController == null) playerController = GetComponent<playercontroller>();

        if (playOnStart)
        {
            StartCinematic();
        }
    }

    void Update()
    {
        if (isPlayingCinematic)
        {
            ProcessCurrentAction();
        }
    }

    public void StartCinematic()
    {
        if (cinematicSequence.Length == 0) return;

        isPlayingCinematic = true;
        currentActionIndex = 0;
        actionTimer = 0f;
        ExecuteAction(cinematicSequence[currentActionIndex]);
    }

    public void StopCinematic()
    {
        isPlayingCinematic = false;
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }
    }

    private void ProcessCurrentAction()
    {
        // OPT: Cachear acceso al array para evitar búsqueda repetida
        var currentAction = cinematicSequence[currentActionIndex];

        switch (currentAction.actionType)
        {
            case CinematicAction.ActionType.MoveToPosition:
                MoveTowardsPosition(currentAction.targetPosition);
                if (Vector2.Distance(rb.position, currentAction.targetPosition) < 0.1f)
                {
                    NextAction();
                }
                break;

            case CinematicAction.ActionType.Wait:
                actionTimer += Time.deltaTime;
                if (actionTimer >= currentAction.duration)
                {
                    NextAction();
                }
                break;

            default:
                actionTimer += Time.deltaTime;
                if (actionTimer >= currentAction.duration)
                {
                    NextAction();
                }
                break;
        }
    }

    private void ExecuteAction(CinematicAction action)
    {
        actionTimer = 0f;

        switch (action.actionType)
        {
            case CinematicAction.ActionType.ChangeDirection:
                SetCharacterDirection(action.direction);
                break;

            case CinematicAction.ActionType.MoveToPosition:
                // El movimiento se maneja en ProcessCurrentAction
                break;

            case CinematicAction.ActionType.PlayAnimation:
                PlayAnimation(action.animationName, action.loopAnimation);
                break;

            case CinematicAction.ActionType.EnableMovement:
                if (playerController != null) playerController.SetMovementEnabled(true);
                break;

            case CinematicAction.ActionType.DisableMovement:
                if (playerController != null) playerController.SetMovementEnabled(false);
                break;
        }
    }

    private void NextAction()
    {
        currentActionIndex++;
        if (currentActionIndex < cinematicSequence.Length)
        {
            // OPT: Cachear acceso al array en lugar de acceder directamente en ExecuteAction
            var nextAction = cinematicSequence[currentActionIndex];
            ExecuteAction(nextAction);
        }
        else
        {
            StopCinematic();
        }
    }

    private void MoveTowardsPosition(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - rb.position).normalized;
        rb.MovePosition(rb.position + direction * Time.deltaTime);
    }

    private void SetCharacterDirection(Vector2 direction)
    {
        if (animator != null)
        {
            animator.SetFloat("Horizontal", direction.x);
            animator.SetFloat("Vertical", direction.y);
            animator.SetFloat("UltimoHorizontal", direction.x);
            animator.SetFloat("UltimoVertical", direction.y);
        }
    }

    private void PlayAnimation(string animationName, bool loop)
    {
        if (animator != null)
        {
            animator.Play(animationName, -1, 0f);
            animator.speed = loop ? 1f : 0f; // Para animaciones que deben quedarse en el último frame
        }
    }
}