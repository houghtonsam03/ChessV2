using System.Threading.Tasks;
using UnityEngine;

public abstract class ChessAgent : ScriptableObject
{
    public abstract void StartAgent(bool isWhite);

    public abstract Move GetMove(Board board);
    public Task<Move> GetMoveAsync(Board board)
    {
        return Task.Factory.StartNew(() =>
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            return GetMove(board);
        }, TaskCreationOptions.LongRunning);
    }
    public abstract float? GetEval(Board board);
}
