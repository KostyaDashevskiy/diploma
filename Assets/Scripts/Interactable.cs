using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string promtMessage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void BaseInteract()
    {
        Interact();
    }
    protected virtual void Interact()
    {
        
    }
}
