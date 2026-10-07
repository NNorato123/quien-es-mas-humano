using UnityEngine;

public class LightFollowPlayer : MonoBehaviour
{
    private const string TAG_PLAYER = "Player";

    public Transform player;
    public Vector2 offset = new Vector2(0, 2);

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(TAG_PLAYER);
            if (playerObject != null) player = playerObject.transform;
            else Debug.LogError("No se encontró un objeto con la etiqueta 'Player'.");
        }
    }

    void LateUpdate()
    {
        if (player != null)
        {
            transform.position = new Vector3(player.position.x + offset.x, player.position.y + offset.y, transform.position.z);
        }
    }

    // ✅ NUEVO: permite cambiar el objetivo desde código (para cinemáticas)
    public void SetTarget(Transform nuevoTarget)
    {
        player = nuevoTarget;
    }
}