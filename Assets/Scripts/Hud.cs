using UnityEngine;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
    public int shotCount;
    public int shotsForPar;
    public Text shotCountText;
    
    public void SetNewPaarCount(int paar)
    {
        shotsForPar = paar;
        ResetShots();
    }

    public void AddShot()
    {
        shotCount++;
        UpdateText();
    }

    public void ResetShots()
    {
        shotCount = 0;
        UpdateText();
    }
    
    private void UpdateText()
    {
        shotCountText.text = $"Shot: {shotCount} / {shotsForPar} for Par";
    }
}
