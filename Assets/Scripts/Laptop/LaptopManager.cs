using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class LaptopManager : MonoBehaviour
{
    public static LaptopManager Instance { get; private set; }

    [SerializeField] private UIDocument uiDoc;
    public InputManager playerInputManager;

    private VisualElement laptopContainer;
    private VisualElement desktopView;
    private VisualElement statusView;
    private Label txtSystemStats;

    public bool isOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        Invoke(nameof(SetupUI), 0.1f);
    }

    private void SetupUI()
    {
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        VisualElement root = uiDoc.rootVisualElement;
        
        laptopContainer = root.Q<VisualElement>("LaptopContainer");
        desktopView = root.Q<VisualElement>("DesktopView");
        statusView = root.Q<VisualElement>("StatusView");
        txtSystemStats = root.Q<Label>("Txt_SystemStats");

        // Привязываем ярлыки на рабочем столе
        Button btnShop = root.Q<Button>("Btn_ShopApp");
        if (btnShop != null) btnShop.clicked += OpenShopApp;

        Button btnStatus = root.Q<Button>("Btn_StatusApp");
        if (btnStatus != null) btnStatus.clicked += OpenStatusApp;

        // Кнопка закрытия окна статуса
        Button btnCloseStatus = root.Q<Button>("Btn_CloseStatus");
        if (btnCloseStatus != null) btnCloseStatus.clicked += CloseAppToDesktop;
    }

    public void OpenLaptop()
    {
        isOpen = true;
        laptopContainer.style.display = DisplayStyle.Flex;
        CloseAppToDesktop(); // Всегда начинаем с рабочего стола

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        if (playerInputManager != null) playerInputManager.onFoot.Disable();
    }

    public void CloseLaptop()
    {
        isOpen = false;
        laptopContainer.style.display = DisplayStyle.None;

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        if (playerInputManager != null) playerInputManager.onFoot.Enable();
    }

    // --- ПРИЛОЖЕНИЕ: МАГАЗИН ---
    private void OpenShopApp()
    {
        // Скрываем ноутбук и открываем наш старый добрый InventoryManager
        laptopContainer.style.display = DisplayStyle.None; 
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.Open();
        }
    }

    // --- ПРИЛОЖЕНИЕ: СОСТОЯНИЕ ПК ---
    private void OpenStatusApp()
    {
        desktopView.style.display = DisplayStyle.None;
        statusView.style.display = DisplayStyle.Flex;
        GenerateReport();
    }

    private void CloseAppToDesktop()
    {
        desktopView.style.display = DisplayStyle.Flex;
        statusView.style.display = DisplayStyle.None;
    }

    // --- ГЕНЕРАЦИЯ ОТЧЕТА О СБОРКЕ ---
    private void GenerateReport()
    {
        string report = "";
        
        // Сканируем всю сцену на наличие Корпусов
        // Ищем все Корпуса и СОРТИРУЕМ их по времени спавна (от старых к новым)
        PCCase[] allCases = FindObjectsByType<PCCase>(FindObjectsSortMode.None)
                            .OrderBy(c => c.spawnTime).ToArray();

        if (allCases.Length == 0)
        {
            txtSystemStats.text = "<i>Системные блоки не обнаружены. Закажите корпус для начала сборки.</i>";
            return;
        }

        int pcCount = 1;
        foreach (PCCase pcCase in allCases)
        {
            report += $"<b>=== СИСТЕМНЫЙ БЛОК #{pcCount} ({pcCase.data.partName}) ===</b>\n\n";
            
            float totalTDP = 0f;
            float psuPower = 0f;
            
            // Ищем все детали, лежащие внутри этого конкретного корпуса
            PickupItem[] attachedParts = pcCase.GetComponentsInChildren<PickupItem>();
            
            bool isEmpty = true;
            foreach (PickupItem part in attachedParts)
            {
                if (part.itemType == ItemType.Case) continue; // Пропускаем сам корпус
                
                isEmpty = false;
                report += $"• <b>{part.itemType}</b>: {part.data.partName}\n";
                report += $"  <size=18><color=#7f8c8d>Сокет: {part.data.socketType} | TDP: {part.data.tdp}W | Габариты: {part.data.length}x{part.data.width}x{part.data.height}мм</color></size>\n";
                
                // --- НОВАЯ ЛОГИКА: ДЕТАЛЬНЫЙ ОТЧЕТ ПО МАТЕРИНСКОЙ ПЛАТЕ ---
                if (part.itemType == ItemType.Motherboard)
                {
                    report += "    <size=18><i>Поддерживаемые слоты:</i>\n";
                    
                    // Выводим информацию из JSON
                    if (part.data.ram_slots > 0)
                    {
                        report += $"    <size=18><i>- Слот ОЗУ (DIMM): {part.data.ram_slots} x {part.data.ram_type}</i>\n";
                    }

                    // Ищем остальные слоты (CPU, GPU и т.д.)
                    Slot[] otherSlots = part.GetComponentsInChildren<Slot>();
                    foreach (Slot slot in otherSlots)
                    {
                        // Пропускаем слоты ОЗУ, так как мы их уже сгруппировали
                        if (slot.acceptableSocket == "DDR4" || slot.acceptableSocket == "DDR5") continue;
                        
                        report += $"    <size=18><i>- {slot.slotName}: {slot.acceptableSocket}</i>\n";
                    }
                    report += "</size>";
                }
                // -----------------------------------------------------------------

                if (part.itemType == ItemType.PowerSupply) psuPower = part.data.tdp;
                else totalTDP += part.data.tdp;
            }

            if (isEmpty)
            {
                report += "<i>Корпус пуст.</i>\n";
            }
            else
            {
                report += $"\n<b>ЭНЕРГОБАЛАНС СБОРКИ #{pcCount}:</b>\n";
                report += $"Суммарное потребление: <b>{totalTDP} W</b>\n";
                report += $"Мощность Блока Питания: <b>{psuPower} W</b>\n";

                float requiredPower = totalTDP * 1.2f; // Запас 20%
                if (psuPower >= requiredPower)
                {
                    report += $"<color=green>✓ Энергобаланс в норме (БП с запасом)</color>\n";
                }
                else
                {
                    report += $"<color=red>⚠ ОШИБКА: Требуется БП от {requiredPower}W</color>\n";
                }
            }
            report += "\n\n"; // Отступ между разными ПК
            pcCount++;
        }

        txtSystemStats.text = report;
    }

    public void ReturnToDesktop()
    {
        laptopContainer.style.display = DisplayStyle.Flex;
        desktopView.style.display = DisplayStyle.Flex;
        statusView.style.display = DisplayStyle.None;
    }
}