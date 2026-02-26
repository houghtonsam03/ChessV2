using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;


[CreateAssetMenu(fileName = "RandomAgent", menuName = "Agents/RandomAgent")]
public class RandomAgent : ChessAgent
{
    // Game Information
    private int colour;
    
    public override void StartAgent(bool white)
    {
        colour = white ? Piece.white : Piece.black;
    }
    public override (Move,float) GetMove(Board board)
    {
        List<Move> moves = MoveGenerator.GenerateMoves(board,colour);

        Random rand = new Random();
        return (moves[rand.Next(moves.Count)],0f);
    }
    public override string GetColour()
    {
        return Piece.IsColour(colour,Piece.white) ? "White" : "Black";
    }
    public override float EvalPos(Board board)
    {
        return 0f;
    }
}
