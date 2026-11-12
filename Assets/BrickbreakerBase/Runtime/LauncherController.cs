using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace BrickbreakerBase
{
    // Aiming, input and firing. Drag inside the field to aim (a dotted preview line shows where
    // the ball will hit), release to fire every ball one after another from the launch point.
    public class LauncherController : MonoBehaviour
    {
        public Transform LaunchPosition;
        public Camera Camera;
        public TMP_Text BallCountText;
        public float LaunchDelay = 0.08f;

        // Large ball counts fire in groups instead of one full LaunchDelay per ball -- balls in
        // the same group only wait GroupedLaunchDelay between each other. Group size starts at 1
        // and grows by 1 for every GroupSizeStep balls owned.
        [Header("Launch Grouping (large ball counts)")]
        public float GroupedLaunchDelay = 0.05f;
        public int GroupSizeStep = 50;

        [Header("Aim Line")]
        public LineRenderer AimLineRenderer;
        public SpriteRenderer AimEndMarker;
        public float AimLineMaxDistance = 20f;

        // Aiming can only START inside this rectangle (roughly the playable field).
        [Header("Input restriction")]
        public Vector2 InputZoneMin = new Vector2(-3f, -6f);
        public Vector2 InputZoneMax = new Vector2(3f, 4f);
        public float MaxAimAngle = 75f;

        public UnityEvent OnShotFinished = new UnityEvent();
        public UnityEvent OnShotStarted = new UnityEvent();

        private Vector2 _direction;
        private bool _isPickingTarget;
        private bool _isAimValid;

        private int _activeBallCount;
        private bool _allLaunchedThisShot;
        private bool _hasFirstReturnX;
        private float _firstReturnX;
        private Vector3 _initialLaunchPosition;

        private LaunchManager _launchManager;

        // Initialized explicitly (not in Start) because the launcher starts disabled until the
        // first RestartGame, and Unity doesn't run Start on a disabled component.
        public void InitializeLauncher(LaunchManager launchManager)
        {
            _launchManager = launchManager;
            _launchManager.OnBallAdded.AddListener(HandleBallAdded);

            _initialLaunchPosition = LaunchPosition.position;
            HideAimLine();
            if (BallCountText != null) BallCountText.gameObject.SetActive(false);

            GameManager.Instance.BrickManager.OnGameOver.AddListener(HandleGameOver);

            enabled = false;
        }

        // Stops accepting input once the game is over -- ResetLauncher (called on restart)
        // re-enables this.
        private void HandleGameOver()
        {
            enabled = false;
        }

        private void HandleBallAdded(BallController ball)
        {
            ball.ReturnY = LaunchPosition.position.y;
            ball.OnReturned.AddListener(HandleBallReturned);
        }

        private void Update()
        {
            if (PointerInput.PressedThisFrame)
            {
                bool overUI = IsPointerOverUI();
                bool withinZone = IsWithinInputZone();
                bool isAiming = GameManager.Instance.StateMachine.CurrentState == GameState.Aiming;
                _isPickingTarget = !overUI && withinZone && isAiming;
            }

            if (_isPickingTarget && PointerInput.IsPressed)
            {
                Vector3 worldPos = Camera.ScreenToWorldPoint(PointerInput.Position);
                worldPos.z = LaunchPosition.position.z;
                Vector2 rawDirection = ((Vector2)worldPos - (Vector2)LaunchPosition.position).normalized;

                float angle = Vector2.SignedAngle(Vector2.up, rawDirection);
                _isAimValid = angle >= -MaxAimAngle && angle <= MaxAimAngle;

                // Out of range disables aiming entirely (no clamp-to-edge) -- direction/line just
                // freeze at the last valid value until the drag comes back within MaxAimAngle.
                if (_isAimValid)
                {
                    _direction = rawDirection;
                    UpdateAimLine();
                }
                else
                {
                    HideAimLine();
                }
            }

            if (PointerInput.ReleasedThisFrame && _isPickingTarget)
            {
                if (_isAimValid)
                {
                    Launch();
                }

                _isPickingTarget = false;
                HideAimLine();
            }
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool IsWithinInputZone()
        {
            Vector3 worldPos = Camera.ScreenToWorldPoint(PointerInput.Position);

            return worldPos.x >= InputZoneMin.x && worldPos.x <= InputZoneMax.x
                && worldPos.y >= InputZoneMin.y && worldPos.y <= InputZoneMax.y;
        }

        // Shows where the ball will actually hit -- not just a straight line to wherever the
        // finger is. Sweeps the same radius the real ball uses, otherwise the preview can show a
        // near-miss on a brick that the ball's actual width still clips.
        private static readonly RaycastHit2D[] _aimLineHitBuffer = new RaycastHit2D[8];

        private void UpdateAimLine()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = false;

            float ballRadius = _launchManager.Balls.Count > 0 ? _launchManager.Balls[0].WorldRadius : 0f;

            int count = Physics2D.CircleCast(LaunchPosition.position, ballRadius, _direction, filter, _aimLineHitBuffer, AimLineMaxDistance);

            Vector3 endPointWorld = (Vector3)((Vector2)LaunchPosition.position + _direction * AimLineMaxDistance);
            float closestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_aimLineHitBuffer[i].distance < closestDistance)
                {
                    closestDistance = _aimLineHitBuffer[i].distance;
                    endPointWorld = _aimLineHitBuffer[i].point;
                }
            }

            // AimLineRenderer is a child of LaunchPosition with useWorldSpace off, so point 0 is
            // local zero and the world-space hit point needs converting into that local space.
            AimLineRenderer.positionCount = 2;
            AimLineRenderer.SetPosition(0, Vector3.zero);
            AimLineRenderer.SetPosition(1, AimLineRenderer.transform.InverseTransformPoint(endPointWorld));

            AimEndMarker.enabled = true;
            AimEndMarker.transform.position = endPointWorld;
        }

        private void HideAimLine()
        {
            AimLineRenderer.positionCount = 0;
            AimEndMarker.enabled = false;
        }

        public void Launch()
        {
            GameManager.Instance.StateMachine.ChangeState(GameState.Shooting);
            OnShotStarted?.Invoke();
            if (BallCountText != null) BallCountText.gameObject.SetActive(true);
            GameManager.Instance.SoundManager.Play(SoundType.Shoot);
            StartCoroutine(LaunchCoroutine());
        }

        // Stops any balls still in flight and puts shot-tracking / the launch position back to how
        // they were before the first shot -- used when restarting the game.
        public void ResetLauncher()
        {
            enabled = true;

            StopAllCoroutines();

            foreach (BallController ball in _launchManager.Balls)
            {
                ball.Stop();
            }

            _activeBallCount = 0;
            _allLaunchedThisShot = false;
            _hasFirstReturnX = false;
            _isPickingTarget = false;
            LaunchPosition.position = _initialLaunchPosition;
            HideAimLine();
            if (BallCountText != null) BallCountText.gameObject.SetActive(false);
        }

        // Every ball shoots from the same start position in the same direction, offset in time by
        // LaunchDelay. No leader/follower bookkeeping needed: each ball's bounce is a
        // deterministic function of (direction, geometry), so it retraces the same path on its own.
        private IEnumerator LaunchCoroutine()
        {
            _activeBallCount = 0;
            _allLaunchedThisShot = false;
            _hasFirstReturnX = false;

            int totalBalls = _launchManager.Balls.Count;
            int groupSize = 1 + (totalBalls / GroupSizeStep);

            for (int i = 0; i < totalBalls; i++)
            {
                _launchManager.Balls[i].Shoot(LaunchPosition.position, _direction);
                _activeBallCount++;

                if (BallCountText != null) BallCountText.text = "x" + (totalBalls - _activeBallCount);

                bool completedGroup = (i + 1) % groupSize == 0;
                yield return new WaitForSeconds(completedGroup ? LaunchDelay : GroupedLaunchDelay);
            }

            if (BallCountText != null) BallCountText.gameObject.SetActive(false);
            _allLaunchedThisShot = true;
            CheckShotFinished();
        }

        // Lets the player skip waiting for every ball to bounce back down. The next launch position
        // is normally the X of whichever ball touches bottom first, so it is approximated here
        // with whichever still-flying ball is currently lowest -- then everything is force-stopped
        // and the shot finishes immediately.
        public void SkipShot()
        {
            StopAllCoroutines();

            if (!_hasFirstReturnX)
            {
                BallController lowestBall = FindLowestActiveBall();
                if (lowestBall != null)
                {
                    _hasFirstReturnX = true;
                    _firstReturnX = lowestBall.transform.position.x;
                }
            }

            foreach (BallController ball in _launchManager.Balls)
            {
                ball.Stop();
            }

            if (BallCountText != null) BallCountText.gameObject.SetActive(false);
            _activeBallCount = 0;
            _allLaunchedThisShot = true;
            CheckShotFinished();
        }

        // Skips the wave BEFORE any shot happens -- only valid while still picking a target (no
        // balls in flight). Fires the same "shot finished" trigger a completed shot would, without
        // touching the launch position.
        public void SkipWave()
        {
            if (_activeBallCount > 0) return;

            OnShotFinished?.Invoke();
        }

        private BallController FindLowestActiveBall()
        {
            BallController lowest = null;
            float lowestY = float.MaxValue;

            foreach (BallController ball in _launchManager.Balls)
            {
                if (!ball.gameObject.activeSelf) continue;

                if (ball.transform.position.y < lowestY)
                {
                    lowestY = ball.transform.position.y;
                    lowest = ball;
                }
            }

            return lowest;
        }

        private void HandleBallReturned(BallController ball)
        {
            if (!_hasFirstReturnX)
            {
                _hasFirstReturnX = true;
                _firstReturnX = ball.transform.position.x;
            }

            _activeBallCount--;
            CheckShotFinished();
        }

        private void CheckShotFinished()
        {
            if (_allLaunchedThisShot && _activeBallCount <= 0)
            {
                if (_hasFirstReturnX)
                {
                    Vector3 launchPos = LaunchPosition.position;
                    launchPos.x = _firstReturnX;
                    LaunchPosition.position = launchPos;
                }

                OnShotFinished?.Invoke();
            }
        }
    }
}
