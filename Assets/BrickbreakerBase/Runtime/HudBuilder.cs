using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrickbreakerBase
{
    // Builds the whole UI in code and wires the HUD components to it: the in-game HUD (score,
    // wave, HP pips, coin + buy-ball buttons, skip buttons), the main menu, and the game-over
    // screen. Layout is in 1920x1080 reference pixels.
    internal static class HudBuilder
    {
        private static readonly Color Light = Color.white;
        private static readonly Color Dim = new Color(0.8f, 0.85f, 0.95f, 1f);

        // ballCountText is handed back because the launcher (not a HUD) owns that label.
        public static void Build(Transform canvasRoot, out TMP_Text ballCountText)
        {
            // HUD components live on this always-active object, NOT on the panels below -- the
            // game HUD is switched off behind the main menu, and a disabled component can't react
            // to anything.
            GameObject controllers = new GameObject("HudControllers");
            controllers.transform.SetParent(canvasRoot, false);

            RectTransform gameHud = UIFactory.CreateRect("GameHUD", canvasRoot);
            UIFactory.Stretch(gameHud);

            ScoreHUD scoreHud = controllers.AddComponent<ScoreHUD>();
            HealthHUD healthHud = controllers.AddComponent<HealthHUD>();
            BallHUD ballHud = controllers.AddComponent<BallHUD>();

            // ---- Top-left: score + wave ----
            scoreHud.ScoreText = UIFactory.CreateText(gameHud, "Score", "SCORE 0", 44f, TextAlignmentOptions.TopLeft, Light);
            UIFactory.Anchor(scoreHud.ScoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(700f, 60f));

            scoreHud.WaveText = UIFactory.CreateText(gameHud, "Wave", "WAVE 1", 36f, TextAlignmentOptions.TopLeft, Dim);
            UIFactory.Anchor(scoreHud.WaveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -90f), new Vector2(700f, 50f));

            // ---- Top-right: HP pips ----
            RectTransform pips = UIFactory.CreateRect("HealthPips", gameHud);
            UIFactory.Anchor(pips, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -35f), new Vector2(500f, 50f));

            HorizontalLayoutGroup pipLayout = pips.gameObject.AddComponent<HorizontalLayoutGroup>();
            pipLayout.spacing = 10f;
            pipLayout.childAlignment = TextAnchor.UpperRight;
            pipLayout.childControlWidth = false;
            pipLayout.childControlHeight = false;
            pipLayout.childForceExpandWidth = false;
            pipLayout.childForceExpandHeight = false;

            healthHud.PipContainer = pips;

            // ---- Bottom-left: coins + buy-ball buttons ----
            ballHud.CoinCountText = UIFactory.CreateText(gameHud, "Coin", "COIN 0", 40f, TextAlignmentOptions.BottomLeft, new Color(1f, 0.84f, 0.1f, 1f));
            UIFactory.Anchor(ballHud.CoinCountText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 190f), new Vector2(500f, 50f));

            ballHud.AddOneBallButton = UIFactory.CreateButton(gameHud, "BuyOne", "BUY 1", new Vector2(260f, 70f), out ballHud.AddOneBallLabel);
            UIFactory.Anchor((RectTransform)ballHud.AddOneBallButton.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 100f), new Vector2(260f, 70f));

            ballHud.AddSixBallButton = UIFactory.CreateButton(gameHud, "BuySix", "BUY 6", new Vector2(260f, 70f), out ballHud.AddSixBallLabel);
            UIFactory.Anchor((RectTransform)ballHud.AddSixBallButton.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(320f, 100f), new Vector2(260f, 70f));

            // ---- Bottom-right: skip buttons ----
            scoreHud.SkipWaveButton = UIFactory.CreateButton(gameHud, "SkipWave", "SKIP WAVE", new Vector2(260f, 70f), out TMP_Text skipWaveLabel);
            UIFactory.Anchor((RectTransform)scoreHud.SkipWaveButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 190f), new Vector2(260f, 70f));

            scoreHud.SkipShotButton = UIFactory.CreateButton(gameHud, "SkipShot", "SKIP SHOT", new Vector2(260f, 70f), out TMP_Text skipShotLabel);
            UIFactory.Anchor((RectTransform)scoreHud.SkipShotButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 100f), new Vector2(260f, 70f));

            // ---- Bottom-center: "xN" balls left to fire ----
            ballCountText = UIFactory.CreateText(gameHud, "BallCount", "x0", 44f, TextAlignmentOptions.Center, Light);
            UIFactory.Anchor(ballCountText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(300f, 60f));

            // ---- Main menu (drawn above the game HUD) ----
            Image menuPanel = UIFactory.CreatePanel(canvasRoot, "MainMenu");

            TMP_Text title = UIFactory.CreateText(menuPanel.transform, "Title", "BRICK BREAKER", 110f, TextAlignmentOptions.Center, Light);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1400f, 140f));

            Button startButton = UIFactory.CreateButton(menuPanel.transform, "Start", "START", new Vector2(380f, 90f), out TMP_Text startLabel);
            UIFactory.Anchor((RectTransform)startButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(380f, 90f));

            Button quitButton = UIFactory.CreateButton(menuPanel.transform, "Quit", "QUIT", new Vector2(380f, 90f), out TMP_Text quitLabel);
            UIFactory.Anchor((RectTransform)quitButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(380f, 90f));

            // ---- Game over ----
            Image overPanel = UIFactory.CreatePanel(canvasRoot, "GameOver");

            TMP_Text overTitle = UIFactory.CreateText(overPanel.transform, "Title", "GAME OVER", 100f, TextAlignmentOptions.Center, Light);
            UIFactory.Anchor(overTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1400f, 130f));

            TMP_Text score = CreateStatLine(overPanel.transform, "Score", 170f);
            TMP_Text wave = CreateStatLine(overPanel.transform, "Wave", 110f);
            TMP_Text time = CreateStatLine(overPanel.transform, "Time", 50f);
            TMP_Text bricks = CreateStatLine(overPanel.transform, "Bricks", -10f);

            Button restartButton = UIFactory.CreateButton(overPanel.transform, "Restart", "RESTART", new Vector2(340f, 90f), out TMP_Text restartLabel);
            UIFactory.Anchor((RectTransform)restartButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200f, -150f), new Vector2(340f, 90f));

            Button menuButton = UIFactory.CreateButton(overPanel.transform, "Menu", "MENU", new Vector2(340f, 90f), out TMP_Text menuLabel);
            UIFactory.Anchor((RectTransform)menuButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, -150f), new Vector2(340f, 90f));

            // ---- Menu + game-over controllers ----
            MainMenuHUD mainMenuHud = controllers.AddComponent<MainMenuHUD>();
            mainMenuHud.PopupRoot = menuPanel.gameObject;
            mainMenuHud.GameHudRoot = gameHud.gameObject;
            mainMenuHud.StartButton = startButton;
            mainMenuHud.QuitButton = quitButton;

            GameOverHUD gameOverHud = controllers.AddComponent<GameOverHUD>();
            gameOverHud.PopupRoot = overPanel.gameObject;
            gameOverHud.RestartButton = restartButton;
            gameOverHud.MenuButton = menuButton;
            gameOverHud.MainMenuHUD = mainMenuHud;
            gameOverHud.TotalScoreText = score;
            gameOverHud.TotalWaveText = wave;
            gameOverHud.DurationText = time;
            gameOverHud.TotalBricksDestroyedText = bricks;

            overPanel.gameObject.SetActive(false);
        }

        private static TMP_Text CreateStatLine(Transform parent, string name, float y)
        {
            TMP_Text line = UIFactory.CreateText(parent, name, "", 48f, TextAlignmentOptions.Center, Dim);
            UIFactory.Anchor(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1000f, 60f));
            return line;
        }
    }
}
