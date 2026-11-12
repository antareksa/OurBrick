using TMPro;
using UnityEngine;

namespace BrickbreakerBase
{
    // Builds every world object (bricks, balls, coins, walls, aim line) in code from generated
    // sprites, so the package needs no prefabs, textures or materials.
    public static class WorldFactory
    {
        public const string BounceTag = "Bouncy";

        private static Sprite _square;
        private static Sprite _circle;
        private static Texture2D _dottedTexture;
        private static bool _warnedAboutTag;

        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    texture.filterMode = FilterMode.Point;
                    texture.name = "BB_Square";

                    // 1 pixel at 1 pixel-per-unit = a 1x1 world unit sprite.
                    _square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }

                return _square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                {
                    const int size = 64;
                    Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    texture.name = "BB_Circle";

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x + 0.5f) / size * 2f - 1f;
                            float dy = (y + 0.5f) / size * 2f - 1f;
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);

                            // About one pixel of soft edge.
                            float alpha = Mathf.Clamp01((1f - distance) * (size * 0.5f));
                            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                    }

                    texture.Apply();

                    // 64 pixels at 64 pixels-per-unit = a 1-unit-diameter circle.
                    _circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                }

                return _circle;
            }
        }

        // 1 opaque texel then 1 transparent texel, tiled along the aim line.
        private static Texture2D DottedTexture
        {
            get
            {
                if (_dottedTexture == null)
                {
                    _dottedTexture = new Texture2D(2, 1, TextureFormat.RGBA32, false);
                    _dottedTexture.SetPixel(0, 0, Color.white);
                    _dottedTexture.SetPixel(1, 0, new Color(1f, 1f, 1f, 0f));
                    _dottedTexture.Apply();
                    _dottedTexture.filterMode = FilterMode.Point;
                    _dottedTexture.wrapMode = TextureWrapMode.Repeat;
                    _dottedTexture.name = "BB_Dotted";
                }

                return _dottedTexture;
            }
        }

        // A tag has to exist in project settings before it can be assigned -- the editor setup
        // script adds "Bouncy" automatically, so this only fires if that didn't run.
        public static void SetBounceTag(GameObject go)
        {
            try
            {
                go.tag = BounceTag;
            }
            catch (UnityException)
            {
                if (_warnedAboutTag) return;

                _warnedAboutTag = true;
                Debug.LogError("[Brickbreaker Base] The tag '" + BounceTag + "' is not defined, so nothing will bounce. " +
                    "Add it under Project Settings > Tags and Layers (the editor setup script normally does this on import).");
            }
        }

        private static Material CreateSpriteMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            return new Material(shader);
        }

        public static BrickController CreateBrick(Transform parent)
        {
            GameObject go = new GameObject("Brick");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.9f;
            SetBounceTag(go);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Square;
            spriteRenderer.sortingOrder = 1;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;

            GameObject textGo = new GameObject("HitPoint");
            textGo.transform.SetParent(go.transform, false);

            TextMeshPro text = textGo.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(0.9f, 0.9f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 1f;
            text.fontSizeMax = 6f;
            text.sortingOrder = 2;

            // Added last, after scale/children exist -- Awake reads the base scale.
            BrickController brick = go.AddComponent<BrickController>();
            brick.SpriteRenderer = spriteRenderer;
            brick.HitPointText = text;

            return brick;
        }

        public static BallController CreateBall(Transform parent, float speed)
        {
            GameObject go = new GameObject("Ball");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.3f;

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Circle;
            spriteRenderer.sortingOrder = 4;

            // Never simulated -- BallController sweeps its own circle -- but its radius is read
            // from this collider, and other balls' casts skip it.
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;

            go.AddComponent<BasicBallHitEffect>();

            // Added last so Awake sees the final scale and collider.
            BallController ball = go.AddComponent<BallController>();
            ball.Configure(speed);

            return ball;
        }

        public static CoinPickup CreateCoin(bool isGold)
        {
            GameObject go = new GameObject(isGold ? "GoldCoin" : "SilverCoin");
            go.transform.localScale = Vector3.one * (isGold ? 0.45f : 0.35f);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Circle;
            spriteRenderer.color = isGold ? new Color(1f, 0.84f, 0.1f) : new Color(0.8f, 0.82f, 0.88f);
            spriteRenderer.sortingOrder = 3;

            return go.AddComponent<CoinPickup>();
        }

        public static GameObject CreateWall(string name, Vector2 center, Vector2 size, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            SetBounceTag(go);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Square;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 0;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;

            return go;
        }

        // A purely visual strip -- no collider, so balls pass through it.
        public static GameObject CreateLine(string name, Vector2 center, Vector2 size, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Square;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 0;

            return go;
        }

        // dotsPerUnit = how many dot/gap pairs tile along one world unit of the line.
        public static void ConfigureAimLine(LineRenderer line, float dotsPerUnit)
        {
            Material material = CreateSpriteMaterial();
            material.mainTexture = DottedTexture;
            material.mainTextureScale = new Vector2(dotsPerUnit, 1f);

            line.material = material;
            line.useWorldSpace = false;
            line.textureMode = LineTextureMode.Tile;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 0;
            line.startWidth = 0.1f;
            line.endWidth = 0.1f;
            line.startColor = new Color(1f, 1f, 1f, 0.9f);
            line.endColor = new Color(1f, 1f, 1f, 0.9f);
            line.sortingOrder = 5;
            line.positionCount = 0;
        }

        public static SpriteRenderer CreateAimMarker(Transform parent)
        {
            GameObject go = new GameObject("AimEndMarker");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.25f;

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Circle;
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.9f);
            spriteRenderer.sortingOrder = 5;
            spriteRenderer.enabled = false;

            return spriteRenderer;
        }
    }
}
