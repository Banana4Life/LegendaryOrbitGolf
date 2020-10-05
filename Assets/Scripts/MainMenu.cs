using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public Hud hud;
    
    public void PlayGame()
    {
        var world = FindObjectOfType<World>();
        world.PlaceBall();

        // Hide menu
        GameObject.Find("Menu").SetActive(false);
        // Show hud
        hud.gameObject.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
