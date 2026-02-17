using System.Data;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using static ChessEngine;

public class PlayerListener : MonoBehaviour, IPointerDownHandler , IPointerUpHandler , IDragHandler
{

    // Game Objects
    private ChessEngine engine;
    private Chessboard board;
    // Selection Logic
    private int selectedID;
    public bool gameOver;

    public void Setup(ChessEngine en,Chessboard bo)
    {
        engine = en;
        board = bo;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (gameOver) {
            selectedID = -1;
            return;
        }
        // This runs whenever mouse1 is pressed
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2Int cell = Chessboard.WorldToCell(mousePos);


        if (cell.x < 0 || cell.x > 7 || cell.y < 0 || cell.y > 7) return;
        selectedID = Chessboard.CellToID(cell.x,cell.y);
        if (engine.hasPiece(selectedID,true)) {
            board.PieceFollowMousePos(selectedID,eventData.position); 
            board.PaintMoves(selectedID);
        }
        else selectedID = -1;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // This runs whenever mouse1 is released

        if (selectedID < 0) return; // If not seleceted a piece

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2Int cell = Chessboard.WorldToCell(mousePos);

        // Recolour the tiles
        board.ColorTiles();
        // If move is oob or no move.
        if (cell.x < 0 || cell.x > 7 || cell.y < 0 || cell.y > 7 || Chessboard.CellToID(cell.x,cell.y) == selectedID)  {
            board.resetPiecePos(selectedID);
            selectedID = -1;
            return;
        }
        Move move = new Move(selectedID,Chessboard.CellToID(cell.x,cell.y));
        if (engine.IsLegalMove(move))
        {
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
        board.PieceFollowMousePos(selectedID,eventData.position);
        board.PaintMoves(selectedID);
    }

}
