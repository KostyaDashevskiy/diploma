using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public float xRotation = 0f;

    public float xSensitivity = 15f;
    public float ySensitivity = 15f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;
        
        //расчитываем поворот камеры
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        //применяем эти расчеты для camera transform
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        //поворачиваем игрока для взляда
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
    }
}
