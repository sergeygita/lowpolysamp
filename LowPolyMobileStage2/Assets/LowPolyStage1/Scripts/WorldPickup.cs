using UnityEngine;

public enum PickupKind { Health, Armor, Money }

// Подбор определяется GameManager по расстоянию, без зависимости от Rigidbody игрока.
public class WorldPickup : MonoBehaviour
{
    public PickupKind Kind { get; private set; }
    public int Amount { get; private set; }
    public bool Available { get { return Time.time >= availableAt; } }
    GameObject visual;
    float availableAt;
    public void Initialize(CharacterBuilder builder, PickupKind kind, int amount, Vector3 position)
    {
        Kind = kind; Amount = amount; transform.position = position;
        Color color = kind == PickupKind.Health ? new Color(.9f, .2f, .2f) : kind == PickupKind.Armor ? new Color(.1f, .7f, 1) : new Color(1, .8f, .1f);
        visual = builder.Part(kind.ToString(), transform, Vector3.up * .65f, new Vector3(.55f, .55f, .55f), color);
        builder.Part("Pedestal", transform, Vector3.up * .04f, new Vector3(.8f, .08f, .8f), color);
    }
    public void Consume() { availableAt = Time.time + 30; visual.SetActive(false); }
    void Update()
    {
        if (!visual) return;
        if (Available && !visual.activeSelf) visual.SetActive(true);
        if (Available) visual.transform.Rotate(0, 65 * Time.deltaTime, 0, Space.Self);
    }
}
