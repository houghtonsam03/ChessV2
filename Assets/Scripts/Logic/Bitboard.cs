using JetBrains.Annotations;
using Unity.Mathematics;
public static class Bitboard
{
    public const int whiteKing = 0;
    public const int whitePawn = 1;
    public const int whiteKnight = 2;
    public const int whiteBishop = 3;
    public const int whiteRook = 4;
    public const int whiteQueen = 5;
    public const int blackKing = 6;
    public const int blackPawn = 7;
    public const int blackKnight = 8;
    public const int blackBishop = 9;
    public const int blackRook = 10;
    public const int blackQueen = 11;

    // Handy constant bitboards
    public const ulong darkSquares = 0xAA55AA55AA55AA55UL;
    public const ulong lightSquares = ~darkSquares;
    public const ulong kingsideCastle = 0x60UL;
    public const ulong queensideCastle = 0xCUL;
    private static readonly int[] DeBruijnTable = 
    {
        0,  1,  48,  2, 57, 49, 28,  3,
        61, 58, 50, 42, 38, 29, 17,  4,
        62, 59, 31, 51, 33, 43, 39, 22,
        54, 30, 24, 18, 11,  5, 10,  7,
        63, 47, 56, 27, 60, 41, 37, 16,
        32, 21, 53, 23, 12,  9, 46, 26,
        40, 36, 15, 20, 52,  8, 45, 25,
        35, 14, 19, 44, 13, 34,  6, 55
    };

    public static int TrailingZeroCount(ulong bitboard) 
    {
        return math.tzcnt(bitboard);
    }
    public static void SetBit(ref ulong bitboard, int square)
    {
        bitboard |= 1UL << square;
    }
    public static void ClearBit(ref ulong bitboard, int square)
    {
        bitboard &= ~(1UL << square);
    }
    public static void ToggleBit(ref ulong bitboard, int square)
    {
        bitboard ^= 1UL << square;
    }
    public static bool HasBit(ulong bitboard, int square)
    {
        return (bitboard & (1UL << square)) != 0UL;
    }
    public static int PopLowestBit(ref ulong bitboard) {
        int index = TrailingZeroCount(bitboard);
        bitboard &= bitboard - 1; // Clears the lowest set bit
        return index;
    }
    public static int Count(ulong x)
    {
        x -= (x >> 1) & 0x5555555555555555UL;
        x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
        x = (x + (x >> 4)) & 0x0f0f0f0f0f0f0f0fUL;
        return (int)((x * 0x0101010101010101UL) >> 56);
    }
}