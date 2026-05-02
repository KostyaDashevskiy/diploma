using UnityEngine;
using UnityEngine.InputSystem; // Добавили для чтения колесика мыши

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public float xRotation = 0f;

    public float xSensitivity = 15f;
    public float ySensitivity = 15f;

    [Header("Настройки Зума")]
    public float normalFOV = 60f; // Обычный угол обзора
    public float maxZoomFOV = 20f; // Максимальное приближение
    public float zoomSpeed = 10f;  // Скорость плавного приближения
    
    private float targetFOV;

    void Start()
    {
        targetFOV = normalFOV;
        if (cam != null) cam.fieldOfView = normalFOV;
    }

    void Update()
    {
        // Читаем колесико мыши (только если игра не на паузе и инвентарь закрыт)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;

            if (scrollY > 0) targetFOV = maxZoomFOV;      // Крутим вперед - приближаем
            else if (scrollY < 0) targetFOV = normalFOV;  // Крутим назад - отдаляем
        }

        // Плавное изменение зума (Lerp)
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;
        
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
    }
}