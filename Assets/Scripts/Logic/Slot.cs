using UnityEngine;

public class Slot : Interactable
{[Header("Настройки слота")]
    public string slotName;         
    public string acceptableSocket; 
    
    [Header("Блокировка (например Кулер)")]
    public Slot blockingSlot; // Перетащи сюда слот кулера, если он должен мешать установке
    
    public bool isOccupied = false;
    public PickupItem installedItem; 

    private void Start()
    {
        // Ищем свою родительскую материнскую плату
        PickupItem parentMotherboard = GetComponentInParent<PickupItem>();

        if (parentMotherboard != null && parentMotherboard.itemType == ItemType.Motherboard)
        {
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
                        player.ShowTempMessage($"<color=red>Видеокарта не влезает в корпус! Макс. длина: {parentCase.data.length}мм, а у детали: {itemInHand.data.length}мм</color>", 4f);
                        return; // Прерываем установку
                    }
                }
                // Проверяем высоту кулера (ширина корпуса = высота кулера)
                else if (itemInHand.itemType == ItemType.Cooler)
                {
                    if (itemInHand.data.height > parentCase.data.width)
                    {
                        player.ShowTempMessage($"<color=red>Кулер слишком высокий! Корпус вмещает {parentCase.data.width}мм, а кулер {itemInHand.data.height}мм</color>", 4f);
                        return; // Прерываем установку
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
                player.ShowTempMessage($"<color=red>Несовместимо! Слот требует {acceptableSocket}, а у детали {itemInHand.data.socketType}</color>", 3f);
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