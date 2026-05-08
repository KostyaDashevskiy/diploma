using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string promptMessage;
    
    // Этот метод позволяет менять текст (например: если руки пусты - "Взять", если полные - "Вставить")
    public virtual string GetPromptMessage(PlayerInteract player)
    {
        return promptMessage;
    }

    public void BaseInteract(PlayerInteract player)
    {
        Interact(player);
    }

    protected virtual void Interact(PlayerInteract player)
    {
        // Базовая логика переопределяется в наследниках
    }

     // --- НОВЫЕ МЕТОДЫ ДЛЯ ПОДСВЕТКИ ---
    public virtual void ApplyHighlight(PlayerInteract player)
    {
        // Переопределяется в наследниках (в слотах)
    }

    public virtual void RemoveHighlight()
    {
        // Переопределяется в наследниках
    }
    
}