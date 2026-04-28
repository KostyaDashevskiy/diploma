using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private UIDocument uiDoc;
    public InputManager playerInputManager;
    public Transform spawnPoint;

    private VisualElement rootEl;
    private VisualElement inventoryEl;
    private VisualElement gridEl;
    private List<Button> categoryButtons = new List<Button>();
    
    // Элементы панели теории
    private VisualElement theoryPanel;
    private Label theoryTitle;
    private Label theoryText;
    private Button theoryToggleBtn;
    
    public bool isOpen = false; 
    private bool isTheoryOpen = false;
    private string currentCategoryPrefix = "";

    // База теории
    private Dictionary<string, string> theoryDatabase = new Dictionary<string, string>()
    {
        { "", "Добро пожаловать в конфигуратор!\n\nЗдесь представлены все доступные компоненты ПК. Используйте фильтры слева, чтобы найти конкретную деталь.\n\nОбращайте внимание на Сокеты и TDP (тепловыделение) для обеспечения совместимости." },
        { "cpu", "<b>Центральный процессор (CPU)</b>\n\nМозг компьютера. Выполняет основные вычислительные операции.\n\n<b>Важно:</b> Процессор должен строго совпадать с сокетом (разъемом) на Материнской плате (например, LGA1700 или AM5). Также требует наличия системы охлаждения." },
        { "mb", "<b>Материнская плата (MB)</b>\n\nСвязующее звено всех компонентов. Обеспечивает питание и передачу данных между деталями.\n\n<b>Важно:</b> Определяет, какой процессор (Socket) и оперативную память (DDR4/DDR5) вы сможете установить." },
        { "gpu", "<b>Видеокарта (GPU)</b>\n\nОтвечает за рендеринг графики и 3D-сцен.\n\n<b>Важно:</b> Это самый энергопотребляющий компонент. Перед покупкой убедитесь, что мощности вашего Блока Питания хватит, а длина видеокарты физически поместится в Корпус." },
        { "ram", "<b>Оперативная память (RAM)</b>\n\nВременное хранилище данных, необходимых процессору прямо сейчас.\n\n<b>Важно:</b> Существуют разные поколения (DDR4, DDR5). Материнская плата поддерживает только одно конкретное поколение." },
        { "cooler", "<b>Система охлаждения (FAN)</b>\n\nОтводит тепло от процессора, не давая ему сгореть.\n\n<b>Важно:</b> Кулер должен иметь крепления под сокет вашего процессора. А высота башни кулера не должна превышать ширину вашего Корпуса." },
        { "psu", "<b>Блок питания (PSU)</b>\n\nОбеспечивает электричеством все компоненты ПК.\n\n<b>Важно:</b> Мощность БП должна перекрывать сумму TDP (потребления) всех компонентов системы минимум на 20%, чтобы компьютер работал стабильно." },
        { "case", "<b>Компьютерный корпус (CASE)</b>\n\nЗащищает детали от повреждений и обеспечивает циркуляцию воздуха.\n\n<b>Важно:</b> Ограничивает размер комплектующих. Учитывайте максимальную длину видеокарты и высоту кулера при выборе." }
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        rootEl = uiDoc.rootVisualElement;
        if (rootEl == null) return;

        inventoryEl = rootEl.Q(className: "InventoryPanel");
        gridEl = rootEl.Q("Grid");

        Button btnBack = rootEl.Q<Button>("Btn_BackToLaptop");
        if (btnBack != null) btnBack.clicked += BackToLaptop;

        // Инициализация Теории
        theoryPanel = rootEl.Q("TheoryPanel");
        theoryTitle = rootEl.Q<Label>("TheoryTitle");
        theoryText = rootEl.Q<Label>("TheoryText");
        theoryToggleBtn = rootEl.Q<Button>("Btn_TheoryToggle");
        
        if (theoryToggleBtn != null) theoryToggleBtn.clicked += ToggleTheoryPanel;

        SetupCategoryButton(rootEl, "Btn_All", "");       
        SetupCategoryButton(rootEl, "Btn_CPU", "cpu");    
        SetupCategoryButton(rootEl, "Btn_MB", "mb");
        SetupCategoryButton(rootEl, "Btn_GPU", "gpu");
        SetupCategoryButton(rootEl, "Btn_RAM", "ram");
        SetupCategoryButton(rootEl, "Btn_Cooler", "cooler");
        SetupCategoryButton(rootEl, "Btn_PSU", "psu");
        SetupCategoryButton(rootEl, "Btn_Case", "case");
    }

    private void Start()
    {
        GenerateCatalogUI(""); 
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen) CloseCompletely();
            else if (LaptopManager.Instance != null && LaptopManager.Instance.isOpen) LaptopManager.Instance.CloseLaptop();
            else if (PauseMenu.Instance != null) PauseMenu.Instance.TogglePause();
        }
    }

    // --- ЛОГИКА ТЕОРИИ ---
    private void ToggleTheoryPanel()
    {
        isTheoryOpen = !isTheoryOpen;
        if (isTheoryOpen)
        {
            theoryPanel.AddToClassList("active");
            theoryToggleBtn.AddToClassList("active");
            UpdateTheoryContent();
        }
        else
        {
            HideTheoryPanel();
        }
    }

    private void HideTheoryPanel()
    {
        isTheoryOpen = false;
        if (theoryPanel != null) theoryPanel.RemoveFromClassList("active");
        if (theoryToggleBtn != null) theoryToggleBtn.RemoveFromClassList("active");
    }

    private void UpdateTheoryContent()
    {
        if (!isTheoryOpen) return;

        string title = "Информация";
        switch (currentCategoryPrefix)
        {
            case "cpu": title = "О Процессорах"; break;
            case "mb": title = "О Мат. платах"; break;
            case "gpu": title = "О Видеокартах"; break;
            case "ram": title = "Об ОЗУ"; break;
            case "cooler": title = "Об Охлаждении"; break;
            case "psu": title = "О Блоках питания"; break;
            case "case": title = "О Корпусах"; break;
        }
        theoryTitle.text = title;
        if (theoryDatabase.ContainsKey(currentCategoryPrefix)) theoryText.text = theoryDatabase[currentCategoryPrefix];
    }
    // ---------------------

    private void SetupCategoryButton(VisualElement root, string btnName, string prefixFilter)
    {
        Button btn = root.Q<Button>(btnName);
        if (btn != null)
        {
            categoryButtons.Add(btn);
            btn.clicked += () => 
            {
                currentCategoryPrefix = prefixFilter;
                SetActiveButton(btn);
                GenerateCatalogUI(prefixFilter);
                UpdateTheoryContent();
            };
        }
    }

    private void SetActiveButton(Button activeBtn)
    {
        foreach (Button btn in categoryButtons) btn.RemoveFromClassList("active");
        activeBtn.AddToClassList("active");
    }

    private void GenerateCatalogUI(string prefixFilter)
    {
        if (gridEl == null || AssemblyManager.Instance == null) return;
        gridEl.Clear(); 

        foreach (KeyValuePair<string, PartData> entry in AssemblyManager.Instance.database)
        {
            PartData part = entry.Value;
            if (!string.IsNullOrEmpty(prefixFilter) && !part.partID.StartsWith(prefixFilter)) continue;

            VisualElement card = new VisualElement();
            card.AddToClassList("card");

            VisualElement image = new VisualElement();
            image.AddToClassList("card-image");
            Sprite partSprite = Resources.Load<Sprite>($"PartImages/{part.partID}");
            if (partSprite != null) image.style.backgroundImage = new StyleBackground(partSprite);

            VisualElement info = new VisualElement();
            info.AddToClassList("card-info");
            Label title = new Label(part.partName);
            title.AddToClassList("card-title");
            info.Add(title);

            Label desc = new Label($"Сокет: {part.socketType}\nTDP: {part.tdp} W");
            desc.AddToClassList("card-desc");
            info.Add(desc);

            Button orderBtn = new Button { text = "ЗАКАЗАТЬ" };
            orderBtn.AddToClassList("order-button");
            orderBtn.clicked += () => OrderItem(part.partID);
            info.Add(orderBtn);

            card.Add(image);
            card.Add(info);
            gridEl.Add(card);
        }
    }

    private void OrderItem(string partID)
    {
        GameObject prefabToSpawn = Resources.Load<GameObject>($"Prefabs/{partID}");
        if (prefabToSpawn != null && spawnPoint != null)
        {
            Vector3 finalPos = spawnPoint.position;
            if (Physics.OverlapSphere(finalPos, 0.2f).Length > 0) finalPos += new Vector3(0.2f, 0.2f, 0f);
            Instantiate(prefabToSpawn, finalPos, spawnPoint.rotation);
            
            BackToLaptop(); 
        }
    }

    public void Open()
    {
        if (inventoryEl == null) return;
        inventoryEl.AddToClassList("InventoryPanel-active");
        isOpen = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        if (playerInputManager != null) playerInputManager.onFoot.Disable();
    }

    public void BackToLaptop()
    {
        if (inventoryEl == null) return;
        inventoryEl.RemoveFromClassList("InventoryPanel-active");
        isOpen = false;
        HideTheoryPanel(); // <-- ЗАКРЫВАЕМ ТЕОРИЮ ПРИ ВОЗВРАТЕ
        if (LaptopManager.Instance != null) LaptopManager.Instance.ReturnToDesktop();
    }

    public void CloseCompletely()
    {
        if (inventoryEl == null) return;
        inventoryEl.RemoveFromClassList("InventoryPanel-active");
        isOpen = false;
        HideTheoryPanel(); // <-- ЗАКРЫВАЕМ ТЕОРИЮ ПРИ ВЫХОДЕ НА ESC
        if (LaptopManager.Instance != null) LaptopManager.Instance.CloseLaptop();
    }
}