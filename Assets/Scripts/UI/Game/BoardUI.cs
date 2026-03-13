using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private TextMeshProUGUI[] ranks;
    private TextMeshProUGUI[] files;
    private bool isFlipped;

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
        tiles = new Tile[64];
        ranks = new TextMeshProUGUI[8];
        files = new TextMeshProUGUI[8];
        this.transform.name = "Chessboard";
    }
    void OnValidate()
    {
        ColorTiles();
    }
    public void Setup(bool flipped)
    {
        CleanBoard();
        isFlipped = flipped;
        for (int x=0;x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                int cellID = 8*y+x;
                bool rankNotation = (!isFlipped && x==0) || (isFlipped && x==7);
                bool fileNotation = (!isFlipped && y==0) || (isFlipped && y==7);
                Vector3 position = IDToWorld(cellID,isFlipped);
                GameObject cell = SpawnTile(cellID,position,rankNotation,fileNotation);
                tiles[cellID] = new Tile{x = x,y = y,cell = cell,piece=null};
            }
        }
        int start = isFlipped ? 7 : 0;
        for (int i=0;i<8;i++)
        {
            files[i] = tiles[start*8+i].cell.transform.Find("File").Find("Text").GetComponent<TextMeshProUGUI>();
            ranks[i] = tiles[start+8*i].cell.transform.Find("Rank").Find("Text").GetComponent<TextMeshProUGUI>();
        }
        ColorTiles();
    }
    public GameObject SpawnTile(int cellID,Vector3 position,bool rank,bool file)
    {
        GameObject tilePrefab = Resources.Load<GameObject>("Tile");
        position += TileGrid.transform.position;
        GameObject tileObject = Instantiate(tilePrefab,position,Quaternion.identity);
        string tile = ChessGame.IDToString(cellID);
        if (rank)
        {
            GameObject rankPrefab = Resources.Load<GameObject>("Rank");
            GameObject rankObject = Instantiate(rankPrefab,position,Quaternion.identity);
            rankObject.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = tile[1].ToString();
            rankObject.transform.SetParent(tileObject.transform);
            rankObject.transform.name = "Rank";
        }
        if (file)
        {
            GameObject fileprefab = Resources.Load<GameObject>("File");
            GameObject fileObject = Instantiate(fileprefab,position,Quaternion.identity);
            fileObject.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = tile[0].ToString();
            fileObject.transform.SetParent(tileObject.transform);
            fileObject.transform.name = "File";
        }
        tileObject.transform.name = tile;
        tileObject.transform.parent = TileGrid.transform;
        return tileObject;
    }
    public void setState(string state)
    {
        CleanPieces();
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
                    SpawnPiece(letter,ChessGame.CellToID(x,y));
                    x++;
                }
            }
            y--;
        }
    }
    public void readBoard(Board b)
    {
        CleanPieces();
        char[] letters = new char[]{'K','P','N','B','R','Q','k','p','n','b','r','q'};
        for (int i=0;i<12;i++)
        {
            ulong bitboard = b.bitboards[i];
            char letter = letters[i];
            while (bitboard != 0)
            {
                int cell = Bitboard.PopLowestBit(ref bitboard);
                SpawnPiece(letter,cell);
            }
        }
    }
    public void UpdatePiece(int cellID,string piece)
    {
        if (piece == "") {
            if (tiles[cellID].piece != null) Destroy(tiles[cellID].piece);
            tiles[cellID].piece = null;
        }
        else
        {
            SpawnPiece(piece.ToCharArray()[0],cellID);
        }
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
            for (int cell=0;cell<64;cell++)
            {
                if (tiles[cell].piece == null) continue;
                tiles[cell].piece.transform.position = IDToWorld(cell,isFlipped);
            }

            return;
        }
        tiles[cellID].piece.transform.position = IDToWorld(cellID,isFlipped);
    }
    public void PaintMove(int cellID)
    {
        tiles[cellID].cell.GetComponent<SpriteRenderer>().color = moveColor;
    }
    public void Select(int selectID)
    {
        tiles[selectID].cell.GetComponent<SpriteRenderer>().color = highlightColor;
    }
    public void DrawGameOver(int winner,int whitePos,int blackPos)
    {
        ColorTiles();
        Color c1 = Color.pink; // Debug color
        Color c2 = Color.pink; // Debug color
        switch (winner)
        {
            case 0 : c1=Color.yellow; c2=Color.yellow; break;
            case 1 : c1=Color.green; c2=Color.red; break;
            case 2 : c1=Color.red; c2=Color.green; break;
        }
        tiles[whitePos].cell.GetComponent<SpriteRenderer>().color = c1;
        tiles[blackPos].cell.GetComponent<SpriteRenderer>().color = c2;
    }
    private void SpawnPiece(char letter,int cell)
    {
        
        string prefab = "";
        if (char.IsUpper(letter)) prefab += "White ";
        else prefab += "Black ";
        switch (char.ToLower(letter))
        {
            case 'k': prefab += "King"; break;
            case 'p': prefab += "Pawn"; break;
            case 'n': prefab += "Knight"; break;
            case 'b': prefab += "Bishop"; break;
            case 'r': prefab += "Rook"; break;
            default : prefab += "Queen"; break;
        }
        GameObject piecePrefab = Resources.Load<GameObject>(prefab);
        GameObject piece = Instantiate(piecePrefab,IDToWorld(cell,isFlipped)+pieces.transform.position,Quaternion.identity);
        piece.name = prefab;
        tiles[cell].piece = piece;
        piece.transform.parent = pieces.transform;

    }
    private void CleanBoard()
    {
        tiles = new Tile[64];
        foreach (Transform child in TileGrid.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in pieces.transform)
        {
            Destroy(child.gameObject);
        }
    }
    private void CleanPieces()
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
    public void ColorTiles()
    {
        this.transform.GetChild(2).GetComponent<SpriteRenderer>().color = backgroundColor;
        if (tiles == null) return;
        for (int x=0;x<8;x++)
        {
            for (int y=0;y<8;y++)
            {
                Color col = (x + y) % 2 == 0 ? color1 : color2;
                tiles[ChessGame.CellToID(x,y)].cell.GetComponent<SpriteRenderer>().color = col;
            }
        }
        for (int i=0;i<8;i++)
        {
            Color col = (!isFlipped && i % 2 == 0) || (isFlipped && i % 2 == 1) ? color2 : color1;
            ranks[i].color = col;
            files[i].color = col;
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
    public void DrawBitboard(ulong bitboard)
    {
        for (int i=0;i<64;i++)
        {
            Color col = Color.blue;
            if (Bitboard.HasBit(bitboard,i)) col = Color.red;
            tiles[i].cell.GetComponent<SpriteRenderer>().color = col;
        }
    }
    public static Vector3 CellToWorld(int x,int y)
    {
        // x,y in (0-7)
        return new Vector3(x-3.5f,y-3.5f,0);
    }
    public static Vector3 IDToWorld(int cellID, bool isFlipped)
    {
        int visualIndex = isFlipped ? (63-cellID) : cellID; 
        var (x, y) = ChessGame.IDToCell(visualIndex);
        return CellToWorld(x,y);
    }
    public static Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x + 4f) ,Mathf.FloorToInt(worldPos.y + 4f));
    }
}
