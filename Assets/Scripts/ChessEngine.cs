using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using Unity.VisualScripting;
using UnityEngine;

public class ChessEngine : MonoBehaviour
{
    public bool graphics;
    public float timeDelay = 0.1f;
    public enum Player1Side {White,Black,Random};
    public Player1Side PlayerOneSide;
    public ChessAgent agent1;
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
    private Board board;

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
    
    static ChessEngine()
    {
        PrecomputeMoveData();
    }
    void Start()
    {
        // Initial gameState
        gameState = Board.startingFen;
        board = new Board();
        board.setPos(gameState);
        moves = GenerateMoves(board);

        // Set Side
        if (PlayerOneSide == Player1Side.White) player1white = true;
        if (PlayerOneSide == Player1Side.Black) player1white = false;
        if (PlayerOneSide == Player1Side.Random) player1white = UnityEngine.Random.value < 0.5f;
        
        // Start agents
        agents = new ChessAgent[2]{agent1,agent2};
        if (agent1 == null) agents[0] = null;
        else {agents[0] = Instantiate(agent1); agents[0].StartAgent(player1white);}
        if (agent2 == null) agents[1] = null;
        else {agents[1] = Instantiate(agent2); agents[1].StartAgent(!player1white);}

        // Spawn chessboard and PlayerListener if we want graphics
        if (graphics || agents[0] == null || agents[1] == null)
        {
            GameObject prefab = Resources.Load<GameObject>("ChessboardPrefab");
            GameObject bObject = Instantiate(prefab,Vector3.zero,Quaternion.identity);
            boardObject = bObject.GetComponent<Chessboard>();
            boardObject.Setup(this);
            boardObject.readBoard(board);
            playerListener = boardObject.AddComponent<PlayerListener>();
            int turnIndex = (player1white == (board.colourToMove == 8)) ? 0 : 1;
            bool[] human = new bool[]{agents[turnIndex]==null,agents[turnIndex^1]==null};
            playerListener.Setup(this,boardObject,human);
        }
    }

