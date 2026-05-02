using UnityEngine;
using UnityEngine.SceneManagement; // Обязательно для смены сцен

public class MainMenuManager : MonoBehaviour
{
    // Метод для запуска первого режима
    public void PlayMode1()
    {
        SceneManager.LoadScene("SampleScene"); // Укажи точное название своей сцены
    }

    // Метод для запуска второго режима
    public void PlayMode2()
    {
        SceneManager.LoadScene("LearningMode"); // Укажи точное название своей сцены
    }

    // Метод для выхода из игры
    public void ExitGame()
    {
        Debug.Log("Игра закрывается..."); // Будет видно только в консоли редактора
        Application.Quit(); // Закроет скомпилированную игру
    }
}