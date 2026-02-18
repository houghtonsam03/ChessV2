using UnityEngine;
using static ChessEngine;

public abstract class ChessAgent : ScriptableObject
{
    public abstract void StartAgent(int colour);

    public abstract Move GetMove(Board board);
    public abstract string GetColour();
}
