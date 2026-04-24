using UnityEngine;
using UnityEngine.InputSystem; // Добавили для работы с кнопкой Q

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private float distance = 3f;[SerializeField] private LayerMask mask;

    [Header("Настройки инвентаря/рук")]
    public Transform holdPosition; 
    public PickupItem heldItem;    
    
    private PlayerUi playerUI;
    private InputManager inputManager;

    private string tempMessage = "";
    private float tempMessageTimer = 0f;

    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        playerUI = GetComponent<PlayerUi>();
        inputManager = GetComponent<InputManager>();
    }

    void Update()
    {
        if (tempMessageTimer > 0)
        {
            tempMessageTimer -= Time.deltaTime;
            playerUI.UpdateText(tempMessage);
        }
        else
        {
            playerUI.UpdateText(string.Empty);
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInfo;
        bool lookingAtInteractable = false;

        if (Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                lookingAtInteractable = true;
                
                if (tempMessageTimer <= 0) 
                {
                    playerUI.UpdateText(interactable.GetPromptMessage(this));
                }

                // Логика установки / Взятия предмета (на E)
                if (inputManager.onFoot.Interact.triggered)
                {
                    interactable.BaseInteract(this);
                }

                // НОВАЯ ЛОГИКА ИЗВЛЕЧЕНИЯ (на Q)
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    PickupItem pItem = interactable as PickupItem;
                    if (pItem != null)
                    {
                        pItem.Extract(this);
                    }
                }
            }
        }

        if (!lookingAtInteractable && heldItem != null)
        {
            if (tempMessageTimer <= 0)
            {
                playerUI.UpdateText("E - Положить " + heldItem.data.partName);
            }

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
        {
            heldItem.transform.position = hit.point + hit.normal * 0.1f; 
        }
        else
        {
            heldItem.transform.position = cam.transform.position + cam.transform.forward * (distance - 0.5f);
        }

        heldItem.transform.rotation = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0);
        heldItem.SetPhysics(true); 
        heldItem = null; 
    }

    public void ShowTempMessage(string message, float duration = 2f)
    {
        tempMessage = "<color=red>" + message + "</color>";
        tempMessageTimer = duration;
    }
}