using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public bool EstaEstable => !enTransicion;
    public Transform target;
    public float smoothTime = 0.4f;
    public Vector3 offset = new Vector3(0, 0, -5);
    public float cameraSize = 0.5f;

    [Tooltip("Qué tan cerca del objetivo debe estar para considerar que 'llegó' y volver al modo rígido")]
    public float umbralLlegada = 0.05f;

    private Vector3 velocity = Vector3.zero;
    private bool enTransicion = false; // true = deslizando, false = pegada rígida

    void Start()
    {
        if (GetComponent<Camera>() != null && GetComponent<Camera>().orthographic)
        {
            GetComponent<Camera>().orthographicSize = cameraSize;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (enTransicion)
        {
            // Modo deslizante: se acerca suave con SmoothDamp
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                smoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            // Cuando llega lo bastante cerca, pasa a modo rígido
            if (Vector3.Distance(transform.position, desiredPosition) < umbralLlegada)
            {
                enTransicion = false;
                velocity = Vector3.zero;
            }
        }
        else
        {
            // Modo rígido: pegada exacta al objetivo, sin retraso
            transform.position = desiredPosition;
        }
    }

    /// <summary>
    /// Cambia el objetivo de la cámara. Cada cambio dispara automáticamente
    /// un deslizamiento suave hasta llegar, y luego vuelve a modo rígido.
    /// </summary>
    public void SetTarget(Transform nuevoTarget)
    {
        target = nuevoTarget;
        enTransicion = true;
    }
}