using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

// Agent V0 - Random Moves
// Base agent where moves are picked at random from possible moves.

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

        Random rng = new Random((int)DateTime.Now.Ticks);
        int randomIndex = rng.Next(0,totalMoves);
        Move randMove = moves[randomIndex];
        return randMove;
    }
    public override float? GetEval(Board board)
    {
        return null;
    }
}
