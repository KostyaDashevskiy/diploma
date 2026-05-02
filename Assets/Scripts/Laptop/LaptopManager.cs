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
    private VisualElement questView;
    private Label txtSystemStats;

    // Элементы квестов
    private Label txtQuestTitle;
    private Label txtQuestDesc;
    private Label txtQuestFeedback;
    private Label txtGlobalRating;
    private Button btnAcceptQuest;
    private Button btnCompleteQuest;
    private ScrollView historyScroll;

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
        questView = root.Q<VisualElement>("QuestView");
        txtSystemStats = root.Q<Label>("Txt_SystemStats");

        // Рабочий стол
        Button btnShop = root.Q<Button>("Btn_ShopApp");
        if (btnShop != null) btnShop.clicked += OpenShopApp;

        Button btnStatus = root.Q<Button>("Btn_StatusApp");
        if (btnStatus != null) btnStatus.clicked += OpenStatusApp;

        Button btnQuest = root.Q<Button>("Btn_QuestApp");
        if (btnQuest != null) btnQuest.clicked += OpenQuestApp;

        // Закрытие окон
        Button btnCloseStatus = root.Q<Button>("Btn_CloseStatus");
        if (btnCloseStatus != null) btnCloseStatus.clicked += CloseAppToDesktop;

        Button btnCloseQuest = root.Q<Button>("Btn_CloseQuest");
        if (btnCloseQuest != null) btnCloseQuest.clicked += CloseAppToDesktop;

        // Квесты
        txtQuestTitle = root.Q<Label>("Txt_ActiveQuestTitle");
        txtQuestDesc = root.Q<Label>("Txt_ActiveQuestDesc");
        txtQuestFeedback = root.Q<Label>("Txt_QuestFeedback");
        txtGlobalRating = root.Q<Label>("Txt_GlobalRating");
        btnAcceptQuest = root.Q<Button>("Btn_AcceptQuest");
        btnCompleteQuest = root.Q<Button>("Btn_CompleteQuest");
        historyScroll = root.Q<ScrollView>("HistoryScroll");

        if (btnAcceptQuest != null) btnAcceptQuest.clicked += OnAcceptQuestClicked;
        if (btnCompleteQuest != null) btnCompleteQuest.clicked += OnCompleteQuestClicked;
    }

    public void OpenLaptop()
    {
        isOpen = true;
        laptopContainer.style.display = DisplayStyle.Flex;
        CloseAppToDesktop(); 

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

    private void OpenShopApp()
    {
        laptopContainer.style.display = DisplayStyle.None; 
        if (InventoryManager.Instance != null) InventoryManager.Instance.Open();
    }

    private void OpenStatusApp()
    {
        desktopView.style.display = DisplayStyle.None;
        statusView.style.display = DisplayStyle.Flex;
        GenerateReport();
    }

    // --- ЛОГИКА БИРЖИ ЗАКАЗОВ ---
    private void OpenQuestApp()
    {
        desktopView.style.display = DisplayStyle.None;
        questView.style.display = DisplayStyle.Flex;
        RefreshQuestUI();
    }

    private void OnAcceptQuestClicked()
    {
        if (QuestManager.Instance == null) return;
        QuestManager.Instance.AcceptRandomQuest();
        RefreshQuestUI();
    }

    private void OnCompleteQuestClicked()
    {
        if (QuestManager.Instance == null) return;
        
        int stars;
        string result = QuestManager.Instance.TryCompleteQuest(out stars);

        if (result == "SUCCESS")
        {
            RefreshQuestUI();
            txtQuestFeedback.text = $"<color=green>Заказ сдан! Вы получили {stars} ⭐</color>";
        }
        else
        {
            txtQuestFeedback.text = result; // Выводим ошибку (не включается)
        }
    }

    private void RefreshQuestUI()
    {
        txtQuestFeedback.text = "";
        txtGlobalRating.text = $"Рейтинг: {QuestManager.Instance.GetAverageRating():F1} ⭐";

        if (QuestManager.Instance.hasActiveQuest)
        {
            txtQuestTitle.text = "АКТИВНЫЙ ЗАКАЗ";
            txtQuestDesc.text = QuestManager.Instance.GetQuestDescription();
            btnAcceptQuest.style.display = DisplayStyle.None;
            btnCompleteQuest.style.display = DisplayStyle.Flex;
        }
        else
        {
            txtQuestTitle.text = "НЕТ АКТИВНЫХ ЗАКАЗОВ";
            txtQuestDesc.text = "Нажмите 'Найти заказ', чтобы получить новую работу и системный блок для починки.";
            btnAcceptQuest.style.display = DisplayStyle.Flex;
            btnCompleteQuest.style.display = DisplayStyle.None;
        }

        // Обновляем историю
        historyScroll.Clear();
        foreach (string log in QuestManager.Instance.historyLogs)
        {
            Label l = new Label(log);
            l.style.fontSize = 16;
            l.style.marginBottom = 10;
            l.style.borderBottomWidth = 1;
            l.style.borderBottomColor = Color.gray;
            l.style.whiteSpace = WhiteSpace.Normal;
            historyScroll.Add(l);
        }
    }

    public void ReturnToDesktop()
    {
        laptopContainer.style.display = DisplayStyle.Flex;
        desktopView.style.display = DisplayStyle.Flex;
        statusView.style.display = DisplayStyle.None;
        questView.style.display = DisplayStyle.None;
    }

    private void CloseAppToDesktop()
    {
        desktopView.style.display = DisplayStyle.Flex;
        statusView.style.display = DisplayStyle.None;
        questView.style.display = DisplayStyle.None;
    }

    // ... (Метод GenerateReport для "Состояния сборки" остается без изменений, я его не копирую для экономии места)
    private void GenerateReport()
    {
        string report = "";
        PCCase[] allCases = FindObjectsByType<PCCase>(FindObjectsSortMode.None).OrderBy(c => c.spawnTime).ToArray();

        if (allCases.Length == 0)
        {
            txtSystemStats.text = "<i>Системные блоки не обнаружены.</i>";
            return;
        }

        int pcCount = 1;
        foreach (PCCase pcCase in allCases)
        {
            report += $"<b>=== СИСТЕМНЫЙ БЛОК #{pcCount} ({pcCase.data.partName}) ===</b>\n\n";
            float totalTDP = 0f, psuPower = 0f;
            PickupItem[] attachedParts = pcCase.GetComponentsInChildren<PickupItem>();
            bool isEmpty = true;
            foreach (PickupItem part in attachedParts)
            {
                if (part.itemType == ItemType.Case) continue; 
                isEmpty = false;
                report += $"• <b>{part.itemType}</b>: {part.data.partName}\n";
                if (part.itemType == ItemType.Motherboard)
                {
                    report += "    <size=18><i>Поддерживаемые слоты:</i>\n";
                    if (part.data.ram_slots > 0) report += $"    <size=18><i>- Слот ОЗУ (DIMM): {part.data.ram_slots} x {part.data.ram_type}</i>\n";
                    Slot[] otherSlots = part.GetComponentsInChildren<Slot>();
                    foreach (Slot slot in otherSlots)
                    {
                        if (slot.acceptableSocket == "DDR4" || slot.acceptableSocket == "DDR5") continue;
                        report += $"    <size=18><i>- {slot.slotName}: {slot.acceptableSocket}</i>\n";
                    }
                    report += "</size>";
                }
                if (part.itemType == ItemType.PowerSupply) psuPower = part.data.tdp;
                else totalTDP += part.data.tdp;
            }

            if (isEmpty) report += "<i>Корпус пуст.</i>\n";
            else
            {
                report += $"\n<b>ЭНЕРГОБАЛАНС СБОРКИ #{pcCount}:</b>\nСуммарное потребление: <b>{totalTDP} W</b>\nМощность БП: <b>{psuPower} W</b>\n";
                float req = totalTDP * 1.2f; 
                if (psuPower >= req) report += $"<color=green>✓ Энергобаланс в норме (БП с запасом)</color>\n";
                else report += $"<color=red>⚠ ОШИБКА: Требуется БП от {req}W</color>\n";
            }
            report += "\n\n"; 
            pcCount++;
        }
        txtSystemStats.text = report;
    }
}