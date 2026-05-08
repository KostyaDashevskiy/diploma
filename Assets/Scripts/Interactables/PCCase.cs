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
                    if (part.itemType == ItemType.GPU && part.data.length > this.data.length)
                    {
                        player.ShowTempMessage($"<color=red><b>УСТАНОВКА МАТЕРИНСКОЙ ПЛАТЫ ОТМЕНЕНА:</b></color>\nУстановленная видеокарта ({part.data.length}мм) упирается в переднюю панель. Сначала снимите видеокарту, установите плату, и подберите более короткий графический ускоритель.", 6f);
                        return; 
                    }
                    else if (part.itemType == ItemType.Cooler && part.data.height > this.data.width)
                    {
                        player.ShowTempMessage($"<color=red><b>УСТАНОВКА МАТЕРИНСКОЙ ПЛАТЫ ОТМЕНЕНА:</b></color>\nВысота установленного башенного кулера ({part.data.height}мм) не позволит закрыть боковую крышку этого корпуса. Снимите кулер и подберите низкопрофильную систему охлаждения.", 6f);
                        return; 
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

    // Метод проверяет: Готов ли этот ПК к работе?
    public bool IsFullyAssembledAndWorking()
    {
        if (!hasMotherboard || !hasPSU) return false;

        PickupItem[] attachedParts = GetComponentsInChildren<PickupItem>();
        
        bool foundCPU = false, foundGPU = false, foundRAM = false;
        bool foundCooler = false; // <-- ДОБАВИЛИ ФЛАГ КУЛЕРА
        
        float totalTdp = 0f, psuPower = 0f;

        foreach (PickupItem part in attachedParts)
        {
            if (part.itemType == ItemType.Case) continue;
            
            if (part.itemType == ItemType.CPU) foundCPU = true;
            if (part.itemType == ItemType.GPU) foundGPU = true;
            if (part.itemType == ItemType.RAM) foundRAM = true;
            if (part.itemType == ItemType.Cooler) foundCooler = true; // <-- ПРОВЕРЯЕМ
            
            if (part.itemType == ItemType.PowerSupply) psuPower = part.data.tdp;
            else totalTdp += part.data.tdp;
        }

        // <-- ТЕПЕРЬ ТРЕБУЕМ НАЛИЧИЕ КУЛЕРА
        if (!foundCPU || !foundGPU || !foundRAM || !foundCooler) return false; 
        
        if (psuPower < (totalTdp * 1.2f)) return false; 

        return true; 
    }
}