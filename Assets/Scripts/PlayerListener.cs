using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static ChessEngine;

public class PlayerListener : MonoBehaviour, IPointerDownHandler , IPointerUpHandler , IDragHandler
{

    // Game Objects
    private ChessEngine engine;
    private BoardUI board;
    // Selection Logic
    private int selectedID;
    public bool gameOver;
    public bool[] isHuman;
    public int turn;
    public void Setup(ChessEngine en,BoardUI bo,bool[] human)
    {
        engine = en;
        board = bo;
        isHuman = new bool[2];
        isHuman[0] = human[0]; isHuman[1] = human[1];
        turn = 0;
    }
    public void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) DebugMethod();
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

        if (cell.x < 0 || cell.x > 7 || cell.y < 0 || cell.y > 7) return;
        selectedID = BoardUI.CellToID(cell.x,cell.y);
        if (engine.hasPiece(selectedID,true)) {
            board.PieceFollowMousePos(selectedID,eventData.position);
            List<Move> moves = engine.GetLegalMoves(selectedID);
            board.PaintMoves(selectedID,moves);
        }
        else selectedID = -1;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // This runs whenever mouse1 is released

        if (selectedID < 0 || gameOver) return; // If not seleceted a piece

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2Int cell = BoardUI.WorldToCell(mousePos);
        int hoverID = BoardUI.CellToID(cell.x,cell.y);

        // Recolour the tiles
        board.ColorTiles();

        // If move is oob or no move.
        if (cell.x < 0 || cell.x > 7 || cell.y < 0 || cell.y > 7 || hoverID == selectedID)  {
            board.resetPiecePos(selectedID);
            selectedID = -1;
            return;
        }
        int promotionPiece = 0;
        if (IsPromotionMove(hoverID))
        {
            // Make player decide
            promotionPiece = Piece.Queen;
        }
        if (engine.HasLegalMove(selectedID,hoverID,promotionPiece))
        {
            Move move = engine.GetMove(selectedID,hoverID,promotionPiece);
            engine.MakeMove(move);
        }
        else {
            board.resetPiecePos(selectedID);
        }
        selectedID = -1;
        
    }
    public void OnDrag(PointerEventData eventData)
    {
        // Runs when pressed down cursor moves
        if (selectedID == -1) return;
        if (gameOver) {
            board.resetPiecePos(selectedID);
            selectedID = -1;
        }
        else board.PieceFollowMousePos(selectedID,eventData.position);
    }
    public bool IsPromotionMove(int targetID)
    {
        bool lastRank = ChessEngine.GetRank(targetID) == (turn^1)*7;
        return engine.hasPiece(selectedID,true,Piece.Pawn) && lastRank;
    }
    public void DebugMethod()
    {
        Debug.Log("Debug");
        engine.UndoMoves();
    }

}
