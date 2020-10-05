using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        var world = FindObjectOfType<World>();
        world.PlaceBall();

        // Hide menu
        GameObject.Find("Menu").SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
