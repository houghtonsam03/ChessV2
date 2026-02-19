using System.Collections.Generic;
using UnityEngine;
using static ChessGame;
using Random = System.Random;


[CreateAssetMenu(fileName = "RandomAgent", menuName = "Agents/RandomAgent")]
public class RandomAgent : ChessAgent
{
    // Game Information
    private int colour;
    
    public override void StartAgent(int col)
    {
        colour = col;
    }
    public override Move GetMove(Board board)
    {
        List<Move> moves = GenerateMoves(board,colour);

        Random rand = new Random();
        return moves[rand.Next(moves.Count)];
    }
    public override string GetColour()
    {
        return Piece.IsColour(colour,Piece.white) ? "White" : "Black";
    }
}
