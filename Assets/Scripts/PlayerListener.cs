using System.Data;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public class PlayerListener : MonoBehaviour, IPointerDownHandler , IPointerUpHandler , IDragHandler
{

    
    private ChessEngine engine;
    private Chessboard board;
    private bool isWhiteTurn;
    // Selection logic
    private int selectedID;

    public void Setup(ChessEngine en,Chessboard bo)
    {
        engine = en;
        board = bo;
        isWhiteTurn = true;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // This runs whenever mouse1 is pressed
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2Int cell = Chessboard.WorldToCell(mousePos);

        if (cell.x < 0 || cell.x > 7 || cell.y < 0 || cell.y > 7) return;
        selectedID = Chessboard.CellToID(cell.x,cell.y);
        if (board.hasPiece(selectedID)) board.PieceFollowMousePos(selectedID,eventData.position); // Update to !engine.hasPiece(selectedID)
        else selectedID = -1;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // This runs whenever mouse1 is released

        if (selectedID < 0) return; // If not seleceted a piece

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2Int cell = Chessboard.WorldToCell(mousePos);

        // If move is oob or no move.
        if (cell.x < 0 || cell.x > 7 || cell.y < 0 || cell.y > 7 || Chessboard.CellToID(cell.x,cell.y) == selectedID)  {
            board.resetPiecePos(selectedID);
            selectedID = -1;
            return;
        }
        if (engine.isLegalMove(selectedID,Chessboard.CellToID(cell.x,cell.y)))
        {
            engine.Move(selectedID,Chessboard.CellToID(cell.x,cell.y));
        }
        else {
            board.resetPiecePos(selectedID);
        }
        selectedID = -1;
        
    }
    public void OnDrag(PointerEventData eventData)
    {
        // Runs when pressed down cursor moves
        board.PieceFollowMousePos(selectedID,eventData.position);
    }


}
