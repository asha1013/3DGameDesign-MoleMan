using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using NaughtyAttributes;

public class PlayerController : MonoBehaviour
{
    [BoxGroup("Movement")] public float moveSpeed = 2.5f;
    [BoxGroup("Movement")] public float jumpForce = 1.5f;
    [BoxGroup("Movement")] public float mouseSensitivity = 2f;

    [BoxGroup("Ground Detection")] public LayerMask groundLayer;
    [BoxGroup("Ground Detection")] public float groundDistance = 1f; //distance to shoot spherecast down from player origin
    [BoxGroup("Ground Detection")] public float sphereRadius = .3f;

    [BoxGroup("Audio")] public AudioSource playerAudio;
    [BoxGroup("Audio")] public float footstepInterval = .5f;
    [BoxGroup("Audio")] public MMF_Player footstepFeedback;

    [BoxGroup("References")] public Transform cameraTransform;

    [BoxGroup("State")] [ReadOnly] public bool isJumping;
    [BoxGroup("State")] [ReadOnly] public bool isOnGround; //using raycasting as more reliable alternative to isGrounded
    [BoxGroup("State")] [ReadOnly] public bool inTunnel;
    [BoxGroup("State")] [ReadOnly] private bool isStepping;

    private CharacterController controller;
    private Vector3 playerVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        RaycastHit hit; //Hit info for layer detection
        isOnGround = Physics.SphereCast(transform.position, sphereRadius, Vector3.down, out hit, groundDistance, groundLayer);

        // Set inTunnel flag when touching ground with Tunnel tag
        if (isOnGround)
        {
            if (hit.collider != null && hit.collider.CompareTag("Tunnel"))
            {
                inTunnel = true;
            }
            else if (hit.collider != null)
            {
                inTunnel = false;
            }
        }

        if (controller != null && controller.enabled)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector3 move = transform.right * horizontal + transform.forward * vertical;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (isOnGround && ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)) && !isStepping))
            {
                StartCoroutine(Footsteps());
            }


            if (controller.isGrounded||isOnGround)
            {
                playerVelocity.y = 0f;
                isJumping = false;
            }

            if (Input.GetButtonDown("Jump") && !isJumping && isOnGround)
            {
                playerVelocity.y += Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
                isJumping = true;
            }

            // Apply gravity to the player
            playerVelocity.y += Physics.gravity.y * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);

            // Player camera control
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);

            // Rotate the camera vertically
            Vector3 currentRotation = cameraTransform.rotation.eulerAngles;
            float desiredRotationX = currentRotation.x - mouseY;
            if (desiredRotationX > 180)
                desiredRotationX -= 360;
            desiredRotationX = Mathf.Clamp(desiredRotationX, -70f, 70f);
            cameraTransform.rotation = Quaternion.Euler(desiredRotationX, currentRotation.y, currentRotation.z);
        }
    }
    IEnumerator Footsteps()
    {
        isStepping = true;
        while (isOnGround && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) && playerAudio != null)
        {
            playerAudio.Play();
            if (footstepFeedback != null) footstepFeedback.PlayFeedbacks();
            yield return new WaitForSeconds(footstepInterval);
        }
        isStepping = false;
    }

}
