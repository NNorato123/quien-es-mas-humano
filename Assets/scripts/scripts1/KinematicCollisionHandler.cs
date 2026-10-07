// === Colisiones.cs ===
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class KinematicCollisionHandler : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Vector2 previousPosition;
    private float skinWidth = 0.02f; // Pequeño margen para evitar quedarse pegado

    // OPT: Cache de array para evitar allocations repetidas
    private RaycastHit2D[] hits = new RaycastHit2D[10];
    
    // OPT: Contador para reducir frecuencia de verificación
    private int collisionCheckCounter = 0;
    private const int COLLISION_CHECK_INTERVAL = 2; // Verificar cada 2 FixedUpdates

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        rb.isKinematic = true; // Asegurarse de que es cinemático
        previousPosition = rb.position;
    }

    void FixedUpdate()
    {
        // Guardar la posición antes de mover
        Vector2 currentPosition = rb.position;

        // OPT: Verificar colisiones solo cada N frames
        collisionCheckCounter++;
        if (collisionCheckCounter >= COLLISION_CHECK_INTERVAL)
        {
            CheckCollisions(currentPosition);
            collisionCheckCounter = 0;
        }

        // Actualizar la posición anterior
        previousPosition = rb.position;
    }

    private void CheckCollisions(Vector2 targetPosition)
    {
        // Calcular la dirección y distancia del movimiento
        Vector2 moveDirection = targetPosition - previousPosition;
        float moveDistance = moveDirection.magnitude;

        // OPT: Early return si no hay movimiento
        if (moveDistance <= 0)
            return;

        Vector2 moveNormal = moveDirection.normalized;

        // OPT: Reutilizar array cacheado en lugar de crear uno nuevo cada frame
        int numHits = playerCollider.Cast(moveNormal, hits, moveDistance + skinWidth);

        for (int i = 0; i < numHits; i++)
        {
            RaycastHit2D hit = hits[i];

            // Ignorar triggers
            if (hit.collider.isTrigger)
                continue;

            // Calcular la nueva posición que evita la colisión
            float pushDistance = hit.distance - skinWidth;
            Vector2 newPosition = previousPosition + moveNormal * pushDistance;

            // Aplicar la nueva posición
            rb.position = newPosition;

            // Salir después de la primera colisión importante
            break;
        }
    }
}