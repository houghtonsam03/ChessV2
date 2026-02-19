
using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using UnityEngine;
using static ChessGame;

public class Board
{
    public int[] Square = new int[64];
    public int colourToMove = 8;
    public bool[] castling = new bool[4]{true,true,true,true}; // {White Kingside,White Queenside,Black Kingside,Black Queenside}
    public int enpassant = -1; // the square on which an En Passant Move is possible. (Behind the pawn that moved 2 squares.) 
    public int halfmove = 0;
    public int fullmove = 1;
    public  bool gameOver = false;
    // Logic variables
    private List<LastMove> lastMoves;
    public struct LastMove
    {
        public Move Move;
        public int CapturedPiece;
        public bool[] Castling;
        public int EnPassant;
        public int HalfMove;
        public int FullMove;
        public LastMove(Move mv,int piece,bool[] cast,int en,int half,int full)
        {
            Move = mv;
            CapturedPiece = piece;
            Castling = new bool[4];
            Castling[0] = cast[0]; Castling[1] = cast[1]; Castling[2] = cast[2]; Castling[3] = cast[3];
            EnPassant = en;
            HalfMove = half;
            FullMove = full;
        }
    }
    public static readonly string startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public List<ulong> positionHistory = new List<ulong>();
    public ulong zobristKey;
    public Board()
    {
        Square = new int[64];
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
        foreach (char letter in pos)
        {
            if (letter == '/') cell -= 16;
            else if (char.IsDigit(letter))
            {
                cell += (int)char.GetNumericValue(letter);
            }
            else
            {
                int piece = char.IsUpper(letter) ? Piece.white : Piece.black;
                if (char.ToLower(letter) == 'k') piece |= Piece.King;
                if (char.ToLower(letter) == 'p') piece |= Piece.Pawn;
                if (char.ToLower(letter) == 'n') piece |= Piece.Knight;
                if (char.ToLower(letter) == 'b') piece |= Piece.Bishop;
                if (char.ToLower(letter) == 'r') piece |= Piece.Rook;
                if (char.ToLower(letter) == 'q') piece |= Piece.Queen;
                Square[cell] = piece;
                cell += 1;
            }
        }
        zobristKey = ZobristHash(this);
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

        for (int i=0;i<64;i++) {Square[i] = b.Square[i];}
        zobristKey = ZobristHash(this);
        positionHistory.Add(zobristKey);
    }
    public void MakeMove(Move move)
    {
        // Board assumes move is possible and legal.

        // Record Move
        int capturedPiece = Square[move.TargetSquare];
        if (move.enpassant) {
            int captureSquare = GetRank(move.StartSquare)*8 + GetFile(move.TargetSquare);
            capturedPiece = Square[captureSquare];
        }
        LastMove lm = new LastMove(move,capturedPiece,castling,enpassant,halfmove,fullmove);
        lastMoves.Add(lm);

        // Zobrist
        if (enpassant >= 0) zobristKey ^= ZobristKeys[12*64+1+4+GetFile(enpassant)];
        // Record movement
        enpassant = -1;
        if (!Piece.IsType(Square[move.StartSquare],Piece.Pawn) && Piece.IsType(lm.CapturedPiece,Piece.None)) halfmove += 1;
        else halfmove = 0;
        if (Piece.IsColour(colourToMove,Piece.black)) fullmove += 1;

        int movingPiece = Square[move.StartSquare];

        // Make Move
        if (move.castling)
        {
            // Move King
            Square[move.TargetSquare] = Square[move.StartSquare];
            Square[move.StartSquare] = Piece.None;
            // Move Rook
            int rank = GetRank(move.StartSquare);
            int startFile = GetFile(move.TargetSquare) == 2 ? 0 : 7;
            int targetFile = GetFile(move.TargetSquare) == 2 ? 3 : 5;
            int rookStart = rank*8+startFile;
            int rookTarget = rank*8+targetFile;
            Square[rookTarget] = Square[rookStart];
            Square[rookStart] = Piece.None;
            // Update legal castling moves
            if (rank == 0) 
            {
                // Zobrist 
                if (castling[0]) zobristKey ^= ZobristKeys[12*64+1+0];
                if (castling[1]) zobristKey ^= ZobristKeys[12*64+1+1];
                // Castling
                castling[0] = false; castling[1] = false;
            }
            else 
            {
                // Zobrist 
                if (castling[2]) zobristKey ^= ZobristKeys[12*64+1+2];
                if (castling[3]) zobristKey ^= ZobristKeys[12*64+1+3];
                castling[2] = false; castling[3] = false;
            }
            // Zobrist
            zobristKey ^= ZobricPositionHash(movingPiece,move.StartSquare) ^ ZobricPositionHash(movingPiece,move.TargetSquare);
            zobristKey ^= ZobricPositionHash(Square[rookTarget],rookStart) ^ ZobricPositionHash(Square[rookTarget],rookTarget);
        }
        else if (move.enpassant)
        {
            // Move friendly pawn
            Square[move.TargetSquare] = Square[move.StartSquare];
            Square[move.StartSquare] = Piece.None;
            // Remove enemy pawn
            int rank = GetRank(move.StartSquare);
            int file = GetFile(move.TargetSquare);
            int capturedSquare = rank*8+file;
            Square[capturedSquare] = Piece.None;
            // Zobrist
            zobristKey ^= ZobricPositionHash(movingPiece,move.StartSquare) ^ ZobricPositionHash(movingPiece,move.TargetSquare);
            zobristKey ^= ZobricPositionHash(capturedPiece,capturedSquare);
        }
        else if (move.promotionPiece != 0)
        {
            Square[move.TargetSquare] = move.promotionPiece | colourToMove;
            Square[move.StartSquare] = Piece.None;
            if (move.TargetSquare == 7 && castling[0]) {castling[0] = false; zobristKey ^= ZobristKeys[12*64+1+0];}
            else if (move.TargetSquare == 0 && castling[1]) {castling[1] = false; zobristKey ^= ZobristKeys[12*64+1+1];}
            else if (move.TargetSquare == 63 && castling[2]) {castling[2] = false; zobristKey ^= ZobristKeys[12*64+1+2];}
            else if (move.TargetSquare == 56 && castling[3]) {castling[3] = false; zobristKey ^= ZobristKeys[12*64+1+3];}
            // Zobrist
            zobristKey ^= ZobricPositionHash(movingPiece,move.StartSquare);
            zobristKey ^= ZobricPositionHash(capturedPiece,move.TargetSquare) ^ ZobricPositionHash(Square[move.TargetSquare],move.TargetSquare);
        }
        else
        {
            // Move pieces
            Square[move.TargetSquare] = Square[move.StartSquare];
            Square[move.StartSquare] = Piece.None;
            // Zobrist 
            zobristKey ^= ZobricPositionHash(movingPiece,move.StartSquare) ^ ZobricPositionHash(movingPiece,move.TargetSquare);
            zobristKey ^= ZobricPositionHash(capturedPiece,move.TargetSquare);
            // Update legal castling moves
            if (Piece.IsType(movingPiece,Piece.King))
            {
                if (Piece.GetColour(movingPiece) == Piece.white) 
                {
                    if (castling[0]) zobristKey ^= ZobristKeys[12*64+1+0];
                    if (castling[1]) zobristKey ^= ZobristKeys[12*64+1+1];
                    castling[0] = false; castling[1] = false;
                }
                else 
                {
                    if (castling[2]) zobristKey ^= ZobristKeys[12*64+1+2];
                    if (castling[3]) zobristKey ^= ZobristKeys[12*64+1+3];
                    castling[2] = false; castling[3] = false;
                }
            }
            foreach (int sq in new int[]{move.StartSquare,move.TargetSquare})
            {
                if (sq == 7 && castling[0]) {castling[0] = false; zobristKey ^= ZobristKeys[12*64+1+0];}
                if (sq == 0 && castling[1]) {castling[1] = false; zobristKey ^= ZobristKeys[12*64+1+1];}
                if (sq == 63 && castling[2]) {castling[2] = false; zobristKey ^= ZobristKeys[12*64+1+2];}
                if (sq == 56 && castling[3]) {castling[3] = false; zobristKey ^= ZobristKeys[12*64+1+3];}
            }
            // Update En Passant possible location
            if (Piece.IsType(movingPiece,Piece.Pawn) && (Math.Abs(move.TargetSquare-move.StartSquare) == 16)) 
            {
                enpassant = move.StartSquare + 8 * Math.Sign(move.TargetSquare-move.StartSquare);
                zobristKey ^= ZobristKeys[12*64+1+4+GetFile(enpassant)];
            }
        }
        colourToMove = Piece.GetOpponentColour(colourToMove);
        zobristKey ^= ZobristKeys[12*64];
        positionHistory.Add(zobristKey);
    }
    public void UndoMove()
    {
        if (lastMoves.Count == 0) return;
        // Get LastMove
        LastMove lastMove = lastMoves[lastMoves.Count-1];
        lastMoves.RemoveAt(lastMoves.Count-1);

        if (lastMove.Move.castling)
        {
            // Revert King
            Square[lastMove.Move.StartSquare] = Square[lastMove.Move.TargetSquare];
            Square[lastMove.Move.TargetSquare] = Piece.None;
            // Revert Rook
            int startFile = (lastMove.Move.TargetSquare-lastMove.Move.StartSquare) > 0 ? 7 : 0;
            int targetFile = (lastMove.Move.TargetSquare-lastMove.Move.StartSquare) > 0 ? 5 : 3;
            int rank = GetRank(lastMove.Move.StartSquare);
            int rookStart = rank*8+startFile;
            int rookTarget = rank*8+targetFile;
            Square[rookStart] = Square[rookTarget];
            Square[rookTarget] = Piece.None;

            
        }
        else if (lastMove.Move.enpassant)
        {

            int capturedSquare = GetRank(lastMove.Move.StartSquare) * 8 + GetFile(lastMove.Move.TargetSquare);

            // Revert move
            Square[lastMove.Move.StartSquare] = Square[lastMove.Move.TargetSquare];
            Square[lastMove.Move.TargetSquare] = Piece.None;
            Square[capturedSquare] = lastMove.CapturedPiece;
        }
        else if (lastMove.Move.promotionPiece != 0)
        {
            // Remove promoted and recreate pawn
            Square[lastMove.Move.StartSquare] = Piece.Pawn | Piece.GetOpponentColour(colourToMove);
            Square[lastMove.Move.TargetSquare] = lastMove.CapturedPiece;
        }
        else
        {
            // Revert move
            Square[lastMove.Move.StartSquare] = Square[lastMove.Move.TargetSquare];
            Square[lastMove.Move.TargetSquare] = lastMove.CapturedPiece;
        }
        // Update variables
        castling[0] = lastMove.Castling[0]; castling[1] = lastMove.Castling[1]; castling[2] = lastMove.Castling[2]; castling[3] = lastMove.Castling[3];
        enpassant = lastMove.EnPassant;
        halfmove = lastMove.HalfMove;
        fullmove = lastMove.FullMove;
        colourToMove = Piece.GetOpponentColour(colourToMove);
        gameOver = false;

        // Update Zobrist positions
        positionHistory.RemoveAt(positionHistory.Count-1);
        zobristKey = positionHistory[positionHistory.Count-1];

    }
    public int FindKing(int colour)
    {
        int mask = colour+Piece.King;
        for (int i=0;i<64;i++)
        {
            if (Square[i] == mask) return i;
        }
        return -1; // No King Found
    }
    public List<int> GetPieces(int colour=24,int type=0)
    {
        List<int> pieces = new List<int>();
        for (int pos=0;pos<64;pos++)
        {   
            if (Piece.IsColour(Square[pos],colour))
            {
                if (type == 0) pieces.Add(Square[pos]);
                else if (type != 0 && Piece.IsType(Square[pos],type)) pieces.Add(Square[pos]);
            }
            // Debug.Log($"ID: {pos} | Piece: {Square[pos]} | pieces length : {pieces.Count}");
        }
        return pieces;
    }
    public List<int> GetPiecePositions(int colour=24,int type=0)
    {
        List<int> positions = new List<int>();
        for (int pos=0;pos<64;pos++)
        {
            if (Piece.IsColour(Square[pos],colour))
            {
                if (type == 0 && !Piece.IsType(Square[pos],Piece.None)) positions.Add(pos);
                else if (type != 0 && Piece.IsType(Square[pos],type)) positions.Add(pos);
            } 
        }
        return positions;
    }
    public List<Move> GetAttackingMoves(int defending,int colour=24,int type=0)
    {
        // Pseudolegal attacks
        List<Move> attacks = new List<Move>();
        List<int> enemyPositions = GetPiecePositions(colour);
        foreach (int pos in enemyPositions) 
        {
            List<Move> moves = ChessGame.GenerateMove(this,pos,includeCastling: false);
            foreach (Move move in moves)
            {   
                if (type == 0 && move.TargetSquare == defending) attacks.Add(move);
                else if (type != 0 && move.TargetSquare == defending && Piece.IsType(move.movingPiece,type)) attacks.Add(move);
            }
        }
        return attacks;
    }
    public bool IsAttacked(int defending,int colour=24,int type=0)
    {
        return GetAttackingMoves(defending,colour,type).Count != 0;
    }
    public bool IsCheck(int colour)
    {
        int kingSquare = FindKing(colour);
        return IsAttacked(kingSquare,Piece.GetOpponentColour(colour));
    }
    public bool IsCheckMate(int colour)
    {
        List<Move> moves = GenerateMoves(this,colour);
        return moves.Count == 0 && IsCheck(colour);
    }
    public int IsGameOver(float whiteTime,float blackTime)
    {
        // Not Gameover - 0
        // White Win - 1,2,3
        // Black Win - 4,5,6
        // Draw - 7,8,9,10,11,12
        
        if (IsCheckMate(colourToMove)) return Piece.IsColour(colourToMove,Piece.black) ? 1 : 4;
        if (IsResigned()) return Piece.IsColour(colourToMove,Piece.black) ? 2 : 5;
        if (IsTimeout(colourToMove,whiteTime,blackTime) == 1) return Piece.IsColour(colourToMove,Piece.black) ? 3 : 6;
        if (IsStaleMate(colourToMove)) return 7;
        if (IsInsufficientMaterial()) return 8;
        if (IsFiftyMoveRule()) return 9;
        if (IsRepetition()) return 10;
        if (IsAgreement()) return 11;
        if (IsTimeout(colourToMove,whiteTime,blackTime) == 2) return 12;
        return 0;
        
    } 
    public bool IsStaleMate(int colour)
    {
        return GenerateMoves(this,colour).Count == 0;
    }
    public bool IsResigned()
    {
        return false;
    }
    public int IsTimeout(int colour,float whiteTime, float blackTime)
    {
        // 0 is no timeout, 1 is timout, 2 is timout vs insufficient material (draw)
        float time = Piece.IsColour(colour,Piece.white) ? whiteTime : blackTime;
        // Factor insufficient material
        List<int> enemyPieces = GetPieces(Piece.GetOpponentColour(colour));


        if (time >= 0) return 0; // Not timeout
        return enemyPieces.Count > 1 ? 1 : 2; // timeout or draw
    }
    public bool IsInsufficientMaterial()
    {
        List<int> whitePieces = GetPieces(Piece.white);
        List<int> blackPieces = GetPieces(Piece.black);
        int[] white = new int[7];
        foreach (int piece in whitePieces)
        {
            int type = Piece.GetType(piece);
            white[type] += 1;
        }
        int[] black = new int[7];
        foreach (int piece in blackPieces)
        {
            int type = Piece.GetType(piece);
            black[type] += 1;
        }
        // If there is a pawn, rook or queen, it's not insufficient.
        if (white[Piece.Pawn] + black[Piece.Pawn] + white[Piece.Rook] + black[Piece.Rook] + white[Piece.Queen] + black[Piece.Queen] > 0) return false;

        int totalKnights = white[Piece.Knight] + black[Piece.Knight];
        int totalBishops = white[Piece.Bishop] + black[Piece.Bishop];

        // King | King
        if (totalKnights == 0 && totalBishops == 0) return true;
        // King + Bishop/Knight | King
        if (totalKnights == 1 && totalBishops == 0) return true;
        // King | King + Bishop/Knight
        // King + Color1-Bishop | King + Color1-Bishop
        if (totalKnights == 0 && totalBishops > 0)
        {
            // Check if bishops of same colour
            int firstBishopColor = -1;
            List<int> positions = GetPiecePositions(24,Piece.Bishop);
            foreach (int pos in positions)
            {
                Vector2Int cell = BoardUI.IDToCell(pos);
                int color = (cell.x+cell.y) % 2;
                if (firstBishopColor == -1) firstBishopColor = color;
                else if (color != firstBishopColor) return false;
            }
            return true;
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
        for (int i=0;i<positionHistory.Count;i++)
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

        for (int y = 7; y >= 0; y--) 
        {
            output += horizontalLine; // Add a line between rows for better visibility
            for (int x = 0; x < 8; x++)
            {
                int cell = y * 8 + x;
                int piece = Square[cell];
                int type = Piece.GetType(piece);

                // Get the character (using a simpler switch for readability)
                char s = type switch {
                    Piece.King => 'k',
                    Piece.Pawn => 'p',
                    Piece.Knight => 'n',
                    Piece.Bishop => 'b',
                    Piece.Rook => 'r',
                    Piece.Queen => 'q',
                    _ => ' ' // Empty square
                };

                char finalChar = Piece.GetColour(piece) == Piece.black ? s : char.ToUpper(s);

                // {0,-4} means: take the first argument, and pad it to 4 characters, left-aligned.
                output += string.Format("| {0} ", piece);
            }
            output += "|\n"; // Close the row
        }
        output += horizontalLine; // Final bottom border
        return output;
    }
    public static string BoardToFen(Board b)
    {
        string fen = "";
        // Position
        char[] typeStr = new char[]{' ','k','p','n','b','r','q'};
        int empty = 0;
        for (int i = 56;i>=0;)
        {
            int piece = b.Square[i];
            if (piece == 0) {
                empty += 1;
            }
            else
            {
                int typeMask = 0b_00111; int sideMask = 0b_11000;
                int type = piece & typeMask; int side = piece & sideMask;
                char s = (side == 8) ? char.ToUpper(typeStr[type]) : typeStr[type];
                fen += (empty != 0) ? empty.ToString() + s : s;
                empty = 0;
            }
            if ((i+1)%8 == 0) {
                fen += (empty != 0) ? empty.ToString() : "";
                if (i >= 8) fen += "/";
                i-=15;
                empty = 0;
            }

            else i+=1;
            
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
        fen += (b.enpassant != -1) ? CellToString(b.enpassant) + " " : "- ";
        // Halfmove
        fen += b.halfmove.ToString() + " ";
        // Fullmove
        fen += b.fullmove.ToString();
        return fen;
    }
    public static ulong ZobricPositionHash(int piece, int cell)
    {
        if (Piece.IsType(piece,Piece.None)) return 0;
        int pieceType = Piece.GetType(piece);
        int pieceTeam = Piece.GetColour(piece);
        int pieceIndex = pieceType-1 + (pieceTeam == Piece.white ? 0 : 6);
        return ZobristKeys[(pieceIndex*64)+cell];
    }
    public static ulong ZobristHash(Board b)
    {
        ulong hash = 0;
        for (int i=0;i<64;i++)
        {
            int pieceType = Piece.GetType(b.Square[i]);
            hash ^= ZobricPositionHash(pieceType,i);
        }
        if (Piece.IsColour(b.colourToMove,Piece.black)) hash ^= ZobristKeys[12*64];
        for (int i=0;i<4;i++)
        {
            if (b.castling[i]) hash ^= ZobristKeys[12*64+1+i];
        }
        if (b.enpassant >= 0) hash ^= ZobristKeys[12*64+1+4+GetFile(b.enpassant)];
        return hash;
    }
}