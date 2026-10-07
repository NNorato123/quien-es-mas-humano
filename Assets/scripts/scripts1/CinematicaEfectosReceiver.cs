using System.Collections;
using UnityEngine;

public class CinematicaEfectosReceiver : MonoBehaviour
{
    [SerializeField] private Color colorPortal = new Color(0.2f, 0.55f, 1f);
    [SerializeField] private float duracionFundidoAzul = 1.5f;

    public void EfectoFundidoAzulPortal()
    {
        StartCoroutine(ScreenFader.Instance.FadeIn(colorPortal, duracionFundidoAzul));
    }
}