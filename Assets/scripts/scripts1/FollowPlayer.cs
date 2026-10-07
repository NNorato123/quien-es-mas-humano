using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private GameObject player;
    
    // OPTIMIZACIÓN: Cachear la referencia de transform para evitar property getter repetido
    private Transform playerTransform;

    private void Awake()
    {
        // Cachear la referencia del transform del jugador una única vez
        playerTransform = player.transform;
    }

    private void LateUpdate()
    {
        // OPTIMIZACIÓN: Usar playerTransform cacheado en lugar de player.transform
        transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, transform.position.z);
    }
}