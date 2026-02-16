using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;

public class ChessEngine : MonoBehaviour
{
    public const string startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
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
    private bool isWhiteTurn = true;
    private bool gameOver = false;
    private bool[] castling = new bool[4]{true,true,true,true}; // {White Kingside,White Queenside,Black Kingside,Black Queenside}
    private int enpassant = -1; // the square on which an En Passant Move is possible. (Behind the pawn that moved 2 squares.) 
    private int halfmove = 0;
    private int fullmove = 1;
    private Board pieceBoard;
    
    void Start()
    {
        // Initial gameState
        gameState = startingFen;

        // Set Side
        if (PlayerOneSide == Player1Side.White) player1white = true;
        if (PlayerOneSide == Player1Side.Black) player1white = false;
        if (PlayerOneSide == Player1Side.Random) player1white = Random.value < 0.5f;
        
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
            boardObject.setState(startingFen);
            PlayerListener playerListener = boardObject.AddComponent<PlayerListener>();
            playerListener.Setup(this,boardObject);
        }

        // Setup pieces.
        pieceBoard = Board.FenToBoard(gameState);

    }

    void Update()
    {
        if (!gameOver)
        {   
            // Time Delay
            timer += Time.deltaTime;
            if (timer < timeDelay) return;
            
            // Find the index of the player whose turn it is.
            int turnIndex = (player1white == isWhiteTurn) ? 0 : 1;
            if (agents[turnIndex] == null) // Human
            {
                // We wait for playerListener to make move.
                return;
            }

            else // AI Agent
            {
                // Main AI Loop
                agents[turnIndex].setState(gameState);
                int[] move = agents[turnIndex].getMove();
                Move(move[0],move[1]);

                // Time delay logic
                timer = 0;
            }   
        }
    }
    public bool hasPiece(int cell)
    {
        return (pieceBoard.Square[cell] & 0b_11111) != 0;
    }
    public bool hasPiece(int cell,bool white)
    {
        // Chess Logic
        int mask = white ? 8 : 16;
        bool has = (pieceBoard.Square[cell] & mask) != 0;
        return has;
    }
    public void Move(int start,int target)
    {
        if (IsLegalMove(start,target))
        {
            // Chess Logic
            int piece = pieceBoard.Square[start];
            pieceBoard.Square[start] = 0;
            pieceBoard.Square[target] = piece;
            isWhiteTurn = !isWhiteTurn;
            UpdateState();
            if (graphics || humanPlayer1 || humanPlayer2) boardObject.Move(start,target);
        }
        // Debug Logic
        else
        {
            string s = "";
            if (isWhiteTurn) s += "white";
            else s+= "black";
            Debug.Log($"Illegal move by {s} player.");
        }
    }
    public bool IsLegalMove(int start,int target)
    {
        // Chess Logic
        return true;
    }
    public bool IsWhiteTurn()
    {
        return isWhiteTurn;
    }
    private void UpdateState()
    {
        gameState = GenerateFen(pieceBoard,isWhiteTurn,castling,enpassant,halfmove,fullmove);
    }
    private static string GenerateFen(Board board,bool whiteTurn,bool[] castling,int enpassant,int halfmove,int fullmove)
    {
        string fen = "";
        // Position
        char[] typeStr = new char[]{' ','k','p','n','b','r','q'};
        int empty = 0;
        for (int i = 56;i>=0;)
        {
            int piece = board.Square[i];
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
        fen += whiteTurn ? " w " : " b ";
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
    }
    public class Board
    {
        public int[] Square;
        public Board()
        {
            Square = new int[64];
        }
        public static Board FenToBoard(string fenstring)
        {
            Board board = new Board();
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
                    board.Square[cell] = piece;
                    cell += 1;
                }
            }
            return board;
        }
    }
}
