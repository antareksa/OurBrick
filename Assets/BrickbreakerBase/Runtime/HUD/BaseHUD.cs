using UnityEngine;

namespace BrickbreakerBase
{
    // Subscribes to the game state machine so any HUD can react to state changes without each one
    // wiring up the subscription itself. Override OnEnterState/OnExitState in a subclass to do
    // something on a specific transition -- both are no-ops by default.
    public abstract class BaseHUD : MonoBehaviour
    {
        protected GameState CurrentState { get; private set; }

        private bool _started;

        // Start (not Awake) -- the only lifecycle method guaranteed to run after every object's
        // Awake, so GameManager.Instance is safe to use regardless of creation order.
        protected virtual void Start()
        {
            _started = true;
            Subscribe();
        }

        // Re-subscribes when a disabled HUD comes back (OnDisable unsubscribes, and Start only
        // runs once), and re-syncs to whatever state it missed while it was off.
        protected virtual void OnEnable()
        {
            if (_started) Subscribe();
        }

        protected virtual void OnDisable()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.StateMachine != null)
            {
                gm.StateMachine.OnStateChanged.RemoveListener(HandleStateChanged);
            }
        }

        private void Subscribe()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.StateMachine == null) return;

            gm.StateMachine.OnStateChanged.RemoveListener(HandleStateChanged);
            gm.StateMachine.OnStateChanged.AddListener(HandleStateChanged);

            CurrentState = gm.StateMachine.CurrentState;
            OnEnterState(CurrentState);
        }

        private void HandleStateChanged(GameState newState)
        {
            OnExitState(CurrentState);
            CurrentState = newState;
            OnEnterState(newState);
        }

        protected virtual void OnEnterState(GameState state) { }
        protected virtual void OnExitState(GameState state) { }
    }
}
