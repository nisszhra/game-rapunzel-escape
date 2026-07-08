using UnityEngine;
using UnityEngine.InputSystem;
// Joystick Pack base class — exposes .Direction (Vector2)
// Make sure the Joystick Pack asset is imported (Assets/Joystick Pack).

[RequireComponent(typeof(CharacterController))]
public class PlayerTPS : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed    = 3f;
    [SerializeField] private float runSpeed     = 7f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce     = 5f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;   // lebih kuat agar tidak melayang
    private float velocityY;

    [Header("Ground Check")]
    [Tooltip("Child kosong di bawah kaki player (harus child dari rapunzel).")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float     groundDistance = 0.25f;
    [SerializeField] private LayerMask groundMask;   // set ke layer 'Tanah'

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator  animator;

    [Header("Mobile Input")]
    [Tooltip("Drag FixedJoystick dari Canvas ke sini.")]
    [SerializeField] private Joystick mobileJoystick;

    // ── state ────────────────────────────────────────────────────────────
    private bool isGrounded;
    private bool mobileRunHeld;   // true hanya selama tombol Run ditekan

    private CharacterController controller;
    private TPS inputActions;

    private Vector2 moveInput;
    private bool    jumpPressed;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        controller   = GetComponent<CharacterController>();
        inputActions = new TPS();

        // Jika ada Rigidbody yang tertinggal, hapus agar tidak konflik
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.LogWarning("[PlayerTPS] Rigidbody ditemukan dan DIHAPUS — konflik dengan CharacterController!");
            Destroy(rb);
        }

        // Auto-cari kamera utama jika belum di-assign di Inspector
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled  += OnMove;
        inputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled  -= OnMove;
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Disable();
    }

    private void Update()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        UpdateAnimator();
    }

    // ── Ground Check ──────────────────────────────────────────────────────
    private void CheckGround()
    {
        // Gunakan dua metode: Physics.CheckSphere AND controller.isGrounded
        // Keduanya digabung agar deteksi lebih andal
        bool sphereGrounded = false;
        if (groundCheck != null && groundMask != 0)
            sphereGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        isGrounded = controller.isGrounded || sphereGrounded;
    }

    // ── Input Callbacks ───────────────────────────────────────────────────
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        jumpPressed = true;
    }

    // ── Movement ──────────────────────────────────────────────────────────
    private void HandleMovement()
    {
        // Gabungkan input keyboard & joystick
        Vector2 joystickDir = (mobileJoystick != null) ? mobileJoystick.Direction : Vector2.zero;
        Vector2 combined    = Vector2.ClampMagnitude(moveInput + joystickDir, 1f);
        Vector3 move        = new Vector3(combined.x, 0f, combined.y);

        bool moving = move.magnitude > 0.1f;

        if (moving)
        {
            // Rotate & move relatif terhadap kamera
            Vector3 camFwd   = cameraTransform.forward; camFwd.y = 0f;
            Vector3 camRight = cameraTransform.right;   camRight.y = 0f;
            Vector3 moveDir  = (camFwd * move.z + camRight * move.x).normalized;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                rotationSpeed * Time.deltaTime);

            // Sprint: Left Shift (keyboard) ATAU tombol Run mobile (hold)
            bool isSprinting = Input.GetKey(KeyCode.LeftShift) || mobileRunHeld;
            float speed = isSprinting ? runSpeed : walkSpeed;

            controller.Move(moveDir * speed * Time.deltaTime);
        }

        // Audio
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetWalkSoundActive(moving && isGrounded);
    }

    // ── Jump ──────────────────────────────────────────────────────────────
    /// <summary>Dipanggil oleh MobileJumpButton.OnPointerDown()</summary>
    public void OnMobileJumpPressed()
    {
        jumpPressed       = true;
        jumpBufferCounter = jumpBufferTime;
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        bool shouldJump = jumpPressed || jumpBufferCounter > 0f;

        if (shouldJump && isGrounded)
        {
            velocityY         = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpBufferCounter = 0f;
            if (animator != null) animator.SetTrigger("jump");
        }

        jumpPressed = false;
    }

    // ── Gravity ───────────────────────────────────────────────────────────
    private void ApplyGravity()
    {
        if (isGrounded && velocityY < 0f)
        {
            // Tahan kecil negatif agar controller.isGrounded terus terpicu
            velocityY = -2f;
        }
        else
        {
            velocityY += gravity * Time.deltaTime;
        }

        controller.Move(new Vector3(0f, velocityY * Time.deltaTime, 0f));
    }

    // ── Animator ──────────────────────────────────────────────────────────
    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector2 joystickDir = (mobileJoystick != null) ? mobileJoystick.Direction : Vector2.zero;
        Vector2 combined    = Vector2.ClampMagnitude(moveInput + joystickDir, 1f);
        bool    moving      = combined.magnitude > 0.1f;
        bool    isSprinting = moving && (Input.GetKey(KeyCode.LeftShift) || mobileRunHeld);

        animator.SetBool("isWalk",     moving && !isSprinting);
        animator.SetBool("isRun",      isSprinting);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("yVelocity", velocityY);
    }

    // ── Run Button (Mobile) ───────────────────────────────────────────────
    /// <summary>Dipanggil MobileRunButton.OnPointerDown() — mulai lari (hold).</summary>
    public void OnMobileRunStart()  => mobileRunHeld = true;

    /// <summary>Dipanggil MobileRunButton.OnPointerUp() — berhenti lari.</summary>
    public void OnMobileRunStop()   => mobileRunHeld = false;
}