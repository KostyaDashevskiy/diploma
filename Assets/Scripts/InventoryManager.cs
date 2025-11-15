using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public bool isOpened;
    public GameObject UIPanel;

    void Start()
    {
        UIPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            isOpened = !isOpened;
            if (isOpened)
            {
                UIPanel.SetActive(true);
                // crosshair.SetActive(false);
            }
            else
            {
                UIPanel.SetActive(false);
                // crosshair.SetActive(true);
            }
        }
    }
}
