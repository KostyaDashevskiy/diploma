using UnityEngine;

public class Slot : Interactable
{[Header("Настройки слота")]
    public string slotName;         
    public string acceptableSocket; 
    
    [Header("Блокировка (например Кулер)")]
    public Slot blockingSlot; // Перетащи сюда слот кулера, если он должен мешать установке
    
    public bool isOccupied = false;
    public PickupItem installedItem; 

    [Header("Визуал для подсветки")]
    public MeshRenderer visualMesh; // Перетащим сюда кубик, который изображает слот
    private Color originalColor;

    private void Start()
    {
        // В URP цвет хранится в переменной _BaseColor
        if (visualMesh != null) originalColor = visualMesh.material.GetColor("_BaseColor");

        PickupItem parentMotherboard = GetComponentInParent<PickupItem>();
        if (parentMotherboard != null && parentMotherboard.itemType == ItemType.Motherboard)
        {
            if (slotName.Contains("процессора") || slotName.Contains("охлаждения"))
                acceptableSocket = parentMotherboard.data.socketType;
            else if (slotName.Contains("ОЗУ"))
                acceptableSocket = parentMotherboard.data.ram_type;
        }
    }

    // --- ПОДСВЕТКА СЛОТА ---
    public override void ApplyHighlight(PlayerInteract player)
    {
        Debug.Log("ЛУЧ ПОПАЛ В СЛОТ: " + slotName); // <-- ДОБАВЬ ЭТО

        if (visualMesh == null || isOccupied) return;

        if (player.heldItem != null)
        {
            if (player.heldItem.data.socketType == acceptableSocket)
            {
                // Для URP используем SetColor и "_BaseColor"
                visualMesh.material.SetColor("_BaseColor", new Color(0f, 1f, 0f, 0.6f)); 
            }
            else 
            {
                visualMesh.material.SetColor("_BaseColor", new Color(1f, 0f, 0f, 0.6f)); 
            }
        }
        else
        {
            visualMesh.material.SetColor("_BaseColor", new Color(1f, 0.9f, 0f, 0.4f)); 
        }
    }
     public override void RemoveHighlight()
    {
        if (visualMesh != null)
        {
            // Возвращаем родной цвет
            visualMesh.material.SetColor("_BaseColor", originalColor);
        }
    }

    public override string GetPromptMessage(PlayerInteract player)
    {
        if (isOccupied) return ""; 

        if (player.heldItem != null)
            return $"E - Вставить {player.heldItem.data.partName} в {slotName}";
        
        return $"Слот: {slotName} (Требуется {acceptableSocket})";
    }

