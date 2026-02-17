using System.Collections.Generic;
using UnityEngine;
using static ChessEngine;
using Random = System.Random;


[CreateAssetMenu(fileName = "RandomAgent", menuName = "Agents/RandomAgent")]
public class RandomAgent : ChessAgent
{
    // Game Information
    private string gameState;
    
    public override void StartAgent(bool white)
    {
    }
    public override Move GetMove(Board board)
    {
        List<Move> moves = GenerateMoves(board);

        Random rand = new Random();
        return moves[rand.Next(moves.Count)];
    }
}
