using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask;

    [Header("Настройки инвентаря/рук")]
    public Transform holdPosition; 
    public PickupItem heldItem;    
    
    private PlayerUi playerUI;
    private InputManager inputManager;

    // Таймер для верхних уведомлений
    private float notificationTimer = 0f;

    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        playerUI = GetComponent<PlayerUi>();
        inputManager = GetComponent<InputManager>();
    }

    void Update()
    {
        if (LaptopManager.Instance != null && LaptopManager.Instance.isOpen) 
        {
            playerUI.UpdateCenterPrompt(string.Empty);
            playerUI.UpdateBottomStats(string.Empty);
            return;
        }
        
        // --- 1. ЕСЛИ ИГРА НА ПАУЗЕ ---
        if (PauseMenu.Instance != null && PauseMenu.Instance.isPaused)
        {
            // Стираем тексты и отключаем луч
            playerUI.UpdateCenterPrompt(string.Empty);
            playerUI.UpdateBottomStats(string.Empty);
            return;
        }
        // Если инвентарь открыт, отключаем взаимодействие с миром
        // --- ФИКС ТЕКСТА ---
        if (InventoryManager.Instance != null && InventoryManager.Instance.isOpen) 
        {
            // Жестко стираем текст прицела, если инвентарь открыт
            playerUI.UpdateCenterPrompt(string.Empty);
            playerUI.UpdateBottomStats(string.Empty);
            return;
        }
        

        // 1. Управление таймером ВЕРХНИХ уведомлений
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
        }
        else
        {
            playerUI.UpdateTopNotification(string.Empty);
        }

        // 2. Очищаем ЦЕНТР и НИЗ каждый кадр по умолчанию
        playerUI.UpdateCenterPrompt(string.Empty);
        playerUI.UpdateBottomStats(string.Empty);

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInfo;
        bool lookingAtInteractable = false;

        // Пускаем луч
        if (Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                lookingAtInteractable = true;
                
                // Выводим короткий текст в ЦЕНТР
                playerUI.UpdateCenterPrompt(interactable.GetPromptMessage(this));

                // Если это деталь, выводим её статы ВНИЗ СЛЕВА
                PickupItem pItem = interactable as PickupItem;
                if (pItem != null)
                {
                    playerUI.UpdateBottomStats(pItem.GetStatsMessage());
                }

                // Логика установки (E)
                if (inputManager.onFoot.Interact.triggered)
                {
                    interactable.BaseInteract(this);
                }

                // Логика извлечения (Q)
                if (Keyboard.current.qKey.wasPressedThisFrame && pItem != null)
                {
                    pItem.Extract(this);
                }
            }
        }

        // Логика сброса предмета на стол
        if (!lookingAtInteractable && heldItem != null)
        {
            playerUI.UpdateCenterPrompt($"E - Положить [{heldItem.itemType}] {heldItem.data.partName}");

            if (inputManager.onFoot.Interact.triggered)
            {
                PlaceHeldItem();
            }
        }
    }

    public void PickUp(PickupItem item)
    {
        heldItem = item;
        heldItem.transform.SetParent(holdPosition);
        heldItem.transform.localPosition = Vector3.zero;
        heldItem.transform.localRotation = Quaternion.identity;
        heldItem.SetPhysics(false); 
    }

    private void PlaceHeldItem()
    {
        heldItem.transform.SetParent(null);
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distance))
            heldItem.transform.position = hit.point + hit.normal * 0.1f; 
        else
            heldItem.transform.position = cam.transform.position + cam.transform.forward * (distance - 0.5f);

        heldItem.transform.rotation = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0);
        heldItem.SetPhysics(true); 
        heldItem = null; 
    }

    // Метод для вывода ВЕРХНИХ уведомлений (цвет задаем прямо в тексте при вызове)
    public void ShowTempMessage(string message, float duration = 2f)
    {
        playerUI.UpdateTopNotification(message);
        notificationTimer = duration;
    }
}