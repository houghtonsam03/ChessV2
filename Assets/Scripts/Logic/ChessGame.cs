using System;
using System.Collections.Generic;
using Unity.Collections;
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

    // Timer variables;
    private float timeLimit;
    private float whiteTimer;
    private float blackTimer;
    private float delayTime;

    // Chess variables
    private string gameFen;
    private List<Move> moves;
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
        moves = MoveGenerator.GenerateMoves(board,board.colourToMove);

        // Set Side
        player1White = p1White;
        turnIndex = 0;
        
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
            evals[0] = agents[0].EvalPos(board);
        }
        if (blackAI)
        {
            agents[1] = player1White ? Agent2 : Agent1;
            agents[1] = Instantiate(agents[1]);
            agents[1].StartAgent(false);
            evals[1] = agents[1].EvalPos(board);
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
        gameFen = Board.startingFen;
        board.setPos(gameFen);
        moves = MoveGenerator.GenerateMoves(board,board.colourToMove);

        player1White = p1White;
        turnIndex = 0;
        endState = 0;

        evals = new float?[2];

        if (agents[0] != null)
        {
            agents[0] = player1White ? Agent1 : Agent2;
            agents[0] = Instantiate(agents[0]);
            agents[0].StartAgent(true);
            evals[0] = agents[0].EvalPos(board);
        }
        if (agents[1] != null)
        {
            agents[1] = player1White ? Agent2 : Agent1;
            agents[1] = Instantiate(agents[1]);
            agents[1].StartAgent(false);
            evals[1] = agents[1].EvalPos(board);
        }

        whiteTimer = timeLimit; blackTimer = timeLimit;
        delayTime = 0f;
        if (UI != null) UI.Rematch(board,agents,evals,timeLimit);
    }
    void Update()
    {
        if (board == null) return;
        if (!board.gameOver)
        {   
            if (UI != null)
            {
                UI.UpdateGraphics(whiteTimer,blackTimer,evals);
            }
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
                ChessAgent agent = agents[turnIndex];
                if (agent != null) // Human
                {
                    Move move = agent.GetMove(board);
                    MakeMove(move);
                }
            }
        }
        // Timer
        delayTime += Time.deltaTime;
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
            bool isCapture = move.enpassant || Bitboard.HasBit(board.bitboards[14],move.TargetSquare);
            board.MakeMove(move);
            bool isCheck = board.IsCheck(board.colourToMove);
            moves = MoveGenerator.GenerateMoves(board,board.colourToMove);

            turnIndex ^= 1;
            delayTime = 0f;

            endState = board.IsGameOver(whiteTimer,blackTimer);
            bool isCheckmate = endState != 0;
            for (int i=0;i<2;i++)
            {
                if (agents[i] == null) continue;
                evals[i] = agents[i].EvalPos(board);
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
        gameFen = Board.BoardToFen(board);
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
        if (UI != null) UI.UpdateGraphics(whiteTimer,blackTimer,evals);
        moves = MoveGenerator.GenerateMoves(board,board.colourToMove);
    }
    public void DebugBitboard()
    {
        n += 1;
        n %= 20;
        if (n == 0) UI.UpdateGraphics(whiteTimer,blackTimer,evals);
        else if (n <= 15) UI.DrawBitboard(board.bitboards[n-1]);
        else if (n == 16) UI.DrawBitboard(board.whiteAttacks);
        else if (n == 17) UI.DrawBitboard(board.blackAttacks);
        else if (n == 18) UI.DrawBitboard(board.whitePins);
        else if (n == 19) UI.DrawBitboard(board.blackPins);
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
