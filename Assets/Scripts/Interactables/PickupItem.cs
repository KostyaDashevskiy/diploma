using UnityEngine;

public enum ItemType { Generic, Case, Motherboard, GPU, CPU, RAM, PowerSupply, Cooler }


public class PickupItem : Interactable
{
    [HideInInspector] public float spawnTime;

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

        // Запоминаем время, когда этот предмет был создан (заспавнен)
        spawnTime = Time.time;
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
    

// Короткий текст для ПРИЦЕЛА (только Тип и Название)
    public override string GetPromptMessage(PlayerInteract player)
    {
        string typeStr = itemType.ToString(); // Превращаем тип (GPU, CPU) в текст

        if (currentSlot != null || currentCase != null)
            return $"<color=yellow>Q - Достать [{typeStr}] {data.partName}</color>";
        
        if (player.heldItem == null)
            return $"E - Взять [{typeStr}] {data.partName}"; 
            
        return ""; 
    }
    // public override string GetPromptMessage(PlayerInteract player)
    // {
    //     // Если деталь установлена куда-либо
    //     if (currentSlot != null || currentCase != null)
    //     {
    //         return $"<color=yellow>Q - Достать\n{data.partName}</color>";
    //     }

    //     // Если валяется на столе и руки пустые
    //     if (player.heldItem == null)
    //         return "E - Взять\n" + data.partName; 
            
    //     return ""; 
    // }
    // НОВЫЙ МЕТОД: Подробный текст для ЛЕВОГО НИЖНЕГО УГЛА
    public string GetStatsMessage()
    {
        if (data == null) return "";

        return $"<b>Тип:</b> {itemType}\n" +
               $"<b>Модель:</b> {data.partName}\n" +
               $"<b>Сокет:</b> {data.socketType}\n" +
               $"<b>TDP:</b> {data.tdp} W\n" +
               $"<b>Габариты:</b> {data.length} x {data.width} x {data.height} мм";
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

    public void ReloadDataFromJSON()
    {
        if (AssemblyManager.Instance == null) return;
        
        data = AssemblyManager.Instance.GetPartInfo(jsonPartID);
        
        if (data == null) 
        {
            Debug.LogError($"[ГЕНЕРАТОР] Ошибка! ID '{jsonPartID}' не найден в JSON!");
            data = new PartData { partName = "Ошибка ID: " + jsonPartID, socketType = "Error", tdp = 0 };
            return;
        }

        // --- НОВЫЙ БЛОК: ОБНОВЛЕНИЕ ДОЧЕРНИХ СЛОТОВ ---
        // Если обновилась материнская плата, мы должны обновить сокет у слота Процессора и Кулера
        if (itemType == ItemType.Motherboard)
        {
            // Находим все слоты внутри материнки
            Slot[] slots = GetComponentsInChildren<Slot>();
            
            foreach (Slot slot in slots)
            {
                // Нам нужно обновить только те слоты, которые связаны с процессором!
                // Слоты ОЗУ и Видеокарты (PCIe) трогать не нужно.
                
                // Проверяем по имени слота (или можно проверять текущий acceptableSocket)
                if (slot.slotName == "Разъем процессора (Socket)" || slot.slotName == "Крепление охлаждения (CPU Fan)")
                {
                    slot.UpdateSocketFromParent(this);
                }
                
                // --- А ТАКЖЕ АВТОМАТИЧЕСКОЕ ОБНОВЛЕНИЕ ОЗУ ---
                // Если плата сменилась с DDR4 на DDR5, слоты ОЗУ тоже должны поменяться!
                if (slot.slotName.Contains("ОЗУ"))
                {
                    slot.UpdateSocketFromParent(this);
                }
            }
        }
        // ----------------------------------------------
    }
}