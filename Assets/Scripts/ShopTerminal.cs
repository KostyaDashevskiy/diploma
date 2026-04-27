using UnityEngine;

// Наследуемся от нашей базовой системы взаимодействия
public class ShopTerminal : Interactable
{
    public override string GetPromptMessage(PlayerInteract player)
    {
        // Проверяем, если инвентарь уже открыт, текст не показываем
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen)
            return "";

        // Если у игрока что-то в руках, он не может открыть магазин
        if (player.heldItem != null)
            return "Освободите руки, чтобы открыть каталог";

        return "E - Заказать комплектующие";
    }

    protected override void Interact(PlayerInteract player)
    {
        // Не даем открыть, если руки заняты
        if (player.heldItem != null) return;

        // Открываем инвентарь через Синглтон!
        if (InventoryManager.Instance != null && !InventoryManager.Instance.isOpen)
        {
            InventoryManager.Instance.Open();
        }
    }
}