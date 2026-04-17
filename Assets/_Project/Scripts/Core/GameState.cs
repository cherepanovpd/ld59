// Path: Assets/_Project/Scripts/Core/GameState.cs
namespace Project.Core
{
    /// <summary>
    /// Simplified game state for Ludum Dare 59.
    /// Only three states: Intro (initial loading), Playing, Paused.
    /// </summary>
    public enum GameState
    {
        /// <summary> Initial loading / intro screen </summary>
        Intro,
        /// <summary> Gameplay is active </summary>
        Playing,
        /// <summary> Game is paused (time scale = 0) </summary>
        Paused
    }
}