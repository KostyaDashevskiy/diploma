using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [SerializeField] private UIDocument uiDoc;
    public InputManager playerInputManager;

    private VisualElement pauseContainer;
    private Label titleText; // Ссылка на наш заголовок
    private Button btnMainMenu;
    private Button btnQuit;

    public bool isPaused = false;
    private bool isVictory = false; // Флаг, чтобы Esc не закрывал экран победы

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
        
        pauseContainer = root.Q<VisualElement>("PauseContainer");
        titleText = root.Q<Label>("TitleText"); // Находим текст
        btnMainMenu = root.Q<Button>("Btn_MainMenu");
        btnQuit = root.Q<Button>("Btn_Quit");

        if (btnMainMenu != null) btnMainMenu.clicked += GoToMainMenu;
        if (btnQuit != null) btnQuit.clicked += QuitGame;
    }

    public void TogglePause()
    {
        if (isVictory) return; // Если мы победили, паузу уже нельзя снять

        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseContainer.style.display = DisplayStyle.Flex;
            titleText.text = "ПАУЗА"; // Стандартный текст
            titleText.style.color = new StyleColor(Color.white);

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            if (playerInputManager != null) playerInputManager.onFoot.Disable();
        }
        else
        {
            Time.timeScale = 1f;
            pauseContainer.style.display = DisplayStyle.None;

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            if (playerInputManager != null) playerInputManager.onFoot.Enable();
        }
    }

    // НОВЫЙ МЕТОД ДЛЯ ОБУЧАЮЩЕГО РЕЖИМА
    public void ShowVictoryScreen()
    {
        isVictory = true;
        Time.timeScale = 0f; // Останавливаем игру
        
        pauseContainer.style.display = DisplayStyle.Flex;
        
        // Меняем текст и цвет
        titleText.text = "ВСЕ ПРОБЛЕМЫ УСТРАНЕНЫ!";
        titleText.style.color = new StyleColor(new Color(0.18f, 0.8f, 0.44f)); // Зеленый цвет

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        if (playerInputManager != null) playerInputManager.onFoot.Disable();
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("mainmenu");
    }

    private void QuitGame()
    {
        Application.Quit(); 
    }
}