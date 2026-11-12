using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BrickbreakerBase
{
    // Owns the roster of balls available to launch -- separate from LauncherController, which owns
    // aiming/input/firing. The place to plug in different ball types later (e.g. a gacha roll in
    // AddBall instead of always creating a basic ball).
    public class LaunchManager : MonoBehaviour
    {
        public LauncherController LauncherController;

        public float BallSpeed = 7f;

        // Flat per-bundle costs, not a per-ball rate -- 6 balls isn't just 6x the 1-ball price,
        // it's a discounted bundle.
        public int OneBallCost = 40;
        public int SixBallCost = 220;

        public UnityEvent<BallController> OnBallAdded = new UnityEvent<BallController>();

        private readonly List<BallController> _balls = new List<BallController>();

        public IReadOnlyList<BallController> Balls => _balls;

        // Destroys every owned ball and rebuilds the starting roster (one basic ball) -- used at
        // game start and on restart, so balls bought before losing don't carry into the next run.
        public void ResetRoster()
        {
            foreach (BallController ball in _balls)
            {
                if (ball != null) Destroy(ball.gameObject);
            }
            _balls.Clear();

            AddBall();
        }

        public BallController AddBall()
        {
            BallController ball = WorldFactory.CreateBall(transform, BallSpeed);
            ball.Stop();
            ball.name = $"Ball_{_balls.Count}";

            _balls.Add(ball);
            OnBallAdded?.Invoke(ball);

            return ball;
        }

        // Only 1-ball and 6-ball bundles have a defined price -- anything else falls back to the
        // 1-ball rate times quantity.
        public int GetBallCost(int totalBall)
        {
            if (totalBall == 6) return SixBallCost;
            return OneBallCost * totalBall;
        }

        public bool IsCanBuyBall(int totalBall)
        {
            return GameManager.Instance.GetCoin() >= GetBallCost(totalBall);
        }

        public void BuyBall(int totalBall)
        {
            if (!IsCanBuyBall(totalBall)) return;
            if (!GameManager.Instance.TrySpendCoin(GetBallCost(totalBall))) return;

            for (int i = 0; i < totalBall; i++)
            {
                AddBall();
            }
        }
    }
}
