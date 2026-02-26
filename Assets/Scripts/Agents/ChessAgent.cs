using UnityEngine;

public abstract class ChessAgent : ScriptableObject
{
    public abstract void StartAgent(bool isWhite);

    public abstract (Move,float) GetMove(Board board);
    public abstract string GetColour();
    public abstract float EvalPos(Board board);
}
