public class Move
{
    public readonly int movingPiece;
    public readonly int StartSquare;
    public readonly int TargetSquare;
    public readonly int promotionPiece;
    public readonly bool castling;
    public readonly bool enpassant;
    public Move(int piece,int start,int target,int promotion=0,bool cast=false,bool enpass=false)
    {
        movingPiece = piece;
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
}