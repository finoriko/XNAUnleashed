using System;
using Microsoft.Xna.Framework;
namespace XELibrary
{
    public interface IGameState
    {
        GameState Value { get; }
    }

    public interface IGameStateManager
    {
        event EventHandler OnStateChange;
        GameState State { get; }
        void PopState();
        void PushState(GameState state, PlayerIndex? playerIndexInControl);
        bool ContainsState(GameState state);
        void ChangeState(GameState newState, PlayerIndex? playerIndexInControl);
    }
}
