// using System.Collections;
// using System.Collections.Generics;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryManager : MonoBehaviour
{
   [SerializeField] private UIDocument uiDoc;
   private VisualElement rootEl;
   private VisualElement inventoryEl;
   private string activeClass = "InventoryPanel-active";
   private bool isOpen = false;

   private void OnEnable()
    {
        rootEl = uiDoc.rootVisualElement;
        inventoryEl = rootEl.Q(className: "InventoryPanel");
    
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!isOpen)
            {
                Open();
                isOpen = true;
            }
            else
            {
                Close();
                isOpen = false;
            }
            
        }
        // if ((Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Escape)) && isOpen == true)
        // {
        //     Close();
        //     isOpen = false;
        // }
    }
    private void Open()
    {
        inventoryEl.AddToClassList(activeClass);
    }

    private void Close()
    {
        inventoryEl.RemoveFromClassList(activeClass);
    }
   
}
