using System.Collections.Generic;
using UnityEngine;
public class AssemblyManager : MonoBehaviour
{
public static AssemblyManager Instance { get; private set; }
[Header("База данных (JSON)")]
public TextAsset jsonFile; // Сюда перетащим наш PartsDB.json в инспекторе

// Словарь для быстрого поиска детали по её partID
public Dictionary<string, PartData> database = new Dictionary<string, PartData>();

[Header("Текущая сборка")]
public List<PartData> installedParts = new List<PartData>();
public float currentPowerSupplyCapacity = 0f; 
public float currentTotalTDP = 0f;            

private void Awake()
{
    if (Instance == null) Instance = this;
    else Destroy(gameObject);

    LoadJSONDatabase();
}

// МЕТОД ЗАГРУЗКИ ИЗ ДИПЛОМА
private void LoadJSONDatabase()
{
    if (jsonFile != null)
    {
        PartDatabase db = JsonUtility.FromJson<PartDatabase>(jsonFile.text);
        foreach (PartData part in db.parts)
        {
            // Записываем деталь в словарь (Ключ = partID, Значение = сама деталь)
            if (!database.ContainsKey(part.partID))
            {
                database.Add(part.partID, part);
            }
        }
        Debug.Log($"<color=green>База данных JSON загружена! Найдено деталей: {database.Count}</color>");
    }
    else
    {
        Debug.LogError("JSON ФАЙЛ НЕ НАЗНАЧЕН В ASSEMBLY MANAGER!");
    }
}

// Метод, который отдаст данные по ID
public PartData GetPartInfo(string searchID)
{
    if (database.ContainsKey(searchID)) return database[searchID];
    return null;
}

public void AddPartToAssembly(PartData newPart, ItemType type)
{
    installedParts.Add(newPart);
    if (type == ItemType.PowerSupply) currentPowerSupplyCapacity = newPart.tdp;
    else currentTotalTDP += newPart.tdp;
    CheckPowerBalance();
}

public void RemovePartFromAssembly(PartData removedPart, ItemType type)
{
    installedParts.Remove(removedPart);
    if (type == ItemType.PowerSupply) currentPowerSupplyCapacity = 0f;
    else currentTotalTDP -= removedPart.tdp;
    CheckPowerBalance();
}

public bool CheckPowerBalance()
{
    if (installedParts.Count == 0) return true;
    float requiredPeakPower = currentTotalTDP * 1.2f;

    if (currentPowerSupplyCapacity >= requiredPeakPower) return true;
    else return false;
}
}