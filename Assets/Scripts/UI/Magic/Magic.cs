using UnityEngine;
using System;
using Random = System.Random;
using System.Data.SqlTypes;
using UnityEngine.InputSystem;
using TMPro;

public class Magic : MonoBehaviour
{
    public static readonly int[] DirectionOffset = new int[] { 8, -8, -1, 1, 7, -9, 9, -7 };
    public static readonly int[] NumSquaresToEdge = new int[]{7,0,0,7,0,0,7,0,7,0,1,6,1,0,6,0,7,0,2,5,2,0,5,0,7,0,3,4,3,0,4,0,7,0,4,3,4,0,3,0,7,0,5,2,5,0,2,0,7,0,6,1,6,0,1,0,7,0,7,0,7,0,0,0,6,1,0,7,0,0,6,1,6,1,1,6,1,1,6,1,6,1,2,5,2,1,5,1,6,1,3,4,3,1,4,1,6,1,4,3,4,1,3,1,6,1,5,2,5,1,2,1,6,1,6,1,6,1,1,1,6,1,7,0,6,1,0,0,5,2,0,7,0,0,5,2,5,2,1,6,1,1,5,2,5,2,2,5,2,2,5,2,5,2,3,4,3,2,4,2,5,2,4,3,4,2,3,2,5,2,5,2,5,2,2,2,5,2,6,1,5,2,1,1,5,2,7,0,5,2,0,0,4,3,0,7,0,0,4,3,4,3,1,6,1,1,4,3,4,3,2,5,2,2,4,3,4,3,3,4,3,3,4,3,4,3,4,3,4,3,3,3,4,3,5,2,4,3,2,2,4,3,6,1,4,3,1,1,4,3,7,0,4,3,0,0,3,4,0,7,0,0,3,4,3,4,1,6,1,1,3,4,3,4,2,5,2,2,3,4,3,4,3,4,3,3,3,4,3,4,4,3,3,4,3,3,3,4,5,2,3,4,2,2,3,4,6,1,3,4,1,1,3,4,7,0,3,4,0,0,2,5,0,7,0,0,2,5,2,5,1,6,1,1,2,5,2,5,2,5,2,2,2,5,2,5,3,4,2,3,2,4,2,5,4,3,2,4,2,3,2,5,5,2,2,5,2,2,2,5,6,1,2,5,1,1,2,5,7,0,2,5,0,0,1,6,0,7,0,0,1,6,1,6,1,6,1,1,1,6,1,6,2,5,1,2,1,5,1,6,3,4,1,3,1,4,1,6,4,3,1,4,1,3,1,6,5,2,1,5,1,2,1,6,6,1,1,6,1,1,1,6,7,0,1,6,0,0,0,7,0,7,0,0,0,7,0,7,1,6,0,1,0,6,0,7,2,5,0,2,0,5,0,7,3,4,0,3,0,4,0,7,4,3,0,4,0,3,0,7,5,2,0,5,0,2,0,7,6,1,0,6,0,1,0,7,7,0,0,7,0,0};
    public static readonly ulong[] BishopAttacks = new ulong[]{18049651735527936,70506452091904,275415828992,1075975168,38021120,8657588224,2216338399232,567382630219776,9024825867763712,18049651735527424,70506452221952,275449643008,9733406720,2216342585344,567382630203392,1134765260406784,4512412933816832,9024825867633664,18049651768822272,70515108615168,2491752130560,567383701868544,1134765256220672,2269530512441344,2256206450263040,4512412900526080,9024834391117824,18051867805491712,637888545440768,1135039602493440,2269529440784384,4539058881568768,1128098963916800,2256197927833600,4514594912477184,9592139778506752,19184279556981248,2339762086609920,4538784537380864,9077569074761728,562958610993152,1125917221986304,2814792987328512,5629586008178688,11259172008099840,22518341868716544,9007336962655232,18014673925310464,2216338399232,4432676798464,11064376819712,22137335185408,44272556441600,87995357200384,35253226045952,70506452091904,567382630219776,1134765260406784,2832480465846272,5667157807464448,11333774449049600,22526811443298304,9024825867763712,18049651735527936};
    public static readonly ulong[] RookAttacks = new ulong[]{282578800148862,565157600297596,1130315200595066,2260630401190006,4521260802379886,9042521604759646,18085043209519166,36170086419038334,282578800180736,565157600328704,1130315200625152,2260630401218048,4521260802403840,9042521604775424,18085043209518592,36170086419037696,282578808340736,565157608292864,1130315208328192,2260630408398848,4521260808540160,9042521608822784,18085043209388032,36170086418907136,282580897300736,565159647117824,1130317180306432,2260632246683648,4521262379438080,9042522644946944,18085043175964672,36170086385483776,283115671060736,565681586307584,1130822006735872,2261102847592448,4521664529305600,9042787892731904,18085034619584512,36170077829103616,420017753620736,699298018886144,1260057572672512,2381576680245248,4624614895390720,9110691325681664,18082844186263552,36167887395782656,35466950888980736,34905104758997504,34344362452452352,33222877839362048,30979908613181440,26493970160820224,17522093256097792,35607136465616896,9079539427579068672,8935706818303361536,8792156787827803136,8505056726876686336,7930856604974452736,6782456361169985536,4485655873561051136,9115426935197958144};
    
