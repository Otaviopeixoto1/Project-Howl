using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Animator characterAnimator;

    [SerializeField]
    private CharacterController characterController;
    [SerializeField]
    private PlayerInputActions playerInputActions;

    private Vector3 movementDir = Vector3.forward;

    [SerializeField]
    private float playerSpeed = 2.0f;
    [SerializeField]
    private float gravityValue = -9.81f;

    void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();
    }

    void Update()
    {
        Vector2 inputDir = playerInputActions.Player.Move.ReadValue<Vector2>();
        Vector2 moveSpeed = playerInputActions.Player.Move.ReadValue<Vector2>() * playerSpeed; 
        
        float appliedGravity;
        if (characterController.isGrounded)
        {
            appliedGravity = 0f;
        }
        else
        {
            appliedGravity = gravityValue;
        }
        characterController.Move(new Vector3(moveSpeed.x, appliedGravity, moveSpeed.y) * Time.deltaTime);
        

        if (inputDir != Vector2.zero)
        {
            movementDir = new Vector3(inputDir.x,0,inputDir.y);
            characterAnimator.SetBool("isMoving",true);

            // Rotation needed to align the object's forward vector with the target direction
            Quaternion targetRotation = Quaternion.LookRotation(movementDir, Vector3.up);

            transform.rotation = targetRotation;
        }
        else
        {
            characterAnimator.SetBool("isMoving",false);
        }
        
        
    }
}
