using UnityEditor.U2D.Aseprite;

public static class Zobrist
{
    public static readonly ulong[] ZobristKeys = new ulong[12*64+1+4+8];
    static Zobrist()
    {
        PrecomputeZobristData();
    }
    private static void PrecomputeZobristData()
    {
        System.Random rand = new System.Random(23); // Fixed seed
        for (int i=0;i<12*64+1+4+8;i++)
        {
            ZobristKeys[i] = RandomUlong(rand);
        }
    }
    private static ulong RandomUlong(System.Random rnd) {
        byte[] buffer = new byte[8];
        rnd.NextBytes(buffer);
        return System.BitConverter.ToUInt64(buffer, 0);
    }
    public static ulong ZobricPositionHash(int piece, int cell)
    {
        // int piece is 0-11 according to bitboards.
        return ZobristKeys[piece*64+cell];
    }
    public static ulong ZobristHash(Board b)
    {
        ulong hash = 0;
        for (int i=0;i<12;i++)
        {
            ulong bitboard = b.bitboards[i];
            while (bitboard != 0)
            {
                int cell = Bitboard.PopLowestBit(ref bitboard);
                hash ^= ZobricPositionHash(i,cell);
            }
        }
        if (Piece.IsColour(b.colourToMove,Piece.black)) hash ^= ZobristKeys[12*64];
        for (int i=0;i<4;i++)
        {
            if (b.castling[i]) hash ^= ZobristKeys[12*64+1+i];
        }
        if (b.enpassant >= 0) hash ^= ZobristKeys[12*64+1+4+ChessGame.GetFile(b.enpassant)];
        return hash;
    }
}