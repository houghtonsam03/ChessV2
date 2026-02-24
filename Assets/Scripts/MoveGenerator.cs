using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NUnit.Framework;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
public static class MoveGenerator
{
    // Helpful constants
    // index = (up,down,left,right,upleft,downleft,upright,downright)
    public static readonly int[] DirectionOffset = { 8, -8, -1, 1, 7, -9, 9, -7 };
    // index = (square * 8) + direction
    public static readonly int[] NumSquaresToEdge = {7,0,0,7,0,0,7,0,7,0,1,6,1,0,6,0,7,0,2,5,2,0,5,0,7,0,3,4,3,0,4,0,7,0,4,3,4,0,3,0,7,0,5,2,5,0,2,0,7,0,6,1,6,0,1,0,7,0,7,0,7,0,0,0,6,1,0,7,0,0,6,1,6,1,1,6,1,1,6,1,6,1,2,5,2,1,5,1,6,1,3,4,3,1,4,1,6,1,4,3,4,1,3,1,6,1,5,2,5,1,2,1,6,1,6,1,6,1,1,1,6,1,7,0,6,1,0,0,5,2,0,7,0,0,5,2,5,2,1,6,1,1,5,2,5,2,2,5,2,2,5,2,5,2,3,4,3,2,4,2,5,2,4,3,4,2,3,2,5,2,5,2,5,2,2,2,5,2,6,1,5,2,1,1,5,2,7,0,5,2,0,0,4,3,0,7,0,0,4,3,4,3,1,6,1,1,4,3,4,3,2,5,2,2,4,3,4,3,3,4,3,3,4,3,4,3,4,3,4,3,3,3,4,3,5,2,4,3,2,2,4,3,6,1,4,3,1,1,4,3,7,0,4,3,0,0,3,4,0,7,0,0,3,4,3,4,1,6,1,1,3,4,3,4,2,5,2,2,3,4,3,4,3,4,3,3,3,4,3,4,4,3,3,4,3,3,3,4,5,2,3,4,2,2,3,4,6,1,3,4,1,1,3,4,7,0,3,4,0,0,2,5,0,7,0,0,2,5,2,5,1,6,1,1,2,5,2,5,2,5,2,2,2,5,2,5,3,4,2,3,2,4,2,5,4,3,2,4,2,3,2,5,5,2,2,5,2,2,2,5,6,1,2,5,1,1,2,5,7,0,2,5,0,0,1,6,0,7,0,0,1,6,1,6,1,6,1,1,1,6,1,6,2,5,1,2,1,5,1,6,3,4,1,3,1,4,1,6,4,3,1,4,1,3,1,6,5,2,1,5,1,2,1,6,6,1,1,6,1,1,1,6,7,0,1,6,0,0,0,7,0,7,0,0,0,7,0,7,1,6,0,1,0,6,0,7,2,5,0,2,0,5,0,7,3,4,0,3,0,4,0,7,4,3,0,4,0,3,0,7,5,2,0,5,0,2,0,7,6,1,0,6,0,1,0,7,7,0,0,7,0,0};
    // Attacking masks
    // index = square
    public static readonly ulong[] KingAttacks = {770,1797,3594,7188,14376,28752,57504,49216,197123,460039,920078,1840156,3680312,7360624,14721248,12599488,50463488,117769984,235539968,471079936,942159872,1884319744,3768639488,3225468928,12918652928,30149115904,60298231808,120596463616,241192927232,482385854464,964771708928,825720045568,3307175149568,7718173671424,15436347342848,30872694685696,61745389371392,123490778742784,246981557485568,211384331665408,846636838289408,1975852459884544,3951704919769088,7903409839538176,15806819679076352,31613639358152704,63227278716305408,54114388906344448,216739030602088448,505818229730443264,1011636459460886528,2023272918921773056,4046545837843546112,8093091675687092224,16186183351374184448,13853283560024178688,144959613005987840,362258295026614272,724516590053228544,1449033180106457088,2898066360212914176,5796132720425828352,11592265440851656704,4665729213955833856};
    // index = (square * 2) + colour
    public static readonly ulong[] PawnAttacks = {512,0,1280,0,2560,0,5120,0,10240,0,20480,0,40960,0,16384,0,131072,2,327680,5,655360,10,1310720,20,2621440,40,5242880,80,10485760,160,4194304,64,33554432,512,83886080,1280,167772160,2560,335544320,5120,671088640,10240,1342177280,20480,2684354560,40960,1073741824,16384,8589934592,131072,21474836480,327680,42949672960,655360,85899345920,1310720,171798691840,2621440,343597383680,5242880,687194767360,10485760,274877906944,4194304,2199023255552,33554432,5497558138880,83886080,10995116277760,167772160,21990232555520,335544320,43980465111040,671088640,87960930222080,1342177280,175921860444160,2684354560,70368744177664,1073741824,562949953421312,8589934592,1407374883553280,21474836480,2814749767106560,42949672960,5629499534213120,85899345920,11258999068426240,171798691840,22517998136852480,343597383680,45035996273704960,687194767360,18014398509481984,274877906944,144115188075855872,2199023255552,360287970189639680,5497558138880,720575940379279360,10995116277760,1441151880758558720,21990232555520,2882303761517117440,43980465111040,5764607523034234880,87960930222080,11529215046068469760,175921860444160,4611686018427387904,70368744177664,0,562949953421312,0,1407374883553280,0,2814749767106560,0,5629499534213120,0,11258999068426240,0,22517998136852480,0,45035996273704960,0,18014398509481984};
    // index = square
    public static readonly ulong[] KnightAttacks = {132096,329728,659712,1319424,2638848,5277696,10489856,4202496,33816580,84410376,168886289,337772578,675545156,1351090312,2685403152,1075839008,8657044482,21609056261,43234889994,86469779988,172939559976,345879119952,687463207072,275414786112,2216203387392,5531918402816,11068131838464,22136263676928,44272527353856,88545054707712,175990581010432,70506185244672,567348067172352,1416171111120896,2833441750646784,5666883501293568,11333767002587136,22667534005174272,45053588738670592,18049583422636032,145241105196122112,362539804446949376,725361088165576704,1450722176331153408,2901444352662306816,5802888705324613632,11533718717099671552,4620693356194824192,288234782788157440,576469569871282176,1224997833292120064,2449995666584240128,4899991333168480256,9799982666336960512,1152939783987658752,2305878468463689728,1128098930098176,2257297371824128,4796069720358912,9592139440717824,19184278881435648,38368557762871296,4679521487814656,9077567998918656};
    // index = square
    public static readonly ulong[] BishopAttacks = {18049651735527936,70506452091904,275415828992,1075975168,38021120,8657588224,2216338399232,567382630219776,9024825867763712,18049651735527424,70506452221952,275449643008,9733406720,2216342585344,567382630203392,1134765260406784,4512412933816832,9024825867633664,18049651768822272,70515108615168,2491752130560,567383701868544,1134765256220672,2269530512441344,2256206450263040,4512412900526080,9024834391117824,18051867805491712,637888545440768,1135039602493440,2269529440784384,4539058881568768,1128098963916800,2256197927833600,4514594912477184,9592139778506752,19184279556981248,2339762086609920,4538784537380864,9077569074761728,562958610993152,1125917221986304,2814792987328512,5629586008178688,11259172008099840,22518341868716544,9007336962655232,18014673925310464,2216338399232,4432676798464,11064376819712,22137335185408,44272556441600,87995357200384,35253226045952,70506452091904,567382630219776,1134765260406784,2832480465846272,5667157807464448,11333774449049600,22526811443298304,9024825867763712,18049651735527936};
    // index = square
    public static readonly ulong[] RookAttacks = {282578800148862,565157600297596,1130315200595066,2260630401190006,4521260802379886,9042521604759646,18085043209519166,36170086419038334,282578800180736,565157600328704,1130315200625152,2260630401218048,4521260802403840,9042521604775424,18085043209518592,36170086419037696,282578808340736,565157608292864,1130315208328192,2260630408398848,4521260808540160,9042521608822784,18085043209388032,36170086418907136,282580897300736,565159647117824,1130317180306432,2260632246683648,4521262379438080,9042522644946944,18085043175964672,36170086385483776,283115671060736,565681586307584,1130822006735872,2261102847592448,4521664529305600,9042787892731904,18085034619584512,36170077829103616,420017753620736,699298018886144,1260057572672512,2381576680245248,4624614895390720,9110691325681664,18082844186263552,36167887395782656,35466950888980736,34905104758997504,34344362452452352,33222877839362048,30979908613181440,26493970160820224,17522093256097792,35607136465616896,9079539427579068672,8935706818303361536,8792156787827803136,8505056726876686336,7930856604974452736,6782456361169985536,4485655873561051136,9115426935197958144};
    // Magic Bitboards
    // index = square
    public static readonly ulong[] BishopMagics = {0x20031108090040UL, 0xC08500700410000UL, 0x1104012206110800UL, 0x4088184108000040UL, 0xC05A040042010UL, 0x8820C8804004UL, 0x4804210402A00380UL, 0x8084202C4200434UL, 0x460040404080600UL, 0x1200A00805828380UL, 0x50C2100912006400UL, 0x442C04008D0043UL, 0x141091040808040UL, 0x1808090420140000UL, 0x41C02029100UL, 0x880838411031000UL, 0x8005081004080802UL, 0x21201004018480UL, 0x9201000208050102UL, 0x1C010202120201UL, 0x100C000080A08600UL, 0x42800410018812UL, 0x1021080400A80500UL, 0x4000840CC800UL, 0x460080024108402UL, 0x8200814414200UL, 0x408020104042200UL, 0x6401080004004090UL, 0x85A88C8004002001UL, 0x600101004A300800UL, 0x800784044084040AUL, 0x100822009010840UL, 0x42020C0082221UL, 0x9AC1040281200800UL, 0x5080228810100060UL, 0x1000100860040400UL, 0x28B0008200206200UL, 0x30020080103003UL, 0x32040044041200UL, 0x3A8120083002080UL, 0x8088090520009020UL, 0x302492C50002042UL, 0x1018201004080UL, 0x13006011008800UL, 0x82C0392000400UL, 0x4005200800804340UL, 0x804040802080540UL, 0x2080060800102UL, 0x22010402400001UL, 0x822028228020120UL, 0x200010288540000UL, 0x44030662881000UL, 0x144000E020411800UL, 0x1B40403902042300UL, 0x1220049108010102UL, 0x10090801005004UL, 0xD02038209100220UL, 0x60810402020600UL, 0x603088800UL, 0x91001000A99C0400UL, 0x20410000C09906A0UL, 0x14000A20080080UL, 0x2800102021040080UL, 0x20011001104080UL};
    public static readonly int[] BishopShifts = {58, 59, 59, 59, 59, 59, 59, 58, 59, 59, 59, 59, 59, 59, 59, 59, 59, 59, 57, 57, 57, 57, 59, 59, 59, 59, 57, 55, 55, 57, 59, 59, 59, 59, 57, 55, 55, 57, 59, 59, 59, 59, 57, 57, 57, 57, 59, 59, 59, 59, 59, 59, 59, 59, 59, 59, 58, 59, 59, 59, 59, 59, 59, 58};   
    public static readonly int[] BishopOffsets = {0,64,96,128,160,192,224,256,320,352,384,416,448,480,512,544,576,608,640,768,896,1024,1152,1184,1216,1248,1280,1408,1920,2432,2560,2592,2624,2656,2688,2816,3328,3840,3968,4000,4032,4064,4096,4224,4352,4480,4608,4640,4672,4704,4736,4768,4800,4832,4864,4896,4928,4992,5024,5056,5088,5120,5152,5184};
    public static ulong[] BishopTable;
    public static readonly ulong[] RookMagics = {0x8080102080004000UL, 0x1240001000402000UL, 0x22000A0080403020UL, 0x480100028000C81UL, 0x200080420106200UL, 0x8100020400486900UL, 0x1C00088804010230UL, 0x4200008201410864UL, 0x8000C0002080UL, 0x3000C01000402005UL, 0x1801802000881002UL, 0x30800800803003UL, 0x1000801001006UL, 0x40A002811020004UL, 0x8014000104021008UL, 0x10A000181020464UL, 0x420A08000844000UL, 0x4008200220102C0UL, 0x4203050010200040UL, 0x4200818010010800UL, 0x4158808004020801UL, 0x2880800A002401UL, 0x821C040010C80619UL, 0x80A000080D104UL, 0x2008200230040UL, 0x44826500400101UL, 0x1050200080100880UL, 0x900100300090022UL, 0x402000A000D6030UL, 0x4044060080140080UL, 0x308102400020801UL, 0x2040200008245UL, 0x4080C000800832UL, 0x18200040401004UL, 0x82080500A802000UL, 0x1048082101001000UL, 0x4022D40080800800UL, 0x6081000249000400UL, 0x8040280104001002UL, 0x10000810000E2UL, 0x810804009208000UL, 0x800201000404000UL, 0x20030041110020UL, 0x1820A01C1220010UL, 0x880100050030UL, 0x42010C10060008UL, 0x3081002140029UL, 0x8A06400820001UL, 0x2005024320800500UL, 0x2004204102008200UL, 0x8220804A005200UL, 0x1005004682100UL, 0x20880080040080UL, 0x80010008022C0100UL, 0x1000100208010400UL, 0x200581044C0200UL, 0x10B200428100A096UL, 0x1014106049008202UL, 0x8042A003000811UL, 0x100009002005UL, 0x2022002410086022UL, 0x101000804001209UL, 0x10220881004UL, 0x30094002042UL};
    public static readonly int[] RookShifts = {52, 53, 53, 53, 53, 53, 53, 52, 53, 54, 54, 54, 54, 54, 54, 53, 53, 54, 54, 54, 54, 54, 54, 53, 53, 54, 54, 54, 54, 54, 54, 53, 53, 54, 54, 54, 54, 54, 54, 53, 53, 54, 54, 54, 54, 54, 54, 53, 53, 54, 54, 54, 54, 54, 54, 53, 52, 53, 53, 53, 53, 53, 53, 52};
    public static readonly int[] RookOffsets = {0,4096,6144,8192,10240,12288,14336,16384,20480,22528,23552,24576,25600,26624,27648,28672,30720,32768,33792,34816,35840,36864,37888,38912,40960,43008,44032,45056,46080,47104,48128,49152,51200,53248,54272,55296,56320,57344,58368,59392,61440,63488,64512,65536,66560,67584,68608,69632,71680,73728,74752,75776,76800,77824,78848,79872,81920,86016,88064,90112,92160,94208,96256,98304};
    public static ulong[] RookTable;
    static MoveGenerator()
    {
        PrecalculateTables();
    }
    private static void PrecalculateTables()
    {
        ulong[][] BishopBlockers = new ulong[64][];
        ulong[][] BishopLegal = new ulong[64][];
        for (int sq=0;sq<64;sq++)
        {
            ulong mask = BishopAttacks[sq];
            BishopBlockers[sq] = CreateAllBlockerBitboards(mask);
            BishopLegal[sq] = new ulong[BishopBlockers[sq].Length];
            for (int i=0;i<BishopBlockers[sq].Length;i++)
            {
                BishopLegal[sq][i] = PrecalculateLegalMoves(sq,BishopBlockers[sq][i],false);
            }
        }
        ulong[][] RookBlockers = new ulong[64][];
        ulong[][] RookLegal = new ulong[64][];
        for (int sq=0;sq<64;sq++)
        {
            ulong mask = RookAttacks[sq];
            RookBlockers[sq] = CreateAllBlockerBitboards(mask);
            RookLegal[sq] = new ulong[RookBlockers[sq].Length];
            for (int i=0;i<RookBlockers[sq].Length;i++)
            {
                RookLegal[sq][i] = PrecalculateLegalMoves(sq,RookBlockers[sq][i],true);
            }
        }

        int BSize = 0;
        int RSize = 0;
        for (int sq = 0; sq < 64; sq++)
        {
            BSize += (1 << (64 - BishopShifts[sq]));

            RSize += (1 << (64 - RookShifts[sq]));
        }
    
        BishopTable = new ulong[BSize];
        RookTable = new ulong[RSize];
        for (int sq = 0; sq < 64; sq++)
        {
            for (int i=0;i<BishopBlockers[sq].Length;i++)
            {
                int index = (int)((BishopBlockers[sq][i] * BishopMagics[sq]) >> BishopShifts[sq]);
                BishopTable[BishopOffsets[sq]+index] = BishopLegal[sq][i];
            } 
            for (int i=0;i<RookBlockers[sq].Length;i++) 
            {
                int index = (int)((RookBlockers[sq][i] * RookMagics[sq]) >> RookShifts[sq]);
                RookTable[RookOffsets[sq]+index] = RookLegal[sq][i];
            }
        }
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
        ulong friendlyPices = white ? board.bitboards[12] : board.bitboards[13];

        ulong movesMask = 0UL;
        if (Piece.IsType(type,Piece.Bishop))
        {
            ulong bishopBlockers = board.bitboards[14] & BishopAttacks[startCell];
            int bishopIndex = (int)((bishopBlockers * BishopMagics[startCell]) >> BishopShifts[startCell]);
            movesMask = BishopTable[BishopOffsets[startCell]+bishopIndex];
        }
        else if (Piece.IsType(type,Piece.Rook))
        {
            ulong rookBlockers = board.bitboards[14] & RookAttacks[startCell];
            int rookIndex = (int)((rookBlockers * RookMagics[startCell]) >> RookShifts[startCell]);
            movesMask = RookTable[RookOffsets[startCell]+rookIndex];
        }
        else if (Piece.IsType(type,Piece.Queen))
        {
            ulong bishopBlockers = board.bitboards[14] & BishopAttacks[startCell];
            int bishopIndex = (int)((bishopBlockers * BishopMagics[startCell]) >> BishopShifts[startCell]);
            ulong bishopMovesMask = BishopTable[BishopOffsets[startCell]+bishopIndex];

            ulong rookBlockers = board.bitboards[14] & RookAttacks[startCell];
            int rookIndex = (int)((rookBlockers * RookMagics[startCell]) >> RookShifts[startCell]);
            ulong rookMovesMask = RookTable[RookOffsets[startCell]+rookIndex];

            movesMask = bishopMovesMask | rookMovesMask;
        }

        movesMask &= ~friendlyPices;
        while (movesMask != 0)
        {
            int target = Bitboard.PopLowestBit(ref movesMask);
            moves.Add(new Move(startCell,target));
        }
    }
    public static void GenerateKingMoves(Board board,List<Move> moves,int startCell,int colour,bool includeCastling)
    {
        bool white = Piece.IsColour(colour,Piece.white);
        int teamMask = white ? 12 : 13;
        ulong attacking = white ? board.blackAttacks : board.whiteAttacks;

        ulong moveBoard = KingAttacks[startCell];
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
        ulong attackSquares = PawnAttacks[startCell*2+team];
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
            if (Bitboard.HasBit(PawnAttacks[startCell*2+team],board.enpassant)) moves.Add(new Move(startCell,board.enpassant,0,false,true));
        }

        // Check pawn move 1 step.
        int targetCell = startCell + DirectionOffset[team];
        if (Bitboard.HasBit(board.bitboards[14],targetCell)) return;
        if (ChessGame.GetRank(targetCell) == lastRank) AddPromotionMoves(moves,startCell,targetCell);
        else moves.Add(new Move(startCell,targetCell));

        // Check pawn move 2 step.
        if (ChessGame.GetRank(startCell) == firstRank)
        {
            targetCell = startCell + DirectionOffset[team] * 2;
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
        ulong moveBoard = KnightAttacks[startCell];
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