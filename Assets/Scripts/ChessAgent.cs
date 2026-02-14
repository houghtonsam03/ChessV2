using UnityEngine;

public abstract class ChessAgent : MonoBehaviour
{
    // Game Information
    private bool isWhite;
    private string gameState;

    public void StartAgent(bool white)
    {
        isWhite = white;
    }

    public void setState(string state)
    {
        gameState = state;
    }
    public int[] getMove()
    {
        return null;
    }
}
