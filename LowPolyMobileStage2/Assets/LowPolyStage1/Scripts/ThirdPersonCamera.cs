using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public float distance = 4.2f, sensitivity = .15f, minPitch = 8, maxPitch = 80;
    public LayerMask collisionMask = 1; // Default: мир. Ignore Raycast: персонажи.
    Transform target;
    Vector3 pivot;
    float yaw, pitch = 18;
    float targetHeight = 1.25f, followDistance;
    public void SetTarget(Transform value, bool vehicle = false)
    {
        target = value;
        targetHeight = vehicle ? 1.45f : 1.25f;
        followDistance = vehicle ? 6.5f : distance;
        if (target) { pivot = target.position + Vector3.up * targetHeight; yaw = target.eulerAngles.y; }
    }
    void LateUpdate()
    {
        if (!target || !target.gameObject.activeInHierarchy) return;
        Vector2 look = MobileInputManager.Instance ? MobileInputManager.Instance.Look : Vector2.zero;
        yaw += look.x * sensitivity;
        pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
        pivot = Vector3.Lerp(pivot, target.position + Vector3.up * targetHeight, 1 - Mathf.Exp(-15 * Time.deltaTime));
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.back;
        float allowed = followDistance;
        RaycastHit hit;
        // Raycast удовлетворяет базовой проверке; SphereCast защищает края near plane.
        if (Physics.Raycast(pivot, direction, out hit, followDistance, collisionMask, QueryTriggerInteraction.Ignore))
            allowed = Mathf.Max(0, hit.distance - .2f);
        if (Physics.SphereCast(pivot, .18f, direction, out hit, followDistance, collisionMask, QueryTriggerInteraction.Ignore))
            allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - .05f));
        // После сглаживания pivot коллизия проверяется заново, без интерполяции сквозь стену.
        transform.SetPositionAndRotation(pivot + direction * allowed, rotation);
    }
}
