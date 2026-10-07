// Objective.cs (refactorizado con sistema de estados)
using UnityEngine;

[System.Serializable]
public class Objective
{
    // Enum para estados del objetivo
    public enum EstadoType
    {
        Pendiente,
        EnCurso,
        Completado
    }

    public string description;
    public string requiredActionType;
    public string requiredActionDetail;
    public int requiredCount;
    private int currentCount;
    private EstadoType estado = EstadoType.Pendiente;

    public Objective(string desc, string actionType = "", string actionDetail = "", int count = 1)
    {
        description = desc;
        requiredActionType = actionType;
        requiredActionDetail = actionDetail;
        requiredCount = count;
        currentCount = 0;
        estado = EstadoType.Pendiente;
    }

    // Propiedad shorthand para compatibilidad
    public bool isCompleted
    {
        get { return estado == EstadoType.Completado; }
    }

    // Propiedad pública para acceder al estado
    public EstadoType Estado
    {
        get { return estado; }
    }

    public void SetInProgress()
    {
        // OPT: Solo cambiar a EnCurso si está en Pendiente
        if (estado == EstadoType.Pendiente)
        {
            estado = EstadoType.EnCurso;
        }
    }

    public string GetProgressText()
    {
        // OPT: Retornar progreso solo si requiredCount > 1
        if (requiredCount > 1)
        {
            return $"{currentCount}/{requiredCount}";
        }
        return "";
    }

    public bool CheckCompletion(string actionType, string actionDetail = "")
    {
        // OPT: No procesar si el estado es Pendiente (aún no asignado)
        if (estado == EstadoType.Pendiente)
        {
            return false;
        }

        if (!isCompleted && actionType == requiredActionType)
        {
            if (string.IsNullOrEmpty(requiredActionDetail) || actionDetail == requiredActionDetail)
            {
                currentCount++;
                if (currentCount >= requiredCount)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void Complete()
    {
        estado = EstadoType.Completado;
    }
}
