using System.Collections.Generic;

[System.Serializable]
public class PartData
{
    public string partID;
    public string partName;
    public string socketType;
    public float tdp;

    public float length; 
    public float width;
    public float height;
}

// Этот класс нужен специально для чтения JSON "из коробки"
[System.Serializable]
public class PartDatabase
{
    public List<PartData> parts;
}