    void Update()
    {
        if (!board.gameOver)
        {   
            // Check for GameOver
            if (moves.Count == 0)
            {
                Debug.Log("GAMEOVER");
                board.gameOver = true;
                playerListener.gameOver = true;
                if (graphics || agents[0] == null || agents[1] == null) boardObject.DrawGameOver(board.FindKing(Piece.GetOpponentColour(board.colourToMove)));
            }

            // Time Delay
            timer += Time.deltaTime;
            if (timer < timeDelay) return;
            
            // Find the index of the player whose turn it is.
            int turnIndex = (player1white == (board.colourToMove == 8)) ? 0 : 1;
            if (agents[turnIndex] == null) // Human
            {
                // We wait for playerListener to make move.
                return;
            }

            else // AI Agent
            {
                // Main AI Loop
                Move move = agents[turnIndex].GetMove(board);
                MakeMove(move);
            }
        }

        // Debugging for undo move
        // timer += Time.deltaTime;
        // if (timer < timeDelay) return;
        // board.UndoMove();
        // // Update engine
        // if (graphics || agents[0] == null || agents[1] == null) boardObject.setState(gameState);
        // UpdateState();
        // moves = GenerateMoves(board);
        // playerListener.turn ^= 1;
    }
    public bool hasPiece(int cell)
    {
        return board.Square[cell] != 0;
    }
    public bool hasPiece(int cell,bool moveColour)
    {
        // Chess Logic
        return (board.Square[cell] & board.colourToMove) != 0;
    }
    public void MakeMove(Move move)
    {
        if (IsLegalMove(move))
        {
            // Chess Logic
            board.MakeMove(move);

            // Update engine
            if (graphics || agents[0] == null || agents[1] == null) boardObject.Move(move.StartSquare,move.TargetSquare);
            UpdateState();
            moves = GenerateMoves(board);
            playerListener.turn ^= 1;

            // Time delay logic
            timer = 0;
        }
        // Debug Logic
        else
        {
            string s = "";
            if (board.colourToMove == 8) s += "white";
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
        gameState = Board.BoardToFen(board);
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
    public void DebugUndo()
    {
        // Debugging for undo move
        board.UndoMove();
        UpdateState();
        if (graphics || agents[0] == null || agents[1] == null) boardObject.setState(gameState);
        moves = GenerateMoves(board);
        playerListener.turn ^= 1;
    }
    public static List<Move> GenerateMoves(Board b)
    {
        List<Move> mv = GeneratePseudoLegalMoves(b);
        SolvePseudoMoves(b,mv);
        return mv;
    }
    private static List<Move> GeneratePseudoLegalMoves(Board b)
    {
        List<Move> mv = new List<Move>();
        for (int start=0;start<64;start++)
        {
            int piece = b.Square[start];
            if (Piece.IsColour(piece,b.colourToMove))
            {
                if (Piece.IsSlidingPiece(piece))
                {
                    GenerateSlidingMoves(b,mv,start,piece);
                }
                else if (Piece.IsType(piece,Piece.King))
                {
                    GenerateKingMoves(b,mv,start,piece);
                }
                else if (Piece.IsType(piece,Piece.Pawn))
                {
                    GeneratePawnMoves(b,mv,start,piece);
                }
                else if (Piece.IsType(piece,Piece.Knight))
                {
                    GenerateKnightMoves(b,mv,start,piece);
                }
            }
        }
        return mv;
    }
    private static void GenerateSlidingMoves(Board b,List<Move> mv,int startCell, int piece)
    {
        int startDirIndex = Piece.IsType(piece,Piece.Bishop) ? 4 : 0;
        int endDirIndex = Piece.IsType(piece,Piece.Rook) ? 4 : 8;
        for (int directionIndex = startDirIndex; directionIndex<endDirIndex;directionIndex++)
        {
            for (int n=1;n <= NumSquaresToEdge[startCell][directionIndex];n++)
            {
                int targetCell = startCell + directionOffsets[directionIndex] * n;
                int pieceOnTarget = b.Square[targetCell];

                if (Piece.IsColour(pieceOnTarget,Piece.GetColour(piece))) // Same colour piece
                {
                    break;
                }
                mv.Add(new Move(startCell,targetCell));
                
                if (Piece.IsColour(pieceOnTarget,Piece.GetOpponentColour(piece)))
                {
                    break;
                }
            }
        }
    }
    private static void GenerateKingMoves(Board b,List<Move> mv,int startCell, int piece)
    {
        for (int i=0;i<8;i++)
        {   
            if (NumSquaresToEdge[startCell][i] == 0) continue;
            int targetCell = startCell + directionOffsets[i];
            int targetPiece = b.Square[targetCell];
            if (Piece.IsColour(targetPiece,Piece.GetColour(piece))) continue;
            mv.Add(new Move(startCell,targetCell));
        }
    }
    private static void GeneratePawnMoves(Board b,List<Move> mv,int startCell, int piece)
    {
        // En Passant logic here
        

        // In case pawn can't move further (pre-promotion code)
        if ((GetRank(startCell) == 8 && Piece.IsColour(piece,Piece.white)) || (GetRank(startCell) == 1 && Piece.IsColour(piece,Piece.black))) return;

        // Check diagonal capture and En Passant
        int team = (Piece.GetColour(piece)/8)-1;

        int[] captureDirIndex = new int[]{4+team,6+team}; // Index for diagonal movements.
        foreach (int dirIndex in captureDirIndex)
        {
            if (NumSquaresToEdge[startCell][dirIndex] == 0) continue;
            int captureCell = startCell + directionOffsets[dirIndex];
            int capturePiece = b.Square[captureCell];
            if ((!Piece.IsColour(capturePiece,Piece.GetColour(piece)) && capturePiece != 0) || b.enpassant == captureCell) mv.Add(new Move(startCell,captureCell));
        }

        // Check pawn move 1 step.
        int targetCell = startCell + directionOffsets[team];
        int targetPiece = b.Square[targetCell];
        if (targetPiece != 0) return;
        mv.Add(new Move(startCell,targetCell));

        // Check pawn move 2 step.
        if ((GetRank(startCell) == 2 && Piece.IsColour(piece,Piece.white)) || (GetRank(startCell) == 7 && Piece.IsColour(piece,Piece.black)))
        {
            targetCell = startCell + directionOffsets[team] * 2;
            targetPiece = b.Square[targetCell];
            if (targetPiece == 0) mv.Add(new Move(startCell,targetCell));
        } 
    }
    private static void GenerateKnightMoves(Board b,List<Move> mv,int startCell, int piece)
    {
        foreach (Move move in knightMoves[startCell])
        {
            int targetPiece = b.Square[move.TargetSquare];
            if (Piece.IsColour(targetPiece,Piece.GetColour(piece))) continue;
            mv.Add(move);
        }
    }
    private static void SolvePseudoMoves(Board b,List<Move> mvs)
    {
        for(int i = mvs.Count-1;i >= 0;i--)
        {
            Move move = mvs[i];
            bool canCaptureKing = false;
            int kingColour = b.colourToMove;

            b.MakeMove(move);
            List<Move> newMoves = GeneratePseudoLegalMoves(b);
            int kingPos = b.FindKing(kingColour);

            foreach (Move newMove in newMoves)
            {
                if (newMove.TargetSquare != kingPos) continue;
                canCaptureKing = true;
                break;
            }
            if (canCaptureKing) mvs.Remove(move);
            b.UndoMove();
        }
    }
    public static int GetRank(int startSquare)
    {
        return (startSquare/8)+1;
    }
    public static int GetFile(int startSquare)
    {
        return (startSquare%8)+1;
    }
    public static string CellToString(int cellID)
    {
        char[] col = new char[]{'a','b','c','d','e','f','g','h'};
        char[] row = new char[]{'1','2','3','4','5','6','7','8'};
        int x = cellID%8 , y = cellID/8;
        return ""+col[x]+row[y];
    }
    // Chess classes 
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
        public static int GetOpponentColour(int piece)
        {
            return GetColour(piece ^ 0b_11000);
        }
    }
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
        public List<LastMove> lastMoves;
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
        }
        public void MakeMove(Move move)
        {
            // Record Move
            LastMove lm = new LastMove(move,Square[move.TargetSquare],castling,enpassant,halfmove,fullmove);
            lastMoves.Add(lm);

            // Make Move
            int movingPiece = Square[move.StartSquare];
            Square[move.StartSquare] = 0;
            Square[move.TargetSquare] = movingPiece;
            colourToMove ^= 0b_11000;
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
                int rookStartFile = (lastMove.Move.TargetSquare-lastMove.Move.StartSquare) > 0 ? 7 : 0;
                int rookTargetFile = (lastMove.Move.TargetSquare-lastMove.Move.StartSquare) > 0 ? 5 : 3;
                int rookRank = GetRank(lastMove.Move.StartSquare);
                int rookStartSquare = rookRank*8+rookStartFile;
                int rookTargetSquare = rookRank*8+rookTargetFile;
                Square[rookStartSquare] = Square[rookTargetSquare];
                Square[rookTargetSquare] = Piece.None;

                
            }
            else if (lastMove.Move.enpassant)
            {
                int yDir = Math.Sign(lastMove.Move.TargetSquare - lastMove.Move.StartSquare);
                int capturedSquare = lastMove.Move.TargetSquare - directionOffsets[0] * yDir;

                // Revert move
                Square[lastMove.Move.StartSquare] = Square[lastMove.Move.TargetSquare];
                Square[lastMove.Move.TargetSquare] = Piece.None;
                Square[capturedSquare] = lastMove.CapturedPiece;
            }
            else if (lastMove.Move.promotionPiece != 0)
            {
                // Remove promoted and recreate pawn
                Square[lastMove.Move.StartSquare] = Piece.Pawn+Piece.GetColour(Square[lastMove.Move.TargetSquare]);
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
            colourToMove ^= 0b_11000;
            gameOver = false;

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
    }
    public struct Move
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
