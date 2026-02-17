using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;

public class ChessEngine : MonoBehaviour
{
    public bool graphics;
    public float timeDelay = 0.1f;
    public enum Player1Side {White,Black,Random};
    public Player1Side PlayerOneSide;
    public bool humanPlayer1;
    public ChessAgent agent1;
    public bool humanPlayer2;
    public ChessAgent agent2;

    // System variables
    private bool player1white;
    private ChessAgent[] agents;
    private Chessboard boardObject;
    private PlayerListener playerListener;

    // Timer variables;
    private float timer;
    // Chess variables
    private string gameState;
    private List<Move> moves;

    // Movement variables
    public static readonly int[] directionOffsets = {8,-8,-1,1,7,-9,9,-7};
    public static readonly int[][] NumSquaresToEdge = new int[64][];
    public static readonly List<Move>[] knightMoves = new List<Move>[64];
    static void PrecomputeMoveData()
    {
        for (int x = 0; x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                int numUp = 7 - y;
                int numDown = y;
                int numLeft = x;
                int numRight = 7 - x;

                int cellID = 8*y+x;

                NumSquaresToEdge[cellID] = new int[]{
                    numUp,
                    numDown,
                    numLeft,
                    numRight,
                    Math.Min(numUp,numLeft),
                    Math.Min(numDown,numLeft),
                    Math.Min(numUp,numRight),
                    Math.Min(numDown,numRight)
                };
                int[][] knightMoveCells = new int[][]
                {
                    new int[]{x-1,y+2},
                    new int[]{x+1,y+2},
                    new int[]{x-1,y-2},
                    new int[]{x+1,y-2},
                    new int[]{x-2,y+1},
                    new int[]{x-2,y-1},
                    new int[]{x+2,y+1},
                    new int[]{x+2,y-1}
                };
                knightMoves[cellID] = new List<Move>();
                foreach (int[] move in knightMoveCells)
                {
                    if (move[0] < 0 || move[0] > 7 || move[1] < 0 || move[1] > 7) continue;
                    knightMoves[cellID].Add(new Move(cellID,move[1]*8+move[0]));
                }
            }
        }
    }
    
    void Start()
    {
        // Initial gameState
        gameState = Board.startingFen;
        Board.FenToBoard(Board.startingFen);
        PrecomputeMoveData(); // Generate values for move generation.
        GenerateMoves();

        // Set Side
        if (PlayerOneSide == Player1Side.White) player1white = true;
        if (PlayerOneSide == Player1Side.Black) player1white = false;
        if (PlayerOneSide == Player1Side.Random) player1white = UnityEngine.Random.value < 0.5f;
        
        // Start agents
        agents = new ChessAgent[2]{agent1,agent2};
        if (humanPlayer1 || agent1 == null) agents[0] = null;
        else agents[0].StartAgent(player1white);
        if (humanPlayer2 || agent2 == null) agents[1] = null;
        else agents[1].StartAgent(!player1white);

        // Spawn chessboard and PlayerListener if we want graphics
        if (graphics || humanPlayer1 || humanPlayer2)
        {
            GameObject prefab = Resources.Load<GameObject>("ChessboardPrefab");
            GameObject bObject = Instantiate(prefab,Vector3.zero,Quaternion.identity);
            boardObject = bObject.GetComponent<Chessboard>();
            boardObject.Setup(this);
            boardObject.readBoard();
            PlayerListener playerListener = boardObject.AddComponent<PlayerListener>();
            playerListener.Setup(this,boardObject);
        }

    }

    void Update()
    {
        if (!Board.gameOver)
        {   
            // Time Delay
            timer += Time.deltaTime;
            if (timer < timeDelay) return;
            
            // Find the index of the player whose turn it is.
            int turnIndex = (player1white == (Board.ColourToMove == 8)) ? 0 : 1;
            if (agents[turnIndex] == null) // Human
            {
                // We wait for playerListener to make move.
                return;
            }

            else // AI Agent
            {
                // Main AI Loop
                Move move = agents[turnIndex].getMove();
                MakeMove(move);

                // Time delay logic
                timer = 0;
            }   
        }
    }
    public bool hasPiece(int cell)
    {
        return Board.Square[cell] != 0;
    }
    public bool hasPiece(int cell,int colour)
    {
        // Chess Logic
        return (Board.Square[cell] & colour) != 0;
    }
    public void MakeMove(Move move)
    {
        if (IsLegalMove(move))
        {
            // Chess Logic
            Board.MakeMove(move);

            // Update engine
            if (graphics || humanPlayer1 || humanPlayer2) boardObject.Move(move.StartSquare,move.TargetSquare);
            moves = GenerateMoves();
        }
        // Debug Logic
        else
        {
            string s = "";
            if (Board.ColourToMove == 8) s += "white";
            else s+= "black";
            Debug.Log($"Illegal move by {s} player.");
        }
    }
    public bool IsLegalMove(Move move)
    {
        // Chess Logic
        return moves.Contains(move);
    }
    private void UpdateState()
    {
        gameState = Board.BoardToFen();
    }
    public List<Move> GetLegalMoves()
    {
        List<Move> legalMoves = new List<Move>();
        foreach (Move move in moves)
        {
            legalMoves.Add(move);
        }
        return legalMoves;
    } 
    public List<Move> GetLegalMoves(int start)
    {
        List<Move> legalMoves = new List<Move>();
        foreach (Move move in moves)
        {
            if (move.StartSquare == start) legalMoves.Add(move);
        }
        return legalMoves;
    } 
    private List<Move> GenerateMoves()
    {
        moves = new List<Move>();
        for (int start=0;start<64;start++)
        {
            int piece = Board.Square[start];
            if (Piece.IsColour(piece,Board.ColourToMove))
            {
                if (Piece.IsSlidingPiece(piece))
                {
                    GenerateSlidingMoves(start,piece);
                }
                else if (Piece.IsType(piece,Piece.King))
                {
                    GenerateKingMoves(start,piece);
                }
                else if (Piece.IsType(piece,Piece.Pawn))
                {
                    GeneratePawnMoves(start,piece);
                }
                else if (Piece.IsType(piece,Piece.Knight))
                {
                    GenerateKnightMoves(start,piece);
                }
            }
        }
        return moves;
    }
    private void GenerateSlidingMoves(int startCell, int piece)
    {
        int startDirIndex = Piece.IsType(piece,Piece.Bishop) ? 4 : 0;
        int endDirIndex = Piece.IsType(piece,Piece.Rook) ? 4 : 8;
        for (int directionIndex = startDirIndex; directionIndex<endDirIndex;directionIndex++)
        {
            for (int n=1;n <= NumSquaresToEdge[startCell][directionIndex];n++)
            {
                int targetCell = startCell + directionOffsets[directionIndex] * n;
                int pieceOnTarget = Board.Square[targetCell];

                if (Piece.IsColour(pieceOnTarget,Piece.GetColour(piece))) // Same colour piece
                {
                    break;
                }
                moves.Add(new Move(startCell,targetCell));
                
                if (Piece.IsColour(pieceOnTarget,Piece.GetOpponentColour(piece)))
                {
                    break;
                }
            }
        }
    }
    private void GenerateKingMoves(int startCell, int piece)
    {
        for (int i=0;i<8;i++)
        {   
            if (NumSquaresToEdge[startCell][i] == 0) continue;
            int targetCell = startCell + directionOffsets[i];
            int targetPiece = Board.Square[targetCell];
            if (Piece.IsColour(targetPiece,Piece.GetColour(piece))) continue;
            moves.Add(new Move(startCell,targetCell));
        }
    }
    private void GeneratePawnMoves(int startCell, int piece)
    {
        // En Passant logic here
        

        // In case pawn can't move further (pre-promotion code)
        if ((GetRank(startCell) == 8 && Piece.IsColour(piece,Piece.white)) || (GetRank(startCell) == 1 && Piece.IsColour(piece,Piece.black))) return;

        // Check diagonal capture 
        int team = (Piece.GetColour(piece)/8)-1;

        int[] captureDirIndex = new int[]{4+team,6+team}; // Index for diagonal movements.
        foreach (int dirIndex in captureDirIndex)
        {
            if (NumSquaresToEdge[startCell][dirIndex] == 0) continue;
            int captureCell = startCell + directionOffsets[dirIndex];
            int capturePiece = Board.Square[captureCell];
            if (!Piece.IsColour(capturePiece,Piece.GetColour(piece)) && capturePiece != 0) moves.Add(new Move(startCell,captureCell));
        }

        // Check pawn move 1 step.
        int targetCell = startCell + directionOffsets[team];
        int targetPiece = Board.Square[targetCell];
        if (targetPiece != 0) return;
        moves.Add(new Move(startCell,targetCell));

        // Check pawn move 2 step.
        if ((GetRank(startCell) == 2 && Piece.IsColour(piece,Piece.white)) || (GetRank(startCell) == 7 && Piece.IsColour(piece,Piece.black)))
        {
            targetCell = startCell + directionOffsets[team] * 2;
            targetPiece = Board.Square[targetCell];
            if (targetPiece == 0) moves.Add(new Move(startCell,targetCell));
        } 
    }
    private void GenerateKnightMoves(int startCell, int piece)
    {
        foreach (Move move in knightMoves[startCell])
        {
            int targetPiece = Board.Square[move.TargetSquare];
            if (Piece.IsColour(targetPiece,Piece.GetColour(piece))) continue;
            moves.Add(move);
        }
    }
    public static int GetRank(int startCell)
    {
        return (startCell/8)+1;
    }
    public static string CellToString(int cellID)
    {
        char[] col = new char[]{'a','b','c','d','e','f','g','h'};
        char[] row = new char[]{'1','2','3','4','5','6','7','8'};
        int x = cellID%8 , y = cellID/8;
        return ""+col[x]+row[y];
    }
    // Chess classes 
    public class Piece
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
        public static int GetOpponentColour(int piece)
        {
            return GetColour(piece ^ 0b_11000);
        }
    }
    public static class Board
    {
        public static int[] Square = new int[64];
        public static int ColourToMove = 8;
        private static bool[] castling = new bool[4]{true,true,true,true}; // {White Kingside,White Queenside,Black Kingside,Black Queenside}
        private static int enpassant = -1; // the square on which an En Passant Move is possible. (Behind the pawn that moved 2 squares.) 
        private static int halfmove = 0;
        private static int fullmove = 1;
        public static  bool gameOver = false;
        public static readonly string startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public static void ResetBoard()
        {
            Square = new int[64];
            ColourToMove = 8;
            castling = new bool[4]{true,true,true,true};
            enpassant = -1; 
            halfmove = 0;
            fullmove = 1;
            gameOver = false;
        }
        public static void MakeMove(Move move)
        {
            int pieceToMove = Square[move.StartSquare];
            Square[move.StartSquare] = 0;
            Square[move.TargetSquare] = pieceToMove;
            ColourToMove ^= 0b_11000;
        }
        public static void FenToBoard(string fenstring)
        {
            string[] fenFields = fenstring.Split(" ");
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
        }
        public static string BoardToFen()
        {
            string fen = "";
            // Position
            char[] typeStr = new char[]{' ','k','p','n','b','r','q'};
            int empty = 0;
            for (int i = 56;i>=0;)
            {
                int piece = Board.Square[i];
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
            fen += ColourToMove == 8 ? " w " : " b ";
            // Castling 
            string cast = "";
            cast += castling[0] ? "K" : "";
            cast += castling[1] ? "Q" : "";
            cast += castling[2] ? "k" : "";
            cast += castling[3] ? "q" : "";
            fen += (cast.Length != 0) ? cast + " " : "- ";
            // En Passant
            fen += (enpassant != -1) ? CellToString(enpassant) + " " : "- ";
            // Halfmove
            fen += halfmove.ToString() + " ";
            // Fullmove
            fen += fullmove.ToString();
            return fen;
        }
    }
    public struct Move
    {
        public readonly int StartSquare;
        public readonly int TargetSquare;
        public Move(int start,int target)
        {
            StartSquare = start;
            TargetSquare = target;
        }
        public static bool operator ==(Move mv1,Move mv2)
        {
            return (mv1.StartSquare == mv2.StartSquare) && (mv1.TargetSquare == mv2.TargetSquare);
        }
        public static bool operator !=(Move mv1,Move mv2)
        {
            return (mv1.StartSquare != mv2.StartSquare) || (mv1.TargetSquare != mv2.TargetSquare);
        }
        public override bool Equals(object obj)
        {
            return obj is Move other && this == other;
        }
        public override int GetHashCode()
        {
            return (StartSquare << 6) ^ TargetSquare;
        }
    }
}
