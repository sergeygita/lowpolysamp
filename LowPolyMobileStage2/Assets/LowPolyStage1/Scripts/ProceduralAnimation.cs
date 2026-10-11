using UnityEngine;

public class ProceduralAnimation : MonoBehaviour
{
    public Transform leftArm, rightArm, leftLeg, rightLeg;
    public float speed;
    public bool grounded = true;
    float phase, amplitude;
    void Update()
    {
        float target = grounded ? Mathf.Clamp01(speed / 6f) * 35f : 0;
        amplitude = Mathf.Lerp(amplitude, target, 1 - Mathf.Exp(-12 * Time.deltaTime));
        phase += Time.deltaTime * Mathf.Lerp(5, 12, Mathf.Clamp01(speed / 6f));
        float angle = Mathf.Sin(phase) * amplitude;
        if (leftArm) leftArm.localRotation = Quaternion.Euler(angle, 0, 0);
        if (rightArm) rightArm.localRotation = Quaternion.Euler(-angle, 0, 0);
        if (leftLeg) leftLeg.localRotation = Quaternion.Euler(-angle, 0, 0);
        if (rightLeg) rightLeg.localRotation = Quaternion.Euler(angle, 0, 0);
    }
}