    protected override void Interact(PlayerInteract player)
    {
        if (isOccupied) return;


        
        if (player.heldItem != null)
        {
            PickupItem itemInHand = player.heldItem;

            // ПРОВЕРКА БЛОКИРОВКИ (если мы ставим проц, а кулер уже стоит)
            if (blockingSlot != null && blockingSlot.isOccupied)
            {
                player.ShowTempMessage("<color=red>Установка невозможна! Мешает другая деталь.</color>");
                return;
            }

            // --- НОВАЯ ЛОГИКА: ПРОВЕРКА ГАБАРИТОВ В КОРПУСЕ ---
            // Пытаемся найти корпус выше по иерархии (если мы уже вставлены в него)
            PCCase parentCase = this.GetComponentInParent<PCCase>();
            if (parentCase != null)
            {
                // Проверяем длину видеокарты
                if (itemInHand.itemType == ItemType.GPU)
                {
                    if (itemInHand.data.length > parentCase.data.length)
                    {
                        player.ShowTempMessage($"<color=red><b>ПРОСТРАНСТВЕННЫЙ КОНФЛИКТ:</b></color>\nВидеокарта ({itemInHand.data.length}мм) упирается в корзины жестких дисков или фронтальные вентиляторы корпуса (лимит {parentCase.data.length}мм). Закрепить ее в слоте PCIe невозможно.", 6f);
                        return; 
                    }
                }
                else if (itemInHand.itemType == ItemType.Cooler)
                {
                    if (itemInHand.data.height > parentCase.data.width)
                    {
                        player.ShowTempMessage($"<color=red><b>ПРОСТРАНСТВЕННЫЙ КОНФЛИКТ:</b></color>\nТепловые трубки кулера ({itemInHand.data.height}мм) выходят за пределы рамы корпуса (лимит {parentCase.data.width}мм). Боковая стеклянная панель системного блока не закроется.", 6f);
                        return; 
                    }
                }
            }
            // ---------------------------------------------------


            if (itemInHand.data.socketType == acceptableSocket)
            {
                player.heldItem = null; 
                isOccupied = true;
                installedItem = itemInHand;
                
                itemInHand.currentSlot = this; 

                // 1. Привязываем к родителю слота (обычно это Root материнки со Scale 1,1,1)
                itemInHand.transform.SetParent(this.transform.parent); 
                
                // 2. УБИРАЕМ "+ 0.02f", чтобы деталь не висела в воздухе. 
                // Теперь она встанет ровно в центр твоего объекта-слота.
                itemInHand.transform.position = this.transform.position; 

                // 3. ЗАМЕНЯЕМ localRotation на глобальный rotation.
                // Теперь деталь скопирует наклон слота, который ты выставил в редакторе.
                itemInHand.transform.rotation = this.transform.rotation;

                itemInHand.SetPhysics(false);
                
                Rigidbody rb = itemInHand.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);
                
                AssemblyManager.Instance.AddPartToAssembly(itemInHand.data, itemInHand.itemType);
                player.ShowTempMessage("<color=green>Деталь успешно установлена!</color>", 1.5f);
            }
            else
            {
                string errorReason = "";

                // Генерируем фундаментальное объяснение в зависимости от типа детали
                if (itemInHand.itemType == ItemType.CPU)
                {
                    errorReason = $"Архитектурная несовместимость! Сокет материнской платы ({acceptableSocket}) физически не совпадает с контактными площадками процессора ({itemInHand.data.socketType}). Попытка установки приведет к замятию контактов (пинов).";
                }
                else if (itemInHand.itemType == ItemType.RAM)
                {
                    errorReason = $"Электротехнический конфликт! Память {itemInHand.data.socketType} работает на другом напряжении и имеет ключ (прорезь) в другом месте. Вставить ее в слот {acceptableSocket} невозможно.";
                }
                else if (itemInHand.itemType == ItemType.Cooler)
                {
                    errorReason = $"Конфликт монтажа! Отверстия для крепления на сокете {acceptableSocket} расположены на другом расстоянии, чем требует ваш кулер ({itemInHand.data.socketType}).";
                }
                else
                {
                    errorReason = $"Интерфейсы несовместимы: слот требует {acceptableSocket}, а деталь имеет {itemInHand.data.socketType}.";
                }

                player.ShowTempMessage($"<color=red><b>КРИТИЧЕСКАЯ ОШИБКА УСТАНОВКИ:</b></color>\n{errorReason}", 6f);
            }
            
        }
        
        
    }

    public void ClearSlot()
    {
        isOccupied = false;
        installedItem = null;
    }

    public void UpdateSocketFromParent(PickupItem parentMotherboard)
    {
        if (parentMotherboard == null || parentMotherboard.itemType != ItemType.Motherboard) return;

        // Автоматически настраиваем сокет процессора и кулера
        if (slotName.Contains("процессора") || slotName.Contains("охлаждения"))
        {
            acceptableSocket = parentMotherboard.data.socketType;
        }
        // Автоматически настраиваем тип ОЗУ
        else if (slotName.Contains("ОЗУ"))
        {
            acceptableSocket = parentMotherboard.data.ram_type;
        }
    }
}