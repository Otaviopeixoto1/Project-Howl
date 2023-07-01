using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Animator characterAnimator;
    private CharacterController characterController;
    private PlayerInputActions playerInputActions;

    private Vector3 movementDir = Vector3.forward;

    [SerializeField]
    private float playerSpeed = 2.0f;
    [SerializeField]
    private float gravityValue = -9.81f;

    [SerializeField]
    private LayerMask terrainLayer;


    //[SerializeField]
    //private TerrainManager terrainManager; //use to sample terrain normals without raycasting

    void Start()
    { 
        characterController = gameObject.GetComponent<CharacterController>();
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();
    }

    void Update()
    {
        Vector3 groundNormal = Vector3.up;
        //if (terrainManager != null)
        //{
            //groundNormal = terrainManager.GetNormal(transform.position);
        //}

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down,out hit ,5, terrainLayer)){
 
            groundNormal = hit.normal; 

        }


        Vector2 inputDir = (playerInputActions.Player.Move.ReadValue<Vector2>()).normalized;
        
        //can also use ProjectOnPlane function to project input dir into the surface's tangent plane

        if (inputDir != Vector2.zero)
        {
            movementDir = new Vector3(inputDir.x,0,inputDir.y);
            characterAnimator.SetBool("isMoving",true);

            // Rotation needed to align the object's forward vector with the target direction
            Quaternion targetRotation = Quaternion.LookRotation(movementDir, transform.up);
            Quaternion RotToGround =  Quaternion.FromToRotation(transform.up, groundNormal);
 
            //transform.rotation = Quaternion.Slerp(transform.rotation, RotToGround*transform.rotation,10);
            Vector3 finaldir = RotToGround * movementDir;
            transform.rotation = Quaternion.Slerp(transform.rotation, RotToGround * targetRotation, 0.1f);
            
            characterController.Move((finaldir * playerSpeed + Vector3.up * gravityValue) * Time.deltaTime); 
        }
        else
        {
            characterController.Move(Vector3.up * gravityValue * Time.deltaTime); 
            characterAnimator.SetBool("isMoving",false);
        }
        

        
        
    }
}
