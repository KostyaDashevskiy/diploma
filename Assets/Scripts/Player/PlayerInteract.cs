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
    private Interactable currentTarget; // Запоминаем, на что смотрим  

    private string tempMessage = "";
    private float tempMessageTimer = 0f;

    
    private PlayerUi playerUI;
    private InputManager inputManager;

    // Таймер для верхних уведомлений
    private float notificationTimer = 0f;

    private Vector3 originalHoldLocalPos;

    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        playerUI = GetComponent<PlayerUi>();
        inputManager = GetComponent<InputManager>();

        // Запоминаем дефолтную позицию точки хвата
        if (holdPosition != null) originalHoldLocalPos = holdPosition.localPosition;
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

        if (Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            
            if (interactable != null && interactable == heldItem)
            {
                interactable = null; 
            }

            if (interactable != null)
            {
                lookingAtInteractable = true;
                
                // --- ЛОГИКА ПОДСВЕТКИ ---
                // Если мы посмотрели на новый предмет, снимаем подсветку со старого
                if (interactable != currentTarget)
                {
                    if (currentTarget != null) currentTarget.RemoveHighlight();
                    currentTarget = interactable;
                }
                // Применяем подсветку к текущему (каждый кадр, чтобы реагировать на деталь в руках)
                currentTarget.ApplyHighlight(this);
                // ------------------------

                if (tempMessageTimer <= 0) 
                {
                    playerUI.UpdateCenterPrompt(interactable.GetPromptMessage(this));
                }

                // --- 1. ОБЪЯВЛЯЕМ ПЕРЕМЕННУЮ ОДИН РАЗ ---
                PickupItem pItem = interactable as PickupItem;

                // --- 2. ВЫВОДИМ СТАТЫ ---
                if (pItem != null)
                {
                    playerUI.UpdateBottomStats(pItem.GetStatsMessage());
                }
                else
                {
                    playerUI.UpdateBottomStats(string.Empty);
                }

                // --- 3. ЛОГИКА ВЗАИМОДЕЙСТВИЯ (E) ---
                if (inputManager.onFoot.Interact.triggered)
                {
                    interactable.BaseInteract(this);
                }

                // --- 4. ЛОГИКА ИЗВЛЕЧЕНИЯ (Q) ---
                // Здесь мы просто используем уже объявленную pItem
                if (Keyboard.current.qKey.wasPressedThisFrame && pItem != null)
                {
                    pItem.Extract(this);
                }
            }
        }

        // --- ЕСЛИ СМОТРИМ В ПУСТОТУ - СНИМАЕМ ПОДСВЕТКУ ---
        if (!lookingAtInteractable)
        {
            if (currentTarget != null)
            {
                currentTarget.RemoveHighlight();
                currentTarget = null;
            }
        }

        // Логика сброса предмета на стол
        // Логика когда деталь в руках (вращение и сброс)
        if (heldItem != null)
        {
            // --- ВРАЩЕНИЕ ДЕТАЛИ ---
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                // Крутим по горизонтали на 90 градусов
                heldItem.transform.Rotate(0, 90f, 0, Space.World);
            }
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                // Крутим по вертикали (кувырок) на 90 градусов
                heldItem.transform.Rotate(90f, 0, 0, Space.World);
            }

            // Добавляем подсказки в UI
            if (!lookingAtInteractable)
            {
                playerUI.UpdateCenterPrompt($"E - Положить | R, F - Вращать\n[{heldItem.itemType}] {heldItem.data.partName}");

                if (inputManager.onFoot.Interact.triggered)
                {
                    PlaceHeldItem();
                }
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

        // --- УМНЫЙ ХВАТ ---
        // Если это Корпус - отодвигаем точку хвата на полметра вперед
        if (heldItem.itemType == ItemType.Case)
        {
            holdPosition.localPosition = originalHoldLocalPos + new Vector3(0, 0, 0.5f);
        }
        else
        {
            // Возвращаем в стандартное положение для мелких деталей
            holdPosition.localPosition = originalHoldLocalPos;
        }
    }

    private void PlaceHeldItem()
    {
        heldItem.transform.SetParent(null);
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distance))
            heldItem.transform.position = hit.point + hit.normal * 0.2f; 
        else
            heldItem.transform.position = cam.transform.position + cam.transform.forward * (distance - 0.5f);

        heldItem.SetPhysics(true); 
        heldItem = null; 

        // Обязательно возвращаем руки на место, когда бросили предмет!
        holdPosition.localPosition = originalHoldLocalPos;
    }

    // Метод для вывода ВЕРХНИХ уведомлений (цвет задаем прямо в тексте при вызове)
    public void ShowTempMessage(string message, float duration = 2f)
    {
        playerUI.UpdateTopNotification(message);
        notificationTimer = duration;
    }
}