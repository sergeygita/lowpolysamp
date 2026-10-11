using UnityEngine;

// Только механика машины. Посадка, повреждения, ремонт и callbacks принадлежат GameManager.
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    public PlayerData Driver { get; private set; }
    public float Health { get; private set; } = 100;
    public float SpeedKmh { get { return body ? body.velocity.magnitude * 3.6f : 0; } }
    public Vector3 HomePosition { get; private set; }
    public const int VehicleLayer = 8;
    Rigidbody body;
    WheelCollider[] wheels;
    Transform[] wheelVisuals;
    float lastImpact = -10;
    public void Initialize(CharacterBuilder builder, Vector3 position)
    {
        HomePosition = position;
        transform.position = position;
        gameObject.layer = VehicleLayer;
        body = GetComponent<Rigidbody>();
        body.mass = 1000; body.drag = .05f; body.angularDrag = 1.5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Аркадная машина для ровной тестовой карты: крен и опрокидывание отключены.
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.centerOfMass = new Vector3(0, .1f, 0);
        var shell = gameObject.AddComponent<BoxCollider>();
        shell.center = new Vector3(0, .45f, 0); shell.size = new Vector3(1.65f, .5f, 3.5f);
        var cabin = gameObject.AddComponent<BoxCollider>();
        cabin.center = new Vector3(0, .95f, -.25f); cabin.size = new Vector3(1.4f, .55f, 1.5f);
        Color paint = new Color(.12f, .55f, .85f);
        builder.Part("Body", transform, shell.center, shell.size, paint);
        builder.Part("Cabin", transform, cabin.center, cabin.size, new Color(.12f, .22f, .3f));
        builder.Part("Roof", transform, new Vector3(0, 1.27f, -.25f), new Vector3(1.45f, .08f, 1.55f), paint);
        builder.Part("FrontBumper", transform, new Vector3(0, .32f, 1.8f), new Vector3(1.7f, .18f, .15f), Color.gray);
        builder.Part("RearBumper", transform, new Vector3(0, .32f, -1.8f), new Vector3(1.7f, .18f, .15f), Color.gray);
        for (int side = -1; side <= 1; side += 2)
        {
            builder.Part("Headlight", transform, new Vector3(side * .55f, .5f, 1.77f), new Vector3(.35f, .18f, .04f), new Color(1, .95f, .65f));
            builder.Part("Taillight", transform, new Vector3(side * .55f, .5f, -1.77f), new Vector3(.35f, .18f, .04f), Color.red);
        }
        wheels = new WheelCollider[4]; wheelVisuals = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var wheel = new GameObject("WheelCollider_" + i);
            wheel.transform.SetParent(transform, false);
            wheel.transform.localPosition = new Vector3(i % 2 == 0 ? -.87f : .87f, 0, i < 2 ? 1.15f : -1.15f);
            var wc = wheel.AddComponent<WheelCollider>(); wheels[i] = wc;
            wc.mass = 25; wc.radius = .36f; wc.suspensionDistance = .22f;
            var spring = wc.suspensionSpring; spring.spring = 35000; spring.damper = 4500; spring.targetPosition = .5f; wc.suspensionSpring = spring;
            var friction = wc.sidewaysFriction; friction.stiffness = 1.6f; wc.sidewaysFriction = friction;
            wc.ConfigureVehicleSubsteps(5, 8, 12);
            var visual = new GameObject("WheelVisual_" + i).transform;
            visual.SetParent(transform, false); wheelVisuals[i] = visual;
            var tire = builder.Part("Tire", visual, Vector3.zero, new Vector3(.72f, .12f, .72f), new Color(.08f, .08f, .09f), true);
            tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        foreach (var t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = VehicleLayer;
    }
    public void SetDriver(PlayerData player) { Driver = player; }
    public void SetHealth(float value) { Health = Mathf.Clamp(value, 0, 100); }
    public void ResetAt(Vector3 position, float yaw)
    {
        body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        body.position = position; body.rotation = Quaternion.Euler(0, yaw, 0);
        transform.SetPositionAndRotation(position, body.rotation);
        body.WakeUp(); // После переноса подвеска должна осесть на землю.
    }
    void FixedUpdate()
    {
        if (wheels == null) return;
        var input = MobileInputManager.Instance;
        bool drive = Driver != null && Driver.isLocal && Driver.isSpawned && Health > 0 && input && input.CanInput;
        float throttle = drive ? input.DriveThrottle : 0;
        float steer = drive ? input.Move.x : 0;
        float forwardSpeed = Vector3.Dot(body.velocity, transform.forward);
        bool opposite = Mathf.Abs(forwardSpeed) > .7f && throttle * forwardSpeed < 0;
        bool stop = !drive || input.Handbrake;
        float torque = stop || opposite ? 0 : throttle * 950;
        if (forwardSpeed > 22 && torque > 0 || forwardSpeed < -6 && torque < 0) torque = 0;
        float brake = stop ? 4500 : opposite ? 3500 : Mathf.Abs(throttle) < .05f ? 100 : 0;
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].steerAngle = i < 2 ? steer * Mathf.Lerp(30, 12, Mathf.Clamp01(SpeedKmh / 80)) : 0;
            wheels[i].motorTorque = i >= 2 ? torque : 0;
            wheels[i].brakeTorque = brake;
        }
        if (transform.position.y < -10 && GameManager.Instance) GameManager.Instance.RecoverVehicle(this);
    }
    void LateUpdate()
    {
        if (wheels == null) return;
        for (int i = 0; i < wheels.Length; i++)
        {
            Vector3 p; Quaternion q; wheels[i].GetWorldPose(out p, out q);
            wheelVisuals[i].SetPositionAndRotation(p, q);
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastImpact < .6f || collision.relativeVelocity.magnitude < 4) return;
        lastImpact = Time.time;
        if (GameManager.Instance) GameManager.Instance.VehicleImpact(this, collision.relativeVelocity.magnitude);
    }
    public bool TryExitPosition(out Vector3 position)
    {
        // Проверяем обе двери и заднюю часть. Нельзя выйти в стену или в воздух.
        Vector3[] offsets = { Vector3.left * 2.1f, Vector3.right * 2.1f, Vector3.back * 2.9f };
        foreach (var offset in offsets)
        {
            Vector3 candidate = transform.TransformPoint(offset);
            RaycastHit hit;
            if (!Physics.Raycast(candidate + Vector3.up * 3, Vector3.down, out hit, 6, 1, QueryTriggerInteraction.Ignore)) continue;
            candidate = hit.point + Vector3.up * .08f;
            if (Vector3.Angle(hit.normal, Vector3.up) > 40) continue;
            if (Physics.CheckCapsule(candidate + Vector3.up * .32f, candidate + Vector3.up * 1.56f, .29f, 1 | (1 << VehicleLayer), QueryTriggerInteraction.Ignore)) continue;
            position = candidate; return true;
        }
        position = Vector3.zero; return false;
    }
}
