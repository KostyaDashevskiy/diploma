using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; // Нужно для загрузки сцен

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [SerializeField] private UIDocument uiDoc;
    public InputManager playerInputManager;

    private VisualElement pauseContainer;
    private Button btnMainMenu;
    private Button btnQuit;

    public bool isPaused = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        VisualElement root = uiDoc.rootVisualElement;
        pauseContainer = root.Q<VisualElement>("PauseContainer");
        
        btnMainMenu = root.Q<Button>("Btn_MainMenu");
        btnQuit = root.Q<Button>("Btn_Quit");

        // Привязываем кнопки к методам
        if (btnMainMenu != null) btnMainMenu.clicked += GoToMainMenu;
        if (btnQuit != null) btnQuit.clicked += QuitGame;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // ОСТАНАВЛИВАЕМ ИГРУ
            Time.timeScale = 0f;
            pauseContainer.style.display = DisplayStyle.Flex; // Показываем UI

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            if (playerInputManager != null) playerInputManager.onFoot.Disable();
        }
        else
        {
            // ВОЗОБНОВЛЯЕМ ИГРУ
            Time.timeScale = 1f;
            pauseContainer.style.display = DisplayStyle.None; // Скрываем UI

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            if (playerInputManager != null) playerInputManager.onFoot.Enable();
        }
    }

    private void GoToMainMenu()
    {
        // ВАЖНО: Возвращаем время в норму перед загрузкой, иначе главное меню тоже "зависнет"
        Time.timeScale = 1f; 
        SceneManager.LoadScene("mainmenu");
    }

    private void QuitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit(); // Закроет игру в сбилженном (.exe) варианте
    }
}