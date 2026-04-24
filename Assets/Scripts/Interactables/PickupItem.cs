using UnityEngine;

// ДОБАВЛЕН COOLER В СПИСОК
public enum ItemType { Generic, Case, Motherboard, GPU, CPU, RAM, PowerSupply, Cooler }

public class PickupItem : Interactable
{
    public ItemType itemType;

    [Header("Технические характеристики")]
    public PartData data; 
    
    public Slot currentSlot; 
    public PCCase currentCase; // Ссылка на корпус, если стоит в нем
    
    private Rigidbody rb;
    private Collider coll;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }

    public override string GetPromptMessage(PlayerInteract player)
    {
        // Если деталь установлена куда-либо
        if (currentSlot != null || currentCase != null)
        {
            return $"<color=yellow>Q - Достать {data.partName}</color>";
        }

        // Если валяется на столе и руки пустые
        if (player.heldItem == null)
            return "E - Взять " + data.partName; 
            
        return ""; 
    }

    protected override void Interact(PlayerInteract player)
    {
        // Берем только если она НЕ установлена (установленные мы достаем на Q)
        if (player.heldItem == null && currentSlot == null && currentCase == null)
        {
            player.PickUp(this);
        }
    }

    // НОВЫЙ МЕТОД ИЗВЛЕЧЕНИЯ
    public void Extract(PlayerInteract player)
    {
        if (player.heldItem != null) return; // Нужны пустые руки, чтобы достать

        // ПРОВЕРКА БЛОКИРОВКИ КУЛЕРОМ
        if (currentSlot != null && currentSlot.blockingSlot != null && currentSlot.blockingSlot.isOccupied)
        {
            player.ShowTempMessage("Нельзя достать! Сначала снимите мешающую деталь.");
            return;
        }

        if (currentSlot != null)
        {
            currentSlot.ClearSlot(); 
            AssemblyManager.Instance.RemovePartFromAssembly(data, itemType); 
            currentSlot = null; 
        }
        else if (currentCase != null)
        {
            currentCase.RemovePart(this);
            AssemblyManager.Instance.RemovePartFromAssembly(data, itemType);
            currentCase = null;
        }
        else
        {
            return; // Если предмет просто на столе, ничего не делаем
        }

        // Возвращаем физику
        if (GetComponent<Rigidbody>() == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        player.PickUp(this); // Помещаем в руки
    }

    public void SetPhysics(bool state)
    {
        if (rb != null) rb.isKinematic = !state;
    }
}