    private ulong[][] BishopBlockers;
    private ulong[][] RookBlockers;
    private ulong[][] BishopLegal;
    private ulong[][] RookLegal; 
    private int calc;
    private ulong[] BishopMagics;
    private int[] BishopShifts;
    private ulong[] RookMagics;
    private int[] RookShifts;
    // Unity Gameobjects
    private TextMeshProUGUI textFrame;
    private void UsefulMagicFunction()
    {
        ulong[] BishopMagics = new ulong[64];
        int[] BishopShifts = new int[64];
        int[] BishopOffsets = new int[64];

        ulong[] RookMagics = new ulong[64];
        int[] RookShifts = new int[64];
        int[] RookOffsets = new int[64];

        ulong[] BishopTable = new ulong[20480];
        ulong[] RookTable = new ulong[20480];

        // Bishop Magic Bitboards
        int currentOffset = 0;
        for (int sq=0;sq<64;sq++)
        {
            ulong mask = BishopAttacks[sq];
            int bits = Bitboard.Count(mask);
            ulong[] blockers = CreateAllBlockerBitboards(mask);
            ulong magicNum = FindMagic(sq, bits, true);

            BishopMagics[sq] = magicNum;
            BishopShifts[sq] = 64 - bits;
            BishopOffsets[sq] = currentOffset;

            // Fill the table for this square
            for (int i = 0; i < blockers.Length; i++) {
                int hash = (int)((blockers[i] * magicNum) >> (64 - bits));
                // Calculate the TRUE moves (including edges/captures)
                BishopTable[currentOffset + hash] = PrecalculateLegalMoves(sq, blockers[i], true);
            }
            currentOffset += (1 << bits);
        }
        // Rook Magic Bitboards
        currentOffset = 0;
        for (int sq=0;sq<64;sq++)
        {
            ulong mask = RookAttacks[sq];
            int bits = Bitboard.Count(mask);
            ulong[] blockers = CreateAllBlockerBitboards(mask);
            ulong magicNum = FindMagic(sq, bits, true);

            RookMagics[sq] = magicNum;
            RookShifts[sq] = 64 - bits;
            RookOffsets[sq] = currentOffset;

            // Fill the table for this square
            for (int i = 0; i < blockers.Length; i++) {
                int hash = (int)((blockers[i] * magicNum) >> (64 - bits));
                // Calculate the TRUE moves (including edges/captures)
                RookTable[currentOffset + hash] = PrecalculateLegalMoves(sq, blockers[i], true);
            }
            currentOffset += (1 << bits);
        }
    }
    void Start()
    {
        calc = 0;
        BishopMagics = new ulong[64];
        BishopShifts = new int[64];
        BishopBlockers = new ulong[64][];
        BishopLegal = new ulong[64][];
        for (int sq=0;sq<64;sq++)
        {
            ulong mask = BishopAttacks[sq];
            BishopShifts[sq] = 64-20;
            BishopBlockers[sq] = CreateAllBlockerBitboards(mask);
            BishopLegal[sq] = new ulong[BishopBlockers[sq].Length];
            for (int i=0;i<BishopBlockers[sq].Length;i++)
            {
                BishopLegal[sq][i] = PrecalculateLegalMoves(sq,BishopBlockers[sq][i],false);
            }
        }
        RookMagics = new ulong[64];
        RookShifts = new int[64];
        RookBlockers = new ulong[64][];
        RookLegal = new ulong[64][];
        for (int sq=0;sq<64;sq++)
        {
            ulong mask = RookAttacks[sq];
            RookShifts[sq] = 64-20;
            RookBlockers[sq] = CreateAllBlockerBitboards(mask);
            RookLegal[sq] = new ulong[RookBlockers[sq].Length];
            for (int i=0;i<RookBlockers[sq].Length;i++)
            {
                RookLegal[sq][i] = PrecalculateLegalMoves(sq,RookBlockers[sq][i],true);
            }
        }
        textFrame = this.transform.Find("TextFrame").Find("Text").GetComponent<TextMeshProUGUI>();
        PrintMagic();
    }
    void Update()
    {
        int sq = calc / 2;
        bool isRook = (calc % 2) == 1;
        ulong mask = isRook ? RookAttacks[sq] : BishopAttacks[sq];
        int maskBits = Bitboard.Count(mask);

        int currentShift = isRook ? RookShifts[sq] : BishopShifts[sq];
        int currentBestBits = 64 - currentShift;
        int floor = isRook ? 10 : 5;
        for (int testBits = currentBestBits-1; testBits >= floor; testBits--) 
        {
            ulong candidate = FindMagic(sq, testBits, isRook);
            if (candidate != 0) 
            {
                int lastShift = isRook ? RookShifts[sq] : BishopShifts[sq];
                SaveMagic(sq, candidate, 64 - testBits, isRook);
                PrintMagic();
                Debug.Log($"Improved {sq} | {lastShift}->{64-testBits}");
            }
            else 
            {
                // If testBits failed, testBits - 1 will definitely fail.
                break; 
            }
        }
        calc++;
        calc %= 64*2;
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExportMagics();
        }
    }
    private void ExportMagics()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- BISHOP MAGICS ---");
        sb.Append("public static readonly ulong[] BishopMagics = { ");
        for (int i = 0; i < 64; i++) sb.Append($"0x{BishopMagics[i]:X}UL, ");
        sb.AppendLine("};");

        sb.Append("public static readonly int[] BishopShifts = { ");
        for (int i = 0; i < 64; i++) sb.Append($"{BishopShifts[i]}, ");
        sb.AppendLine("};");

        sb.AppendLine("\n--- ROOK MAGICS ---");
        sb.Append("public static readonly ulong[] RookMagics = { ");
        for (int i = 0; i < 64; i++) sb.Append($"0x{RookMagics[i]:X}UL, ");
        sb.AppendLine("};");

        sb.Append("public static readonly int[] RookShifts = { ");
        for (int i = 0; i < 64; i++) sb.Append($"{RookShifts[i]}, ");
        sb.AppendLine("};");

        Debug.Log(sb.ToString());
        Debug.Log("Magics exported to Console! You can now copy-paste these into your MoveGenerator.");
    }
    private void SaveMagic(int square,ulong magic,int shift,bool isRook)
    {
        if (!isRook)
        {
            BishopMagics[square] = magic;
            BishopShifts[square] = shift;
        }
        else
        {
            RookMagics[square] = magic;
            RookShifts[square] = shift;
        }
    }
    private void PrintMagic()
    {
        int BMagicCount = 0, RMagicCount = 0;
        int BLowest = 64, BHighest = 0;
        int RLowest = 64, RHighest = 0;
        long BTotalEntries = 0, RTotalEntries = 0;
        for (int sq=0;sq<64;sq++)
        {
            if (BishopMagics[sq] != 0) 
            {
                BMagicCount++;
                int bits = 64 - BishopShifts[sq];
                BLowest = Math.Min(BLowest,bits);
                BHighest = Math.Max(BHighest,bits);
                BTotalEntries += (1L << bits);
            }
            if (RookMagics[sq] != 0) 
            {
                RMagicCount++;
                int bits = 64 - RookShifts[sq];
                RLowest = Math.Min(RLowest,bits);
                RHighest = Math.Max(RHighest,bits);
                RTotalEntries += (1L << bits);
            }
        }
        double BSizeKB = (BTotalEntries * 8.0) / 1024.0;
        double RSizeKB = (RTotalEntries * 8.0) / 1024.0;
        string line1 = "Bishop Magics";
        string line2 = BMagicCount+"/64 Magics found";
        string line3 = "Lowest Bit Count: "+BLowest;
        string line4 = "Highest Bit Count: "+BHighest;
        string line5 = "Total size: "+BSizeKB+"KB";
        string line6 = "";
        string line7 = "Rook Magics";
        string line8 = RMagicCount+"/64 Magics found";
        string line9 = "Lowest Bit Count: "+RLowest;
        string line10 = "Highest Bit Count: "+RHighest;
        string line11 = "Total size: "+RSizeKB+"KB";
        textFrame.text = line1+"\n"+line2+"\n"+line3+"\n"+line4+"\n"+line5+"\n"+line6+"\n"+line7+"\n"+line8+"\n"+line9+"\n"+line10+"\n"+line11;
     }
    private static ulong[] CreateAllBlockerBitboards(ulong movementMask)
    {
        int bitCount = Bitboard.Count(movementMask);
        int numPatterns = 1 << bitCount;
        ulong[] blockerPatterns = new ulong[numPatterns];
        for (int patternIndex=0;patternIndex<numPatterns;patternIndex++)
        {
            ulong tempMask = movementMask;
            for (int bitIndex = 0;bitIndex < bitCount;bitIndex++)
            {
                int bit = (patternIndex >> bitIndex) & 1;
                blockerPatterns[patternIndex] |= (ulong)bit << Bitboard.PopLowestBit(ref tempMask);
            }
        }
        return blockerPatterns;
    }
    private static ulong PrecalculateLegalMoves(int startSquare, ulong blockers, bool isRook)
    {
        ulong attacks = 0;
        // Rook uses directions 0-3, Bishop uses 4-7
        int startDir = isRook ? 0 : 4;
        int endDir = isRook ? 4 : 8;

        for (int dir = startDir; dir < endDir; dir++)
        {
            // Use your precomputed distance to the edge
            for (int n = 1; n <= NumSquaresToEdge[startSquare * 8 + dir]; n++)
            {
                int target = startSquare + DirectionOffset[dir] * n;
                
                // Add the square to our attacks
                attacks |= (1UL << target);

                // If there is a blocker on this square, the ray is BLOCKED.
                // We stop here (but we already added the target, representing a capture).
                if (((blockers >> target) & 1) != 0) break;
            }
        }
        return attacks;
    }
    private ulong FindMagic(int sq, int bits, bool isRook)
    {
        ulong[] blockers = isRook ? RookBlockers[sq] : BishopBlockers[sq];
        ulong[] targetMoves = isRook ? RookLegal[sq] : BishopLegal[sq];
        Random rng = new Random();

        int tableSize = 1 << bits;
        ulong[] testTable = new ulong[tableSize];
        // We use a separate array to track if a slot is occupied
        // This handles cases where the legal move bitboard is actually 0
        bool[] occupied = new bool[tableSize]; 
        int shift = 64 - bits;

        for (int iter = 0; iter < 100000; iter++)
        {
            // Generate candidate
            ulong magic = RandomU64(rng) & RandomU64(rng) & RandomU64(rng);
            
            // Reset tracking arrays without re-allocating memory
            Array.Clear(testTable, 0, tableSize);
            Array.Clear(occupied, 0, tableSize);

            bool fail = false;
            for (int i = 0; i < blockers.Length; i++)
            {
                int hash = (int)((blockers[i] * magic) >> shift);

                if (!occupied[hash])
                {
                    occupied[hash] = true;
                    testTable[hash] = targetMoves[i];
                }
                else if (testTable[hash] != targetMoves[i])
                {
                    fail = true;
                    break;
                }
            }

            if (!fail) return magic;
        }
        return 0;
    }
    private static ulong RandomU64(Random rng)
    {
        ulong u1 = (ulong)rng.Next(0, 65536);
        ulong u2 = (ulong)rng.Next(0, 65536);
        ulong u3 = (ulong)rng.Next(0, 65536);
        ulong u4 = (ulong)rng.Next(0, 65536);
        return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
    }
}