using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;


[CreateAssetMenu(fileName = "Random", menuName = "Agents/Random")]
public class V0_Random : ChessAgent
{
    // Game Information
    private int colour;
    
    public override void StartAgent(bool white)
    {
        colour = white ? Piece.white : Piece.black;
    } 
    public override Move GetMove(Board board)
    {
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,colour,moves);

        Move randMove = moves[UnityEngine.Random.Range(0,totalMoves)];
        return randMove;
    }
    public override float? GetEval(Board board)
    {
        return null;
    }
}
