using UnityEngine;

public class LearningModeManager : MonoBehaviour
{
    private PCCase[] allCasesInScene;
    private bool missionCompleted = false;

    void Start()
    {
        InvokeRepeating(nameof(CheckWinCondition), 2f, 2f); // Проверяем раз в 2 секунды
    }

    private void CheckWinCondition()
    {
        if (missionCompleted) return;

        allCasesInScene = FindObjectsByType<PCCase>(FindObjectsSortMode.None);
        if (allCasesInScene.Length == 0) return;

        bool allWorking = true;
        foreach (PCCase pc in allCasesInScene)
        {
            if (!pc.IsFullyAssembledAndWorking())
            {
                allWorking = false;
                break; 
            }
        }

        if (allWorking)
        {
            missionCompleted = true;
            if (PauseMenu.Instance != null) PauseMenu.Instance.ShowVictoryScreen();
        }
    }
}