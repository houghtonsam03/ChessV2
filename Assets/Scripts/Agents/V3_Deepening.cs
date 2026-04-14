using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Math = System.Math;
using Random = System.Random;

// Agent V3 - Iterativ deepening
// Agent implements MinMax search (NegaMax) with iterative deepening.
// Evaluates positions based on piece counts and piece square tables (PST).
[CreateAssetMenu(fileName = "IterativeDeepening", menuName = "Agents/IterativeDeepening")]
public class V3_Deepening : ChessAgent
{
    // Random
    private static readonly Random rng = new Random();
    // Game Information
    private int colour;
    // Static values
    private static readonly int[] pieceScores = {0,100,320,330,500,900}; // Centipawns i.e 100 = 1 Pawn
    private static readonly int endgameStart = 75;
    // PST Tables are for WHITE and visually flipped on y-axis
    private static readonly float[] kingMiddlePST =
    { 
     20, 30, 10,  0,  0, 10, 30, 20,
     20, 20,  0,  0,  0,  0, 20, 20,
    -10,-20,-20,-20,-20,-20,-20,-10,
    -20,-30,-30,-40,-40,-30,-30,-20,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    };
    private static readonly float[] kingEndPST =
    { 
    -50,-30,-30,-30,-30,-30,-30,-50,
    -30,-30,  0,  0,  0,  0,-30,-30,
    -30,-10, 20, 30, 30, 20,-10,-30,
    -30,-10, 30, 40, 40, 30,-10,-30,
    -30,-10, 30, 40, 40, 30,-10,-30,
    -30,-10, 20, 30, 30, 20,-10,-30,
    -30,-20,-10,  0,  0,-10,-20,-30,
    -50,-40,-30,-20,-20,-30,-40,-50,
    };
    private static readonly float[] pawnPST =
    { 
      0,  0,  0,  0,  0,  0,  0,  0,
      5, 10, 10,-20,-20, 10, 10,  5,
      5, -5,-10,  0,  0,-10, -5,  5,
      0,  0,  0, 20, 20,  0,  0,  0,
      5,  5, 10, 25, 25, 10,  5,  5,
     10, 10, 20, 30, 30, 20, 10, 10,
     50, 50, 50, 50, 50, 50, 50, 50,
      0,  0,  0,  0,  0,  0,  0,  0,
    };
    private static readonly float[] knightPST =
    {
    -50,-40,-30,-30,-30,-30,-40,-50,
    -40,-20,  0,  5,  5,  0,-20,-40,
    -30,  5, 10, 15, 15, 10,  5,-30,
    -30,  0, 15, 20, 20, 15,  0,-30,
    -30,  5, 15, 20, 20, 15,  5,-30,
    -30,  0, 10, 15, 15, 10,  0,-30,
    -40,-20,  0,  0,  0,  0,-20,-40,
    -50,-40,-30,-30,-30,-30,-40,-50,
    };
    private static readonly float[] bishopPST =
    {
    -20,-10,-10,-10,-10,-10,-10,-20,
    -10,  5,  0,  0,  0,  0,  5,-10,
    -10, 10, 10, 10, 10, 10, 10,-10,
    -10,  0, 10, 10, 10, 10,  0,-10,
    -10,  5,  5, 10, 10,  5,  5,-10,
    -10,  0,  5, 10, 10,  5,  0,-10,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -20,-10,-10,-10,-10,-10,-10,-20,
    };
    private static readonly float[] rookPST =
    {
      0,  0,  0,  5,  5,  0,  0,  0,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
      5, 10, 10, 10, 10, 10, 10,  5,
      0,  0,  0,  5,  5,  0,  0,  0,
    };
    private static readonly float[] queenPST =
    {
    -20,-10,-10, -5, -5,-10,-10,-20,
    -10,  0,  5,  0,  0,  0,  0,-10,
    -10,  5,  5,  5,  5,  5,  0,-10,
      0,  0,  5,  5,  5,  5,  0, -5,
     -5,  0,  5,  5,  5,  5,  0, -5,
    -10,  0,  5,  5,  5,  5,  0,-10,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -20,-10,-10, -5, -5,-10,-10,-20,
    };

    public static readonly float checkmateValue = 100000f; //1e5
    public static readonly float drawValue = 0f;
    public static readonly float randomMoveMargin = 1f;
    public static readonly float moveTime = 0.3f; // max time per move
    public struct SearchTimer
    {
        public Stopwatch watch;
        public bool abandoned;
    }
    public override void StartAgent(bool white)
    {
        colour = white ? Piece.white : Piece.black;
    }
    public override Move GetMove(Board board)
    {
        SearchTimer timer = new SearchTimer{watch=Stopwatch.StartNew(),abandoned=false};
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,colour,moves);

