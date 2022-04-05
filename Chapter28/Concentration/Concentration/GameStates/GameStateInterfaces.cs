using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using XELibrary;

namespace Concentration
{
    public interface ITitleIntroState : IGameState { }
    public interface IStartMenuState : IGameState { }
    public interface IOptionsMenuState : IGameState { }
    public interface IPausedState : IGameState { }
    public interface ILostGameState : IGameState { }
    public interface IHelpState : IGameState { }
    public interface ICreditsState : IGameState { }
    public interface IMultiplayerMenuState : IGameState { }
    public interface IHighScoresState : IGameState
    {
        void SaveHighScore();
        bool AlwaysDisplay { get; set;  }
    }
    public interface IPlayingState : IGameState
    {
        void StartGame(int numberOfPlayers);
        void PlayerLeft(byte gamerId);
        void JoinInProgressGame(NetworkGamer gamer, int numberOfPlayers);
        int Score { get; }
    }
    public interface IFadingState : IGameState
    {
        Color Color { get; set; }
    }
    public interface IMessageDialogState : IGameState
    {
        string Message { get; set; }
        bool IsError { get; set; }
    }
    public interface INetworkMenuState : IGameState
    {
        NetworkSessionType NetworkSessionType { get; set; }
    }
    public interface ISessionListState : IGameState
    {
        NetworkSessionType NetworkSessionType { get; set; }
    }
    public interface ISessionLobbyState : IGameState
    {
        void InviteAcceptedEventHandler(object sender, InviteAcceptedEventArgs e);
    }
    public interface IWonGameState : IGameState
    {
        string Gamertag { get; set;  }
    }


}
