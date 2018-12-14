using UnityEngine;
using UnityEngine.UI;

public class StatusEffectObject : MonoBehaviour
{
    public Text numTurns;

    public void UpdateSE(bool on, int turns)
    {
        if (numTurns != null && turns > 0)
        {
            numTurns.text = turns.ToString();
        }

        gameObject.SetActive(on);
    }
}
