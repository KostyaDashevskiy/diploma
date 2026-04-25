using UnityEngine;

public class Slot : Interactable
{[Header("Настройки слота")]
    public string slotName;         
    public string acceptableSocket; 
    
    [Header("Блокировка (например Кулер)")]
    public Slot blockingSlot; // Перетащи сюда слот кулера, если он должен мешать установке
    
    public bool isOccupied = false;
    public PickupItem installedItem; 

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
                player.ShowTempMessage("Установка невозможна! Мешает другая деталь.");
                return;
            }

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
                player.ShowTempMessage($"Несовместимо! Слот требует {acceptableSocket}, а у детали {itemInHand.data.socketType}", 3f);
            }
        }
    }

    public void ClearSlot()
    {
        isOccupied = false;
        installedItem = null;
    }
}