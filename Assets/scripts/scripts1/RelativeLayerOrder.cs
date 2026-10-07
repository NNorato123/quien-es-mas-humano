// === Detras.cs ===
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RelativeLayerOrder : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string interactableTag = "Interactable";
    [SerializeField] private float yOffsetThreshold = 0.1f;
    [SerializeField] private int basePlayerOrder = 0;
    [SerializeField] private int baseObjectOrder = 0;

    private SpriteRenderer playerRenderer;
    private Collider2D playerCollider;

    // OPT: Caché de componentes para evitar GetComponent() repetidos
    private Dictionary<Collider2D, (SpriteRenderer, Collider2D)> componentCache = 
        new Dictionary<Collider2D, (SpriteRenderer, Collider2D)>();

    // OPT: Magic number para el rango de búsqueda
    private const float DETECTION_RADIUS = 5f;
    private const float UPDATE_INTERVAL = 0.15f; // Cada 150ms en lugar de cada frame

    void Start()
    {
        playerRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        playerRenderer.sortingOrder = basePlayerOrder;

        // OPT: Iniciar coroutine en lugar de llamar cada frame
        StartCoroutine(UpdateNearbyObjectsRoutine());
    }

    // OPT: Coroutine para reducir frecuencia de Physics2D queries
    private IEnumerator UpdateNearbyObjectsRoutine()
    {
        while (true)
        {
            UpdateNearbyObjects();
            yield return new WaitForSeconds(UPDATE_INTERVAL);
        }
    }

    private void UpdateNearbyObjects()
    {
        // OPT: Usar array reutilizable para evitar allocations
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DETECTION_RADIUS);

        // Limpiar caché de objetos que ya no están en rango
        List<Collider2D> collidersToRemove = new List<Collider2D>();
        foreach (var cachedCollider in componentCache.Keys)
        {
            if (cachedCollider == null || Vector2.Distance(transform.position, cachedCollider.transform.position) > DETECTION_RADIUS)
            {
                collidersToRemove.Add(cachedCollider);
            }
        }

        foreach (var collider in collidersToRemove)
        {
            componentCache.Remove(collider);
        }

        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider.CompareTag(interactableTag) && collider.transform != transform)
            {
                ProcessObject(collider);
            }
        }
    }

    private void ProcessObject(Collider2D collider)
    {
        Transform objTransform = collider.transform;

        // OPT: Cachear componentes para evitar GetComponent() repetido
        if (!componentCache.TryGetValue(collider, out var components))
        {
            SpriteRenderer newRenderer = objTransform.GetComponent<SpriteRenderer>();
            Collider2D newCollider = objTransform.GetComponent<Collider2D>();

            if (newRenderer == null || newCollider == null) 
                return;

            components = (newRenderer, newCollider);
            componentCache[collider] = components;
        }

        SpriteRenderer objRenderer = components.Item1;
        Collider2D objCollider = components.Item2;

        // Calcular diferencia relativa entre los centros
        float relativeY = transform.position.y - objTransform.position.y;

        if (relativeY > yOffsetThreshold)
        {
            // Player está ARRIBA del objeto (debe verse DETRÁS)
            objRenderer.sortingOrder = baseObjectOrder + 1;
            playerRenderer.sortingOrder = baseObjectOrder;
        }
        else if (relativeY < -yOffsetThreshold)
        {
            // Player está ABAJO del objeto (debe verse DELANTE)
            objRenderer.sortingOrder = baseObjectOrder - 1;
            playerRenderer.sortingOrder = baseObjectOrder;
        }
        else
        {
            // Caso de igualdad (resetear órdenes)
            objRenderer.sortingOrder = baseObjectOrder;
            playerRenderer.sortingOrder = basePlayerOrder;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, DETECTION_RADIUS);
    }

    // OPT: Limpiar caché al destruir
    private void OnDestroy()
    {
        componentCache.Clear();
    }
}