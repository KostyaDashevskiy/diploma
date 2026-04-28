using UnityEngine;

public class ShopTerminal : Interactable
{
    public override string GetPromptMessage(PlayerInteract player)
    {
        if (LaptopManager.Instance != null && LaptopManager.Instance.isOpen) return "";
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen) return "";

        if (player.heldItem != null) return "Освободите руки, чтобы использовать ноутбук";

        return "E - Воспользоваться ноутбуком"; // Изменили текст
    }

    protected override void Interact(PlayerInteract player)
    {
        if (player.heldItem != null) return;

        if (LaptopManager.Instance != null && !LaptopManager.Instance.isOpen)
        {
            LaptopManager.Instance.OpenLaptop();
        }
    }
}