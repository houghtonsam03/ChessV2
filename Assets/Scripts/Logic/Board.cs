
using System;
using System.Collections.Generic;
using UnityEditor.EngineDiagnostics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public struct LastMove
    {
        public Move Move;
        public int MovingPiece;
        public int CapturedPiece;
        public bool[] Castling;
        public int EnPassant;
        public int HalfMove;
        public int FullMove;
        public LastMove(Move mv,int mPiece,int cPiece,bool[] cast,int en,int half,int full)
        {
            Move = mv;
            MovingPiece = mPiece;
            CapturedPiece = cPiece;
            Castling = new bool[4];
            Castling[0] = cast[0]; Castling[1] = cast[1]; Castling[2] = cast[2]; Castling[3] = cast[3];
            EnPassant = en;
            HalfMove = half;
            FullMove = full;
        }
    }
public class Board
{
    public int colourToMove = 8;
    public bool[] castling = new bool[4]{true,true,true,true}; // {White Kingside,White Queenside,Black Kingside,Black Queenside}
    public int enpassant = -1; // the square on which an En Passant Move is possible. (Behind the pawn that moved 2 squares.) 
    public int halfmove = 0;
    public int fullmove = 1;
    public  bool gameOver = false;
    // Logic variables
    private List<LastMove> lastMoves;
    // Bitboards
    public ulong[] bitboards = new ulong[15]; // 0-5 for white 6-11 for black. 12 for white pieces, 13 for black pieces, 14 for all pieces.
    public ulong whiteAttacks;
    public ulong blackAttacks;
    public ulong whitePins;
    public ulong blackPins;
    
