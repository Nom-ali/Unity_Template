
using UnityEngine;

[System.Serializable]
public class GameStatus
{
    [MyBox.ReadOnly,SerializeField] private GameState gameStatus = GameState.None;
    public GameState CurrentGameState => gameStatus;

    public void SetGameStatus(GameState gameStatus)
    {
        this.gameStatus = gameStatus;
        Debug.Log("Game Status changed: " + this.gameStatus);
    }

    public void Reset()
    {
        gameStatus = GameState.None;
        Debug.Log("Game Status Reset: " + this.gameStatus);
    }

    [System.Serializable]
    public enum GameState
    {
        None, LevelComplete, LevelFail, GamePause, Reward
    }
}



