using System;

public readonly struct Move
{
    public static readonly Move NullMove = new Move(0, 0, MoveFlags.None);
    // Pack everything into 16 bits: 
    // From (6 bits) | To (6 bits) | Flags (4 bits)
    public readonly ushort Value;

    public byte From => (byte)(Value & 0x3F);
    public byte To => (byte)((Value >> 6) & 0x3F);
    public MoveFlags Flags => (MoveFlags)(Value >> 12);

    public Move(int from, int to, MoveFlags flags = MoveFlags.None)
    {
        Value = (ushort)(from | (to << 6) | ((int)flags << 12));
    }

    public static bool operator ==(Move mv1,Move mv2)
    {
        return  mv1.Value == mv2.Value;
    }
    public static bool operator !=(Move mv1,Move mv2)
    {
        return !(mv1 == mv2);
    }
    public override bool Equals(object obj)
    {
        return obj is Move other && this == other;
    }
    public override int GetHashCode()
    {
        return (From << 6) ^ To;
    }
    public bool IsCastling()
    {
        return Flags == MoveFlags.Castling;
    }
    public bool IsEnPassant()
    {
        return Flags == MoveFlags.EnPassant;
    }
    public bool IsPromotion()
    {
        return (int)Flags >= (int)MoveFlags.KnightPromotion;
    }
    public int GetPromotionPiece()
    {
        return Flags switch
        {
            MoveFlags.KnightPromotion => Piece.Knight,
            MoveFlags.BishopPromotion => Piece.Bishop,
            MoveFlags.RookPromotion => Piece.Rook,
            MoveFlags.QueenPromotion => Piece.Queen,
            _ => Piece.None
        };
    }
    public static MoveFlags GetPromotionFlag(int promotionPiece)
    {
        if (Piece.IsType(promotionPiece,Piece.Knight)) return MoveFlags.KnightPromotion;
        else if (Piece.IsType(promotionPiece,Piece.Bishop)) return MoveFlags.BishopPromotion;
        else if (Piece.IsType(promotionPiece,Piece.Rook)) return MoveFlags.RookPromotion;
        else if (Piece.IsType(promotionPiece,Piece.Queen)) return MoveFlags.QueenPromotion;
        return MoveFlags.None;
    }
    public override string ToString()
    {
        string s = ChessGame.IDToString(From) + ChessGame.IDToString(To);

        return s;
    }
    public static string AlgebraicNotation(Move move,bool isCheck,bool isCheckmate,bool isCapture,int movingPiece)
    {
        if (move.IsCastling() && ChessGame.GetRank(move.To) == 2) return "O-O-O";
        else if (move.IsCastling() && ChessGame.GetRank(move.To) == 6) return "O-O";
        string moveNote = "";
        moveNote += Piece.IsType(movingPiece,Piece.Pawn) ? "" : Piece.ToString(movingPiece);
        if (isCapture) 
        {
            if (Piece.IsType(movingPiece,Piece.Pawn))
            {
                string[] files = {"a","b","c","d","e","f","g","h"};
                moveNote += files[ChessGame.GetFile(move.From)];
            }
            moveNote += "x";
        }
        moveNote += ChessGame.IDToString(move.To);
        if (move.IsPromotion()) moveNote += Piece.ToString(move.GetPromotionPiece()+Piece.GetColour(movingPiece));
        if (isCheckmate) moveNote += "#";
        else if (isCheck) moveNote += "+";
        return moveNote; 
    }   
}
public enum MoveFlags : byte
{
    None = 0,
    Castling = 1,
    EnPassant = 2,
    KnightPromotion = 3,
    BishopPromotion = 4,
    RookPromotion = 5,
    QueenPromotion = 6
}