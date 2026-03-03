public static class Piece
{
    public const int None = 0;
    public const int King = 1;
    public const int Pawn = 2;
    public const int Knight = 3;
    public const int Bishop = 4;
    public const int Rook = 5;
    public const int Queen = 6;

    public const int white = 8;
    public const int black = 16;
    public static int GetColour(int piece)
    {
        return piece & 0b_11000;
    }
    public static int GetType(int piece)
    {
        return piece & 0b_00111;
    }
    public static bool IsSlidingPiece(int piece)
    {
        return (piece & 0b_00100) != 0;
    }
    public static bool IsColour(int piece,int colour)
    {
        return (piece & colour) != 0;
    }
    public static bool IsType(int piece,int type)
    {
        return GetType(piece) == type;
    }
    public static bool IsTypeAndColour(int piece,int type,int colour)
    {
        return IsType(piece,type) && IsColour(piece,colour);
    }
    public static int GetOpponentColour(int piece)
    {
        return GetColour(piece ^ 0b_11000);
    }
}