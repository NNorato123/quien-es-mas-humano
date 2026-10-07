// ObjectiveManager.cs (refactorizado con historial completo)
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [SerializeField] private ObjectiveUI objectiveUI;
    // OPT: Cambiar de Queue a List para mantener historial completo
    private List<Objective> allObjectives = new List<Objective>();
    private int currentIndex = -1;
    private bool isDisplayingObjective = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddObjective(Objective newObjective)
    {
        allObjectives.Add(newObjective);
        TryAdvanceToNext();
    }

    // Propiedad pública para que la UI del menú pueda leer todos los objetivos
    public IReadOnlyList<Objective> AllObjectives
    {
        get { return allObjectives.AsReadOnly(); }
    }

    // Propiedad pública para acceder al objetivo activo
    public Objective CurrentObjective
    {
        get
        {
            if (currentIndex >= 0 && currentIndex < allObjectives.Count)
            {
                return allObjectives[currentIndex];
            }
            return null;
        }
    }

    private void TryAdvanceToNext()
    {
        // OPT: Evitar mostrar si ya está mostrando algo
        if (isDisplayingObjective) return;

        // Buscar el primer objetivo con estado Pendiente
        int nextIndex = -1;
        for (int i = 0; i < allObjectives.Count; i++)
        {
            if (allObjectives[i].Estado == Objective.EstadoType.Pendiente)
            {
                nextIndex = i;
                break;
            }
        }

        // Si se encontró un objetivo pendiente, prepararlo y mostrarlo
        if (nextIndex >= 0)
        {
            currentIndex = nextIndex;
            Objective objective = allObjectives[currentIndex];
            objective.SetInProgress();
            isDisplayingObjective = true;

            objectiveUI.ShowObjective(objective, () => {
                // OPT: El objetivo se mantiene indefinidamente visible hasta completarse
            });
        }
    }

    public void CheckObjectiveCompletion(string actionType, string actionDetail = "")
    {
        Objective current = CurrentObjective;
        if (current != null && !current.isCompleted)
        {
            if (current.CheckCompletion(actionType, actionDetail))
            {
                current.Complete();
                // OPT: Permitir que se busque el siguiente objetivo
                isDisplayingObjective = false;

                // Mostrar animación de completado
                objectiveUI.ShowCompleted(current, () => {
                    TryAdvanceToNext();
                });
            }
        }
    }
}
