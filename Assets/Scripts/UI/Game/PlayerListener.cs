using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static ChessGame;

public class PlayerListener : MonoBehaviour, IPointerDownHandler , IPointerUpHandler , IDragHandler
{

    // Game Objects
    private ChessGame game;
    private BoardUI board;
    // Selection Logic
    private int selectedID = -1;
    private bool[] isHuman;
    private int promotionCell = -1;
    public int turn;
    public bool gameOver;
    public void Setup(ChessGame en,BoardUI bo,bool[] human)
    {
        game = en;
        board = bo;
        isHuman = new bool[2];
        isHuman[0] = human[0]; isHuman[1] = human[1];
        turn = 0;
    }
    public void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) ResetGame();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (gameOver || !isHuman[turn]) {
            selectedID = -1;
            return;
        }
        // This runs whenever mouse1 is pressed
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2Int cell = BoardUI.WorldToCell(mousePos);
        int cellID = BoardUI.CellToID(cell);
        if (IsOOB(mousePos) && selectedID >= 0) board.ResetPiecePos(selectedID);
        else if (promotionCell >= 0)
        {
            int promotionPiece = GetPromotionPiece(mousePos);
            if (game.IsLegalMove(selectedID,promotionCell,promotionPiece))
            {
                Move move = game.GetMove(selectedID,promotionCell,promotionPiece);
                game.MakeMove(move);
                board.RemovePromotionTile();
                UnSelect();
            }
            else UnSelect();
        }
        else if (cellID == selectedID) UnSelect();
        else if (selectedID == -1 && promotionCell == -1) Select(cellID,eventData.position);
        else if (IsPromotionMove(cellID) && game.HasLegalMove(selectedID,cellID))
        {
            SpawnPromotionUI(cellID);
            promotionCell = cellID;
            return;
        }
        // Move to square
        else if (game.HasLegalMove(selectedID,cellID))
        {
            Move move = game.GetMove(selectedID,cellID);
            game.MakeMove(move);
            UnSelect();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // This runs whenever mouse1 is released

        if (selectedID < 0 || gameOver || promotionCell != -1) return;  // If: No piece selected , Gameover , Promotion pending.

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2Int cell = BoardUI.WorldToCell(mousePos);
        int cellID = BoardUI.CellToID(cell);

        // If move is oob or no legal move -> reset the piece.
        if (IsOOB(mousePos)) board.ResetPiecePos(selectedID);
        else if (cellID == selectedID || !game.HasLegalMove(selectedID,cellID)) board.ResetPiecePos(selectedID);
        // Pick Promotion
        else if (IsPromotionMove(cellID) && game.HasLegalMove(selectedID,cellID))
        {
            SpawnPromotionUI(cellID);
            promotionCell = cellID;
            return;
        }
        // Move to square
        else if (game.HasLegalMove(selectedID,cellID))
        {
            Move move = game.GetMove(selectedID,cellID);
            game.MakeMove(move);
            UnSelect();
        }
        
    }
    public void OnDrag(PointerEventData eventData)
    {
        // Runs when pressed down cursor moves
        if (selectedID == -1) return;
        if (gameOver) {
            board.ResetPiecePos(selectedID);
            board.RemovePromotionTile();
            UnSelect();
        }
        else board.PieceFollowMousePos(selectedID,eventData.position);
    }
    public void Select(int ID,Vector3 pos)
    {
        if (!game.hasPiece(ID,true)) return;
        selectedID = ID;
        List<Move> moves = game.GetLegalMoves(ID);
        board.PaintMoves(ID,moves);
        board.PieceFollowMousePos(selectedID,pos);
    }
    public void UnSelect()
    {
        board.ResetPiecePos();
        board.RemovePromotionTile();
        selectedID = -1;
        promotionCell = -1;
        board.ColorTiles();
    }
    public void SpawnPromotionUI(int ID)
    {
        int colour = (BoardUI.IDToCell(ID).y == 7) ? Piece.white : Piece.black;
        promotionCell = ID;
        board.ResetPiecePos(selectedID);
        board.SpawnPromotionTile(ID,colour);

    }
    public int GetPromotionPiece(Vector3 mousePos)
    {
        if (-1 <= mousePos.x && mousePos.x <= 0 && 0 <= mousePos.y && mousePos.y <= 1) return Piece.Queen;
        if (0 <= mousePos.x && mousePos.x <= 1 && 0 <= mousePos.y && mousePos.y <= 1) return Piece.Rook;
        if (-1 <= mousePos.x && mousePos.x <= 0 && -1 <= mousePos.y && mousePos.y <= 0) return Piece.Knight;
        if (0 <= mousePos.x && mousePos.x <= 1 && -1 <= mousePos.y && mousePos.y <= 0) return Piece.Bishop;
        return 0;
    }
    public bool IsPromotionMove(int targetID)
    {
        bool lastRank = ChessGame.GetRank(targetID) == (turn^1)*7;
        return game.hasPiece(selectedID,true,Piece.Pawn) && lastRank;
    }
    public bool IsOOB(Vector3 pos)
    {
        Vector2Int cell = BoardUI.WorldToCell(pos);
        if (0 > cell.x || cell.x > 7 || 0 > cell.y || cell.y > 7) return true;
        return false;
    }
    public void DebugMethod()
    {
        Debug.Log("Debug");
        game.UndoMoves();
    }
    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
