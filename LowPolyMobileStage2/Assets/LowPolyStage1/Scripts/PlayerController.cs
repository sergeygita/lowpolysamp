using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 3.2f, runSpeed = 6f, jumpHeight = 1.1f, gravity = -22f;
    CharacterController controller;
    ProceduralAnimation animationDriver;
    PlayerData player;
    Transform view;
    float verticalSpeed;
    void Awake() { controller = GetComponent<CharacterController>(); animationDriver = GetComponent<ProceduralAnimation>(); }
    public void Bind(PlayerData data, Transform cameraTransform) { player = data; view = cameraTransform; }
    public void ResetMotion() { verticalSpeed = 0; if (animationDriver) animationDriver.speed = 0; }
    void Update()
    {
        if (player == null || !player.isLocal || !player.isSpawned || player.currentVehicle) return;
        var input = MobileInputManager.Instance;
        Vector2 axis = input ? input.Move : Vector2.zero;
        Vector3 forward = view ? view.forward : Vector3.forward;
        forward.y = 0; forward.Normalize();
        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        Vector3 direction = forward * axis.y + right * axis.x;
        float speed = input && input.Sprint ? runSpeed : walkSpeed;
        if (direction.sqrMagnitude > .001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-14 * Time.deltaTime));
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        bool jump = input && input.ConsumeJump();
        if (jump && controller.isGrounded) verticalSpeed = Mathf.Sqrt(jumpHeight * -2 * gravity);
        verticalSpeed += gravity * Time.deltaTime;
        Vector3 before = transform.position;
        CollisionFlags flags = controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        if (animationDriver)
        {
            Vector3 delta = transform.position - before; delta.y = 0;
            animationDriver.speed = delta.magnitude / Mathf.Max(Time.deltaTime, .0001f);
            animationDriver.grounded = controller.isGrounded;
        }
        // Action обрабатывает GameManager: один обработчик для пешего и водителя.
    }
}
