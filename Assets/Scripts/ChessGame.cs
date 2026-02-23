using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChessGame : MonoBehaviour
{
    public bool Graphics;
    public float MoveDelay;
    public ChessAgent Agent1;
    public ChessAgent Agent2;
    public enum Player1Side {White,Black,Random};
    public Player1Side PlayerOneSide;
    public float TimeLimit;
    

    // System variables
    private int player1Colour;
    private ChessAgent[] agents;
    private BoardUI boardUI;
    private PlayerListener playerListener;
    private bool UI = false;

    // Timer variables;
    private float playerTimer;
    private float whiteTimer;
    private float blackTimer;
    private float delayTime;

    // Chess variables
    private string gameState;
    private List<Move> moves;
    private Board board;
    private int endState;
    // Debugging variables
    private int n;
    
    void Start()
    {
        // Initial gameState
        gameState = Board.startingFen;
        board = new Board();
        board.setPos(gameState);
        moves = MoveGenerator.GenerateMoves(board,board.colourToMove);

        // Set Side
        if (PlayerOneSide == Player1Side.White) player1Colour = Piece.white;
        if (PlayerOneSide == Player1Side.Black) player1Colour = Piece.black;
        if (PlayerOneSide == Player1Side.Random) player1Colour = UnityEngine.Random.value < 0.5f ? Piece.white : Piece.black;
        
        // Start agents
        agents = new ChessAgent[2]{Agent1,Agent2};
        if (Agent1 == null) agents[0] = null;
        else {agents[0] = Instantiate(Agent1); agents[0].StartAgent(player1Colour);}
        if (Agent2 == null) agents[1] = null;
        else {agents[1] = Instantiate(Agent2); agents[1].StartAgent(Piece.GetOpponentColour(player1Colour));}

        // Start Timers
        TimeLimit *= 60;
        whiteTimer = TimeLimit; blackTimer = TimeLimit;
        playerTimer = Time.realtimeSinceStartup;
        delayTime = 0f;

        // Spawn BoardUI and PlayerListener if we want graphics
        if (Graphics || agents[0] == null || agents[1] == null)
        {
            UI  = true;
            GameObject prefab = Resources.Load<GameObject>("ChessboardPrefab");
            GameObject boardObject = Instantiate(prefab,Vector3.zero,Quaternion.identity);
            boardObject.transform.parent = this.transform;
            boardUI = boardObject.GetComponent<BoardUI>();
            boardUI.readBoard(board);
            playerListener = boardUI.GetComponent<PlayerListener>();
            int turnIndex = Piece.IsColour(player1Colour,board.colourToMove) ? 0 : 1;
            bool[] human = new bool[]{agents[turnIndex]==null,agents[turnIndex^1]==null};
            playerListener.Setup(this,boardUI,human);
        }
        else MoveDelay = 0f;
    }
    void Update()
    {
        if (board == null) return;
        if (!board.gameOver)
        {   
            // Check for GameOver
            int state = board.IsGameOver(whiteTimer,blackTimer);
            if (state != 0)
            {
                board.gameOver = true;
                if (UI) {
                    boardUI.DrawGameOver(state,board.FindKing(player1Colour),board.FindKing(Piece.GetOpponentColour(player1Colour)));
                    playerListener.EndGame();
                }
                endState = state;
                return;
            }
            if (delayTime < MoveDelay) {
                delayTime += Time.deltaTime;
                return;
            }

            if (Piece.IsColour(board.colourToMove,Piece.white)) whiteTimer -= Time.realtimeSinceStartup - playerTimer;
            else blackTimer -= Time.realtimeSinceStartup - playerTimer;
            playerTimer = Time.realtimeSinceStartup;
            
            // Find the index of the player whose turn it is.
            int turnIndex = Piece.IsColour(player1Colour,board.colourToMove) ? 0 : 1;
            ChessAgent agent = agents[turnIndex];
            if (agent == null) // Human
            {
                // We wait for playerListener to make move.
            }
            else // AI Agent
            {
                // Main AI Loop
                Move move = agent.GetMove(board);
                MakeMove(move);
            }
        }
    }
    public bool hasPiece(int cell,int moveColour=24,int pieceType=Piece.None)
    {
        if (pieceType == Piece.None)
        {
            bool white = (moveColour & Piece.white) != 0;
            bool black = (moveColour & Piece.black) != 0;
            if (white && Bitboard.HasBit(board.bitboards[12],cell)) return true;
            if (black && Bitboard.HasBit(board.bitboards[13],cell)) return true;
            return false;
        }
        int pieceMask = pieceType-1;
        if (Piece.IsColour(moveColour,Piece.white) && Bitboard.HasBit(board.bitboards[pieceMask],cell)) return true;
        if (Piece.IsColour(moveColour,Piece.black) && Bitboard.HasBit(board.bitboards[pieceMask+6],cell)) return true;
        return false;
    }
    public void MakeMove(Move move)
    {
        if (IsLegalMove(move))
        {
            // Update Engine
            board.MakeMove(move);
            moves = MoveGenerator.GenerateMoves(board,board.colourToMove);  // TO BE OPTIMISED
            // Update dependencies
            if (UI) {
                boardUI.setState(Board.BoardToFen(board));
                playerListener.EndTurn();
                delayTime = 0f;
            }
            // Debugging
            // Debug.Log(board);
            // Debug.Log(move);

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
    public Move GetMove(int start, int target,int promotionPiece=0)
    {
        // Returns the move from the moves list that is equal to mv
        foreach (Move move in moves)
        {
            Move mv = new Move(start,target,promotionPiece);
            if (move == mv) return move;
        }
        return null;
    }
    public bool IsLegalMove(Move move)
    {
        return moves.Contains(move);
    }
    public bool IsLegalMove(int start, int target, int promotionPiece)
    {
        return moves.Contains(new Move(start,target,promotionPiece));
    }
    public bool HasLegalMove(int start, int target)
    {
        foreach (Move move in moves)
        {
            Move mv = new Move(start,target);
            if (move.StartSquare == mv.StartSquare && move.TargetSquare == mv.TargetSquare) return true;
        }
        return false;
    }
    private void UpdateState()
    {
        gameState = Board.BoardToFen(board);
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
    public int GetEndState()
    {
        return endState;
    }
    public void UndoMoves()
    {
        // Debugging for undoing two moves
        board.UndoMove();
        moves = MoveGenerator.GenerateMoves(board,board.colourToMove);
        board.UndoMove();
        moves = MoveGenerator.GenerateMoves(board,board.colourToMove);
        UpdateState();
        if (UI) boardUI.readBoard(board);
        moves = MoveGenerator.GenerateMoves(board,board.colourToMove);
    }
    public void DebugBitboard()
    {
        n += 1;
        n %= 20;
        if (n==0) boardUI.ColorTiles();
        else if (n <= 15) boardUI.DrawBitboard(board.bitboards[n-1]);
        else if (n == 16) boardUI.DrawBitboard(board.whiteAttacks);
        else if (n == 17) boardUI.DrawBitboard(board.blackAttacks);
        else if (n == 18) boardUI.DrawBitboard(board.whitePins);
        else if (n == 19) boardUI.DrawBitboard(board.blackPins);
    }
    public static int GetRank(int startSquare)
    {
        return (startSquare/8);
    }
    public static int GetFile(int startSquare)
    {
        return (startSquare%8);
    }
    public static string IDToString(int cellID)
    {
        char[] col = new char[]{'a','b','c','d','e','f','g','h'};
        char[] row = new char[]{'1','2','3','4','5','6','7','8'};
        int x = cellID%8 , y = cellID/8;
        return ""+col[x]+row[y];
    }
    public static int CellToID(int x,int y)
    {
        return 8*y+x;
    }
    public static (int x, int y) IDToCell(int cellID)
    {
        return (cellID%8,cellID/8);
    }
    public static int StringToID(string square)
    {
        List<char> files = new List<char>();
        files.AddRange(new char[]{'a','b','c','d','e','f','g','h'});
        int y = files.IndexOf(square.ToCharArray()[0]) - 1;
        int x = square.ToCharArray()[1]-'0';
        return CellToID(x,y);
    }
    public static void PrintState(int state)
    {
        string endstate = "Not Gameover";
        if (state == 1) endstate = "White Win | Checkmate";
        if (state == 2) endstate = "White Win | Resign";
        if (state == 3) endstate = "White Win | Timeout";
        if (state == 4) endstate = "Black Win | Checkmate";
        if (state == 5) endstate = "Black Win | Resign";
        if (state == 6) endstate = "Black Win | Timeout";
        if (state == 7) endstate = "Draw | Stalemate";
        if (state == 8) endstate = "Draw | Insufficient Material";
        if (state == 9) endstate = "Draw | Fify-Move-Rule";
        if (state == 10) endstate = "Draw | Threefold Repetition";
        if (state == 11) endstate = "Draw | Agreement";
        if (state == 12) endstate = "Draw | Timeout";
        Debug.Log(endstate);
    }
}
