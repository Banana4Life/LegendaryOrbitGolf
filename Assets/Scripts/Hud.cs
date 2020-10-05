using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
    public int shotCount;
    public int shotsForPar;
    private Text shotCountText;
    
    public void SetNewPaarCount(int paar)
    {
        shotCount = 0;
        shotsForPar = paar;
        UpdateText();
    }

    public void AddShot()
    {
        shotCount++;
        UpdateText();
    }
    
    private void UpdateText()
    {
        if (!shotCountText)
        {
            shotCountText = gameObject.GetComponentInChildren<Text>();
            shotCountText.text = "";
        }
        
        shotCountText.text = $"Shot: {shotCount} / {shotsForPar} for Par";
    }
}
