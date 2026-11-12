using UnityEngine;
using UnityEngine.Events;

namespace BrickbreakerBase
{
    // A ball moves by sweeping its own circle forward each frame instead of using Rigidbody
    // physics. Each frame it asks "how far can I go before I'd touch something", stops exactly
    // there, and reflects off that collider's nearest face. That makes every bounce a pure function
    // of position + direction + geometry, so every ball fired in the same direction retraces the
    // same path -- no physics-step timing involved.
    public class BallController : MonoBehaviour
    {
        public float Speed = 7f;
        public string BounceTag = WorldFactory.BounceTag;

        // Avoids the player watching one ball bounce forever: every RampInterval seconds spent
        // still moving, Speed is multiplied by RampMultiplier. Reset on each launch.
        [Header("Speed Ramp")]
        public float RampInterval = 3f;
        public float RampMultiplier = 1.5f;

        // Below this Y the ball has flown past the bottom of the field and is returned (stopped) --
        // there is no bottom wall. Set by whoever launches it (the launcher's own height).
        public float ReturnY;
        public UnityEvent<BallController> OnReturned = new UnityEvent<BallController>();

        private const int MaxBouncesPerFrame = 8;

        // Small gap kept between the ball and a surface it just touched, so the next cast from the
        // same spot doesn't immediately re-detect that collider at ~0 distance.
        private const float SkinWidth = 0.01f;

        private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];

        public float WorldRadius => _worldRadius;

        private CircleCollider2D _collider;
        private BaseBallHitEffect[] _hitEffects;
        private float _worldRadius;
        private Vector2 _direction;
        private bool _isMoving;
        private float _baseSpeed;
        private float _rampTimer;

        // Persists across frames: a phantom re-hit against the wall we just bounced off can happen
        // on the very next frame's first cast. Only cleared once a cast confirms open space ahead.
        private Collider2D _lastHitCollider;

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
            _worldRadius = _collider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            _baseSpeed = Speed;
        }

        public void Configure(float speed)
        {
            Speed = speed;
            _baseSpeed = speed;
        }

        public void Shoot(Vector3 startPosition, Vector2 direction)
        {
            gameObject.SetActive(true);
            transform.position = startPosition;
            _direction = direction.normalized;
            _isMoving = true;
            _lastHitCollider = null;
            Speed = _baseSpeed;
            _rampTimer = 0f;
        }

        public void Stop()
        {
            _isMoving = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isMoving) return;

            _rampTimer += Time.deltaTime;
            if (_rampTimer >= RampInterval)
            {
                _rampTimer -= RampInterval;
                Speed *= RampMultiplier;
            }

            float remainingDistance = Speed * Time.deltaTime;
            int bounces = 0;

            while (remainingDistance > 0f && bounces < MaxBouncesPerFrame)
            {
                RaycastHit2D hit = CastIgnoringSelf(transform.position, _direction, remainingDistance, _lastHitCollider);

                if (!hit)
                {
                    transform.position += (Vector3)(_direction * remainingDistance);
                    remainingDistance = 0f;
                    _lastHitCollider = null;
                    break;
                }

                float moveDistance = Mathf.Max(hit.distance - SkinWidth, 0f);
                transform.position += (Vector3)(_direction * moveDistance);
                remainingDistance -= hit.distance;

                // CircleCast's raw normal blends continuously right at a collider's corner, and the
                // exact sub-pixel contact angle varies by a hair between balls. Snapping to the
                // closest of the collider's own face directions collapses that jitter so every ball
                // gets the same result.
                Vector2 normal = SnapNormalToNearestFace(hit.collider, hit.normal);

                // Stopping short of the wall isn't enough near a corner, where a second collider can
                // already be touching the ball. Push clear of THIS collider using its snapped normal.
                transform.position += (Vector3)(normal * SkinWidth);

                bool handled = false;

                if (hit.collider.TryGetComponent<BrickController>(out BrickController brickController))
                {
                    handled = true;
                    OnHitBrick(brickController);
                }

                if (hit.collider.CompareTag(BounceTag))
                {
                    handled = true;
                    _direction = Reflect(_direction, normal);
                    GameManager.Instance.SoundManager.Play(SoundType.WallBounce);
                }

                if (!handled) break;

                _lastHitCollider = hit.collider;
                bounces++;
            }

            if (_isMoving && transform.position.y <= ReturnY)
            {
                Stop();
                GameManager.Instance.SoundManager.Play(SoundType.BallReturn);
                OnReturned?.Invoke(this);
            }
        }

        // Sweeps the ball's own radius forward so the reported hit distance is where its edge
        // would touch, not its center -- and skips this ball, any other ball, and whatever collider
        // it just bounced off ("not the same collider object" can't flake the way a distance
        // threshold can).
        private RaycastHit2D CastIgnoringSelf(Vector2 origin, Vector2 direction, float distance, Collider2D excludeCollider)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = false;

            int count = Physics2D.CircleCast(origin, _worldRadius, direction, filter, _hitBuffer, distance);

            RaycastHit2D closest = default;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hitBuffer[i];

                if (hit.collider.gameObject == gameObject) continue;
                if (hit.collider == excludeCollider) continue;
                if (hit.collider.GetComponent<BallController>() != null) continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closest = hit;
                }
            }

            return closest;
        }

        // Picks whichever of the collider's own 4 box-face directions is closest to the raw contact
        // normal. For a flat-face hit this is a no-op; near a corner it discretizes an otherwise
        // continuously-varying value.
        private static Vector2 SnapNormalToNearestFace(Collider2D collider, Vector2 rawNormal)
        {
            Transform t = collider.transform;
            Vector2 right = t.right;
            Vector2 up = t.up;

            Vector2[] faceNormals = { right, -right, up, -up };

            Vector2 best = faceNormals[0];
            float bestAlignment = float.MinValue;

            foreach (Vector2 candidate in faceNormals)
            {
                float alignment = Vector2.Dot(rawNormal, candidate);
                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    best = candidate;
                }
            }

            return best;
        }

        // X = D - 2 * (D . A) * A
        private static Vector2 Reflect(Vector2 direction, Vector2 normal)
        {
            return direction - 2f * Vector2.Dot(direction, normal) * normal;
        }

        private void OnHitBrick(BrickController brickController)
        {
            if (brickController == null) return;

            if (_hitEffects == null) _hitEffects = GetComponents<BaseBallHitEffect>();

            foreach (BaseBallHitEffect hitEffect in _hitEffects)
            {
                hitEffect.OnHitBrick(brickController);
            }
        }
    }
}
