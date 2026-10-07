using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menuinicial : MonoBehaviour
{
    public void Jugar()
    {
        // Borrar progreso guardado de partidas anteriores antes de empezar
        // Esto garantiza que el Nivel 1 siempre empiece con movimientos bloqueados
        playercontroller.ResetearProgreso();
        Debug.Log("[Menuinicial] Progreso reseteado — iniciando nueva partida.");

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Salir()
    {
        Debug.Log("Salir...");
        Application.Quit();
    }
}