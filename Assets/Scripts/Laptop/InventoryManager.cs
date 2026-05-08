using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private UIDocument uiDoc;
    public InputManager playerInputManager;
    public Transform spawnPoint;

    [Header("База теории (JSON)")]
    public TextAsset theoryJsonFile; // СЮДА ПЕРЕТАЩИТЬ TheoryDB.json В ИНСПЕКТОРЕ

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
    private Dictionary<string, string> theoryDatabase = new Dictionary<string, string>();
    private Dictionary<string, string> theoryTitles = new Dictionary<string, string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (theoryJsonFile != null)
        {
            TheoryDatabase db = JsonUtility.FromJson<TheoryDatabase>(theoryJsonFile.text);
            foreach (TheoryEntry entry in db.entries)
            {
                theoryDatabase[entry.category] = entry.text;
                theoryTitles[entry.category] = entry.title;
            }
        }
        else
        {
            Debug.LogError("Файл TheoryDB.json не назначен в InventoryManager!");
        }
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
        // Не рисуем UI сразу. Ждем сигнала от AssemblyManager!
        StartCoroutine(WaitForDatabase());
    }

     private System.Collections.IEnumerator WaitForDatabase()
    {
        // Ждем, пока AssemblyManager не скажет, что база полностью скачана и распарсена
        while (AssemblyManager.Instance == null || !AssemblyManager.Instance.isDatabaseReady)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Как только база готова - генерируем стартовую вкладку "ВСЕ" (пустой префикс)
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

        // Берем заголовок из словаря (если есть), иначе дефолтный
        if (theoryTitles.ContainsKey(currentCategoryPrefix))
            theoryTitle.text = theoryTitles[currentCategoryPrefix];
        else
            theoryTitle.text = "Информация";

        // Берем текст из словаря (если есть), иначе дефолтный
        if (theoryDatabase.ContainsKey(currentCategoryPrefix))
            theoryText.text = theoryDatabase[currentCategoryPrefix];
        else
            theoryText.text = "Текст не найден в базе данных.";
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
            
            // --- НОВАЯ УМНАЯ ЗАГРУЗКА КАРТИНОК (С ФОЛЛБЕКОМ) ---
            Texture2D partTex = Resources.Load<Texture2D>($"PartImages/{part.partID}");
            
            // Если родной картинки нет, подбираем случайную заглушку из той же категории
            if (partTex == null)
            {
                string fallbackPrefix = "";
                if (part.partID.StartsWith("cpu")) fallbackPrefix = "cpu_";
                else if (part.partID.StartsWith("gpu")) fallbackPrefix = "gpu_";
                else if (part.partID.StartsWith("mb")) fallbackPrefix = "mb_";
                else if (part.partID.StartsWith("ram")) fallbackPrefix = "ram_";
                else if (part.partID.StartsWith("cooler")) fallbackPrefix = "cooler_";
                else if (part.partID.StartsWith("psu")) fallbackPrefix = "psu_";
                else if (part.partID.StartsWith("case")) fallbackPrefix = "case_";

                Texture2D[] allTextures = Resources.LoadAll<Texture2D>("PartImages");
                List<Texture2D> validFallbacks = new List<Texture2D>();

                foreach (var tex in allTextures)
                {
                    if (tex.name.StartsWith(fallbackPrefix)) validFallbacks.Add(tex);
                }

                if (validFallbacks.Count > 0)
                {
                    partTex = validFallbacks[Random.Range(0, validFallbacks.Count)];
                    Debug.Log($"[Магазин] Для детали {part.partID} картинка не найдена. Использована заглушка: {partTex.name}");
                }
            }
            
            if (partTex != null) 
            {
                image.style.backgroundImage = new StyleBackground(partTex);
            }
            else
            {
                Debug.LogWarning($"Картинка не найдена, и заглушек для категории '{part.partID}' тоже нет!");
            }
            // ---------------------------------------------------

            VisualElement info = new VisualElement();
            info.AddToClassList("card-info");
            Label title = new Label(part.partName);
            title.AddToClassList("card-title");
            info.Add(title);

            // --- УМНОЕ ОПИСАНИЕ КАРТОЧКИ ТОВАРА ---
            string descText = "";

            if (part.partID.StartsWith("case") || part.partID.StartsWith("psu"))
            {
                descText += $"<b>Форм-фактор:</b> {part.form_factor}\n";
            }
            else if (part.partID.StartsWith("mb"))
            {
                descText += $"<b>Форм-фактор:</b> {part.form_factor}\n";
                descText += $"<b>Сокет процессора:</b> {part.socketType}\n";
                descText += $"<b>Слоты ОЗУ:</b> {part.ram_slots}x {part.ram_type}\n";
                descText += $"<b>Слот GPU:</b> PCI-Express x16\n";
            }
            else if (part.partID.StartsWith("gpu"))
            {
                descText += $"<b>Интерфейс:</b> {part.socketType}\n"; 
            }
            else if (part.partID.StartsWith("ram"))
            {
                descText += $"<b>Тип памяти:</b> {part.socketType}\n"; 
            }
            else if (part.partID.StartsWith("cooler"))
            {
                descText += $"<b>Поддержка сокета:</b> {part.socketType}\n"; 
            }
            else 
            {
                descText += $"<b>Сокет:</b> {part.socketType}\n"; 
            }

            if (part.tdp > 0) descText += $"<b>Мощность/TDP:</b> {part.tdp} W\n";
            if (part.max_tdp > 0) descText += $"<b>Отвод тепла:</b> {part.max_tdp} W\n";

            if (part.cores > 0) descText += $"<b>Ядра:</b> {part.cores} ({part.frequency} ГГц)\n";
            if (part.vram > 0) descText += $"<b>Видеопамять:</b> {part.vram} ГБ\n";
            if (part.partID.StartsWith("ram")) descText += $"<b>Частота:</b> {part.frequency} МГц\n";

            if (part.length > 0 && part.width > 0)
            {
                if (part.partID.StartsWith("case"))
                    descText += $"<b>Вместимость:</b> GPU до {part.length}мм, Кулер до {part.width}мм";
                else
                    descText += $"<b>Габариты:</b> {part.length}x{part.width}x{part.height} мм";
            }
            else if (part.partID.StartsWith("cooler") && part.height > 0)
            {
                descText += $"<b>Высота кулера:</b> {part.height} мм\n";
            }

            Label desc = new Label(descText);
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

    public void ForceRefreshCatalog()
    {
        // Просто вызываем наш собственный метод генерации UI
        GenerateCatalogUI(currentCategoryPrefix);
    }
    
    // --- НОВЫЙ МЕТОД: ВСТАВИТЬ В КОНЕЦ КЛАССА InventoryManager ---
    // Метод фонового скачивания картинки по ссылке
    private System.Collections.IEnumerator DownloadImageAndApply(string url, VisualElement imageElement)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            // Ждем пока скачается
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                
                if (imageElement != null && downloadedTex != null)
                {
                    imageElement.style.backgroundImage = new StyleBackground(downloadedTex);
                }
            }
            else
            {
                Debug.LogError($"[Магазин] Ошибка скачивания картинки по ссылке {url}: {request.error}");
            }
        }
    }

 private void OrderItem(string partID)
    {
        // 1. ОПРЕДЕЛЯЕМ, КАКУЮ БОЛВАНКУ СПАВНИТЬ
        string prefabName = "";
        
        if (partID.StartsWith("cpu")) prefabName = "Generic_CPU";
        else if (partID.StartsWith("gpu")) prefabName = "Generic_GPU";
        else if (partID.StartsWith("mb")) prefabName = "Generic_Motherboard";
        else if (partID.StartsWith("ram")) prefabName = "Generic_RAM";
        else if (partID.StartsWith("psu")) prefabName = "Generic_PSU";
        else if (partID.StartsWith("cooler")) prefabName = "Generic_Cooler";
        else if (partID.StartsWith("case")) prefabName = "Generic_Case";

        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError($"[Магазин] Не удалось определить тип детали для ID: {partID}");
            return;
        }

        // 2. ЗАГРУЖАЕМ БОЛВАНКУ
        GameObject prefabToSpawn = Resources.Load<GameObject>($"Prefabs/{prefabName}");
        
        if (prefabToSpawn != null && spawnPoint != null)
        {
            // Умный спавн (смещение, если занято)
            Vector3 finalPos = spawnPoint.position;
            if (Physics.OverlapSphere(finalPos, 0.2f).Length > 0) finalPos += new Vector3(0.2f, 0.2f, 0f);
            
            // 3. СПАВНИМ ОБЪЕКТ НА СТОЛЕ
            GameObject spawnedObj = Instantiate(prefabToSpawn, finalPos, spawnPoint.rotation);
            
            // 4. МАГИЯ: ВНЕДРЯЕМ В БОЛВАНКУ ИДЕНТИФИКАТОР ИЗ JSON
            PickupItem pItem = spawnedObj.GetComponent<PickupItem>();
            if (pItem != null)
            {
                pItem.jsonPartID = partID;      // Присваиваем ей купленный ID
                pItem.ReloadDataFromJSON();     // Заставляем её прочитать свои новые характеристики!
                
                // Если это материнская плата - заставляем её слоты обновиться
                if (pItem.itemType == ItemType.Motherboard)
                {
                    pItem.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
                }
            }
            
            BackToLaptop(); 
        }
        else
        {
            Debug.LogError($"Не найден префаб-болванка: Resources/Prefabs/{prefabName}");
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