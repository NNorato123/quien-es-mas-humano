// PortalNivel.cs
// Trigger del portal al final del Nivel 2 (y reutilizable para Nivel 3)
// El jugador entra al trigger → confirma con E → LevelDirector2 carga el siguiente nivel
//
// SETUP EN UNITY:
// 1. Crea un GameObject "Portal" con:
//    - Collider2D (trigger)
//    - SpriteRenderer o ParticleSystem para el efecto visual del portal
//    - Este script
// 2. LevelDirector2.IniciarCierreNivel() activa este GameObject cuando las plantas están listas

using UnityEngine;

public class PortalNivel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject iconoInteraccion; // "E para avanzar"
    [SerializeField] private ParticleSystem efectoPortal;

    private bool jugadorEnRango = false;

    private void Start()
    {
        if (iconoInteraccion != null) iconoInteraccion.SetActive(false);
        if (efectoPortal != null) efectoPortal.Play();
    }

    private void Update()
    {
        if (!jugadorEnRango) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[PortalNivel] Jugador entra al portal.");
            LevelDirector2.Instance?.CargarNivel3();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        jugadorEnRango = true;
        if (iconoInteraccion != null) iconoInteraccion.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        jugadorEnRango = false;
        if (iconoInteraccion != null) iconoInteraccion.SetActive(false);
    }
}