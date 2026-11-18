using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WeaponAimAligner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the root Player transform. Auto-detects parent if left empty.")]
    [SerializeField] private Transform playerRoot;

    [Header("Fine-Tuning")]
    [Tooltip("Use if your gun model's forward axis isn't +Z (e.g., try Y = 90 or -90).")]
    [SerializeField] private Vector3 modelRotationOffset = Vector3.zero;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        if (playerRoot == null)
            playerRoot = transform.root;
    }

    // LateUpdate runs AFTER Mixamo animations update the hand bone
    private void LateUpdate()
    {
        if (mainCam == null) return;

        Vector2 screenPos = GetMouseScreenPosition();
        Ray ray = mainCam.ScreenPointToRay(screenPos);

        // Intersect mouse ray with a horizontal plane at player height (no collider/layer needed)
        Plane aimPlane = new Plane(Vector3.up, playerRoot.position);
        if (aimPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);

            // Aim from the weapon's actual hand position toward the cursor on a flat plane
            Vector3 targetAtMuzzleHeight = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
            Vector3 aimDirection = targetAtMuzzleHeight - transform.position;

            if (aimDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(aimDirection) * Quaternion.Euler(modelRotationOffset);
            }
        }
    }

    private Vector2 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}