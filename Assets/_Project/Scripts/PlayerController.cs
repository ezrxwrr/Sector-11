using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float turnSpeed = 8f;

    private CharacterController _controller;
    private Animator _animator;
    private Camera _mainCam;
    private float _verticalVelocity;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _mainCam = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        HandleAimRotation();
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        // Move relative to where Swat is aiming (mouse direction)
        Vector3 moveDir = (transform.forward * input.y) + (transform.right * input.x);

        if (_animator != null)
        {
            _animator.SetFloat(MoveXHash, input.x, 0.08f, Time.deltaTime);
            _animator.SetFloat(MoveZHash, input.y, 0.08f, Time.deltaTime);
        }

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = (moveDir * moveSpeed) + (Vector3.up * _verticalVelocity);
        _controller.Move(velocity * Time.deltaTime);
    }

    private void HandleAimRotation()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Ray ray = _mainCam.ScreenPointToRay(mouseScreenPos);
        Plane aimPlane = new Plane(Vector3.up, transform.position);

        if (aimPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0f;

            // Deadzone prevents spinning when holding W directly onto the mouse cursor
            if (lookDir.sqrMagnitude > 0.15f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
    }
}