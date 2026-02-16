using System.Data;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public class Chessboard : MonoBehaviour
{
    public Color color1;
    public Color color2;
    public Color background;
    public GameObject tilePrefab;
    private Tile[] tiles;
    private GameObject TileGrid;
    private GameObject pieces;
    private ChessEngine engine;
    // Selection logic

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

    void OnValidate()
    {
        colorTiles();
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
                    spawnPiece(letter,x,y);
                    x++;
                }
            }
            y--;
        }
    }
    public void Move(int start, int target)
    {
        GameObject piece = tiles[start].piece;
        tiles[start].piece = null;
        Destroy(tiles[target].piece);
        tiles[target].piece = piece;
        Vector2Int targetCell = IDToCell(target);
        piece.transform.position = CellToWorld(targetCell.x,targetCell.y);
    }
    public void PieceFollowMousePos(int cellID,Vector3 position)
    {
        if (cellID < 0) return;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(position);
        mouseWorldPos.z = -1;
        tiles[cellID].piece.transform.position = mouseWorldPos;
    }
    public void resetPiecePos(int cellID)
    {
        Vector2Int cell = IDToCell(cellID);
        tiles[cellID].piece.transform.position = CellToWorld(cell.x,cell.y);
    }
    private void spawnPiece(char letter,int x,int y)
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
        GameObject piece = Instantiate(piecePrefab,CellToWorld(x,y),Quaternion.identity);
        piece.name = prefab;
        tiles[CellToID(x,y)].piece = piece;
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
    private void colorTiles()
    {
        this.transform.GetChild(2).GetComponent<SpriteRenderer>().color = background;
        if (tiles == null) return;
        for (int x=0;x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                Color col = (x + y) % 2 == 0 ? color1 : color2;
                tiles[CellToID(x,y)].cell.GetComponent<SpriteRenderer>().color = col;
            }
        }
    }
    public static Vector3 CellToWorld(int x, int y)
    {
        // x,y in (0-7)
        return new Vector3(x-3.5f,y-3.5f,0);
    }
    public static int CellToID(int x, int y)
    {
        return 8*y+x;
    }
    public static Vector2Int IDToCell(int cellID)
    {
        return new Vector2Int(cellID%8,cellID/8);
    }
    public static Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x + 4f) ,Mathf.FloorToInt(worldPos.y + 4f));
    }
}
