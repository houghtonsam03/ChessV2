using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class MoveGenerator
{
    public static PrecomputedMoveData PrecomputedMove;
    public struct PrecomputedMoveData
    {
        public int[] directionOffset;
        public int[][] numSquaresToEdge;
        public ulong[] kingAttacks;
        public ulong[][] pawnAttacks;
        public ulong[] knightAttacks;
        
    }
    static MoveGenerator()
    {
        PrecomputeMoveData();
    }
    private static void PrecomputeMoveData()
    {
        PrecomputedMove.directionOffset = new int[] { 8, -8, -1, 1, 7, -9, 9, -7 };
        PrecomputedMove.numSquaresToEdge = new int[64][];
        PrecomputedMove.kingAttacks = new ulong[64];
        PrecomputedMove.pawnAttacks = new ulong[64][];
        PrecomputedMove.knightAttacks = new ulong[64];

        int[] kingDX = {-1,0,1,-1,1,-1,0,1};
        int[] kinDY = {1,1,1,0,0,-1,-1,-1};
        int[] knightDX = {-1,1,-1,1,-2,-2,2,2};
        int[] knightDY = {2,2,-2,-2,1,-1,1,-1};
        int[] pawnDX = {-1,1,-1,1};
        int[] pawnDY = {1,1,-1,-1};
        for (int x = 0; x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                // Square to edge
                int numUp = 7 - y;
                int numDown = y;
                int numLeft = x;
                int numRight = 7 - x;

                int cellID = 8*y+x;
                PrecomputedMove.numSquaresToEdge[cellID] = new int[]{
                    numUp,
                    numDown,
                    numLeft,
                    numRight,
                    Math.Min(numUp,numLeft),
                    Math.Min(numDown,numLeft),
                    Math.Min(numUp,numRight),
                    Math.Min(numDown,numRight)
                };
                // King moves
                for (int i=0;i<8;i++)
                {
                    int nx = x + kingDX[i];
                    int ny = y + kinDY[i];
                    if (nx < 0 || nx > 7 || ny < 0 || ny > 7) continue;
                    int target = ny*8 + nx;
                    Bitboard.SetBit(ref PrecomputedMove.kingAttacks[cellID],target);
                }
                // Knight moves
                for (int i=0;i<8;i++)
                {
                    int nx = x + knightDX[i];
                    int ny = y + knightDY[i];
                    if (nx < 0 || nx > 7 || ny < 0 || ny > 7) continue;
                    int target = ny*8 + nx;
                    Bitboard.SetBit(ref PrecomputedMove.knightAttacks[cellID],target);
                }
                // Pawn moves
                PrecomputedMove.pawnAttacks[cellID] = new ulong[2];
                for (int i=0;i<4;i++)
                {
                    int nx = x + pawnDX[i];
                    int ny = y + pawnDY[i];
                    if (nx < 0 || nx > 7 || ny < 0 || ny > 7) continue;
                    int target = ny*8 + nx;
                    Bitboard.SetBit(ref PrecomputedMove.pawnAttacks[cellID][i/2],target);
                }
            }
        }
    }
    public static List<Move> GenerateMoves(Board board,int colour, int type=Piece.None)
    {
        List<Move> allMoves = GeneratePseudoLegalMoves(board,colour,type);
        SolvePseudoMoves(board,allMoves);
        return allMoves;
    
    }
    public static List<Move> GeneratePseudoLegalMoves(Board board,int colour, int type=Piece.None)
    {
        List<Move> moves = new List<Move>();
        int offset = Piece.IsColour(colour,Piece.white) ? 0 : 6;
        if (type==Piece.None)
        {
            for (int i=0;i<6;i++)
            {
                ulong bitboard = board.bitboards[i+offset];
                while (bitboard != 0)
                {
                    int start = Bitboard.PopLowestBit(ref bitboard);
                    GenerateMove(board,moves,start,colour,i+1,true);
                }
            }  
        }
        else
        {
            int typeMask = type-1;
            ulong bitboard = board.bitboards[typeMask+offset];
            while (bitboard != 0)
            {
                int start = Bitboard.PopLowestBit(ref bitboard);
                GenerateMove(board,moves,start,colour,type,true);
            }
        }
        return moves;
    }
    public static void GenerateMove(Board board,List<Move> moves,int start,int colour,int type,bool includeCastling = true)
    {
        if (Piece.IsSlidingPiece(type))
        {
            GenerateSlidingMoves(board,moves,start,colour,type);
        }
        else if (Piece.IsType(type,Piece.King))
        {
            GenerateKingMoves(board,moves,start,colour,includeCastling);
        }
        else if (Piece.IsType(type,Piece.Pawn))
        {
            GeneratePawnMoves(board,moves,start,colour);
        }
        else if (Piece.IsType(type,Piece.Knight))
        {
            GenerateKnightMoves(board,moves,start,colour);
        }
    }
    public static void GenerateSlidingMoves(Board board,List<Move> moves,int startCell, int colour,int type)
    {
        bool white = Piece.IsColour(colour,Piece.white);
        int friendlyMask = white ? 12 : 13;
        int enemyMask = white ? 13 : 12;
        int startDirIndex = Piece.IsType(type,Piece.Bishop) ? 4 : 0;
        int endDirIndex = Piece.IsType(type,Piece.Rook) ? 4 : 8;

        for (int directionIndex = startDirIndex; directionIndex<endDirIndex;directionIndex++)
        {
            for (int n=1;n <= PrecomputedMove.numSquaresToEdge[startCell][directionIndex];n++)
            {
                int targetCell = startCell + PrecomputedMove.directionOffset[directionIndex] * n;

                if (Bitboard.HasBit(board.bitboards[friendlyMask],targetCell)) break;
                moves.Add(new Move(startCell,targetCell));
                if (Bitboard.HasBit(board.bitboards[enemyMask],targetCell)) break;
            }
        }
    }
    public static void GenerateKingMoves(Board board,List<Move> moves,int startCell,int colour,bool includeCastling)
    {
        bool white = Piece.IsColour(colour,Piece.white);
        int teamMask = white ? 12 : 13;
        ulong attacking = white ? board.blackAttacks : board.whiteAttacks;

        ulong moveBoard = PrecomputedMove.kingAttacks[startCell];
        moveBoard &= ~board.bitboards[teamMask]; // Can't capture friendlies
        moveBoard &= ~attacking; // Can't put King in check
        while (moveBoard != 0)
        {
            int target = Bitboard.PopLowestBit(ref moveBoard);
            moves.Add(new Move(startCell,target));
        }

        if (!includeCastling) return;
        // Castling
        if (board.IsCheck(colour)) return;
        int teamOffset = white ? 0 : 2;
        int teamShift = white ? 0 : 7;
        // If castling is allowed and squares between are and not under attack clear we can castle.
        // Kingside
        if (board.castling[0+teamOffset] && ((attacking & (Bitboard.kingsideCastle << teamShift*8)) == 0) && (board.bitboards[14] & (Bitboard.kingsideCastle << teamShift*8)) == 0) moves.Add(new Move(startCell,ChessGame.CellToID(6,teamShift),0,true));
        // Queenside
        if (board.castling[1+teamOffset] && ((attacking & (Bitboard.queensideCastle << teamShift*8)) == 0) && (board.bitboards[14] & ((Bitboard.queensideCastle | 0x2) << teamShift*8)) == 0) moves.Add(new Move(startCell,ChessGame.CellToID(2,teamShift),0,true));
    }
    public static void GeneratePawnMoves(Board board,List<Move> moves,int startCell, int colour)
    {
        bool white = Piece.IsColour(colour,Piece.white);
        // Check diagonal capture and En Passant
        int team = (colour/8)-1;
        int firstRank = white ? 1: 6;
        int lastRank = white ? 7 : 0;
        int enemyMask = white ? 13 : 12;

        // Diagonal capture
        ulong captureSquares = board.bitboards[enemyMask];
        ulong attackSquares = PrecomputedMove.pawnAttacks[startCell][colour/8-1];
        attackSquares &= captureSquares;
        while (attackSquares != 0)
        {
            int target = Bitboard.PopLowestBit(ref attackSquares);
            if (ChessGame.GetRank(target) == lastRank) AddPromotionMoves(moves,startCell,target);
            else moves.Add(new Move(startCell,target));
        }
        // En Passant
        if (board.enpassant >= 0)
        {
            if (Bitboard.HasBit(PrecomputedMove.pawnAttacks[startCell][colour/8-1],board.enpassant)) moves.Add(new Move(startCell,board.enpassant,0,false,true));
        }

        // Check pawn move 1 step.
        int targetCell = startCell + PrecomputedMove.directionOffset[team];
        if (Bitboard.HasBit(board.bitboards[14],targetCell)) return;
        if (ChessGame.GetRank(targetCell) == lastRank) AddPromotionMoves(moves,startCell,targetCell);
        else moves.Add(new Move(startCell,targetCell));

        // Check pawn move 2 step.
        if (ChessGame.GetRank(startCell) == firstRank)
        {
            targetCell = startCell + PrecomputedMove.directionOffset[team] * 2;
            if (!Bitboard.HasBit(board.bitboards[14],targetCell)) moves.Add(new Move(startCell,targetCell));
        } 
    }
    private static void AddPromotionMoves(List<Move> moves, int start, int target)
    {
        moves.Add(new Move(start, target, Piece.Queen));
        moves.Add(new Move(start, target, Piece.Rook));
        moves.Add(new Move(start, target, Piece.Knight));
        moves.Add(new Move(start, target, Piece.Bishop));
    }
    public static void GenerateKnightMoves(Board board,List<Move> moves,int startCell,int colour)
    {
        int teamMask = Piece.IsColour(colour,Piece.white) ? 12 : 13;
        ulong moveBoard = PrecomputedMove.knightAttacks[startCell];
        moveBoard &= ~board.bitboards[teamMask];
        while (moveBoard != 0)
        {
            int target = Bitboard.PopLowestBit(ref moveBoard);
            moves.Add(new Move(startCell,target));
        }
    }
    public static void SolvePseudoMoves(Board board,List<Move> moves)
    {
        for(int i = moves.Count-1;i >= 0;i--)
        {
            board.MakeMove(moves[i]);
            if (board.IsCheck(Piece.GetOpponentColour(board.colourToMove))) moves.RemoveAt(i);
            board.UndoMove();
        }
    }
    public static int GetDirectionIndex(int start, int target)
    {
        int startFile = start % 8, startRank = start / 8;
        int targetFile = target % 8, targetRank = target / 8;

        int fileDiff = targetFile - startFile;
        int rankDiff = targetRank - startRank;

        // Normalize differences to -1, 0, or 1
        int dirX = Math.Sign(fileDiff);
        int dirY = Math.Sign(rankDiff);

        // If they aren't on the same line/diagonal, they aren't aligned
        if (fileDiff != 0 && rankDiff != 0 && Math.Abs(fileDiff) != Math.Abs(rankDiff))
            return -1; 

        // Mapping to your indices:
        // Assuming 0: North (+8), 1: South (-8), 2: West (-1), 3: East (+1)
        // Assuming 4: NW (+7), 5: SE (-7), 6: NE (+9), 7: SW (-9)
        if (dirX == 0 && dirY == 1)  return 0; // North
        if (dirX == 0 && dirY == -1) return 1; // South
        if (dirX == -1 && dirY == 0) return 2; // West
        if (dirX == 1 && dirY == 0)  return 3; // East
        
        if (dirX == -1 && dirY == 1)  return 4; // NW
        if (dirX == 1 && dirY == -1)  return 5; // SE
        if (dirX == 1 && dirY == 1)   return 6; // NE
        if (dirX == -1 && dirY == -1) return 7; // SW

        return -1;
    }
}