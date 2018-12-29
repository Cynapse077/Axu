using UnityEngine;
using UnityEngine.UI;

public class DisplayHelpMessages : MonoBehaviour
{
    Text text;
    float waitTime = 5.0f;

    void Start()
    {
        text = GetComponent<Text>();
        GetNewHelpMessage();
    }

    void Update()
    {
        waitTime -= Time.deltaTime;

        if (waitTime <= 0.0f)
        {
            GetNewHelpMessage();
            waitTime = 5.0f;
        }
    }

    void GetNewHelpMessage()
    {
        text.text = LocalizationManager.GetRandomHelpMessage();
    }
}
