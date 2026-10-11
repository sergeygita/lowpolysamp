using System;
using UnityEngine;

public enum Direction { Up, Right, Down, Left }

public static class SpriteGenerator
{
    // Владельцу возвращённого спрайта принадлежат и Sprite, и его Texture2D.
    static Sprite Make(int w, int h, Func<int, int, Color> pixel)
    {
        if (w < 2 || h < 2 || w > 2048 || h > 2048) throw new ArgumentOutOfRangeException("size");
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.name = "GeneratedUI"; texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) pixels[y * w + x] = pixel(x, y);
        texture.SetPixels(pixels); texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
    }
    public static Sprite GenerateCircle(int size, Color fill, Color border, float borderWidth)
    {
        float radius = size * .5f;
        borderWidth = Mathf.Clamp(borderWidth, 0, radius);
        return Make(size, size, (x, y) => {
            float d = new Vector2(x + .5f - radius, y + .5f - radius).magnitude;
            return d > radius ? Color.clear : d >= radius - borderWidth ? border : fill;
        });
    }
    public static Sprite GenerateRoundedRect(int w, int h, int radius, Color fill, Color border)
    {
        radius = Mathf.Clamp(radius, 0, Mathf.Min(w, h) / 2);
        return Make(w, h, (x, y) => {
            Vector2 q = new Vector2(Mathf.Abs(x + .5f - w * .5f) - (w * .5f - radius),
                                    Mathf.Abs(y + .5f - h * .5f) - (h * .5f - radius));
            float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
            return d > 0 ? Color.clear : d > -2 ? border : fill;
        });
    }
    public static Sprite GenerateArrow(int size, Color color, Direction dir)
    {
        return Make(size, size, (x, y) => {
            Vector2 p = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1);
            // Приведение к стрелке вверх.
            if (dir == Direction.Right) p = new Vector2(-p.y, p.x);
            else if (dir == Direction.Down) p = -p;
            else if (dir == Direction.Left) p = new Vector2(p.y, -p.x);
            bool head = p.y >= 0 && p.y <= .8f && Mathf.Abs(p.x) <= .8f - p.y;
            bool stem = p.y >= -.8f && p.y < 0 && Mathf.Abs(p.x) <= .22f;
            return head || stem ? color : Color.clear;
        });
    }
    public static Sprite GenerateGradientRect(int w, int h, Color top, Color bottom)
    {
        return Make(w, h, (x, y) => Color.Lerp(bottom, top, (float)y / (h - 1)));
    }
}