    // Standard Fenstrings
    public static readonly string startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    // Hash variables
    public List<ulong> positionHistory = new List<ulong>();
    public ulong zobristKey;
    public Board()
    {
        colourToMove = 8;
        castling = new bool[4]{true,true,true,true};
        enpassant = -1; 
        halfmove = 0;
        fullmove = 1;
        gameOver = false;
        lastMoves = new List<LastMove>();
    }
    public void setPos(string fen)
    {
        string[] fenFields = fen.Split(" ");
        string pos = fenFields[0];
        int cell = 56;
        bitboards = new ulong[15];
        // Get Piece positions
        foreach (char letter in pos)
        {
            if (letter == '/') cell -= 16;
            else if (char.IsDigit(letter))
            {
                cell += (int)char.GetNumericValue(letter);
            }
            else
            {
                int color = char.IsUpper(letter) ? 0 : 6;
                int type;
                switch (char.ToLower(letter))
                {
                    case 'k':
                        type = 0;
                        break;
                    case 'p':
                        type = 1;
                        break;
                    case 'n':
                        type = 2;
                        break;
                    case 'b':
                        type = 3;
                        break;
                    case 'r':
                        type = 4;
                        break;
                    default:
                        type = 5;
                        break;
                }
                Bitboard.SetBit(ref bitboards[type+color],cell);
                cell +=1;
            }
        }
        for (int i=0;i<6;i++)
        {
            bitboards[12] |= bitboards[i];
            bitboards[13] |= bitboards[i+6];
        }
        bitboards[14] = bitboards[12] | bitboards[13];

        whiteAttacks = GetAttacks(Piece.white);
        blackAttacks = GetAttacks(Piece.black);
        whitePins = GetPins(Piece.white);
        blackPins = GetPins(Piece.black);


        colourToMove = fenFields[1] == "w" ? Piece.white : Piece.black;
        string cast = fenFields[2];
        castling = new bool[]{false,false,false,false};
        foreach (char s in cast)
        {
            switch (s)
            {
                case 'K': 
                    castling[0] = true;
                    break;
                case 'Q':
                    castling[1] = true;
                    break;
                case 'k':
                    castling[2] = true;
                    break;
                case 'q':
                    castling[3] = true;
                    break;
                
            }
        }
        if (fenFields[3] != "-") enpassant = ChessGame.StringToID(fenFields[3]);
        halfmove = fenFields[4].ToCharArray()[0] -'0';
        fullmove = fenFields[5].ToCharArray()[0] -'0';
        zobristKey = Zobrist.ZobristHash(this);
        positionHistory.Add(zobristKey);
    }
    public void setPos(Board b)
    {
        colourToMove = b.colourToMove;
        for(int i=0;i<4;i++) {castling[i] = b.castling[i];}
        enpassant = b.enpassant;
        halfmove = b.halfmove;
        fullmove = b.fullmove;
        gameOver = b.gameOver;

        bitboards = new ulong[15];
        for (int i=0;i<15;i++) {bitboards[i] = b.bitboards[i];}
        zobristKey = Zobrist.ZobristHash(this);
        positionHistory.Add(zobristKey);
    }
    public void MakeMove(Move move)
    {
        // Board assumes move is possible and legal.

        // Bitboard values
        int capturedPiece = GetPieceType(move.TargetSquare);
        int movingPiece = GetPieceType(move.StartSquare);
        // Record Move
        if (move.enpassant) {
            int captureSquare = ChessGame.GetRank(move.StartSquare)*8 + ChessGame.GetFile(move.TargetSquare);
            capturedPiece = GetPieceType(captureSquare);
        }
        LastMove lm = new LastMove(move,movingPiece,capturedPiece,castling,enpassant,halfmove,fullmove);
        lastMoves.Add(lm);
        int movingPieceIndex = movingPiece-1;
        int capturedPieceIndex = capturedPiece-1;

        // Zobrist
        if (enpassant >= 0) zobristKey ^= Zobrist.ZobristKeys[12*64+1+4+ChessGame.GetFile(enpassant)];
        // Record movement
        enpassant = -1;
        if (!Piece.IsType(movingPiece,Piece.Pawn) && Piece.IsType(capturedPiece,Piece.None)) halfmove += 1;
        else halfmove = 0;
        if (Piece.IsColour(colourToMove,Piece.black)) fullmove += 1;


        // Make Move
        if (move.castling)
        {
            // Move King
            ToggleMove(0,colourToMove,move.StartSquare);
            ToggleMove(0,colourToMove,move.TargetSquare);
            // Move Rook
            int rank = ChessGame.GetRank(move.StartSquare);
            int startFile = ChessGame.GetFile(move.TargetSquare) == 2 ? 0 : 7;
            int targetFile = ChessGame.GetFile(move.TargetSquare) == 2 ? 3 : 5;
            int rookStart = rank*8+startFile;
            int rookTarget = rank*8+targetFile;
            ToggleMove(4,colourToMove,rookStart);
            ToggleMove(4,colourToMove,rookTarget);
        }
        else if (move.enpassant)
        {
            int capturedSquare = ChessGame.GetRank(move.StartSquare) * 8 + ChessGame.GetFile(move.TargetSquare);
            // Move friendly pawn
            ToggleMove(1,colourToMove,move.StartSquare);
            ToggleMove(1,colourToMove,move.TargetSquare);
            ToggleMove(1,Piece.GetOpponentColour(colourToMove),capturedSquare);
        
        }
        else if (move.promotionPiece != 0)
        {
            ToggleMove(1,colourToMove,move.StartSquare);
            if (!Piece.IsType(capturedPiece,Piece.None)) ToggleMove(capturedPieceIndex,Piece.GetOpponentColour(colourToMove),move.TargetSquare);
            ToggleMove(move.promotionPiece-1,colourToMove,move.TargetSquare);
        }
        else
        {
            // Move pieces
            ToggleMove(movingPieceIndex,colourToMove,move.StartSquare);
            if (!Piece.IsType(capturedPiece,Piece.None)) ToggleMove(capturedPieceIndex,Piece.GetOpponentColour(colourToMove),move.TargetSquare);
            ToggleMove(movingPieceIndex,colourToMove,move.TargetSquare);
            // Update En Passant possible location
            if (Piece.IsType(movingPiece,Piece.Pawn) && (Math.Abs(move.TargetSquare-move.StartSquare) == 16)) 
            {
                enpassant = move.StartSquare + 8 * Math.Sign(move.TargetSquare-move.StartSquare);
                zobristKey ^= Zobrist.ZobristKeys[12*64+1+4+ChessGame.GetFile(enpassant)];
            }
        }

        // Update legal castling moves
        if (Piece.IsType(movingPiece,Piece.King) && Piece.IsColour(colourToMove,Piece.white))
        {
            if (castling[0]) zobristKey ^= Zobrist.ZobristKeys[12*64+1+0];
            if (castling[1]) zobristKey ^= Zobrist.ZobristKeys[12*64+1+1];
            castling[0] = false; castling[1] = false;
        }
        else if (Piece.IsType(movingPiece,Piece.King) && Piece.IsColour(colourToMove,Piece.black))
        {
            if (castling[2]) zobristKey ^= Zobrist.ZobristKeys[12*64+1+2];
            if (castling[3]) zobristKey ^= Zobrist.ZobristKeys[12*64+1+3];
            castling[2] = false; castling[3] = false;
        }
        foreach (int sq in new int[]{move.StartSquare,move.TargetSquare})
        {
            if (sq == 7 && castling[0]) {castling[0] = false; zobristKey ^= Zobrist.ZobristKeys[12*64+1+0];}
            if (sq == 0 && castling[1]) {castling[1] = false; zobristKey ^= Zobrist.ZobristKeys[12*64+1+1];}
            if (sq == 63 && castling[2]) {castling[2] = false; zobristKey ^= Zobrist.ZobristKeys[12*64+1+2];}
            if (sq == 56 && castling[3]) {castling[3] = false; zobristKey ^= Zobrist.ZobristKeys[12*64+1+3];}
        }

        // Finish
        colourToMove = Piece.GetOpponentColour(colourToMove);
        zobristKey ^= Zobrist.ZobristKeys[12*64];

        whiteAttacks = GetAttacks(Piece.white);
        blackAttacks = GetAttacks(Piece.black);
        whitePins = GetPins(Piece.white);
        blackPins = GetPins(Piece.black);
        
        positionHistory.Add(zobristKey);
    }
    public void UndoMove()
    {
        if (lastMoves.Count == 0) return;
        // Get LastMove
        LastMove lastMove = lastMoves[lastMoves.Count-1];
        lastMoves.RemoveAt(lastMoves.Count-1);

        int movingPieceIndex = lastMove.MovingPiece-1;
        int capturedPieceIndex = lastMove.CapturedPiece-1;
        int oppColour = Piece.GetOpponentColour(colourToMove);
        if (lastMove.Move.castling)
        {
            // Revert King
            ToggleMove(0,oppColour,lastMove.Move.StartSquare,false);
            ToggleMove(0,oppColour,lastMove.Move.TargetSquare,false);
            // Revert Rook
            int startFile = (lastMove.Move.TargetSquare-lastMove.Move.StartSquare) > 0 ? 7 : 0;
            int targetFile = (lastMove.Move.TargetSquare-lastMove.Move.StartSquare) > 0 ? 5 : 3;
            int rank = ChessGame.GetRank(lastMove.Move.StartSquare);
            int rookStart = rank*8+startFile;
            int rookTarget = rank*8+targetFile;
            ToggleMove(4,oppColour,rookStart,false);
            ToggleMove(4,oppColour,rookTarget,false);

            
        }
        else if (lastMove.Move.enpassant)
        {
            int capturedSquare = ChessGame.GetRank(lastMove.Move.StartSquare) * 8 + ChessGame.GetFile(lastMove.Move.TargetSquare);
            // Revert move
            ToggleMove(1,oppColour,lastMove.Move.StartSquare,false);
            ToggleMove(1,oppColour,lastMove.Move.TargetSquare,false);
            ToggleMove(1,colourToMove,capturedSquare,false);
        }
        else if (lastMove.Move.promotionPiece != 0)
        {
            // Remove promoted and recreate pawn
            ToggleMove(1,oppColour,lastMove.Move.StartSquare,false);
            if (!Piece.IsType(lastMove.CapturedPiece,Piece.None)) ToggleMove(capturedPieceIndex,colourToMove,lastMove.Move.TargetSquare,false);
            ToggleMove(lastMove.Move.promotionPiece-1,oppColour,lastMove.Move.TargetSquare,false);
        }
        else
        {
            // Revert move
            ToggleMove(movingPieceIndex,oppColour,lastMove.Move.StartSquare,false);
            if (!Piece.IsType(lastMove.CapturedPiece,Piece.None)) ToggleMove(capturedPieceIndex,colourToMove,lastMove.Move.TargetSquare,false);
            ToggleMove(movingPieceIndex,oppColour,lastMove.Move.TargetSquare,false);
        }
        // Update variables
        castling[0] = lastMove.Castling[0]; castling[1] = lastMove.Castling[1]; castling[2] = lastMove.Castling[2]; castling[3] = lastMove.Castling[3];
        enpassant = lastMove.EnPassant;
        halfmove = lastMove.HalfMove;
        fullmove = lastMove.FullMove;
        colourToMove = Piece.GetOpponentColour(colourToMove);

        whiteAttacks = GetAttacks(Piece.white);
        blackAttacks = GetAttacks(Piece.black);
        whitePins = GetPins(Piece.white);
        blackPins = GetPins(Piece.black);

        // Update Zobrist positions
        positionHistory.RemoveAt(positionHistory.Count-1);
        zobristKey = positionHistory[positionHistory.Count-1];

    }
    public void ToggleMove(int pieceIndex, int colour,int cell,bool hashing=true)
    {
        int offset = Piece.IsColour(colour,Piece.white) ? 0 : 6;
        int team = Piece.IsColour(colour,Piece.white) ? 12 : 13;
        Bitboard.ToggleBit(ref bitboards[pieceIndex+offset],cell);
        Bitboard.ToggleBit(ref bitboards[team],cell);
        Bitboard.ToggleBit(ref bitboards[14],cell);
        if (hashing) zobristKey ^= Zobrist.ZobricPositionHash(pieceIndex+offset,cell);
    }
    public int FindKing(int colour)
    {
        int offset = (colour == Piece.white) ? 0 : 6;
        ulong king = bitboards[0 + offset];
        if (king == 0)
        {
            Debug.Log("King doesn't exist?");
            Debug.Log(this);
        }

        return Bitboard.PopLowestBit(ref king);
    }
    public int GetPieceType(int square) 
    {
        ulong mask = 1UL << square;
        
        // Check if any piece is there at all
        if ((bitboards[14] & mask) == 0) return Piece.None;

        // Check color first (bitboards[12] = all white, 13 = all black)
        int start = (bitboards[12] & mask) != 0 ? 0 : 6;
        
        // Iterate only through those 6 piece types
        for (int i = start; i < start + 6; i++) {
            if ((bitboards[i] & mask) != 0) return i-start+1; // return the Piece.cs value of piece
        }
        return Piece.None;
    }
    public ulong GetAttacks(int colour)
    {
        // Pseudo legal non special moves
        ulong attacks = 0;
        int offset = (colour == Piece.white) ? 0 : 6;
        // Kings
        int kingSquare = FindKing(colour);
        attacks |= MoveGenerator.KingAttacks[kingSquare];
        // Pawns
        ulong pawns = bitboards[1 + offset];
        if (colour == Piece.white)
        {
            attacks |= (pawns << 7) & 0x7f7f7f7f7f7f7f7fUL;
            attacks |= (pawns << 9) & 0xfefefefefefefefeUL;
        }
        else
        {
            attacks |= (pawns >> 7) & 0xfefefefefefefefeUL;
            attacks |= (pawns >> 9) & 0x7f7f7f7f7f7f7f7fUL;
        }
        // Knights
        ulong knights = bitboards[2 + offset];
        while (knights != 0)
        {
            int sq = Bitboard.PopLowestBit(ref knights);
            attacks |= MoveGenerator.KnightAttacks[sq];
        }
        // Sliders (Bishop,Rook,Queen)
        ulong bishops = bitboards[3 + offset] | bitboards[5 + offset];
        while (bishops != 0)
        {
            int sq = Bitboard.PopLowestBit(ref bishops);
            attacks |= GetSliderAttacks(sq,colour,true);
        }

        ulong rooks = bitboards[4 + offset] | bitboards[5 + offset];
        while (rooks != 0)
        {
            int sq = Bitboard.PopLowestBit(ref rooks);
            attacks |= GetSliderAttacks(sq,colour,false);
        }
        return attacks;
    }
    public ulong GetSliderAttacks(int square,int colour,bool diagonal)
    {
        ulong kingMask = 1UL << FindKing(Piece.GetOpponentColour(colour)); 
        ulong occupied = bitboards[14] & ~kingMask;
        ulong attacks = 0;
    
        int startDir = diagonal ? 4 : 0;
        int endDir = diagonal ? 8 : 4;
        for (int dirIndex = startDir;dirIndex<endDir;dirIndex++)
        {
            for (int n=1; n<=MoveGenerator.NumSquaresToEdge[square*8+dirIndex];n++)
            {
                int target = square + MoveGenerator.DirectionOffset[dirIndex] * n;
                attacks |= (1UL << target);
                if ((occupied & (1UL << target)) != 0) break;
            }
        }
        return attacks;
    }
    public ulong GetPins(int kingColour)
    {
        ulong pins = 0;
        int kingSquare = FindKing(kingColour);
        ulong allOccupied = bitboards[14];
        
        // We need to look for ENEMY sliders
        int enemyOffset = (kingColour == Piece.white) ? 6 : 0;
        ulong enemyBishops = bitboards[3 + enemyOffset] | bitboards[5 + enemyOffset];
        ulong enemyRooks = bitboards[4 + enemyOffset] | bitboards[5 + enemyOffset];

        for (int dirIndex = 0; dirIndex < 8; dirIndex++)
        {
            bool isDiagonal = dirIndex >= 4;
            ulong rayMask = 0;
            int friendlyCount = 0;

            for (int n = 1; n <= MoveGenerator.NumSquaresToEdge[kingSquare*8+dirIndex]; n++)
            {
                int target = kingSquare + MoveGenerator.DirectionOffset[dirIndex] * n;
                ulong targetBit = 1UL << target;
                rayMask |= targetBit;

                if ((allOccupied & targetBit) != 0) // Is square occupied?
                {
                    // Check if the piece belongs to the King's side
                    ulong friendlyBits = kingColour == Piece.white ? bitboards[12] : bitboards[13];
                    bool isFriendly = (friendlyBits & targetBit) != 0;
                    
                    if (isFriendly)
                    {
                        friendlyCount++;
                    }
                    else // It's an enemy piece
                    {
                        // If we've found exactly one friendly piece so far...
                        if (friendlyCount == 1)
                        {
                            // Check if this enemy is a slider that attacks in this direction
                            if (isDiagonal && (enemyBishops & targetBit) != 0) pins |= rayMask;
                            else if (!isDiagonal && (enemyRooks & targetBit) != 0) pins |= rayMask;
                        }
                        // The ray is now blocked regardless, stop searching this direction
                        break;
                    }
                }
                
                // If more than 1 friendly piece is in the way, this ray can't be a pin
                if (friendlyCount > 1) break;
            }
        }
        return pins;
    }
    public bool IsAttacked(int square,int colour)
    {
        // Is attacked by side COLOUR.
        ulong attacked = Piece.IsColour(colour,Piece.white) ? whiteAttacks : blackAttacks;
        return Bitboard.HasBit(attacked,square);
    }
    public bool IsCheck(int colour)
    {
        int kingSquare = FindKing(colour);
        return IsAttacked(kingSquare,Piece.GetOpponentColour(colour));
    }
    public int IsGameOver(float whiteTime,float blackTime)
    {
        // Not Gameover - 0
        // White Win - 1,2,3
        // Black Win - 4,5,6
        // Draw - 7,8,9,10,11,12
        bool hasMoves = MoveGenerator.GenerateMoves(this,colourToMove).Count != 0;
        
        if (IsCheckMate(hasMoves,colourToMove)) return Piece.IsColour(colourToMove,Piece.black) ? 1 : 4;
        if (IsResigned()) return Piece.IsColour(colourToMove,Piece.black) ? 2 : 5;
        if (IsTimeout(colourToMove,whiteTime,blackTime) == 1) return Piece.IsColour(colourToMove,Piece.black) ? 3 : 6;
        if (IsStaleMate(hasMoves,colourToMove)) return 7;
        if (IsInsufficientMaterial()) return 8;
        if (IsFiftyMoveRule()) return 9;
        if (IsRepetition()) return 10;
        if (IsAgreement()) return 11;
        if (IsTimeout(colourToMove,whiteTime,blackTime) == 2) return 12;
        return 0;
        
    } 
    public bool IsCheckMate(bool hasMoves,int colour)
    {
        return !hasMoves && IsCheck(colour);
    }
    public bool IsResigned()
    {
        return false;
    }
    public int IsTimeout(int colour,float whiteTime, float blackTime)
    {
        // 0 is no timeout, 1 is timout, 2 is timout vs insufficient material (draw)
        float time = Piece.IsColour(colour,Piece.white) ? whiteTime : blackTime;
        if (time >= 0) return 0;

        // Factor insufficient material for opponent
        // If there is a pawn, rook or queen, it's not insufficient.
        int opMask = Piece.IsColour(Piece.GetOpponentColour(colour), Piece.white) ? 0 : 6;
        int myMask = Piece.IsColour(colour, Piece.white) ? 0 : 6;
        if ((bitboards[1+opMask] | bitboards[4+opMask] | bitboards[5+opMask]) != 0) return 1;

        // King | King
        if ((bitboards[2+opMask] | bitboards[3+opMask]) == 0) return 2;
        // King + Knight | King OR King | King + Knight
        if (Bitboard.Count(bitboards[2] | bitboards[8]) == 1 && (bitboards[3] | bitboards[9]) == 0) return 2;
        // King + Color1-Bishop | King + Color1-Bishop
        if ((bitboards[2] | bitboards[8]) == 0)
        {
            // Check if bishops of same colour
            bool anyOnDark = ((bitboards[3] | bitboards[9]) & Bitboard.darkSquares) != 0;
            bool anyOnLight = ((bitboards[3] | bitboards[9]) & Bitboard.lightSquares) != 0;
            if (anyOnDark != anyOnLight) return 2;
        }
        return 1;
    }
    public bool IsStaleMate(bool hasMoves,int colour)
    {
        return !hasMoves && !IsCheck(colour);
    }
    public bool IsInsufficientMaterial()
    {
        // If there is a pawn, rook or queen, it's not insufficient.
        if ((bitboards[1] | bitboards[4] | bitboards[5] | bitboards[7] | bitboards[10] | bitboards[11]) != 0) return false;

        // King | King
        if ((bitboards[2] | bitboards[3] | bitboards[8] | bitboards[9]) == 0) return true;
        // King + Knight | King OR King | King + Knight
        if (Bitboard.Count(bitboards[2] | bitboards[8]) == 1 && (bitboards[3] | bitboards[9]) == 0) return true;
        // King + Color1-Bishop | King + Color1-Bishop
        if ((bitboards[2] | bitboards[8]) == 0)
        {
            // Check if bishops of same colour
            bool anyOnDark = ((bitboards[3] | bitboards[9]) & Bitboard.darkSquares) != 0;
            bool anyOnLight = ((bitboards[3] | bitboards[9]) & Bitboard.lightSquares) != 0;
            if (anyOnDark != anyOnLight) return true;
        }
        return false;
    }
    public bool IsFiftyMoveRule()
    {
        return halfmove >= 100; // 50 for each player
    }
    public bool IsRepetition()
    {
        int count = 0;
        for (int i=positionHistory.Count-1;i>=0;i--)
        {
            if (positionHistory[i] == zobristKey) count++;
        }
        return count >= 3;
    }
    public bool IsAgreement()
    {
        return false;
    }
    public override string ToString()
    {
        string output = "\n";
        string horizontalLine = new string('-', 24) + "\n"; // Creates "---------------------------------"
        for (int rank = 7; rank >= 0; rank--)
        {
            output += horizontalLine;
            for (int file = 0;file<=7;file++)
            {
                int cell = ChessGame.CellToID(file,rank);
                char c = ' ';
                if (Bitboard.HasBit(bitboards[0],cell)) c = 'K';
                else if (Bitboard.HasBit(bitboards[1],cell)) c = 'P';
                else if (Bitboard.HasBit(bitboards[2],cell)) c = 'N';
                else if (Bitboard.HasBit(bitboards[3],cell)) c = 'B';
                else if (Bitboard.HasBit(bitboards[4],cell)) c = 'R';
                else if (Bitboard.HasBit(bitboards[5],cell)) c = 'Q';
                else if (Bitboard.HasBit(bitboards[6],cell)) c = 'k';
                else if (Bitboard.HasBit(bitboards[7],cell)) c = 'p';
                else if (Bitboard.HasBit(bitboards[8],cell)) c = 'n';
                else if (Bitboard.HasBit(bitboards[9],cell)) c = 'b';
                else if (Bitboard.HasBit(bitboards[10],cell)) c = 'r';
                else if (Bitboard.HasBit(bitboards[11],cell)) c = 'q';

                output += string.Format("| {0} ", c);
            }
        }
        output += "\n"+horizontalLine; // Final bottom border
        return output;
    }
    public static string BoardToFen(Board b)
    {
        string fen = "";
        // Position
        int empty = 0;
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0;file<=7;file++)
            {
                int cell = ChessGame.CellToID(file,rank);
                char c = ' ';
                if (Bitboard.HasBit(b.bitboards[0],cell)) c = 'K';
                else if (Bitboard.HasBit(b.bitboards[1],cell)) c = 'P';
                else if (Bitboard.HasBit(b.bitboards[2],cell)) c = 'N';
                else if (Bitboard.HasBit(b.bitboards[3],cell)) c = 'B';
                else if (Bitboard.HasBit(b.bitboards[4],cell)) c = 'R';
                else if (Bitboard.HasBit(b.bitboards[5],cell)) c = 'Q';
                else if (Bitboard.HasBit(b.bitboards[6],cell)) c = 'k';
                else if (Bitboard.HasBit(b.bitboards[7],cell)) c = 'p';
                else if (Bitboard.HasBit(b.bitboards[8],cell)) c = 'n';
                else if (Bitboard.HasBit(b.bitboards[9],cell)) c = 'b';
                else if (Bitboard.HasBit(b.bitboards[10],cell)) c = 'r';
                else if (Bitboard.HasBit(b.bitboards[11],cell)) c = 'q';
                if (c == ' ') {
                    empty += 1;
                }
                else
                {
                    fen += (empty != 0) ? empty.ToString() + c : c;
                    empty = 0;
                }
                if (file == 7) {
                    fen += (empty != 0) ? empty.ToString() : "";
                    if (rank > 0) fen += "/";
                    empty = 0;
                }
            }
        }

        // SideToMove
        fen += b.colourToMove == 8 ? " w " : " b ";
        // Castling 
        string cast = "";
        cast += b.castling[0] ? "K" : "";
        cast += b.castling[1] ? "Q" : "";
        cast += b.castling[2] ? "k" : "";
        cast += b.castling[3] ? "q" : "";
        fen += (cast.Length != 0) ? cast + " " : "- ";
        // En Passant
        fen += (b.enpassant >= 0) ? ChessGame.IDToString(b.enpassant) + " " : "- ";
        // Halfmove
        fen += b.halfmove.ToString() + " ";
        // Fullmove
        fen += b.fullmove.ToString();
        return fen;
    }
}