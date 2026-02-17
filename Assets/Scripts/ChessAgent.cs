using UnityEngine;
using static ChessEngine;

public abstract class ChessAgent : ScriptableObject
{
    public abstract void StartAgent(bool white);

    public abstract Move GetMove(Board board);
}
