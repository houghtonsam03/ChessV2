using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameoverMenu : MonoBehaviour
{
    private GameUI UI;
    private ChessGame game;
    private bool p1White;
    public void Setup(GameUI ui,ChessGame g,bool p1W)
    {
        gameObject.SetActive(false);
        UI = ui;
        game = g;
        p1White = p1W;
    }
    public void Show(int state)
    {
        transform.Find("Panel").Find("Text").GetComponent<TextMeshProUGUI>().text = $"Game over\n"+ChessGame.StringState(state);
        gameObject.SetActive(true);
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }
    public void Rematch()
    {
        game.Rematch(p1White);
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}