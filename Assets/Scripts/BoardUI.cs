using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using static ChessEngine;

public class BoardUI : MonoBehaviour
{
    public Color color1;
    public Color color2;
    public Color backgroundColor;
    public Color highlightColor;
    public Color moveColor;
    public GameObject tilePrefab;
    private Tile[] tiles;
    private GameObject TileGrid;
    private GameObject pieces;
    private GameObject promotionTile;
    private ChessEngine engine;

    private struct Tile
    {
        public int x;
        public int y;
        public GameObject cell;
        public GameObject piece;
    }

    void Awake()
    {
        TileGrid = this.transform.GetChild(0).gameObject;
        pieces = this.transform.GetChild(1).gameObject;
        CreateBoard();
        this.transform.name = "Chessboard";
    }
    public void Setup(ChessEngine en)
    {
        engine = en;
    }
    void OnValidate()
    {
        ColorTiles();
    }

    public void setState(string state)
    {
        CleanBoard();
        string pos = state.Split(" ")[0];
        string[] rows = pos.Split("/");
        int x, y = 7;
        foreach (string row in rows)
        {
            x = 0;
            foreach (char letter in row)
            {
                if (char.IsDigit(letter))
                {
                    x += (int)char.GetNumericValue(letter);
                }
                else
                {
                    spawnPiece(letter,new Vector2Int(x,y));
                    x++;
                }
            }
            y--;
        }
    }
    public void readBoard(Board b)
    {
        CleanBoard();
        for (int i=0;i<64;i++)
        {
            int piece = b.Square[i];
            if (piece == 0) continue;
            char s = ' ';
            if (Piece.GetType(piece) == 1) s = 'k';
            if (Piece.GetType(piece) == 2) s = 'p';
            if (Piece.GetType(piece) == 3) s = 'n';
            if (Piece.GetType(piece) == 4) s = 'b';
            if (Piece.GetType(piece) == 5) s = 'r';
            if (Piece.GetType(piece) == 6) s = 'q';
            if (Piece.GetColour(piece) == 8) s = char.ToUpper(s);
            Vector2Int cell = IDToCell(i);
            spawnPiece(s,cell);
        }
    }
    public void Move(int start, int target)
    {
        GameObject piece = tiles[start].piece;
        tiles[start].piece = null;
        Destroy(tiles[target].piece);
        tiles[target].piece = piece;
        Vector2Int targetCell = IDToCell(target);
        piece.transform.position = CellToWorld(targetCell);
    }
    public void PieceFollowMousePos(int cellID,Vector3 position)
    {
        if (cellID < 0) return;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(position);
        mouseWorldPos.z = -1;
        tiles[cellID].piece.transform.position = mouseWorldPos;
    }
    public void ResetPiecePos(int cellID = -1)
    {
        if (cellID == -1)
        {
            for (int x=0;x<8;x++)
            {
                for (int y=0;y<8;y++)
                {
                    Tile tile = tiles[CellToID(new Vector2Int(x,y))];
                    if (tile.piece == null) continue;
                    tile.piece.transform.position = CellToWorld(new Vector2Int(x,y));
                }
            }
            
            return;
        }
        Vector2Int cell = IDToCell(cellID);
        tiles[cellID].piece.transform.position = CellToWorld(cell);
    }
    public void PaintMoves(int startID,List<Move> moves)
    {
        Select(startID);
        foreach (Move move in moves)
        {
            tiles[move.TargetSquare].cell.GetComponent<SpriteRenderer>().color = moveColor;
        }
    }
    public void Select(int selectID)
    {
        tiles[selectID].cell.GetComponent<SpriteRenderer>().color = highlightColor;
    }
    public void DrawGameOver(int state,int whitePos,int blackPos)
    {
        ColorTiles();
        Color c1;
        Color c2;
        if (new int[]{1,2,3}.Contains(state)) {c1=Color.green; c2=Color.red;}
        else if (new int[]{4,5,6}.Contains(state)) {c1=Color.red; c2=Color.green;}
        else if (new int[]{7,8,9,10,11,12}.Contains(state)) {c1=Color.yellow; c2=Color.yellow;}
        else return;
        tiles[whitePos].cell.GetComponent<SpriteRenderer>().color = c1;
        tiles[blackPos].cell.GetComponent<SpriteRenderer>().color = c2;
        string endstate = "";
        if (state == 1) endstate += "Black Checkmated";
        if (state == 2) endstate += "Black Resigned";
        if (state == 3) endstate += "Black Timeout";
        if (state == 4) endstate += "White Checkmated";
        if (state == 5) endstate += "White Resigned";
        if (state == 6) endstate += "White Timeout";
        if (state == 7) endstate += "Stalemate";
        if (state == 8) endstate += "Insufficient Material";
        if (state == 9) endstate += "Fify-Move-Rule";
        if (state == 10) endstate += "Threefold Repetition";
        if (state == 11) endstate += "Draw Vote";
        if (state == 12) endstate += "Timeout";
        Debug.Log(endstate);
    }
    private void spawnPiece(char letter,Vector2Int cell)
    {
        string prefab = "";
        if (char.IsUpper(letter)) prefab += "White ";
        else prefab += "Black ";
        if (char.ToLower(letter) == 'p') prefab += "Pawn";
        if (char.ToLower(letter) == 'r') prefab += "Rook";
        if (char.ToLower(letter) == 'n') prefab += "Knight";
        if (char.ToLower(letter) == 'b') prefab += "Bishop";
        if (char.ToLower(letter) == 'q') prefab += "Queen";
        if (char.ToLower(letter) == 'k') prefab += "King";
        GameObject piecePrefab = Resources.Load<GameObject>(prefab);
        GameObject piece = Instantiate(piecePrefab,CellToWorld(cell),Quaternion.identity);
        piece.name = prefab;
        tiles[CellToID(cell)].piece = piece;
        piece.transform.parent = pieces.transform;

    }
    private void CleanBoard()
    {
        for (int i=0;i<tiles.Length;i++)
        {
            tiles[i].piece = null;
        }
        foreach (Transform child in pieces.transform)
        {
            Destroy(child.gameObject);
        }
    }
    private void CreateBoard()
    {
        tiles = new Tile[64];
        for (int i=0;i<64;i++)
        {
            GameObject c = TileGrid.transform.GetChild(i).gameObject;
            tiles[i] = CreateTile(c,i);
        }
    }
    private Tile CreateTile(GameObject ob,int i) 
    {
        Tile t = new Tile();
        t.cell = ob;
        t.piece = null;
        t.x = i % 8;
        t.y = i / 8;
        return t;
    }
    public void ColorTiles()
    {
        this.transform.GetChild(2).GetComponent<SpriteRenderer>().color = backgroundColor;
        if (tiles == null) return;
        for (int x=0;x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                Color col = (x + y) % 2 == 0 ? color1 : color2;
                tiles[CellToID(new Vector2Int(x,y))].cell.GetComponent<SpriteRenderer>().color = col;
            }
        }
    }
    public void SpawnPromotionTile(int cellID,int colour)
    {
        GameObject prefab = Resources.Load<GameObject>("PromotionTile");
        promotionTile = Instantiate(prefab,Vector3.zero,Quaternion.identity);
        promotionTile.name = "Promotiontile";
        promotionTile.transform.position += new Vector3(0,0,-2);
        promotionTile.transform.parent = this.transform;
        for (int i=0;i<promotionTile.transform.childCount;i++)
        {
            promotionTile.transform.GetChild(i).GetComponent<SpriteRenderer>().color = Color.white;
        }
        string col = Piece.IsColour(colour,Piece.white) ? "White " : "Black ";
        string[] names = new string[]{"Knight","Bishop","Queen","Rook"};
        for (int i=0;i<4;i++)
        {
            string name = col + names[i];
            GameObject piecePrefab = Resources.Load<GameObject>(name);
            int x = i%2;
            int y = i/2;
            GameObject piece = Instantiate(piecePrefab,Vector3.zero,Quaternion.identity);
            piece.name = names[i];
            piece.transform.parent = promotionTile.transform;
            piece.transform.localScale = Vector3.one;
            piece.transform.position = promotionTile.transform.position + new Vector3(x-0.5f,y-0.5f,0) + Vector3.back;
        }
    }
    public void RemovePromotionTile()
    {
        if (promotionTile != null) {
            Destroy(promotionTile);
            promotionTile = null;
        }
    }
    public static Vector3 CellToWorld(Vector2Int cell)
    {
        // x,y in (0-7)
        return new Vector3(cell.x-3.5f,cell.y-3.5f,0);
    }
    public static int CellToID(Vector2Int cell)
    {
        return 8*cell.y+cell.x;
    }
    public static Vector2Int IDToCell(int cellID)
    {
        return new Vector2Int(cellID%8,cellID/8);
    }
    public static Vector3 IDToWorld(int cellID)
    {
        return CellToWorld(IDToCell(cellID));
    }
    public static Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x + 4f) ,Mathf.FloorToInt(worldPos.y + 4f));
    }
}
