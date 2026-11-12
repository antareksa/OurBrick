using UnityEngine;
using UnityEngine.Events;

namespace BrickbreakerBase
{
    // Run-wide state (coin, wave, score, HP) and the references every other system looks each
    // other up through. Built and wired by BrickbreakerBootstrap.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public LaunchManager LaunchManager;
        public BrickManager BrickManager;
        public CoinManager CoinManager;
        public SoundManager SoundManager;
        public GameStateMachine StateMachine;

        // Where spawned coins animate to before their value gets added.
        public Transform CollectPoint;

        public int MaxHp = 3;

        public float BaseScoreHit = 1f;
        public float BaseScoreDestroy = 5f;

        public UnityEvent<int> OnCoinChanged = new UnityEvent<int>();
        public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();
        public UnityEvent<int> OnWaveChanged = new UnityEvent<int>();
        public UnityEvent<int> OnHpChanged = new UnityEvent<int>();

        private int _coin;
        private int _wave;
        private int _score;
        private int _hp;
        private int _totalBricksDestroyed;
        private float _runStartTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- Coin ----

        public int GetCoin() => _coin;

        public void AddCoin(int coin)
        {
            _coin += coin;
            OnCoinChanged?.Invoke(_coin);
        }

        // Checks funds first -- reports success so callers (buying balls) know whether to go through.
        public bool TrySpendCoin(int coin)
        {
            if (_coin < coin) return false;

            _coin -= coin;
            OnCoinChanged?.Invoke(_coin);
            return true;
        }

        public void ResetCoin()
        {
            _coin = 0;
            OnCoinChanged?.Invoke(_coin);
        }

        // ---- Wave ----

        public int GetWave() => _wave;

        public void AdvanceWave()
        {
            _wave++;
            OnWaveChanged?.Invoke(_wave);
        }

        public void ResetWave()
        {
            _wave = 0;
            OnWaveChanged?.Invoke(_wave);
        }

        // ---- Score ----

        public int GetScore() => _score;

        public void AddHitScore(int damage)
        {
            // _wave + 1: _wave is 0 during the first wave, which would otherwise zero out all score
            // from it entirely.
            _score += Mathf.RoundToInt(damage * (_wave + 1) * BaseScoreHit);
            OnScoreChanged?.Invoke(_score);
        }

        public void AddDestroyScore(int damage)
        {
            _score += Mathf.RoundToInt(damage * (_wave + 1) * BaseScoreDestroy);
            OnScoreChanged?.Invoke(_score);
        }

        public void ResetScore()
        {
            _score = 0;
            OnScoreChanged?.Invoke(_score);
        }

        // ---- Run stats ----

        public int GetTotalBricksDestroyed() => _totalBricksDestroyed;
        public void AddBrickDestroyed() => _totalBricksDestroyed++;
        public void ResetBricksDestroyed() => _totalBricksDestroyed = 0;

        public void ResetPlayDuration() => _runStartTime = Time.time;
        public float GetPlayDuration() => Time.time - _runStartTime;

        // ---- HP ----

        public int GetHp() => _hp;
        public int GetMaxHp() => MaxHp;

        public void SetHp(int hp)
        {
            _hp = Mathf.Clamp(hp, 0, GetMaxHp());
            OnHpChanged?.Invoke(_hp);
        }

        // Always takes the hit (decrements), then reports whether the player is still alive. The
        // hit that brings it to 0 is a death blow on that same hit, not a save that only fails
        // next time.
        public bool TakeHit()
        {
            SetHp(_hp - 1);
            return _hp > 0;
        }
    }
}
