using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BrickbreakerBase
{
    // Drop this on an empty GameObject in any scene and press Play -- it builds the whole game at
    // runtime (camera, walls, grid, balls, UI), so the package needs no prefabs or scene.
    // Everything tunable is a public field on the managers it creates (see the Inspector at
    // runtime) or a field below.
    [DefaultExecutionOrder(-1000)]
    public class BrickbreakerBootstrap : MonoBehaviour
    {
        [Header("Rules")]
        public int MaxHp = 3;
        public int OneBallCost = 40;
        public int SixBallCost = 220;
        public int SilverCoinValue = 15;
        public int GoldCoinValue = 100;

        [Header("Balls")]
        public float BallSpeed = 7f;
        public float LaunchDelay = 0.08f;

        [Header("Aim line")]
        // Dot/gap pairs per world unit of the dotted preview line.
        public float AimDotsPerUnit = 4f;

        [Header("Camera")]
        // Turns an existing Main Camera orthographic and frames the field; a camera is created
        // if the scene has none. Untick to keep your own camera setup.
        public bool ConfigureCamera = true;
        public float CameraSize = 5.8f;
        public Vector2 CameraCenter = new Vector2(0f, -0.25f);
        public Color BackgroundColor = new Color(0.07f, 0.08f, 0.12f, 1f);

        [Header("Field")]
        public Color WallColor = new Color(0.18f, 0.2f, 0.28f, 1f);
        // Y of the launcher -- balls are returned (and the next shot starts) at this height.
        public float LauncherY = -4.4f;

        [Header("Sounds (optional)")]
        // Ships empty. Add an entry per SoundType and assign a clip to hear it.
        public List<SoundEntry> Sounds = new List<SoundEntry>();

        private void Awake()
        {
            if (FindFirstObjectByType<GameManager>() != null)
            {
                Debug.LogWarning("[Brickbreaker Base] A game is already running in this scene -- ignoring this extra bootstrap.");
                return;
            }

            Build();
        }

        private void Build()
        {
            Camera camera = SetupCamera();
            EnsureEventSystem();

            Transform root = new GameObject("Brickbreaker Base (runtime)").transform;

            BuildWorld(root);

            // ---- Managers ----
            GameObject managers = new GameObject("Managers");
            managers.transform.SetParent(root, false);

            GameManager gm = managers.AddComponent<GameManager>();
            gm.MaxHp = MaxHp;
            gm.StateMachine = managers.AddComponent<GameStateMachine>();

            SoundManager sound = managers.AddComponent<SoundManager>();
            AudioSource audioSource = managers.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            sound.AudioSource = audioSource;
            sound.Sounds = Sounds;
            sound.SetClips(Sounds);
            gm.SoundManager = sound;

            CoinManager coins = managers.AddComponent<CoinManager>();
            coins.SilverCoinValue = SilverCoinValue;
            coins.GoldCoinValue = GoldCoinValue;
            gm.CoinManager = coins;

            Transform collectPoint = new GameObject("CollectPoint").transform;
            collectPoint.SetParent(root, false);
            collectPoint.position = new Vector3(-4.6f, -5.4f, 0f);
            gm.CollectPoint = collectPoint;

            // ---- UI (the launcher needs the "xN" label it builds) ----
            Canvas canvas = UIFactory.CreateCanvas("Brickbreaker Canvas");
            canvas.transform.SetParent(root, false);
            HudBuilder.Build(canvas.transform, out TMP_Text ballCountText);

            // ---- Launcher + balls + bricks ----
            GameObject launcherGo = new GameObject("Launcher");
            launcherGo.transform.SetParent(root, false);
            launcherGo.transform.position = new Vector3(0f, LauncherY, 0f);

            LauncherController launcher = launcherGo.AddComponent<LauncherController>();
            launcher.LaunchPosition = launcherGo.transform;
            launcher.Camera = camera;
            launcher.LaunchDelay = LaunchDelay;
            launcher.BallCountText = ballCountText;

            GameObject aimLineGo = new GameObject("AimLine");
            aimLineGo.transform.SetParent(launcherGo.transform, false);
            LineRenderer aimLine = aimLineGo.AddComponent<LineRenderer>();
            WorldFactory.ConfigureAimLine(aimLine, AimDotsPerUnit);
            launcher.AimLineRenderer = aimLine;
            launcher.AimEndMarker = WorldFactory.CreateAimMarker(launcherGo.transform);

            LaunchManager launchManager = managers.AddComponent<LaunchManager>();
            launchManager.LauncherController = launcher;
            launchManager.BallSpeed = BallSpeed;
            launchManager.OneBallCost = OneBallCost;
            launchManager.SixBallCost = SixBallCost;
            gm.LaunchManager = launchManager;

            GameObject bricksGo = new GameObject("Bricks");
            bricksGo.transform.SetParent(root, false);
            BrickManager brickManager = bricksGo.AddComponent<BrickManager>();
            gm.BrickManager = brickManager;

            // Order matters: the launcher subscribes to the roster and to BrickManager.OnGameOver,
            // so both must exist before it initializes, and the roster is built last so the launcher
            // is already listening for the first ball.
            brickManager.Initialize(launcher);
            launcher.InitializeLauncher(launchManager);
            launchManager.ResetRoster();
        }

        private Camera SetupCamera()
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                camera = go.AddComponent<Camera>();
            }

            if (FindFirstObjectByType<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            if (ConfigureCamera)
            {
                camera.orthographic = true;
                camera.orthographicSize = CameraSize;
                camera.transform.position = new Vector3(CameraCenter.x, CameraCenter.y, -10f);
                camera.transform.rotation = Quaternion.identity;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = BackgroundColor;
            }

            return camera;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.UI.InputSystemUIInputModule module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        // Playfield: 6 columns at x = -3..3 (bricks sit at x = -2.5..2.5), top wall at y = 4.
        // There is deliberately no floor -- balls fall out the bottom and are returned.
        private void BuildWorld(Transform root)
        {
            Transform world = new GameObject("World").transform;
            world.SetParent(root, false);

            WorldFactory.CreateWall("WallLeft", new Vector2(-3.5f, 0f), new Vector2(1f, 14f), WallColor, world);
            WorldFactory.CreateWall("WallRight", new Vector2(3.5f, 0f), new Vector2(1f, 14f), WallColor, world);
            WorldFactory.CreateWall("WallTop", new Vector2(0f, 4.5f), new Vector2(8f, 1f), WallColor, world);

            // Danger line under the bottom brick row -- visual only.
            WorldFactory.CreateLine("DangerLine", new Vector2(0f, -4f), new Vector2(6f, 0.04f), new Color(1f, 1f, 1f, 0.25f), world);
        }
    }
}
