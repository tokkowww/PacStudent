using UnityEngine;
using UnityEngine.UI;

public class SpriteBorder : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite[] sprites = new Sprite[5];
    public float frameRate = 4f;

    [Header("Border")]
    public Image itemPrefab;
    public int itemCount = 40;
    public float moveSpeed = 0.03f;

    RectTransform area;
    Image[] items;
    float offset;

    void Start()
    {
        area = (RectTransform)transform;

        items = new Image[itemCount];
        for (int i = 0; i < itemCount; i++)
        {
            items[i] = Instantiate(itemPrefab, area);
            items[i].preserveAspect = true;
            RectTransform rt = items[i].rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void Update()
    {
        offset = Mathf.Repeat(offset + moveSpeed * Time.deltaTime, 1f);
        float w = area.rect.width, h = area.rect.height;
        int frame = (int)(Time.time * frameRate);

        for (int i = 0; i < itemCount; i++)
        {
            float t = Mathf.Repeat(offset + (float)i / itemCount, 1f);
            items[i].rectTransform.anchoredPosition = PointOnPerimeter(t, w, h);
            items[i].sprite = sprites[(frame + i) % sprites.Length];
        }
    }

    Vector2 PointOnPerimeter(float t, float w, float h)
    {
        float d = t * 2f * (w + h);
        float x0 = -w / 2f, y0 = -h / 2f;

        if (d < w) return new Vector2(x0 + d, y0 + h);
        d -= w;
        if (d < h) return new Vector2(x0 + w, y0 + h - d);
        d -= h;
        if (d < w) return new Vector2(x0 + w - d, y0);
        d -= w;
        return new Vector2(x0, y0 + d);
    }
}