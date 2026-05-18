//using UnityEngine;
//using UnityEngine.InputSystem;

//[RequireComponent(typeof(CharacterController))]
//public class PlayerController : MonoBehaviour
//{
//    [SerializeField]
//    private float playerSpeed = 5.0f;
//    [SerializeField]
//    private float jumpHeight = 1.5f;
//    [SerializeField]
//    private float gravityValue = -9.81f;

//    public CharacterController controller;
//    private Vector3 playerVelocity;
//    private bool groundedPlayer;

//    [Header("Input Actions")]
//    public InputActionReference moveAction;
//    public InputActionReference jumpAction;

//    private InputManager inputManager;
//    private Transform cameraTransform;

//    private void OnEnable()
//    {
//        moveAction.action.Enable();
//        jumpAction.action.Enable();
//        inputManager = InputManager.Instance;
//        cameraTransform = cameraTransform.main.transform;
//    }

//    private void OnDisable()
//    {
//        moveAction.action.Disable();
//        jumpAction.action.Disable();
//    }

//    void Update()
//    {
//        groundedPlayer = controller.isGrounded;

//        if (groundedPlayer)
//        {
           
//            if (playerVelocity.y < -2f)
//                playerVelocity.y = -2f;
//        }

       
//        Vector2 movement = inputManager.GetPlayerMovement();
//        Vector3 move = new Vector3(movement.x, 0, movement.y);
//        move = Vector3.ClampMagnitude(move, 1f);
//        move = cameraTransform.forward * move.z * cameraTransform.right * move.x;
//        move.y = 0f;
//        controller.Move(move * Time.deltaTime * playerSpeed);

//        //if (move != Vector3.zero)
//        //{
//        //    gameObject.transform.forward = move;
//        //}
            
//        if (groundedPlayer && inputManager.PlayerJumpedThisFrame())
//        {
//            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
//        }

//        playerVelocity.y += gravityValue * Time.deltaTime;
//        controller.Move(playerVelocity * Time.deltaTime);
//    }
//}
