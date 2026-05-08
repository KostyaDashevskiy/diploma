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

        // БАЗОВАЯ РАБОЧАЯ СБОРКА (настраиваем IDs, чтобы если не сломали, она работала)
        activePC.jsonPartID = "case_standard";
        if (mb != null) mb.jsonPartID = "mb_b660m";       
        if (cpu != null) cpu.jsonPartID = "cpu_i5_12400";  
        if (gpu != null) gpu.jsonPartID = "gpu_3060";      
        if (ram != null) ram.jsonPartID = "ram_ddr4";      
        if (cooler != null) cooler.jsonPartID = "cooler_air_intel";
        if (psu != null) psu.jsonPartID = "psu_500"; 

        switch (errorType)
        {
            case 0: // БП СГОРЕЛ
                if (cpu != null) cpu.jsonPartID = "cpu_i9_13900k";
                if (gpu != null) gpu.jsonPartID = "gpu_4090";
                if (psu != null) psu.jsonPartID = "psu_300"; 
                break;
            case 1: // КЛИЕНТ КУПИЛ НЕ ТОТ ПРОЦ
                if (mb != null) mb.jsonPartID = "mb_b660m";      
                if (cpu != null) cpu.jsonPartID = "cpu_r5_5600";  
                if (cpu != null) EjectPart(cpu);
                break;
            case 2: // КЛИЕНТ КУПИЛ ОГРОМНУЮ ВИДЮХУ
                activePC.jsonPartID = "case_mini";
                if (gpu != null) gpu.jsonPartID = "gpu_4090"; 
                if (gpu != null) EjectPart(gpu); 
                if (cooler != null) { cooler.jsonPartID = "cooler_tower_huge"; EjectPart(cooler); }
                break;
            
            // --- НОВЫЕ КВЕСТЫ: СБОРКА С НУЛЯ ---
            case 3: // ЗАКАЗ: ОФИСНЫЙ ПК
            case 4: // ЗАКАЗ: ИГРОВОЙ ПК
                // Клиент прислал ТОЛЬКО пустой корпус. Игрок должен собрать всё сам!
                if (mb != null) EjectPart(mb);
                if (cpu != null) EjectPart(cpu);
                if (gpu != null) EjectPart(gpu);
                if (ram != null) EjectPart(ram);
                if (cooler != null) EjectPart(cooler);
                if (psu != null) EjectPart(psu);
                
                // Делаем пустой корпус
                activePC.jsonPartID = "case_standard";
                break;
        }

        activePC.ReloadDataFromJSON();
        foreach (var part in parts) 
        {
            if (part != activePC && part != null) part.ReloadDataFromJSON();
        }
        foreach (var part in parts) 
        {
            if (part.itemType == ItemType.Motherboard && part != null) part.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
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

    // ПРОВЕРКА ЗАКАЗА
    public string TryCompleteQuest(out int stars)
    {
        stars = 0;
        if (activePC == null) return "Где компьютер клиента?";

        if (!activePC.IsFullyAssembledAndWorking())
        {
            return "<b>ОТКАЗ В ПРИЁМЕ РАБОТЫ!</b>\nКомпьютер не проходит POST-тесты (самодиагностику при включении). Убедитесь, что установлены все критически важные компоненты (CPU, GPU, RAM, Охлаждение), а мощности блока питания хватает с запасом минимум в 20%.";
        }

        stars = 5;
        string feedback = "Отличная работа!";

        PickupItem[] parts = activePC.GetComponentsInChildren<PickupItem>();
        float totalTdp = 0, psuTdp = 0, cpuTdp = 0, coolerMaxTdp = 0;
        int gpuVram = 0, cpuCores = 0;
        bool badFormFactor = false;

        foreach (var part in parts)
        {
            if (part.itemType == ItemType.PowerSupply) psuTdp = part.data.tdp;
            else totalTdp += part.data.tdp;

            if (part.itemType == ItemType.CPU) { cpuTdp = part.data.tdp; cpuCores = part.data.cores; }
            if (part.itemType == ItemType.Cooler) coolerMaxTdp = part.data.max_tdp;
            if (part.itemType == ItemType.GPU) gpuVram = part.data.vram;
            
            if (part.itemType == ItemType.Motherboard && part.data.form_factor == "Mini-ITX" && activePC.data.form_factor == "ATX")
                badFormFactor = true;
        }

        // --- СПЕЦИФИЧНЫЕ ПРОВЕРКИ ДЛЯ НОВЫХ КВЕСТОВ ---
        if (currentErrorType == 3) // ОФИСНЫЙ ПК
        {
            // Штраф, если поставили дорогую видеокарту
            if (gpuVram >= 12) { stars -= 2; feedback = "Я просил дешевый ПК для ворда, а вы поставили игровую видеокарту! Мой бюджет уничтожен."; }
            // Штраф, если поставили процессор на 250W
            if (cpuTdp > 65) { stars--; feedback = "Компьютер шумит как самолет. Для офиса нужен был холодный процессор."; }
        }
        else if (currentErrorType == 4) // ИГРОВОЙ ПК
        {
            // Ошибка: Слишком слабая видеокарта (Меньше 8 Гб)
            if (gpuVram < 8) return "Клиент недоволен! Игры тормозят. Вы поставили слишком слабую видеокарту (нужно от 8 ГБ VRAM). Переделывайте!";
            // Ошибка: Слишком мало ядер (Нужно 6+)
            if (cpuCores < 6) return "Процессор не тянет современные игры. Клиент просил минимум 6 ядер. Переделывайте!";
        }

        // Базовые штрафы (если это не специфичный квест)
        if (currentErrorType < 3) 
        {
            if (psuTdp >= 800 && psuTdp > totalTdp * 2.5f) { stars--; feedback = "Всё работает, но зачем такой мощный БП? Я переплатил!"; }
            if (coolerMaxTdp >= 250 && cpuTdp <= 65) { stars--; feedback = "Работает, но охлаждение избыточно дорогое для этого процессора."; }
            if (badFormFactor) { stars--; feedback = "Зачем вы поставили крошечную плату в мой огромный корпус? Выглядит ужасно."; }
        }

        // Защита от отрицательных звезд
        if (stars < 1) stars = 1;

        totalCompletedQuests++;
        totalStars += stars;
        string log = $"<b>Заказ #{totalCompletedQuests}</b>\nОценка: {stars} ⭐\nОтзыв: <i>{feedback}</i>\n";
        historyLogs.Insert(0, log); 

        // Удаляем ПК
        Destroy(activePC.gameObject);
        foreach (var oldPart in clientOriginalParts)
        {
            if (oldPart != null && oldPart.gameObject != null) Destroy(oldPart.gameObject);
        }
        clientOriginalParts.Clear();

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
            case 0: return "<b>Клиент: Иван</b>\n«Компьютер выключается в играх. В СЦ сказали, проблема с питанием. Помогите!»";
            case 1: return "<b>Клиент: Сергей</b>\n«Купил новый процессор, а он не лезет в материнскую плату! Поставьте подходящий, пожалуйста.»";
            case 2: return "<b>Клиент: Анна</b>\n«Мне подарили видеокарту, но она не помещается в мой маленький корпус. Замените на ту, что влезет!»";
            case 3: return "<b>ЗАКАЗ: ОФИСНЫЙ ПК</b>\n«Соберите мне недорогой ПК для работы в Word. Процессор не должен быть горячим (максимум 65W TDP), а дорогая видеокарта мне вообще не нужна!»";
            case 4: return "<b>ЗАКАЗ: ИГРОВОЙ ПК</b>\n«Соберите мощную игровую систему. Мне нужно минимум 6 ядер процессора и видеокарта от 8 ГБ видеопамяти!»";
            default: return "Требуется ремонт.";
        }
    }
}