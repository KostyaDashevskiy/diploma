using System.Collections.Generic;
using UnityEngine;

public class AssemblyManager : MonoBehaviour
{
    // Паттерн Singleton для глобального доступа
    public static AssemblyManager Instance { get; private set; }[Header("Текущая сборка")]
    public List<PartData> installedParts = new List<PartData>();
    
    public float currentPowerSupplyCapacity = 0f; // Мощность установленного БП
    public float currentTotalTDP = 0f;            // Суммарное потребление

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Добавление установленной детали в базу
    public void AddPartToAssembly(PartData newPart, ItemType type)
    {
        installedParts.Add(newPart);

        // Если это блок питания - записываем его мощность
        if (type == ItemType.PowerSupply)
        {
            currentPowerSupplyCapacity = newPart.tdp;
        }
        else
        {
            // Иначе прибавляем потребление к общей сумме
            currentTotalTDP += newPart.tdp;
        }

        CheckPowerBalance();
    }

    // Метод для удаления детали из базы (когда мы вытаскиваем её из слота)
    public void RemovePartFromAssembly(PartData removedPart, ItemType type)
    {
        installedParts.Remove(removedPart);

        if (type == ItemType.PowerSupply)
            currentPowerSupplyCapacity = 0f;
        else
            currentTotalTDP -= removedPart.tdp;

        CheckPowerBalance();
    }

    // Логика проверки из Листинга А.2 (Уровень 2: Энергетический аудит)
    public bool CheckPowerBalance()
    {
        if (installedParts.Count == 0) return true;

        // Требуется запас мощности 20% по диплому
        float requiredPeakPower = currentTotalTDP * 1.2f;

        if (currentPowerSupplyCapacity >= requiredPeakPower)
        {
            Debug.Log($"Энергобаланс в норме. Потребление: {currentTotalTDP}W, Блок питания: {currentPowerSupplyCapacity}W");
            return true;
        }
        else
        {
            float deficit = requiredPeakPower - currentPowerSupplyCapacity;
            Debug.LogWarning($"ОШИБКА: Недостаточно мощности блока питания! Дефицит {deficit}W.");
            return false;
        }
    }
}