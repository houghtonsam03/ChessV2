using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UI;

public class ChessGame : MonoBehaviour
{
    public float MoveDelay;
    public ChessAgent Agent1;
    public ChessAgent Agent2;
    
    // System variables
    private bool player1White;
    private ChessAgent[] agents;
    private float?[] evals;
    private GameUI UI;
    private bool isAgentThinking;

    // Timer variables;
    private float timeLimit;
    private float whiteTimer;
    private float blackTimer;
    private float delayTime;

    // Chess variables
    private string gameFen;
    private Board board;
    private int endState;
    private int turnIndex;


    // Debugging variables
    private int n;
    
    public void Begin(bool p1White,bool player1AI,bool player2AI,float timeSeconds,bool graphics)
    {
        // Initial gameState
        gameFen = Board.startingFen;
        board = new Board();
        board.setPos(gameFen);

        // Set Side
        player1White = p1White;
        turnIndex = 0;
        isAgentThinking = false;
        
        // Start agents
        agents = new ChessAgent[2];
        evals = new float?[2];
        bool whiteAI = (p1White && player1AI) || (!p1White && player2AI);
        bool blackAI = (p1White && player2AI) || (!p1White && player1AI);
        if (whiteAI)
        {
            agents[0] = player1White ? Agent1 : Agent2;
            agents[0] = Instantiate(agents[0]);
            agents[0].StartAgent(true);
            GetAIEval(0);
        }
        if (blackAI)
        {
            agents[1] = player1White ? Agent2 : Agent1;
            agents[1] = Instantiate(agents[1]);
            agents[1].StartAgent(false);
            GetAIEval(1);
        }

        // Start Timers
        timeLimit = timeSeconds;
        whiteTimer = timeLimit; blackTimer = timeLimit;
        delayTime = 0f;

        // Spawn UI if we want graphics
        if (graphics || agents[0] == null || agents[1] == null)
        {
            UI = this.AddComponent<GameUI>();
            UI.Setup(this,board,player1White,agents,evals,timeLimit);
        }
        else MoveDelay = 0f;
    }
    public void Rematch(bool p1White)
    {
        board = new Board();
        board.setPos(Board.startingFen);

        player1White = p1White;
        turnIndex = 0;
        endState = 0;
        isAgentThinking = false;

        evals = new float?[2];

        if (agents[0] != null)
        {
            agents[0] = player1White ? Agent1 : Agent2;
            agents[0] = Instantiate(agents[0]);
            agents[0].StartAgent(true);
            GetAIEval(0);
        }
        if (agents[1] != null)
        {
            agents[1] = player1White ? Agent2 : Agent1;
            agents[1] = Instantiate(agents[1]);
            agents[1].StartAgent(false);
            GetAIEval(1);
        }

        whiteTimer = timeLimit; blackTimer = timeLimit;
        delayTime = 0f;
        if (UI != null) UI.Rematch(board,agents,evals,timeLimit);
    }
    void Update()
    {
        if (board == null || board.gameOver) return;
        // Update graphics
        if (UI != null) UI.UpdateGraphics(whiteTimer,blackTimer,evals);
        // Check Gameover
        if (whiteTimer < 0 || blackTimer < 0) endState = board.IsGameOver(whiteTimer,blackTimer); // Check timeout
        if (endState != 0)
        {
            board.gameOver = true;
            if (UI != null) UI.EndGame(endState,board.FindKing(Piece.white),board.FindKing(Piece.black));
            return;
        }
        // Main Game Loop
        if (delayTime >= MoveDelay)
        {
            // Update timers
            if (board.colourToMove == Piece.white) whiteTimer -= Time.deltaTime;
            else if (board.colourToMove == Piece.black) blackTimer -= Time.deltaTime;

            // Ask Agent for move
            if (!isAgentThinking)
            {
                if (agents[turnIndex] != null)
                {
                    GetAIMove(turnIndex);
                }   
            }
        }
        // Timer
        delayTime += Time.deltaTime;
    }
    async void GetAIMove(int agentIndex)
    {
        ChessAgent agent = agents[agentIndex];
        isAgentThinking = true;

        Board boardCopy = new Board(); 
        boardCopy.setPos(board);

        Move move = await Task.Run(() => {System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;return agent.GetMove(boardCopy);});
        MakeMove(move);

        isAgentThinking = false;
    }
    async void GetAIEval(int agentIndex)
    {
        ChessAgent agent = agents[agentIndex];

        Board boardCopy = new Board(); 
        boardCopy.setPos(board);

        evals[agentIndex] = await Task.Run(() => {System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.AboveNormal;return agent.GetEval(boardCopy);});
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
            // Debugging
            // Debug.Log(board);
            // Debug.Log(move);

            // Update Engine
            bool isWhite = board.colourToMove == Piece.white;
            bool isCapture = move.IsEnPassant() || Bitboard.HasBit(board.bitboards[14],move.To);
            board.MakeMove(move);
            bool isCheck = board.IsCheck(board.colourToMove);

            turnIndex ^= 1;
            delayTime = 0f;

            endState = board.IsGameOver(whiteTimer,blackTimer);
            bool isCheckmate = endState != 0;
            for (int i=0;i<2;i++)
            {
                if (agents[i] == null) continue;
                GetAIEval(i);
            }
            // Check for GameOver
            if (UI != null)
            {
                UI.EndTurn(move,isWhite,isCapture,isCheck,isCheckmate);
                UI.UpdateGraphics(whiteTimer,blackTimer,evals);
            }
        }
        // Debug Logic
        else
        {
            string s = "";
            if (board.colourToMove == 8) s += "white";
            else s+= "black";
            Debug.Log($"Illegal move {move} by {s} player.");
        }
    }
    public Move GetMove(int start, int target,int promotionPiece=0)
    {
        // Returns the move from the moves list that is equal to mv
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);
        for(int i=0;i<moveCount;i++)
        {
            Move move = moves[i];
            if (move.From == start && move.To == target)
            {
                if (move.IsPromotion())
                {
                    if (move.GetPromotionPiece() == promotionPiece) return move;
                    else continue;
                }
                return move;
            }
        }
        return Move.NullMove;
    }
    public bool IsLegalMove(Move move)
    {
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);
        for(int i=0;i<moveCount;i++)
        {
            Move mv = moves[i];
            if (move == mv) return true;
        }
        return false;
    }
    public bool IsLegalMove(int start, int target, int promotionPiece)
    {
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);
        for(int i=0;i<moveCount;i++)
        {
            Move move = moves[i];
            Move mv = new Move(start,target,Move.GetPromotionFlag(promotionPiece));
            if (move == mv) return true;
        }
        return false;
    }
    public bool HasLegalMove(int start, int target)
    {
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);
        for(int i=0;i<moveCount;i++)
        {
            Move move = moves[i];
            Move mv = new Move(start,target);
            if (move.From == mv.From && move.To == mv.To) return true;
        }
        return false;
    }
    private void UpdateState()
    {
        gameFen = Board.BoardToFen(board);
    }
    public List<Move> GetLegalMoves(int start)
    {
        List<Move> legalMoves = new List<Move>();
        Span<Move> moves = stackalloc Move[256];
        int moveCount = MoveGenerator.GenerateMoves(board,board.colourToMove,moves);
        for(int i=0;i<moveCount;i++)
        {
            Move move = moves[i];
            if (move.From == start) legalMoves.Add(move);
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
        board.UndoMove();
        UpdateState();
        if (UI != null) UI.UpdateGraphics(whiteTimer,blackTimer,evals);
    }
    public void DebugBitboard()
    {
        n += 1;
        n %= 16;
        if (n == 0) UI.UpdateGraphics(whiteTimer,blackTimer,evals);
        else if (n <= 15) UI.DrawBitboard(board.bitboards[n-1]);
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
    public static string StringState(int state)
    {
        string endstate;
        switch (state)
        {
            case 1: endstate = "White Win - Checkmate"; break;
            case 2: endstate = "White Win - Resign"; break;
            case 3: endstate = "White Win - Timeout"; break;
            case 4: endstate = "Black Win - Checkmate"; break;
            case 5: endstate = "Black Win - Resign"; break;
            case 6: endstate = "Black Win - Timeout"; break;
            case 7: endstate = "Draw - Stalemate"; break;
            case 8: endstate = "Draw - Insufficient Material"; break;
            case 9: endstate = "Draw | Fify-Move-Rule"; break;
            case 10: endstate = "Draw - Threefold Repetition"; break;
            case 11: endstate = "Draw - Agreement"; break;
            case 12: endstate = "Draw - Timeout & Insuff. Material"; break;
            default: endstate = "Not Gameover"; break;
        }
        return endstate;
    }
}
