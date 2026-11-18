using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.5f;
    [Tooltip("Jika dicentang, W bergerak lurus ke arah mouse, S mundur, A/D strafe relatif terhadap aim.")]
    [SerializeField] private bool aimRelativeMovement = true;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Aiming")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float aimHeight = 1.3f;
    [SerializeField] private float rotationSpeed = 25f;

    [Tooltip("Kompensasi sudut animasi Mixamo. Sesuaikan angka yang sudah kamu kalibrasi tadi.")]
    [SerializeField] private float aimYawOffset = -50f;

    [Header("Debug")]
    [SerializeField] private bool drawAimDebugLine = true;

    private CharacterController characterController;
    private Rigidbody rb;
    private Animator animator;
    private float verticalVelocity;
    private Vector3 currentAimPoint;
    private Vector3 currentAimDirection = Vector3.forward;
    private bool hasValidAimPoint;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Hitung arah mouse terlebih dahulu agar pergerakan W/A/S/D memakai arah aim terbaru di frame ini
        HandleAiming();

        Vector2 moveInput = ReadMoveInput();
        Vector3 moveDirection = CalculateMoveDirection(moveInput);

        HandleMovement(moveDirection);
        UpdateAnimatorParameters(moveDirection);
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;

        Vector2 input = new Vector2(x, y);
        return input.sqrMagnitude > 1f ? input.normalized : input;
#else
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return input.sqrMagnitude > 1f ? input.normalized : input;
#endif
    }

    private Vector2 ReadMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private void HandleAiming()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector2 mouseScreenPos = ReadMouseScreenPosition();
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

        float planeY = transform.position.y + aimHeight;
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (aimPlane.Raycast(ray, out float enterDistance))
        {
            currentAimPoint = ray.GetPoint(enterDistance);
            hasValidAimPoint = true;

            Vector3 flatTargetDir = currentAimPoint - transform.position;
            flatTargetDir.y = 0f;

            if (flatTargetDir.sqrMagnitude > 0.001f)
            {
                currentAimDirection = flatTargetDir.normalized;

                Quaternion baseLookRotation = Quaternion.LookRotation(currentAimDirection);
                Quaternion compensatedRotation = baseLookRotation * Quaternion.Euler(0f, aimYawOffset, 0f);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    compensatedRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            if (drawAimDebugLine)
            {
                Vector3 chestOrigin = new Vector3(transform.position.x, planeY, transform.position.z);
                Debug.DrawLine(chestOrigin, currentAimPoint, Color.magenta);
            }
        }
    }

    private Vector3 CalculateMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f) return Vector3.zero;

        if (aimRelativeMovement)
        {
            Vector3 aimForward = currentAimDirection;
            Vector3 aimRight = Vector3.Cross(Vector3.up, aimForward).normalized;

            return (aimForward * input.y + aimRight * input.x).normalized;
        }

        if (mainCamera == null)
        {
            return new Vector3(input.x, 0f, input.y).normalized;
        }

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return (camForward * input.y + camRight * input.x).normalized;
    }

    private void HandleMovement(Vector3 moveDirection)
    {
        if (characterController != null)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }
        else if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else
        {
            transform.position += moveDirection * (moveSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimatorParameters(Vector3 worldMoveDir)
    {
        if (animator == null) return;

        float speed = worldMoveDir.magnitude;
        Vector3 aimForward = currentAimDirection;
        Vector3 aimRight = Vector3.Cross(Vector3.up, aimForward).normalized;

        // Proyeksi arah gerak terhadap arah bidikan mouse agar animasi run/strafe sesuai
        float moveX = Vector3.Dot(worldMoveDir, aimRight);
        float moveZ = Vector3.Dot(worldMoveDir, aimForward);

        if (HasAnimatorParameter(SpeedHash)) animator.SetFloat(SpeedHash, speed);
        if (HasAnimatorParameter(MoveXHash)) animator.SetFloat(MoveXHash, moveX);
        if (HasAnimatorParameter(MoveZHash)) animator.SetFloat(MoveZHash, moveZ);
    }

    private bool HasAnimatorParameter(int paramHash)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == paramHash) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!hasValidAimPoint) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(currentAimPoint, 0.15f);
    }
}