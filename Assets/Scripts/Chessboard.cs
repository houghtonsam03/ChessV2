using System.Data;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public class Chessboard : MonoBehaviour, IPointerDownHandler , IPointerUpHandler , IDragHandler
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
    private int selectedID;

    private struct Tile
    {
        public int x;
        public int y;
        public GameObject cell;
        public GameObject piece;
    }

    void Start()
    {
        TileGrid = this.transform.GetChild(0).gameObject;
        pieces = this.transform.GetChild(1).gameObject;
        CreateBoard();
        setState(ChessEngine.startingFen);
    }
    public void setEngine(ChessEngine eng)
    {
        engine = eng;
    } 

    void OnValidate()
    {
        colorTiles();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // This runs whenever the tile is clicked
        // 1. Get the mouse position in World coordinates
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);
        int x = Mathf.FloorToInt(localPos.x + 4f); int y = Mathf.FloorToInt(localPos.y + 4f);

        if (x < 0 || x > 7 || y < 0 || y > 7) return;
        selectedID = CellToID(x,y);
        if (tiles[selectedID].piece == null) selectedID = -1;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (selectedID < 0) return;
        // This runs whenever the tile is clicked
        // 1. Get the mouse position in World coordinates
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);
        int x = Mathf.FloorToInt(localPos.x + 4f); int y = Mathf.FloorToInt(localPos.y + 4f);

        if (x < 0 || x > 7 || y < 0 || y > 7 || CellToID(x,y) == selectedID)  {
            tiles[selectedID].piece.transform.position = CellToWorld(tiles[selectedID].x,tiles[selectedID].y);
            selectedID = -1;
            return;
        }
        bool legal = engine.Move(CellToID(tiles[selectedID].x,tiles[selectedID].y),CellToID(x,y));
        if (legal)
        {
            tiles[selectedID].piece.transform.position = CellToWorld(x,y);
            Destroy(tiles[CellToID(x,y)].piece);
            tiles[CellToID(x,y)].piece = tiles[selectedID].piece;
            tiles[selectedID].piece = null;

        }
        else tiles[selectedID].piece.transform.position = CellToWorld(tiles[selectedID].x,tiles[selectedID].y);
        selectedID = -1;
        
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (selectedID < 0) return;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = -1;
        tiles[selectedID].piece.transform.position = mouseWorldPos;
    }

    public void setState(string state)
    {
        string pos = state.Split(" ")[0];
        string[] rows = pos.Split("/");
        int x, y = 8;
        foreach (string row in rows)
        {
            x = 1;
            foreach (char letter in row)
            {
                if (char.IsDigit(letter))
                {
                    x += (int)char.GetNumericValue(letter);
                }
                else
                {
                    spawnPiece(letter,x-1,y-1);
                    x++;
                }
            }
            y--;
        }
    }
    private void spawnPiece(char letter,int x,int y)
    {
        string prefab = "";
        bool white = char.IsUpper(letter);
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
    private void cleanBoard()
    {
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
    private Vector3 CellToWorld(int x, int y)
    {
        // x,y in (0-7)
        return new Vector3(x-3.5f,y-3.5f,0);
    }
    private int CellToID(int x, int y)
    {
        return 8*y+x;
    }
}
