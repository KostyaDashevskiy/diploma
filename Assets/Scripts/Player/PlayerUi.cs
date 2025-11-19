using UnityEngine;
using TMPro;

public class PlayerUi : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI promtText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    public void UpdateText(string promtMessage)  
    {
        promtText.text = promtMessage;
    }
}
