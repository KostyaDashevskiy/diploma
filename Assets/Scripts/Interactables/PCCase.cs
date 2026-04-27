using UnityEngine;

public class PCCase : PickupItem
{[Header("Настройки Корпуса")]
    public bool hasMotherboard = false;
    public bool hasPSU = false;

    // Пустышки внутри корпуса для позиционирования
    public Transform motherboardPoint; 
    public Transform psuPoint;

    public override string GetPromptMessage(PlayerInteract player)
    {
        if (player.heldItem == null)
            return "E - Взять\nКорпус";
        else if (player.heldItem.itemType == ItemType.Case)
            return "Нельзя вставить корпус в корпус!";
        else
            return "E - Установить\n" + player.heldItem.data.partName;
    }

    // Метод для очистки статуса, когда мы достаем деталь
    public void RemovePart(PickupItem part)
    {
        if (part.itemType == ItemType.Motherboard) hasMotherboard = false;
        if (part.itemType == ItemType.PowerSupply) hasPSU = false;
    }

    protected override void Interact(PlayerInteract player)
    {
        if (player.heldItem != null)
        {
            PickupItem itemInHand = player.heldItem;

            if (itemInHand.itemType == ItemType.Case)
            {
                player.ShowTempMessage("<color=red>Нельзя вставить корпус в корпус!</color>");
                return;
            }

            // Защита от установки мелких деталей прямо в корпус
            if (itemInHand.itemType == ItemType.CPU || 
                itemInHand.itemType == ItemType.GPU || 
                itemInHand.itemType == ItemType.RAM ||
                itemInHand.itemType == ItemType.Cooler)
            {
                player.ShowTempMessage("<color=red>Эту деталь нужно вставлять в слот на материнской плате!</color>");
                return;
            }

            // ЛОГИКА БЛОКА ПИТАНИЯ (Устанавливается независимо)
            if (itemInHand.itemType == ItemType.PowerSupply)
            {
                if (hasPSU)
                {
                    player.ShowTempMessage("<color=red>Блок питания уже установлен!</color>");
                    return;
                }
                hasPSU = true;
            }
            // ЛОГИКА МАТЕРИНСКОЙ ПЛАТЫ
            // ЛОГИКА МАТЕРИНСКОЙ ПЛАТЫ
            else if (itemInHand.itemType == ItemType.Motherboard)
            {
                if (hasMotherboard)
                {
                    player.ShowTempMessage("<color=red>Материнская плата уже установлена!</color>");
                    return;
                }

                // --- СКАНИРОВАНИЕ ГАБАРИТОВ СБОРКИ ---
                // Ищем все скрипты PickupItem внутри материнки (это детали в слотах)
                PickupItem[] attachedParts = itemInHand.GetComponentsInChildren<PickupItem>();

                foreach (PickupItem part in attachedParts)
                {
                    // Проверяем длинные видеокарты
                    if (part.itemType == ItemType.GPU)
                    {
                        if (part.data.length > this.data.length) // this.data - это данные текущего корпуса
                        {
                            player.ShowTempMessage($"<color=red>Установка отменена! Установленная видеокарта ({part.data.length}мм) не влезет в этот корпус ({this.data.length}мм).</color>", 4f);
                            return; // Прерываем установку материнки!
                        }
                    }
                    // Проверяем высокие кулеры (высота кулера сравнивается с шириной корпуса)
                    else if (part.itemType == ItemType.Cooler)
                    {
                        if (part.data.height > this.data.width)
                        {
                            player.ShowTempMessage($"<color=red>Установка отменена! Башня кулера ({part.data.height}мм) не закроет боковую крышку корпуса ({this.data.width}мм).</color>", 4f);
                            return; // Прерываем установку материнки!
                        }
                    }
                }
                // -------------------------------------

                hasMotherboard = true; // Все проверки пройдены, разрешаем установку
            }
            else
            {
                player.ShowTempMessage("<color=red>Нельзя вставить это в корпус!</color>");
                return;
            }

            // --- ОБЩАЯ ЛОГИКА УСТАНОВКИ В КОРПУС ---
            player.heldItem = null; 
            
            // Выбираем, куда крепить деталь
            Transform targetPoint = this.transform;
            if (itemInHand.itemType == ItemType.Motherboard && motherboardPoint != null) targetPoint = motherboardPoint;
            if (itemInHand.itemType == ItemType.PowerSupply && psuPoint != null) targetPoint = psuPoint;

            itemInHand.transform.SetParent(targetPoint);
            itemInHand.transform.localPosition = Vector3.zero; 
            itemInHand.transform.localRotation = Quaternion.identity;
            

            itemInHand.SetPhysics(false);
            itemInHand.currentCase = this; // Запоминаем, что деталь в корпусе

            // Удаляем ТОЛЬКО Rigidbody, чтобы не было взрывов. PickupItem и Collider ОСТАВЛЯЕМ!
            Rigidbody rb = itemInHand.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            AssemblyManager.Instance.AddPartToAssembly(itemInHand.data, itemInHand.itemType);
            player.ShowTempMessage("<color=green>Успешно установлено!</color>", 1.5f);
        }
        else
        {
            base.Interact(player); 
        }
    }
}