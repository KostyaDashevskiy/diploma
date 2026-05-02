using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Точки Спавна")]
    public Transform pcSpawnPoint; 
    public Transform ejectPoint;   

    private PCCase activePC;
    public bool hasActiveQuest = false;
    private int currentErrorType = -1;

    public int totalCompletedQuests = 0;
    public float totalStars = 0;
    public List<string> historyLogs = new List<string>();

    // --- НОВЫЙ СПИСОК: ЗАПОМИНАЕМ ДЕТАЛИ КЛИЕНТА ДЛЯ УБОРКИ ---
    private List<PickupItem> clientOriginalParts = new List<PickupItem>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AcceptRandomQuest()
    {
        if (hasActiveQuest) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/BasePC");
        if (prefab == null) return;

        GameObject spawned = Instantiate(prefab, pcSpawnPoint.position, pcSpawnPoint.rotation);
        activePC = spawned.GetComponent<PCCase>();
        hasActiveQuest = true;

        currentErrorType = Random.Range(0, 3);
        BreakPC(currentErrorType);

        // --- ЗАПОМИНАЕМ ВСЕ ДЕТАЛИ, КОТОРЫЕ ПРИНЕС КЛИЕНТ ---
        clientOriginalParts.Clear();
        clientOriginalParts.AddRange(activePC.GetComponentsInChildren<PickupItem>());
    }

    private void BreakPC(int errorType)
    {
        PickupItem[] parts = activePC.GetComponentsInChildren<PickupItem>();
        PickupItem cpu = null, mb = null, gpu = null, psu = null, cooler = null, ram = null;

        foreach (var part in parts)
        {
            if (part.itemType == ItemType.CPU) cpu = part;
            if (part.itemType == ItemType.Motherboard) mb = part;
            if (part.itemType == ItemType.GPU) gpu = part;
            if (part.itemType == ItemType.PowerSupply) psu = part;
            if (part.itemType == ItemType.Cooler) cooler = part; 
            if (part.itemType == ItemType.RAM) ram = part;       
        }

        activePC.jsonPartID = "case_standard";
        if (mb != null) mb.jsonPartID = "mb_b660m";       
        if (cpu != null) cpu.jsonPartID = "cpu_i5_12400";  
        if (gpu != null) gpu.jsonPartID = "gpu_3060";      
        if (ram != null) ram.jsonPartID = "ram_ddr4";      
        if (cooler != null) cooler.jsonPartID = "cooler_air_intel";

        switch (errorType)
        {
            case 0: 
                if (cpu != null) cpu.jsonPartID = "cpu_i9_13900k";
                if (gpu != null) gpu.jsonPartID = "gpu_4090";
                if (psu != null) psu.jsonPartID = "psu_300"; 
                break;
            case 1: 
                if (mb != null) mb.jsonPartID = "mb_b660m";      
                if (cpu != null) cpu.jsonPartID = "cpu_r5_5600";  
                if (cpu != null) EjectPart(cpu);
                break;
            case 2: 
                activePC.jsonPartID = "case_mini";
                if (gpu != null) gpu.jsonPartID = "gpu_4090"; 
                if (gpu != null) EjectPart(gpu); 
                
                if (cooler != null)
                {
                    cooler.jsonPartID = "cooler_tower_huge"; 
                    EjectPart(cooler); 
                }
                break;
        }

        activePC.ReloadDataFromJSON();
        foreach (var part in parts) 
        {
            if (part != activePC) part.ReloadDataFromJSON();
        }
        foreach (var part in parts) 
        {
            if (part.itemType == ItemType.Motherboard) part.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void EjectPart(PickupItem part)
    {
        if (part.currentSlot != null) part.currentSlot.ClearSlot();
        if (part.currentCase != null) part.currentCase.RemovePart(part);
        part.currentSlot = null;
        part.currentCase = null;
        part.transform.SetParent(null);
        part.transform.position = ejectPoint.position;
        Collider coll = part.GetComponent<Collider>();
        if (coll != null) coll.enabled = true;
        if (part.GetComponent<Rigidbody>() == null) part.gameObject.AddComponent<Rigidbody>();
        part.SetPhysics(true);
    }

    public string TryCompleteQuest(out int stars)
    {
        stars = 0;
        if (activePC == null) return "Где компьютер клиента?";

        if (!activePC.IsFullyAssembledAndWorking())
        {
            return "Компьютер не включается или собран не до конца!";
        }

        stars = 5;
        string feedback = "Отличная работа!";

        PickupItem[] parts = activePC.GetComponentsInChildren<PickupItem>();
        float totalTdp = 0, psuTdp = 0, cpuTdp = 0, coolerMaxTdp = 0;
        bool badFormFactor = false;

        foreach (var part in parts)
        {
            if (part.itemType == ItemType.PowerSupply) psuTdp = part.data.tdp;
            else totalTdp += part.data.tdp;

            if (part.itemType == ItemType.CPU) cpuTdp = part.data.tdp;
            if (part.itemType == ItemType.Cooler) coolerMaxTdp = part.data.max_tdp;
            
            if (part.itemType == ItemType.Motherboard && part.data.form_factor == "Mini-ITX" && activePC.data.form_factor == "ATX")
                badFormFactor = true;
        }

        // --- УМНЫЕ ШТРАФЫ ---
        // Штрафуем за БП, только если он мощнее 800W и при этом система ест в 2.5 раза меньше
        if (psuTdp >= 800 && psuTdp > totalTdp * 2.5f) { stars--; feedback = "Всё работает, но зачем такой мощный БП? Я переплатил!"; }
        
        // Штрафуем за кулер, только если он рассчитан на 250W+, а проц слабый (65W)
        if (coolerMaxTdp >= 250 && cpuTdp <= 65) { stars--; feedback = "Работает, но охлаждение избыточно дорогое для этого процессора."; }
        
        if (badFormFactor) { stars--; feedback = "Зачем вы поставили крошечную плату в мой огромный корпус? Выглядит ужасно."; }

        totalCompletedQuests++;
        totalStars += stars;
        string log = $"<b>Заказ #{totalCompletedQuests}</b>\nОценка: {stars} ⭐\nОтзыв: <i>{feedback}</i>\n";
        historyLogs.Insert(0, log); 

        // --- УНИЧТОЖАЕМ ПК И ВЕСЬ МУСОР КЛИЕНТА ---
        Destroy(activePC.gameObject);
        foreach (var oldPart in clientOriginalParts)
        {
            // Если деталь осталась валяться на столе (и еще не удалена вместе с ПК) - удаляем её
            if (oldPart != null && oldPart.gameObject != null)
            {
                Destroy(oldPart.gameObject);
            }
        }
        clientOriginalParts.Clear();
        // ------------------------------------------

        hasActiveQuest = false;
        activePC = null;

        return "SUCCESS";
    }

    public float GetAverageRating()
    {
        if (totalCompletedQuests == 0) return 5.0f;
        return totalStars / totalCompletedQuests;
    }

    public string GetQuestDescription()
    {
        if (!hasActiveQuest) return "Нет активных заказов.";
        switch (currentErrorType)
        {
            case 0: return "<b>Клиент: Иван</b>\n«Компьютер выключается, когда я запускаю игру. В СЦ сказали, проблема с питанием. Помогите!»";
            case 1: return "<b>Клиент: Сергей</b>\n«Купил новый процессор, а он не лезет в материнскую плату! Поставьте подходящий, пожалуйста.»";
            case 2: return "<b>Клиент: Анна</b>\n«Мне подарили видеокарту, но она не помещается в мой маленький корпус. Замените на ту, что влезет!»";
            default: return "Требуется ремонт.";
        }
    }
}