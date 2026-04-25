using UnityEngine;

public enum ItemType { Generic, Case, Motherboard, GPU, CPU, RAM, PowerSupply, Cooler }

public class PickupItem : Interactable
{
    public ItemType itemType;

    [Header("Связь с JSON базой")][Tooltip("Напиши сюда partID из JSON (например: cpu_i5)")]
    public string jsonPartID; 

    // Скрываем это из инспектора, чтобы не заполнять руками!
    [HideInInspector] public PartData data; 
    
    public Slot currentSlot; 
    public PCCase currentCase; 
    
    private Rigidbody rb;
    private Collider coll;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
    }

    // НОВЫЙ МЕТОД START
    private void Start()
    {
        // При старте игры деталь обращается к Менеджеру и просит выдать ей её характеристики
        data = AssemblyManager.Instance.GetPartInfo(jsonPartID);

        // Защита от опечаток
        if (data == null)
        {
            Debug.LogError($"ВНИМАНИЕ! Деталь с ID '{jsonPartID}' не найдена в JSON базе! Проверьте опечатки.");
            // Создаем заглушку, чтобы игра не сломалась с ошибкой NullReference
            data = new PartData { partName = "НЕИЗВЕСТНАЯ ДЕТАЛЬ", socketType = "Error", tdp = 0 };
        }
    }
    

    public override string GetPromptMessage(PlayerInteract player)
    {
        // Если деталь установлена куда-либо
        if (currentSlot != null || currentCase != null)
        {
            return $"<color=yellow>Q - Достать\n{data.partName}</color>";
        }

        // Если валяется на столе и руки пустые
        if (player.heldItem == null)
            return "E - Взять\n" + data.partName; 
            
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