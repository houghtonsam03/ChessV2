using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Random = System.Random;


[CreateAssetMenu(fileName = "MinMax", menuName = "Agents/MinMax")]
public class V1_MinMax : ChessAgent
{
    // Game Information
    private int colour;
    private int depth;
    // Static values
    private static int[] pieceScores = {0,1,3,3,5,9};
    public static readonly float checkmateValue = 100f;
    public static readonly float drawValue = 0f;
    public override void StartAgent(bool white)
    {
        colour = white ? Piece.white : Piece.black;
        depth = 3;
    }
    public override Move GetMove(Board board)
    {
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,colour,moves);
        if (totalMoves == 0) return Move.NullMove;
        float bestScore = float.MinValue;
        Move bestMove = moves[0];
        for (int i=0;i<totalMoves;i++)
        {
            Move move = moves[i];
            board.MakeMove(move);
            float score = -NegaMax(board,depth-1); // Since first depth is in this function (-1)
            board.UndoMove();
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
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

        if (depth == 0) return Evaluation(board,totalMoves);

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
    private float Evaluation(Board board,int moveCount)
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
