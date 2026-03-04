public class Move
{
    public readonly int StartSquare;
    public readonly int TargetSquare;
    public readonly int promotionPiece;
    public readonly bool castling;
    public readonly bool enpassant;
    public Move(int start,int target,int promotion=0,bool cast=false,bool enpass=false)
    {
        StartSquare = start;
        TargetSquare = target;
        promotionPiece = promotion;
        castling = cast;
        enpassant = enpass;
    }
    public static bool operator ==(Move mv1,Move mv2)
    {
        return  (mv1.StartSquare == mv2.StartSquare) &&
                (mv1.TargetSquare == mv2.TargetSquare) &&
                (mv1.promotionPiece == mv2.promotionPiece);
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
        return (StartSquare << 6) ^ TargetSquare;
    }
    public override string ToString()
    {
        string s = ChessGame.IDToString(StartSquare) + ChessGame.IDToString(TargetSquare);

        return s;
    }
    public static string AlgebraicNotation(Move move,bool isCheck,bool isCheckmate,bool isCapture,int movingPiece)
    {
        if (move.castling && ChessGame.GetRank(move.TargetSquare) == 2) return "O-O-O";
        else if (move.castling && ChessGame.GetRank(move.TargetSquare) == 6) return "O-O";
        string moveNote = "";
        moveNote += Piece.IsType(movingPiece,Piece.Pawn) ? "" : Piece.ToString(movingPiece);
        if (isCapture) 
        {
            if (Piece.IsType(movingPiece,Piece.Pawn))
            {
                string[] files = {"a","b","c","d","e","f","g","h"};
                moveNote += files[ChessGame.GetFile(move.StartSquare)];
            }
            moveNote += "x";
        }
        moveNote += ChessGame.IDToString(move.TargetSquare);
        if (move.promotionPiece != Piece.None) moveNote += Piece.ToString(move.promotionPiece);
        if (isCheckmate) moveNote += "#";
        else if (isCheck) moveNote += "+";
        return moveNote; 
    }   
}