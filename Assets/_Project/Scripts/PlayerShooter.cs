using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerShooter : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 8f;
    [SerializeField] private bool automatic = true;

    [Header("Animation")]
    [SerializeField] private string upperBodyLayerName = "UpperBody";
    [SerializeField] private string shootStateName = "Shoot";

    private Animator animator;
    private int upperBodyLayerIndex = -1;
    private float nextFireTime;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            upperBodyLayerIndex = animator.GetLayerIndex(upperBodyLayerName);
        }
    }

    private void Update()
    {
        if (IsFireInputHeld() && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + (1f / fireRate);
            Fire();
        }
    }

    private void Fire()
    {
        if (animator != null && upperBodyLayerIndex >= 0)
        {
            animator.CrossFadeInFixedTime(shootStateName, 0.05f, upperBodyLayerIndex, 0f);
        }
    }

    private bool IsFireInputHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;
        return automatic ? Mouse.current.leftButton.isPressed : Mouse.current.leftButton.wasPressedThisFrame;
#else
        return automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
#endif
    }
}