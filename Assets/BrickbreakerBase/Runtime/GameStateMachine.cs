using UnityEngine;
using UnityEngine.Events;

namespace BrickbreakerBase
{
    public class GameStateMachine : MonoBehaviour
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        public UnityEvent<GameState> OnStateChanged = new UnityEvent<GameState>();

        public void ChangeState(GameState newState)
        {
            if (newState == CurrentState) return;

            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
