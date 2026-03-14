using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Math = System.Math;
using Random = System.Random;

// Agent V4 - Transposition Tables
// Agent implements MinMax search (NegaMax) with Alpha-Beta-Pruning and Transposition Tables [Depth = 3]
// Evaluates positions based on piece counts and Piece Square Tables (PST)
[CreateAssetMenu(fileName = "TranspositionTables", menuName = "Agents/TranspositionTables")]
public class V4_Transpos : ChessAgent
{
    // Random
    private static readonly Random rng = new Random();
    // Game Information
    private int colour;
    // Agent variables
    private State[] TransposTable;
    private struct State
    {
        public ulong Zobrist;
        public Move move;
        public float score;
        public byte depth;
        public byte nodeType; // 0 = Exact, 1 = Alpha (Upper-Bound), 2 = Beta (Lower-Bound)
    }
    // Static values
    private static readonly int TableSize = 1 << 20; // 1,048,576 entries
    private static readonly int IndexMask = TableSize - 1;
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
    public static readonly int depth = 3;
    public override void StartAgent(bool white)
    {
        colour = white ? Piece.white : Piece.black;
        TransposTable = new State[TableSize];
    }
    public override Move GetMove(Board board)
    {

        // Check lookup table
        int index = (int)(board.zobristKey & (ulong)IndexMask);
        State entry = TransposTable[index];

        if (entry.Zobrist == board.zobristKey && entry.depth >= depth && entry.nodeType == 0) return entry.move;

        // Generate moves
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,colour,moves);

        // MOVE ORDERING: Find the TT move in our list and swap it to index 0
        if (entry.Zobrist == board.zobristKey)
        {
            for (int i = 0; i < totalMoves; i++)
            {
                if (moves[i] == entry.move) 
                {
                    Move temp = moves[0];
                    moves[0] = moves[i];
                    moves[i] = temp;
                    break;
                }
            }
        }

        float alpha = float.NegativeInfinity;
        float beta = float.PositiveInfinity;
        float bestScore = float.MinValue;
        Move bestMove = entry.move;

        for (int i=0;i<totalMoves;i++)
        {
            Move move = moves[i];
            board.MakeMove(move);
            float score = -NegaMaxAlphaBeta(board,depth-1,-beta,-alpha); // Since first depth is in this function (-1)
            board.UndoMove();
            if (score > bestScore)  
            {
                bestScore = score;
                bestMove = move;
                if (score > alpha) alpha = score;
            }
            if (score >= beta) return bestMove;
        }


        return bestMove;
    }
    public override float? GetEval(Board board)
    {

        float score = NegaMaxAlphaBeta(board,depth,float.NegativeInfinity,float.PositiveInfinity);
        return Piece.IsColour(board.colourToMove,colour) ? score/100 : -score/100;
    }
    private float NegaMaxAlphaBeta(Board board,int depth,float alpha,float beta)
    {
        float originAlpha = alpha;
        // Check if position is in lookup table
        int index = (int)(board.zobristKey & (ulong)IndexMask);
        State entry = TransposTable[index];

        if (entry.Zobrist == board.zobristKey && entry.depth >= depth) 
        {
            // Adjust checkmate by current depth search
            float storedScore = entry.score;
            if (storedScore > checkmateValue - 1000) storedScore -= depth;
            else if (storedScore < -checkmateValue + 1000) storedScore += depth;

            // Exact
            if (entry.nodeType == 0) return storedScore;
            // Upper Bound
            if (entry.nodeType == 1 && entry.score <= alpha) return alpha;
            // Lower Bound
            if (entry.nodeType == 2 && entry.score >= beta) return beta;
        }
        // Generate moves
        Span<Move> moves = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);
        
        // Check gameover states
        if (board.IsCheckMate(totalMoves > 0,board.colourToMove)) {UpdateTransTable(board.zobristKey,Move.NullMove,-checkmateValue,(byte)depth,0);return -checkmateValue-depth;} // I am in checkmate -> Bad
        else if (board.isDraw(totalMoves > 0,3)) {UpdateTransTable(board.zobristKey,Move.NullMove,drawValue,(byte)depth,0);return drawValue;}

        // Evaluate leaf nodes
        if (depth == 0) return Evaluation(board);

        // MOVE ORDERING: Find the TT move in our list and swap it to index 0
        if (entry.Zobrist == board.zobristKey)
        {
            for (int i = 0; i < totalMoves; i++)
            {
                if (moves[i] == entry.move) 
                {
                    Move temp = moves[0];
                    moves[0] = moves[i];
                    moves[i] = temp;
                    break;
                }
            }
        }

        // Search through child nodes
        float bestScore = float.MinValue;
        Move bestMove = moves[0];
        for (int i=0;i<totalMoves;i++)
        {
            Move move = moves[i];
            board.MakeMove(move);
            float score = -NegaMaxAlphaBeta(board,depth-1,-beta,-alpha);
            board.UndoMove();

            if (score > bestScore) {bestScore = score; bestMove = move;}
            if (score > alpha) alpha = score;
            
            if (score >= beta) {
                // Adjust checkmate by current depth search
                float scoreToStore = bestScore;
                if (scoreToStore > checkmateValue - 1000) scoreToStore += depth;
                else if (scoreToStore < -checkmateValue + 1000) scoreToStore -= depth;

                UpdateTransTable(board.zobristKey,bestMove,bestScore,(byte)depth,2);
                return bestScore;
            }

        }
        byte nodeType = (bestScore <= originAlpha) ? (byte)1 : (byte)0;
        UpdateTransTable(board.zobristKey,bestMove,bestScore,(byte)depth,nodeType);
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
    private void UpdateTransTable(ulong key,Move m,float sc,byte d,byte nType)
    {
        int index = (int)(key & (ulong)IndexMask);
        State entry = TransposTable[index];

        // Replacement strategy: Only overwrite if search was deeper or position is new
        if (key != entry.Zobrist || d >= entry.depth)
        {
            TransposTable[index] = new State
            {
                Zobrist = key,
                move = m,
                score = sc,
                depth = d,
                nodeType = nType,
            };
        }
    }
}