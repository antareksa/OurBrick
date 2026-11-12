using UnityEngine;
using UnityEngine.UI;

namespace BrickbreakerBase
{
    public class MainMenuHUD : BaseHUD
    {
        public GameObject PopupRoot;

        // The in-game HUD is hidden behind the menu rather than shown with dead buttons.
        public GameObject GameHudRoot;

        public Button StartButton;
        public Button QuitButton;

        protected override void Start()
        {
            base.Start();

            StartButton.onClick.AddListener(HandleStartClicked);
            QuitButton.onClick.AddListener(HandleQuitClicked);

            Open();
        }

        public void Open()
        {
            PopupRoot.SetActive(true);
            if (GameHudRoot != null) GameHudRoot.SetActive(false);

            GameManager.Instance.StateMachine.ChangeState(GameState.MainMenu);
        }

        public void Close()
        {
            PopupRoot.SetActive(false);
            if (GameHudRoot != null) GameHudRoot.SetActive(true);
        }

        private void HandleStartClicked()
        {
            Close();
            GameManager.Instance.BrickManager.RestartGame();
        }

        private static void HandleQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
