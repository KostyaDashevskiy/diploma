using System.Collections.Generic;

[System.Serializable]
public class PartData
{
    public string partID;
    public string partName;
    public string socketType;
    public string form_factor; // НОВОЕ ПОЛЕ: ATX, Mini-ITX и т.д.
    public float tdp;
     public float length; 
    public float width;
    public float height;

    // СПЕЦИФИЧНЫЕ ДЛЯ МАТЕРИНКИ
    public int ram_slots;
    public string ram_type;
    
    // СПЕЦИФИЧНЫЕ ДЛЯ ПРОЦЕССОРА
    public int cores;       // Количество ядер
    public float frequency; // Частота в ГГц

    // СПЕЦИФИЧНЫЕ ДЛЯ ВИДЕОКАРТЫ
    public int vram;        // Объем видеопамяти в ГБ

    // СПЕЦИФИЧНЫЕ ДЛЯ КУЛЕРА
    public float max_tdp;   // Сколько Ватт тепла может отвести кулер
   
}

// Этот класс нужен специально для чтения JSON "из коробки"
[System.Serializable]
public class PartDatabase
{
    //public List<PartData> parts;
    public System.Collections.Generic.List<PartData> parts;
}

[System.Serializable]
public class TheoryEntry
{
    public string category; // "cpu", "gpu" и т.д.
    public string title;
    public string text;
}

[System.Serializable]
public class TheoryDatabase
{
    public System.Collections.Generic.List<TheoryEntry> entries;
}