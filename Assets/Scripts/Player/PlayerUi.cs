using UnityEngine;
using TMPro;

public class PlayerUi : MonoBehaviour
{
    [Header("Текстовые панели (перетащить с Canvas)")]
    [SerializeField] private TextMeshProUGUI centerPromptText;     // Текст по центру (Прицел)
    [SerializeField] private TextMeshProUGUI bottomStatsText;      // Текст слева снизу (Характеристики)
    [SerializeField] private TextMeshProUGUI topNotificationText;  // Текст сверху (Ошибки/Успехи)

    public void UpdateCenterPrompt(string msg) 
    { 
        if (centerPromptText != null) centerPromptText.text = msg; 
    }

    public void UpdateBottomStats(string msg) 
    { 
        if (bottomStatsText != null) bottomStatsText.text = msg; 
    }

    public void UpdateTopNotification(string msg) 
    { 
        if (topNotificationText != null) topNotificationText.text = msg; 
    }
}