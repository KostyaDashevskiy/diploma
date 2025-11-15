using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
  [SerializeField] private float rotateSpeed = 75;
  [SerializeField] private float walkSpeed = 7;
  [SerializeField] private float runSpeed = 10;
  [SerializeField] private float jumpForce = 6;
  [SerializeField] private float gravity = -9.81f;

  private CharacterController characterController;
  private Camera playerCamera;

  private Vector3 velocity;
  private Vector2 rotation;
  private Vector2 direction;

  private void Start()
  {
    characterController = GetComponent<CharacterController>();
    playerCamera = GetComponentInChildren<Camera>();
    Cursor.lockState = CursorLockMode.Locked;
  }


  private void Update()
  {
    characterController.Move(velocity * Time.deltaTime);
    direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    if (characterController.isGrounded) velocity.y = Input.GetKeyDown(KeyCode.Space) ? jumpForce : -0.1f;
    else velocity.y += gravity * Time.deltaTime;
    
    mouseDelta *= rotateSpeed * Time.deltaTime;
    rotation.y += mouseDelta.x;
    rotation.x = Mathf.Clamp(rotation.x - mouseDelta.y, -90, 90);
    playerCamera.transform.localEulerAngles = rotation;
  }


  private void FixedUpdate()
  {
    direction *= Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
    Vector3 move = Quaternion.Euler(0, playerCamera.transform.eulerAngles.y, 0) * new Vector3(direction.x, 0, direction.y);
    velocity = new Vector3(move.x, velocity.y, move.z);
  }
}