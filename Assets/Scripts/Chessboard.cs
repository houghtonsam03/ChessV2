using System.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public class Chessboard : MonoBehaviour, IPointerClickHandler
{
    public Color color1;
    public Color color2;
    public GameObject tilePrefab;
    private GameObject[] tiles;
    private GameObject TileGrid;
    private GameObject pieces;
    private ChessEngine engine;

    // Selecting logic
    private Vector2Int select;
    void Start()
    {
        tiles = new GameObject[64];
        TileGrid = new GameObject("TileGrid"); TileGrid.transform.parent = this.transform;
        pieces = new GameObject("Pieces"); pieces.transform.parent = this.transform;
        select = new Vector2Int(-1,-1);
        CreateBoard();
        setState(ChessEngine.startingFen);
    }
    public void setEngine(ChessEngine eng)
    {
        engine = eng;
    } 

    void Update()
    {
        Color currCol1 = tiles[0].GetComponent<SpriteRenderer>().material.color;
        Color currCol2 = tiles[1].GetComponent<SpriteRenderer>().material.color;
        if (color1 != currCol1 || color2 != currCol2)
        {
            colorTiles();
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // This runs whenever the tile is clicked
        // 1. Get the mouse position in World coordinates
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);

        Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);

        int x = Mathf.FloorToInt(localPos.x + 4f);
        int y = Mathf.FloorToInt(localPos.y + 4f);

        if (x < 0 || x > 7 || y < 0 || y > 7) return;
        if (select.x >= 0)
        {
            pieces 
            engine.checkMove(x,y)
        }
        if (engine.IsOccupied(x,y))
        {
            string[] moves = engine.getAllPossibleMoves()
        }
    }

    public void setState(string state)
    {
        cleanBoard();
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
                    spawnPiece(letter,x,y);
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
        GameObject piece = Instantiate(piecePrefab,new Vector3(x-4.5f,y-4.5f,0),Quaternion.identity);
        piece.transform.parent = pieces.transform;

    } 
    private void cleanBoard()
    {
        for (int i = pieces.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(pieces.transform.GetChild(i).gameObject);
        }
    }
    private void CreateBoard()
    {
        for (int x=0;x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                Color col = (x + y) % 2 == 0 ? color1 : color2;
                GameObject tile = CreateTile(col,x,y);
                tiles[8*x+y] = tile;
            }
        }
        colorTiles();
    }
    private GameObject CreateTile(Color col,int x,int y)
    {
        GameObject tile = Instantiate(tilePrefab,new Vector3(x-3.5f,y-3.5f,0),Quaternion.identity,TileGrid.transform);
        tile.name = $"Tile ({x},{y})";
        return tile;
    }
    private void colorTiles()
    {
        for (int x=0;x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                Color col = (x + y) % 2 == 0 ? color1 : color2;
                tiles[8*x+y].GetComponent<SpriteRenderer>().material.color = col;
            }
        }
    }
}
