using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChessGame : MonoBehaviour
{
    public bool Graphics;
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

    // Timer variables;
    private float playerTimer;
    private float whiteTimer;
    private float blackTimer;
    // Chess variables
    private string gameState;
    private List<Move> moves;
    private Board board;
    private int endState;

    // Movement variables
    public static readonly int[] directionOffsets = {8,-8,-1,1,7,-9,9,-7};
    public static readonly int[][] NumSquaresToEdge = new int[64][];
    public static readonly List<Move>[] knightMoves = new List<Move>[64];
    private static void PrecomputeMoveData()
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
                    knightMoves[cellID].Add(new Move(Piece.Knight,cellID,move[1]*8+move[0]));
                }
            }
        }
    }
    // Zobrist values
    public static readonly ulong[] ZobristKeys = new ulong[12*64+1+4+8];
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
    
    static ChessGame()
    {
        PrecomputeMoveData();
        PrecomputeZobristData();
    }
    void Start()
    {
        // Initial gameState
        gameState = Board.startingFen;
        board = new Board();
        board.setPos(gameState);
        moves = GenerateMoves(board,board.colourToMove);

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
        TimeLimit *= 60;
        whiteTimer = TimeLimit; blackTimer = TimeLimit;
        playerTimer = Time.realtimeSinceStartup;

        // Spawn BoardUI and PlayerListener if we want graphics
        if (Graphics || agents[0] == null || agents[1] == null)
        {
            GameObject prefab = Resources.Load<GameObject>("ChessboardPrefab");
            GameObject boardObject = Instantiate(prefab,Vector3.zero,Quaternion.identity);
            boardObject.transform.parent = this.transform;
            boardUI = boardObject.GetComponent<BoardUI>();
            boardUI.readBoard(board);
            playerListener = boardUI.AddComponent<PlayerListener>();
            int turnIndex = Piece.IsColour(player1Colour,board.colourToMove) ? 0 : 1;
            bool[] human = new bool[]{agents[turnIndex]==null,agents[turnIndex^1]==null};
            playerListener.Setup(this,boardUI,human);
        }
    }
    public void Reset()
    {
    }
    void Update()
    {
        if (board == null) return;
        if (!board.gameOver)
        {   
            if (Piece.IsColour(board.colourToMove,Piece.white)) whiteTimer -= Time.realtimeSinceStartup - playerTimer;
            else blackTimer -= Time.realtimeSinceStartup - playerTimer;
            playerTimer = Time.realtimeSinceStartup;
            // Debug.Log($"WhiteTime: {whiteTimer} | BlackTime: {blackTimer}");
            
            // Find the index of the player whose turn it is.
            int turnIndex = Piece.IsColour(player1Colour,board.colourToMove) ? 0 : 1;
            ChessAgent agent = agents[turnIndex];
            if (agent == null) // Human
            {
                // We wait for playerListener to make move.
                return;
            }

            else // AI Agent
            {
                // Main AI Loop
                Move move = agent.GetMove(board);
                MakeMove(move);
            }

            // Check for GameOver
            int state = board.IsGameOver(whiteTimer,blackTimer);
            if (state != 0)
            {
                board.gameOver = true;
                if (Graphics || agents[0] == null || agents[1] == null) {
                    boardUI.DrawGameOver(state,board.FindKing(player1Colour),board.FindKing(Piece.GetOpponentColour(player1Colour)));
                    playerListener.gameOver = true;
                }
                endState = state;
                return;
            }
        }
    }
    public bool hasPiece(int cell,bool moveColour)
    {
        // Chess Logic
        if (moveColour) return Piece.IsColour(board.Square[cell],board.colourToMove);
        else return !Piece.IsType(board.Square[cell],Piece.None);
    }
    public bool hasPiece(int cell,bool moveColour,int pieceType)
    {
        // Chess Logic
        if (moveColour) return Piece.IsColour(board.Square[cell],board.colourToMove) && Piece.IsType(board.Square[cell],pieceType);
        else return Piece.IsType(board.Square[cell],pieceType);
    }
    public void MakeMove(Move move)
    {
        if (IsLegalMove(move))
        {
            // Update Engine
            board.MakeMove(move);
            moves = GenerateMoves(board,board.colourToMove);
            // Update dependencies
            UpdateState();
            if (Graphics || agents[0] == null || agents[1] == null) {
                boardUI.setState(gameState);
                playerListener.turn ^= 1;
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
            Move mv = new Move(board.Square[start],start,target,promotionPiece);
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
        return moves.Contains(new Move(board.Square[start],start,target,promotionPiece));
    }
    public bool HasLegalMove(int start, int target)
    {
        foreach (Move move in moves)
        {
            Move mv = new Move(board.Square[start],start,target);
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
        moves = GenerateMoves(board,board.colourToMove);
        board.UndoMove();
        moves = GenerateMoves(board,board.colourToMove);
        UpdateState();
        if (Graphics || agents[0] == null || agents[1] == null) boardUI.readBoard(board);
        moves = GenerateMoves(board,board.colourToMove);
    }
    public static List<Move> GenerateMoves(Board board,int colour=Piece.white+Piece.black, int type=0)
    {
        List<Move> allMoves = new List<Move>();
        for (int i=0;i<64;i++)
        {
            if (!Piece.IsColour(board.Square[i],colour)) continue;
            if (type == 0) // Don't care about type
            {
                List<Move> squareMoves = GenerateMove(board,i);
                SolvePseudoMoves(board,squareMoves);
                allMoves.AddRange(squareMoves);
            }
            else
            {
                if (!Piece.IsType(board.Square[i],type)) continue;
                List<Move> squareMoves = GenerateMove(board,i);
                SolvePseudoMoves(board,squareMoves);
                allMoves.AddRange(squareMoves);
            }

        }
        return allMoves;
    }
    public static List<Move> GenerateMove(Board board,int start,bool includeCastling = true)
    {
        int piece = board.Square[start];
        List<Move> mv = new List<Move>();
        if (Piece.IsSlidingPiece(piece))
        {
            GenerateSlidingMoves(board,mv,start,piece);
        }
        else if (Piece.IsType(piece,Piece.King))
        {
            GenerateKingMoves(board,mv,start,piece,includeCastling);
        }
        else if (Piece.IsType(piece,Piece.Pawn))
        {
            GeneratePawnMoves(board,mv,start,piece);
        }
        else if (Piece.IsType(piece,Piece.Knight))
        {
            GenerateKnightMoves(board,mv,start,piece);
        }
        return mv;
    }
    public static void GenerateSlidingMoves(Board board,List<Move> mv,int startCell, int piece)
    {
        int startDirIndex = Piece.IsType(piece,Piece.Bishop) ? 4 : 0;
        int endDirIndex = Piece.IsType(piece,Piece.Rook) ? 4 : 8;
        for (int directionIndex = startDirIndex; directionIndex<endDirIndex;directionIndex++)
        {
            for (int n=1;n <= NumSquaresToEdge[startCell][directionIndex];n++)
            {
                int targetCell = startCell + directionOffsets[directionIndex] * n;
                int pieceOnTarget = board.Square[targetCell];

                if (Piece.IsColour(pieceOnTarget,Piece.GetColour(piece))) // Same colour piece
                {
                    break;
                }
                mv.Add(new Move(piece,startCell,targetCell));
                
                if (Piece.IsColour(pieceOnTarget,Piece.GetOpponentColour(piece)))
                {
                    break;
                }
            }
        }
    }
    public static void GenerateKingMoves(Board board,List<Move> mv,int startCell, int piece,bool includeCastling)
    {
        for (int i=0;i<8;i++)
        {   
            if (NumSquaresToEdge[startCell][i] == 0) continue;
            int targetCell = startCell + directionOffsets[i];
            int targetPiece = board.Square[targetCell];
            if (Piece.IsColour(targetPiece,Piece.GetColour(piece))) continue;
            mv.Add(new Move(piece,startCell,targetCell));
        }
        if (!includeCastling) return;
        // Castling
        // If castling is allowed and squares between are and not under attack clear we can castle.
        if (board.IsCheck(Piece.GetColour(piece))) return;
        int teamOffset = Piece.IsColour(piece, Piece.white) ? 0 : 2;
        int rankOffset = Piece.IsColour(piece, Piece.white) ? 0 : 56;
        // Kingside
        if (board.castling[0+teamOffset])
        {
            if (!board.IsAttacked(rankOffset+5,Piece.GetOpponentColour(piece)) && Piece.IsType(board.Square[rankOffset+5],Piece.None) &&
                !board.IsAttacked(rankOffset+6,Piece.GetOpponentColour(piece)) && Piece.IsType(board.Square[rankOffset+6],Piece.None))
            {
                mv.Add(new Move(piece,startCell,rankOffset+6,Piece.None,true,false));
            }
        }
        // Queenside
        if (board.castling[1+teamOffset])
        {
            if (Piece.IsType(board.Square[rankOffset+1],Piece.None) &&
                !board.IsAttacked(rankOffset+2,Piece.GetOpponentColour(piece)) && Piece.IsType(board.Square[rankOffset+2],Piece.None) &&
                !board.IsAttacked(rankOffset+3,Piece.GetOpponentColour(piece)) && Piece.IsType(board.Square[rankOffset+3],Piece.None))
            {
                mv.Add(new Move(piece,startCell,rankOffset+2,Piece.None,true,false));
            }
        }
    }
    public static void GeneratePawnMoves(Board board,List<Move> mv,int startCell, int piece)
    {
        // Check diagonal capture and En Passant
        int team = (Piece.GetColour(piece)/8)-1;
        int lastRank = Piece.GetColour(piece) == Piece.white ? 7 : 0;

        int[] captureDirIndex = new int[]{4+team,6+team}; // Index for diagonal movements.
        foreach (int dirIndex in captureDirIndex)
        {
            if (NumSquaresToEdge[startCell][dirIndex] == 0) continue;
            int captureCell = startCell + directionOffsets[dirIndex];
            int capturePiece = board.Square[captureCell];
            if (Piece.IsColour(capturePiece,Piece.GetOpponentColour(piece)) && capturePiece != Piece.None)
            {
                if (GetRank(captureCell) == lastRank)
                {
                    mv.Add(new Move(piece,startCell,captureCell,Piece.Queen));
                    mv.Add(new Move(piece,startCell,captureCell,Piece.Rook));
                    mv.Add(new Move(piece,startCell,captureCell,Piece.Knight));
                    mv.Add(new Move(piece,startCell,captureCell,Piece.Bishop));
                }
                else mv.Add(new Move(piece,startCell,captureCell));
            }
            if (board.enpassant == captureCell) mv.Add(new Move(piece,startCell,captureCell,Piece.None,false,true));
        }

        // Check pawn move 1 step.
        int targetCell = startCell + directionOffsets[team];
        int targetPiece = board.Square[targetCell];
        if (targetPiece != 0) return;
        if (GetRank(targetCell) == lastRank)
        {
            mv.Add(new Move(piece,startCell,targetCell,Piece.Queen));
            mv.Add(new Move(piece,startCell,targetCell,Piece.Rook));
            mv.Add(new Move(piece,startCell,targetCell,Piece.Knight));
            mv.Add(new Move(Piece.Pawn,startCell,targetCell,Piece.Bishop));
        } 
        else mv.Add(new Move(piece,startCell,targetCell));

        // Check pawn move 2 step.
        if ((GetRank(startCell) == 1 && Piece.IsColour(piece,Piece.white)) || (GetRank(startCell) == 6 && Piece.IsColour(piece,Piece.black)))
        {
            targetCell = startCell + directionOffsets[team] * 2;
            targetPiece = board.Square[targetCell];
            if (targetPiece == 0) mv.Add(new Move(piece,startCell,targetCell));
        } 
    }
    public static void GenerateKnightMoves(Board board,List<Move> mv,int startCell, int piece)
    {
        foreach (Move move in knightMoves[startCell])
        {
            int targetPiece = board.Square[move.TargetSquare];
            if (Piece.IsColour(targetPiece,Piece.GetColour(piece))) continue;
            mv.Add(move);
        }
    }
    public static void SolvePseudoMoves(Board board,List<Move> mvs)
    {
        for(int i = mvs.Count-1;i >= 0;i--)
        {
            Move move = mvs[i];
            int kingColour = board.colourToMove;

            board.MakeMove(move);
            if (board.IsCheck(kingColour)) mvs.Remove(move);
            board.UndoMove();
        }
    }
    public static int GetRank(int startSquare)
    {
        return (startSquare/8);
    }
    public static int GetFile(int startSquare)
    {
        return (startSquare%8);
    }
    public static string CellToString(int cellID)
    {
        char[] col = new char[]{'a','b','c','d','e','f','g','h'};
        char[] row = new char[]{'1','2','3','4','5','6','7','8'};
        int x = cellID%8 , y = cellID/8;
        return ""+col[x]+row[y];
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
