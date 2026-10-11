using System;
using UnityEngine;

[Serializable]
public class CharacterAppearance
{
    public Color skinColor = new Color(0.78f, 0.54f, 0.36f);
    public Color hairColor = new Color(0.16f, 0.09f, 0.05f);
    public Color shirtColor = new Color(0.1f, 0.65f, 0.95f);
    public Color pantsColor = new Color(0.12f, 0.18f, 0.3f);
    public Color shoesColor = new Color(0.1f, 0.1f, 0.12f);
    public Color hatColor = new Color(0.9f, 0.3f, 0.15f);
    public int hatType; // 0 — нет; 1 — кепка; 2 — шляпа.
    public int shirtType; // 0 — футболка; 1 — куртка.
    public CharacterAppearance Copy() { return (CharacterAppearance)MemberwiseClone(); }
}
