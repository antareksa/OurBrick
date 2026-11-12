using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BrickbreakerBase
{
    // Owns the brick grid: spawning each wave, descending one row per wave, and the
    // bottom-row breach (lose HP, clear the bottom rows, or game over). Also drives the post-shot
    // sequence: collect coins -> advance wave -> descend -> spawn -> back to aiming.
    public class BrickManager : MonoBehaviour
    {
        public LauncherController Launcher;

        // World position of grid cell (0,0), and world distance between adjacent cells. Columns
        // 1-6 at x = -2.5..2.5, rows 0 (bottom) to SpawnRow (top).
        [Header("Grid")]
        public Vector2 GridOrigin = new Vector2(-3.5f, -3.5f);
        public Vector2 CellSize = new Vector2(1f, 1f);
        public int SpawnRow = 6;
        public Vector2Int SpawnColumnRange = new Vector2Int(1, 6);
        public int BottomRow = 0;

        [Header("Descend")]
        // Pause between bricks landing on the bottom and the bottom rows being resolved -- without
        // it they vanish the same frame, with no chance to read what hit you.
        public float BottomRowClearDelay = 0.5f;
        public float DescendMoveDuration = 0.25f;
        public int BottomRowsClearedOnBreach = 3;

        public UnityEvent OnGameOver = new UnityEvent();

        private bool _isGameOver;
        private readonly Dictionary<Vector2Int, BrickController> _bricks = new Dictionary<Vector2Int, BrickController>();

        public int GetBrickCount() => _bricks.Count;

        // Snapshot copy -- callers that destroy bricks while iterating would break on the live dictionary.
        public List<BrickController> GetAllBricks() => new List<BrickController>(_bricks.Values);

        public void Initialize(LauncherController launcher)
        {
            Launcher = launcher;
            Launcher.OnShotFinished.AddListener(HandleShotFinished);
        }

        // Clears every brick, resets the run state, and spawns wave 0 -- the whole game back to
        // its just-started state.
        public void RestartGame()
        {
            StopAllCoroutines();

            foreach (BrickController brick in _bricks.Values)
            {
                if (brick != null) Destroy(brick.gameObject);
            }
            _bricks.Clear();

            GameManager gm = GameManager.Instance;
            gm.ResetWave();
            gm.ResetCoin();
            gm.ResetScore();
            gm.ResetPlayDuration();
            gm.ResetBricksDestroyed();
            gm.CoinManager.ResetCoins();
            gm.SetHp(gm.GetMaxHp());
            _isGameOver = false;

            Launcher.ResetLauncher();
            gm.LaunchManager.ResetRoster();

            SpawnWaveBricks();
            RefreshDangerState();
            gm.StateMachine.ChangeState(GameState.Aiming);
        }

        private void HandleShotFinished()
        {
            if (_isGameOver) return;

            StartCoroutine(HandleShotFinishedRoutine());
        }

        // Each state change is what blocks player input -- the launcher only accepts aim/shoot
        // input while the state is Aiming, so nothing can be fired until this routine ends.
        private IEnumerator HandleShotFinishedRoutine()
        {
            GameManager gm = GameManager.Instance;

            gm.StateMachine.ChangeState(GameState.CheckCoin);
            yield return gm.CoinManager.CollectAllCoins();

            gm.StateMachine.ChangeState(GameState.AdvanceWave);
            gm.AdvanceWave();
            gm.SoundManager.Play(SoundType.WaveClear);
            yield return DescendBricksRoutine();

            if (_isGameOver) yield break;

            gm.StateMachine.ChangeState(GameState.SpawnWave);
            SpawnWaveBricks();
            RefreshDangerState();

            gm.StateMachine.ChangeState(GameState.Aiming);
        }

        // Every alive brick moves one row closer to the player. Rebuilds the lookup since the keys
        // (grid positions) are changing -- iterating and mutating the same dictionary isn't safe.
        private IEnumerator DescendBricksRoutine()
        {
            Dictionary<Vector2Int, BrickController> shifted = new Dictionary<Vector2Int, BrickController>();

            foreach (KeyValuePair<Vector2Int, BrickController> entry in _bricks)
            {
                BrickController brick = entry.Value;
                if (brick == null) continue;

                Vector2Int newPosition = new Vector2Int(entry.Key.x, entry.Key.y - 1);
                brick.SetGridPosition(newPosition);
                brick.MoveTo(GridToWorld(newPosition), DescendMoveDuration);
                shifted[newPosition] = brick;
            }

            _bricks.Clear();
            foreach (KeyValuePair<Vector2Int, BrickController> entry in shifted)
            {
                _bricks[entry.Key] = entry.Value;
            }

            // Let the ease finish before anything reacts to bricks being at the bottom.
            yield return new WaitForSeconds(DescendMoveDuration);

            if (!AnyBrickAtBottom()) yield break;

            yield return new WaitForSeconds(BottomRowClearDelay);

            bool survived = GameManager.Instance.TakeHit();

            if (survived)
            {
                ClearBottomRows(BottomRowsClearedOnBreach);
            }
            else
            {
                GameOver();
            }
        }

        // Flags every brick that will breach on the NEXT descend, so the warning is up while the
        // player aims -- hence the predictive y - 1.
        private void RefreshDangerState()
        {
            foreach (KeyValuePair<Vector2Int, BrickController> entry in _bricks)
            {
                if (entry.Value == null) continue;

                entry.Value.SetDanger(entry.Key.y - 1 <= BottomRow);
            }
        }

        // Post-shift check -- bricks have already moved, so this tests where they ARE.
        private bool AnyBrickAtBottom()
        {
            foreach (KeyValuePair<Vector2Int, BrickController> entry in _bricks)
            {
                if (entry.Key.y <= BottomRow) return true;
            }
            return false;
        }

        // Directly destroys every brick in the bottom rowCount rows -- no coin/score reward, since
        // this is an emergency save, not a normal kill.
        private void ClearBottomRows(int rowCount)
        {
            for (int i = 0; i < rowCount; i++)
            {
                foreach (BrickController brick in GetBricksInRow(BottomRow + i))
                {
                    _bricks.Remove(brick.GridPosition);
                    if (brick != null) Destroy(brick.gameObject);
                }
            }
        }

        private void SpawnWaveBricks()
        {
            int wave = GameManager.Instance.GetWave();
            int spawnCount = BrickConfig.GetBricksSpawned(wave);
            int baseHp = BrickConfig.GetBaseHP(wave);
            int tankCount = BrickConfig.GetTankCount(wave);
            int tankHp = BrickConfig.GetTankHP(wave);
            bool isGoldWave = BrickConfig.IsGoldWave(wave);

            for (int i = 0; i < spawnCount; i++)
            {
                if (!TryPickFreeSpawnPosition(out Vector2Int gridPosition)) continue;

                // The first tankCount bricks spawned this wave are tanks (2x HP).
                int hp = i < tankCount ? tankHp : baseHp;

                BrickController brick = WorldFactory.CreateBrick(transform);
                brick.transform.position = GridToWorld(gridPosition);
                brick.Spawn(hp, gridPosition);
                brick.OnDestroyed.AddListener(HandleBrickDestroyed);

                // Just one gold brick per gold wave -- the first one spawned.
                brick.SetGold(isGoldWave && i == 0);

                _bricks[gridPosition] = brick;
            }
        }

        private bool TryPickFreeSpawnPosition(out Vector2Int gridPosition)
        {
            const int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int column = Random.Range(SpawnColumnRange.x, SpawnColumnRange.y + 1);
                Vector2Int candidate = new Vector2Int(column, SpawnRow);

                if (!_bricks.ContainsKey(candidate))
                {
                    gridPosition = candidate;
                    return true;
                }
            }

            gridPosition = default;
            return false;
        }

        private void HandleBrickDestroyed(BrickController brick)
        {
            _bricks.Remove(brick.GridPosition);

            GameManager.Instance.AddBrickDestroyed();
            GameManager.Instance.CoinManager.SpawnCoin(brick.transform.position, brick.IsGold);
        }

        private void GameOver()
        {
            _isGameOver = true;
            GameManager.Instance.StateMachine.ChangeState(GameState.GameOver);
            OnGameOver?.Invoke();
        }

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector3(
                GridOrigin.x + gridPosition.x * CellSize.x,
                GridOrigin.y + gridPosition.y * CellSize.y,
                0f);
        }

        // ---- Grid queries (extension points for ball effects / skills) ----

        public List<BrickController> GetBricksInRow(int row)
        {
            List<BrickController> bricksInRow = new List<BrickController>();
            foreach (KeyValuePair<Vector2Int, BrickController> entry in _bricks)
            {
                if (entry.Key.y == row) bricksInRow.Add(entry.Value);
            }
            return bricksInRow;
        }

        public List<BrickController> GetBricksInColumn(int column)
        {
            List<BrickController> bricksInColumn = new List<BrickController>();
            foreach (KeyValuePair<Vector2Int, BrickController> entry in _bricks)
            {
                if (entry.Key.x == column) bricksInColumn.Add(entry.Value);
            }
            return bricksInColumn;
        }

        // reach = how many steps to walk along each direction (1 = immediate neighbors only).
        public List<BrickController> GetNeighbors(BrickController brick, bool diagonal, int reach = 1)
        {
            Vector2Int[] directions = diagonal
                ? new[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) }
                : new[] { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };

            List<BrickController> neighbors = new List<BrickController>();
            foreach (Vector2Int direction in directions)
            {
                for (int step = 1; step <= reach; step++)
                {
                    if (_bricks.TryGetValue(brick.GridPosition + direction * step, out BrickController neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }
            return neighbors;
        }
    }
}
