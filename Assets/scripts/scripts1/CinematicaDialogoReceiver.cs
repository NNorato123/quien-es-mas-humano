using UnityEngine;

/// <summary>
/// Recibe señales del Timeline y dispara líneas de diálogo del Creador
/// durante la cinemática. Cada método corresponde a UNA línea específica.
/// </summary>
public class CinematicaDialogoReceiver : MonoBehaviour
{
    [SerializeField] private Dialogo dialogoCreador;

    private void DecirLinea(params string[] textos)
    {
        if (dialogoCreador == null)
        {
            Debug.LogWarning("[CinematicaDialogoReceiver] dialogoCreador no asignado.");
            return;
        }

        dialogoCreador.ResetearDialogo();
        dialogoCreador.IniciarDialogoPrograma(textos);
    }

    // Un método público por cada línea del guion — el Signal los llama por nombre
    public void Linea_Jummmm()
    {
        DecirLinea("Jummmm.....");
    }

    public void Linea_CreoQueEstaVez()
    {
        DecirLinea("Creo.... que esta vez sí funcionará.");
    }

    public void Linea_MiMayorCreacion()
    {
        DecirLinea("Creo.... creo que es mi mayor creación...");
    }

    public void Linea_TengoQueProbarte()
    {
        DecirLinea("Jummmm, tengo que probarte primero.....");
    }

    public void Linea_DespedidaViaje()
{
    DecirLinea(
        "Irás a Karlsruhe, Alemania, en 1909.",
        "Ahí Fritz Haber descubrirá cómo sintetizar fertilizantes a partir del nitrógeno del aire.",
        "Ten muchas precauciones..... pero sobre todo, aprende.",
        "Que tengas buen viaje."
    );
}
}