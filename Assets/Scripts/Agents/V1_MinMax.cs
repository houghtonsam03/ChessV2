using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Random = System.Random;

// Agent V1 - Naive MinMax
// Agent implements MinMax search (NegaMax) [Depth = 2]
//  Evaluates positions based on piece counts
[CreateAssetMenu(fileName = "MinMax", menuName = "Agents/MinMax")]
public class V1_MinMax : ChessAgent
{
    // Random
    private static readonly Random rng = new Random();
    // Game Information
    private int colour;
    // Static values
    private static readonly int[] pieceScores = {0,100,320,330,500,900};
    public static readonly float checkmateValue = 100000f;
    public static readonly float drawValue = 0f;
    public static readonly float randomMoveMargin = 1f;
    public static readonly  int depth = 2;
    public override void StartAgent(bool white)
    {
        colour = white ? Piece.white : Piece.black;
    }
    public override Move GetMove(Board board)
    {
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,colour,moves);
        if (totalMoves == 0) return Move.NullMove;

        float bestScore = float.MinValue;
        Move bestMove = moves[0];
        
        float[] movesScores = new float[totalMoves];
        for (int i=0;i<totalMoves;i++)
        {
            Move move = moves[i];
            board.MakeMove(move);
            movesScores[i] = -NegaMax(board,depth-1); // Since first depth is in this function (-1)
            board.UndoMove();
            if (movesScores[i] > bestScore)  
            {
                bestScore = movesScores[i];
                bestMove = move;
            }
        }
        List<Move> candidates = new List<Move>();
        for (int i=0;i<totalMoves;i++)
        {
            if (movesScores[i] >= bestScore-randomMoveMargin) candidates.Add(moves[i]);
        }

        return candidates[rng.Next(0,candidates.Count)];
    }
    public override float? GetEval(Board board)
    {
        float score = NegaMax(board,depth);
        return Piece.IsColour(board.colourToMove,colour) ? score : -score;
    }
    private float NegaMax(Board board,int depth)
    {
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);

        if (board.IsCheckMate(totalMoves > 0,board.colourToMove)) return -checkmateValue; // I am in checkmate -> Bad
        else if (board.isDraw(totalMoves > 0,3)) return drawValue;

        if (depth == 0) return Evaluation(board);

        float bestScore = float.MinValue;
        for (int i=0;i<totalMoves;i++)
        {
            Move move = moves[i];
            board.MakeMove(move);
            float score = -NegaMax(board,depth-1);
            board.UndoMove();
            if (score > bestScore) bestScore = score;
        }
        return bestScore;
    }
    private float Evaluation(Board board)
    {
        int offset = Piece.IsColour(board.colourToMove,Piece.white) ? 0 : 6;

        float score = 0;
        for (int i=0;i<6;i++)
        {
            score += Bitboard.Count(board.bitboards[i+offset])*pieceScores[i];
            score -= Bitboard.Count(board.bitboards[(i+offset+6)%12])*pieceScores[i];
        }
        return score;
    }
}
