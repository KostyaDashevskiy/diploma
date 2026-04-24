using UnityEngine;

// Класс характеристик из твоего диплома
[System.Serializable]
public class PartData
{
    public string partID;       // Например: "cpu_i5"
    public string partName;     // Например: "Intel Core i5"
    public string socketType;   // "LGA1700", "AM5", "DDR4", "ATX"
    public float tdp;           // Потребление энергии (Ватт). Для БП это будет запас мощности.
}