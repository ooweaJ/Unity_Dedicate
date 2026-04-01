// GradientTextureGenerator.cs
// 아무 오브젝트에 붙여서 한 번만 실행하면 됨

using UnityEngine;
using UnityEngine.UI;

public class GradientTextureGenerator : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Color topColor = new Color(0, 0, 0, 0.85f);
    [SerializeField] private Color bottomColor = new Color(0, 0, 0, 0f);
    [SerializeField] private int textureHeight = 64;  // 해상도 (높을수록 부드러움)

    void Awake()
    {
        targetImage.sprite = CreateGradientSprite();
    }

    Sprite CreateGradientSprite()
    {
        Texture2D tex = new Texture2D(1, textureHeight);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < textureHeight; y++)
        {
            float t = (float)y / (textureHeight - 1);
            // y=0이 아래쪽 → 위쪽(y높음)이 어두움
            Color c = Color.Lerp(bottomColor, topColor, t);
            tex.SetPixel(0, y, c);
        }

        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, 1, textureHeight),
            new Vector2(0.5f, 0.5f)
        );
    }
}