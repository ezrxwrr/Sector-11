using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class VisionCone : MonoBehaviour
{
    [Header("Target & Position")]
    [Tooltip("Biarkan kosong agar otomatis mengikuti Ch15_nonPBR.")]
    [SerializeField] private Transform characterBody;
    [SerializeField] private float eyeHeight = 1.55f;
    [Tooltip("Jarak maju di depan dada/wajah agar cahaya tidak pernah tertinggal di belakang kepala.")]
    [SerializeField] private float forwardOffset = 0.35f;

    [Header("Aim Sync")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float aimHeight = 1.3f;
    [Tooltip("Kemiringan sorotan cahaya ke bawah lantai (derajat).")]
    [SerializeField] private float downwardTilt = 18f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (characterBody == null && transform.parent != null)
        {
            Animator anim = transform.parent.GetComponentInChildren<Animator>();
            characterBody = anim != null ? anim.transform : transform.parent;
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 basePos = characterBody != null
            ? characterBody.position
            : (transform.parent != null ? transform.parent.position : transform.position);

        Vector2 mouseScreenPos = ReadMouseScreenPosition();
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

        float planeY = basePos.y + aimHeight;
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (aimPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 targetPoint = ray.GetPoint(enterDistance);
            Vector3 flatDir = targetPoint - basePos;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude > 0.001f)
            {
                Vector3 dirNormalized = flatDir.normalized;

                // Kunci posisi lampu tepat di depan wajah/dada karakter setiap frame
                transform.position = basePos + (Vector3.up * eyeHeight) + (dirNormalized * forwardOffset);

                // Putar lampu lurus ke arah kursor mouse
                Quaternion lookHorizontal = Quaternion.LookRotation(dirNormalized);
                transform.rotation = lookHorizontal * Quaternion.Euler(downwardTilt, 0f, 0f);
            }
        }
    }

    private Vector2 ReadMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}