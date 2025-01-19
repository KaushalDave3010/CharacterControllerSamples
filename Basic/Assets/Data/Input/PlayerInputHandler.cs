using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public static PlayerInputHandler Instance { get; private set; }

    [Header("Fire Action States (Read-Only)")]
    [SerializeField] private bool fireStarted;
    [SerializeField] private bool firePerformed;
    [SerializeField] private bool fireCanceled;

    [Header("Jump Action States (Read-Only)")]
    [SerializeField] private bool jumpStarted;
    [SerializeField] private bool jumpPerformed;
    [SerializeField] private bool jumpCanceled;

    public bool FireStarted => fireStarted;
    public bool FirePerformed => firePerformed;
    public bool FireCanceled => fireCanceled;

    public bool JumpStarted => jumpStarted;
    public bool JumpPerformed => jumpPerformed;
    public bool JumpCanceled => jumpCanceled;

    private BasicInputActions inputActions;

    private void Awake()
    {
        // Singleton pattern to access this MonoBehaviour from ECS
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        inputActions = new BasicInputActions();
        inputActions.Enable();

        // Subscribe to Fire events
        inputActions.DefaultMap.Fire.started += OnFireStarted;
        inputActions.DefaultMap.Fire.performed += OnFirePerformed;
        inputActions.DefaultMap.Fire.canceled += OnFireCanceled;

        // Subscribe to Jump events
        inputActions.DefaultMap.Jump.started += OnJumpStarted;
        inputActions.DefaultMap.Jump.performed += OnJumpPerformed;
        inputActions.DefaultMap.Jump.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        // Unsubscribe from Fire events
        inputActions.DefaultMap.Fire.started -= OnFireStarted;
        inputActions.DefaultMap.Fire.performed -= OnFirePerformed;
        inputActions.DefaultMap.Fire.canceled -= OnFireCanceled;

        // Unsubscribe from Jump events
        inputActions.DefaultMap.Jump.started -= OnJumpStarted;
        inputActions.DefaultMap.Jump.performed -= OnJumpPerformed;
        inputActions.DefaultMap.Jump.canceled -= OnJumpCanceled;

        inputActions.Disable();
    }

    private void OnFireStarted(InputAction.CallbackContext context) => fireStarted = true;
    private void OnFirePerformed(InputAction.CallbackContext context) => firePerformed = true;
    private void OnFireCanceled(InputAction.CallbackContext context) => fireCanceled = true;

    private void OnJumpStarted(InputAction.CallbackContext context) => jumpStarted = true;
    private void OnJumpPerformed(InputAction.CallbackContext context) => jumpPerformed = true;
    private void OnJumpCanceled(InputAction.CallbackContext context) => jumpCanceled = true;

    public void ResetAllStates()
    {
        fireStarted = false;
        firePerformed = false;
        fireCanceled = false;

        jumpStarted = false;
        jumpPerformed = false;
        jumpCanceled = false;
    }
    
    public void ResetFireStates()
    {
        fireStarted = false;
        firePerformed = false;
        fireCanceled = false;
    }

    public void ResetJumpStates()
    {
        jumpStarted = false;
        jumpPerformed = false;
        jumpCanceled = false;
    }
}