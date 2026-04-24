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
            return "E - Взять Корпус";
        else if (player.heldItem.itemType == ItemType.Case)
            return "Нельзя вставить корпус в корпус!";
        else
            return "E - Установить " + player.heldItem.data.partName;
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
                player.ShowTempMessage("Нельзя вставить корпус в корпус!");
                return;
            }

            // Защита от установки мелких деталей прямо в корпус
            if (itemInHand.itemType == ItemType.CPU || 
                itemInHand.itemType == ItemType.GPU || 
                itemInHand.itemType == ItemType.RAM ||
                itemInHand.itemType == ItemType.Cooler)
            {
                player.ShowTempMessage("Эту деталь нужно вставлять в слот на материнской плате!");
                return;
            }

            // ЛОГИКА БЛОКА ПИТАНИЯ (Устанавливается независимо)
            if (itemInHand.itemType == ItemType.PowerSupply)
            {
                if (hasPSU)
                {
                    player.ShowTempMessage("Блок питания уже установлен!");
                    return;
                }
                hasPSU = true;
            }
            // ЛОГИКА МАТЕРИНСКОЙ ПЛАТЫ
            else if (itemInHand.itemType == ItemType.Motherboard)
            {
                if (hasMotherboard)
                {
                    player.ShowTempMessage("Материнская плата уже установлена!");
                    return;
                }
                hasMotherboard = true;
            }
            else
            {
                player.ShowTempMessage("Нельзя вставить это в корпус!");
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