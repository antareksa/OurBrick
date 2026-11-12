using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrickbreakerBase
{
    public class ScoreHUD : BaseHUD
    {
        public TMP_Text ScoreText;
        public TMP_Text WaveText;
        public Button SkipWaveButton;
        public Button SkipShotButton;

        protected override void Start()
        {
            base.Start();

            GameManager gm = GameManager.Instance;

            SkipWaveButton.onClick.AddListener(() => gm.LaunchManager.LauncherController.SkipWave());
            SkipShotButton.onClick.AddListener(() => gm.LaunchManager.LauncherController.SkipShot());

            gm.OnScoreChanged.AddListener(HandleScoreChanged);
            gm.OnWaveChanged.AddListener(HandleWaveChanged);

            // A subscription only fires on future changes, not the value that already existed.
            HandleScoreChanged(gm.GetScore());
            HandleWaveChanged(gm.GetWave());
        }

        // Skip Shot only makes sense while balls are in flight; Skip Wave only before firing.
        protected override void OnEnterState(GameState state)
        {
            if (SkipShotButton != null) SkipShotButton.interactable = state == GameState.Shooting;
            if (SkipWaveButton != null) SkipWaveButton.interactable = state == GameState.Aiming;
        }

        private void HandleScoreChanged(int score)
        {
            ScoreText.text = "SCORE " + score;
        }

        private void HandleWaveChanged(int wave)
        {
            WaveText.text = "WAVE " + (wave + 1);
        }
    }
}
