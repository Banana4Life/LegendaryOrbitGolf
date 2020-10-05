using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
    public int shotCount;
    public int shotsForPar;
    public Text shotCountText;
    
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
        shotCountText.text = $"Shot: {shotCount} / {shotsForPar} for Par";
    }
}
