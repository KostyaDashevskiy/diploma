using UnityEngine;

public class LearningLevelGenerator : MonoBehaviour
{
    [Header("Собранный ПК на сцене")]
    public PCCase targetPC; 
    
    [Header("Точка выброса несовместимых деталей")]
    [Tooltip("Создай пустышку над столом и перетащи сюда")]
    public Transform ejectPoint;

    void Start()
    {
        // Увеличил задержку до 1 секунды, чтобы JSON точно загрузился на слабых ПК
        Invoke(nameof(GenerateRandomError), 1.0f);
    }

    private void GenerateRandomError()
    {
        if (targetPC == null) return;

        PickupItem[] parts = targetPC.GetComponentsInChildren<PickupItem>();
        PickupItem cpu = null, mb = null, gpu = null, psu = null, ram = null, cooler = null;

        foreach (PickupItem part in parts)
        {
            if (part.itemType == ItemType.CPU) cpu = part;
            if (part.itemType == ItemType.Motherboard) mb = part;
            if (part.itemType == ItemType.GPU) gpu = part;
            if (part.itemType == ItemType.PowerSupply) psu = part;
            if (part.itemType == ItemType.RAM) ram = part;
            if (part.itemType == ItemType.Cooler) cooler = part;
        }

        if (cpu == null || mb == null || gpu == null || psu == null) return;

        // БАЗОВАЯ СБОРКА
        targetPC.jsonPartID = "case_standard";
        mb.jsonPartID = "mb_b660m";       
        cpu.jsonPartID = "cpu_i5_12400";  
        gpu.jsonPartID = "gpu_3060";      
        ram.jsonPartID = "ram_ddr4";      
        psu.jsonPartID = "psu_500";       
        if (cooler != null) cooler.jsonPartID = "cooler_air_intel";

        int errorType = Random.Range(0, 3);

        switch (errorType)
        {
            case 0: // НЕХВАТКА МОЩНОСТИ
                cpu.jsonPartID = "cpu_i9_13900k";
                gpu.jsonPartID = "gpu_4090";
                psu.jsonPartID = "psu_300"; 
                break;
                
            case 1: // КОНФЛИКТ СОКЕТА
                mb.jsonPartID = "mb_b660m";      
                cpu.jsonPartID = "cpu_r5_5600";  
                EjectPart(cpu);
                break;
                
            case 2: // ПРОСТРАНСТВЕННЫЙ КОНФЛИКТ (ГАБАРИТЫ)
                targetPC.jsonPartID = "case_mini";
                gpu.jsonPartID = "gpu_4090"; 
                EjectPart(gpu); // Выбрасываем огромную видюху
                
                if (cooler != null)
                {
                    cooler.jsonPartID = "cooler_tower_huge"; 
                    EjectPart(cooler); // Выбрасываем огромный кулер!
                }
                break;
        }

        // Применяем изменения, перезагружая данные
        targetPC.ReloadDataFromJSON(); // Добавь обратно ReloadData!
        foreach (PickupItem part in parts) 
        {
            if (part != targetPC) // Пропускаем сам корпус
                part.ReloadDataFromJSON();
        }

        // --- НОВЫЙ КОД: Заставляем все материнки на сцене обновить свои слоты ---
        PickupItem[] allMotherboards = FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        foreach (var motherboard in allMotherboards)
        {
            if (motherboard.itemType == ItemType.Motherboard)
            {
                motherboard.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
        }
        
        Debug.Log("Учебная сборка успешно сгенерирована!");
    }

    private void EjectPart(PickupItem part)
    {
        if (part.currentSlot != null) part.currentSlot.ClearSlot();
        if (part.currentCase != null) part.currentCase.RemovePart(part);
        part.currentSlot = null;
        part.currentCase = null;
        
        part.transform.SetParent(null);
        
        // --- БЕЗОПАСНАЯ ПОЗИЦИЯ ---
        if (ejectPoint != null) 
            part.transform.position = ejectPoint.position;
        else 
            part.transform.position = targetPC.transform.position + Vector3.up * 0.4f + targetPC.transform.right * 0.4f;

        // Обязательно возвращаем коллайдер! (из-за этого деталь улетала под карту при взятии)
        Collider coll = part.GetComponent<Collider>();
        if (coll != null) coll.enabled = true;

        if (part.GetComponent<Rigidbody>() == null) part.gameObject.AddComponent<Rigidbody>();
        part.SetPhysics(true);
    }
}