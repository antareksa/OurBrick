using TMPro;
using UnityEngine.UI;

namespace BrickbreakerBase
{
    public class BallHUD : BaseHUD
    {
        public TMP_Text CoinCountText;
        public Button AddOneBallButton;
        public Button AddSixBallButton;
        public TMP_Text AddOneBallLabel;
        public TMP_Text AddSixBallLabel;

        protected override void Start()
        {
            base.Start();

            GameManager gm = GameManager.Instance;

            AddOneBallButton.onClick.AddListener(() => OnBuyBall(1));
            AddSixBallButton.onClick.AddListener(() => OnBuyBall(6));

            AddOneBallLabel.text = "BUY 1  (" + gm.LaunchManager.GetBallCost(1) + ")";
            AddSixBallLabel.text = "BUY 6  (" + gm.LaunchManager.GetBallCost(6) + ")";

            gm.OnCoinChanged.AddListener(HandleCoinChanged);

            // A subscription only fires on future changes, not the value that already existed.
            HandleCoinChanged(gm.GetCoin());
        }

        private void HandleCoinChanged(int coin)
        {
            CoinCountText.text = "COIN " + coin;
            RefreshButtons();
        }

        // Balls can only be bought while Aiming -- mid-shot isn't a valid time to grow the roster.
        protected override void OnEnterState(GameState state)
        {
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.LaunchManager == null || gm.StateMachine == null) return;
            if (AddOneBallButton == null || AddSixBallButton == null) return;

            bool isAiming = gm.StateMachine.CurrentState == GameState.Aiming;

            AddOneBallButton.interactable = isAiming && gm.LaunchManager.IsCanBuyBall(1);
            AddSixBallButton.interactable = isAiming && gm.LaunchManager.IsCanBuyBall(6);
        }

        private void OnBuyBall(int totalBall)
        {
            GameManager.Instance.LaunchManager.BuyBall(totalBall);
        }
    }
}