        if (totalMoves == 0) return Move.NullMove;

        float[] movesScores = new float[totalMoves];
        int maxDepth = 0;
        // Iterative Deepening
        for (int depth=1;depth<100;depth++)
        {
            float[] depthScoring = new float[totalMoves];
            for (int i=0;i<totalMoves;i++)
            {
                Move move = moves[i];
                board.MakeMove(move);
                depthScoring[i] = -NegaMax(board,depth-1,ref timer); // Since first depth is in this function (-1)
                board.UndoMove();
            }
            if (timer.abandoned) break;
            maxDepth = depth;
            for (int i=0;i<totalMoves;i++)
            {
                movesScores[i] = depthScoring[i];
            }
        }
        // Find best score
        float bestScore = float.MinValue;
        for (int i=0;i<totalMoves;i++)
        {
            if (movesScores[i] > bestScore) bestScore = movesScores[i];
        }

        // Pick move within a margin of best score (random noise)
        List<Move> candidates = new List<Move>();
        for (int i=0;i<totalMoves;i++)
        {
            if (movesScores[i] >= bestScore-randomMoveMargin) candidates.Add(moves[i]);
        }
        UnityEngine.Debug.Log($"Turn: {board.fullmove} - Colour: {colour} -> Depth: {maxDepth}");
        return candidates[rng.Next(0,candidates.Count)];
    }
    public override float? GetEval(Board board)
    {
        SearchTimer timer = new SearchTimer{watch=Stopwatch.StartNew(),abandoned=false};
        float finalScore = 0f;
        for (int depth=1;depth<100;depth++)
        {
            float score = NegaMax(board,depth,ref timer);
            if (timer.abandoned) break;
            finalScore = score;
        }
        return Piece.IsColour(board.colourToMove,colour) ? finalScore/100 : -finalScore/100;
    }
    private float NegaMax(Board board,int depth,ref SearchTimer timer)
    {
        if (timer.watch.Elapsed.TotalSeconds > moveTime)
        {
            timer.abandoned = true;
            return 0; // Search is abandoned
        }
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);

        if (board.IsCheckMate(totalMoves > 0,board.colourToMove)) return -(checkmateValue-board.fullmove); // I am in checkmate -> Bad
        else if (board.isDraw(totalMoves > 0,3)) return drawValue;

        if (depth == 0) return Evaluation(board);

        float bestScore = float.MinValue;
        for (int i=0;i<totalMoves;i++)
        {
            Move move = moves[i];
            board.MakeMove(move);
            float score = -NegaMax(board,depth-1,ref timer);
            board.UndoMove();
            if (timer.abandoned) return 0; // Propogate abandon search upward.
            if (score > bestScore) bestScore = score;
        }
        return bestScore;
    }
    private float Evaluation(Board board)
    {
        float whiteScore = EvaluateSide(board, Piece.white);
        float blackScore = EvaluateSide(board, Piece.black);

        float totalScore = whiteScore - blackScore;

        // NegaMax requirement: Return score relative to the moving player
        return (board.colourToMove == Piece.white) ? totalScore : -totalScore;
    }

    private float EvaluateSide(Board board, int sideColour)
    {
        float score = 0;

        // Material and PST
        score += CountPiecePST(board,1, sideColour, pawnPST); // Index 0: Pawn
        score += CountPiecePST(board,2, sideColour, knightPST);
        score += CountPiecePST(board,3, sideColour, bishopPST);
        score += CountPiecePST(board,4, sideColour, rookPST);
        score += CountPiecePST(board,5, sideColour, queenPST);

        // King logic (handling mid vs end game)
        int kingSq = board.FindKing(sideColour);
        float endgameFactor = Math.Clamp(board.fullmove / (float)endgameStart, 0, 1);
        int pstIdx = GetPSTIndex(sideColour, kingSq);
        score += (1 - endgameFactor) * kingMiddlePST[pstIdx] + endgameFactor * kingEndPST[pstIdx];

        return score;
    }

    private float CountPiecePST(Board board,int type, int colour, float[] pst)
    {
        float score = 0;
        int offset = (colour == Piece.white) ? 0 : 6;
        ulong bitboard = board.bitboards[type+offset];
        while (bitboard != 0)
        {
            int sq = Bitboard.PopLowestBit(ref bitboard);
            score += pieceScores[type]; 
            score += pst[GetPSTIndex(colour, sq)];
        }
        return score;
    }
    private int GetPSTIndex(int colour, int square)
    {
        return Piece.IsColour(colour,Piece.white) ? square : square ^ 56;
    }
}
