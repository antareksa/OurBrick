using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace BrickbreakerBase
{
    // One brick: HP, color-by-HP-tier, and the cosmetic scale-in / move / hit-punch motion. Built
    // by WorldFactory.CreateBrick, which assigns HitPointText and SpriteRenderer.
    public class BrickController : MonoBehaviour
    {
        public TMP_Text HitPointText;
        public SpriteRenderer SpriteRenderer;

        public Color GoldColor = new Color(1f, 0.84f, 0.1f);
        public Color DangerColor = new Color(1f, 0.25f, 0.25f);

        public float SpawnScaleDuration = 0.3f;
        public float HitPunchScale = 1.15f;
        public float HitPunchDuration = 0.12f;

        public Vector2Int GridPosition { get; private set; }
        public bool IsGold { get; private set; }

        public UnityEvent<BrickController> OnDestroyed = new UnityEvent<BrickController>();

        // One entry per HpColorStep of HP, cycling by hue; ShadeLevels then darkens each full lap
        // so a tier's color doesn't repeat until Palette.Length * HpColorStep * ShadeLevels HP
        // (1000 HP with the defaults).
        private static readonly Color[] HpColorPalette =
        {
            new Color(0.20f, 0.70f, 0.30f),
            new Color(0.20f, 0.60f, 0.70f),
            new Color(0.25f, 0.45f, 0.85f),
            new Color(0.55f, 0.35f, 0.85f),
            new Color(0.80f, 0.30f, 0.75f),
            new Color(0.85f, 0.30f, 0.40f),
            new Color(0.85f, 0.45f, 0.20f),
            new Color(0.80f, 0.65f, 0.15f),
            new Color(0.55f, 0.55f, 0.55f),
            new Color(0.35f, 0.20f, 0.20f),
        };

        private const int HpColorStep = 10;
        private const int ShadeLevels = 10;

        private int _hitPoint;
        private bool _isDead;
        private bool _isInDanger;
        private Color _baseColor = Color.white;
        private Vector3 _baseScale;
        private Coroutine _moveRoutine;
        private Coroutine _scaleRoutine;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            if (!_isInDanger || SpriteRenderer == null) return;

            SpriteRenderer.color = Color.Lerp(_baseColor, DangerColor, Mathf.PingPong(Time.time * 2f, 1f));
        }

        public void Spawn(int hitPoint, Vector2Int gridPosition)
        {
            _hitPoint = hitPoint;
            GridPosition = gridPosition;
            HitPointText.text = _hitPoint.ToString();

            RefreshColor();
            PlayScale(Vector3.zero, _baseScale, SpawnScaleDuration);
        }

        public void SetGridPosition(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;
        }

        public void SetGold(bool isGold)
        {
            IsGold = isGold;
            RefreshColor();
        }

        // Pulses toward DangerColor while this brick will breach the bottom on the next descend.
        public void SetDanger(bool isInDanger)
        {
            _isInDanger = isInDanger;

            if (!isInDanger && SpriteRenderer != null)
            {
                SpriteRenderer.color = _baseColor;
            }
        }

        private void RefreshColor()
        {
            _baseColor = IsGold ? GoldColor : GetHpColor(_hitPoint);

            if (SpriteRenderer != null && !_isInDanger)
            {
                SpriteRenderer.color = _baseColor;
            }
        }

        private static Color GetHpColor(int hitPoint)
        {
            int tier = Mathf.Max(0, hitPoint) / HpColorStep;
            int hueIndex = tier % HpColorPalette.Length;
            int shadeIndex = (tier / HpColorPalette.Length) % ShadeLevels;

            Color.RGBToHSV(HpColorPalette[hueIndex], out float h, out float s, out float v);
            v *= 1f - (shadeIndex / (float)ShadeLevels) * 0.5f;
            return Color.HSVToRGB(h, s, Mathf.Clamp01(v));
        }

        // Visually eases toward targetPosition -- the grid position is already updated by the
        // caller, so this is purely cosmetic.
        public void MoveTo(Vector3 targetPosition, float duration)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveToRoutine(targetPosition, duration));
        }

        private IEnumerator MoveToRoutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.EaseOutBack(Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
            _moveRoutine = null;
        }

        private void PlayScale(Vector3 from, Vector3 to, float duration)
        {
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScaleRoutine(from, to, duration));
        }

        private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration)
        {
            transform.localScale = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Easing.EaseOutBack(Mathf.Clamp01(elapsed / duration));
                transform.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            transform.localScale = to;
            _scaleRoutine = null;
        }

        public void DamageBrick(int damage)
        {
            if (_isDead) return;

            GameManager gm = GameManager.Instance;

            _hitPoint -= damage;
            HitPointText.text = Mathf.Max(0, _hitPoint).ToString();
            RefreshColor();

            gm.AddHitScore(damage);
            gm.SoundManager.Play(SoundType.HitBlock);

            if (_hitPoint <= 0)
            {
                _isDead = true;
                OnDestroyed?.Invoke(this);
                gm.AddDestroyScore(damage);
                gm.SoundManager.Play(SoundType.Destroyed);
                Destroy(gameObject);
                return;
            }

            PlayScale(_baseScale * HitPunchScale, _baseScale, HitPunchDuration);
        }
    }
}
