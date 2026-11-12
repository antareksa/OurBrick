using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrickbreakerBase
{
    public class GameOverHUD : BaseHUD
    {
        public GameObject PopupRoot;
        public Button RestartButton;
        public Button MenuButton;
        public MainMenuHUD MainMenuHUD;

        public TMP_Text TotalScoreText;
        public TMP_Text TotalWaveText;
        public TMP_Text DurationText;
        public TMP_Text TotalBricksDestroyedText;

        protected override void Start()
        {
            base.Start();

            GameManager.Instance.BrickManager.OnGameOver.AddListener(HandleGameOver);
            RestartButton.onClick.AddListener(OnRestartButtonClicked);
            MenuButton.onClick.AddListener(OnMenuButtonClicked);

            PopupRoot.SetActive(false);
        }

        private void HandleGameOver()
        {
            GameManager gm = GameManager.Instance;

            TotalScoreText.text = "Score: " + gm.GetScore();
            // +1 to match the in-game wave display -- GetWave() is 0-indexed internally.
            TotalWaveText.text = "Wave reached: " + (gm.GetWave() + 1);
            DurationText.text = "Time: " + FormatDuration(gm.GetPlayDuration());
            TotalBricksDestroyedText.text = "Bricks destroyed: " + gm.GetTotalBricksDestroyed();

            PopupRoot.SetActive(true);
        }

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void OnRestartButtonClicked()
        {
            PopupRoot.SetActive(false);
            GameManager.Instance.BrickManager.RestartGame();
        }

        private void OnMenuButtonClicked()
        {
            PopupRoot.SetActive(false);
            MainMenuHUD.Open();
        }
    }
}
