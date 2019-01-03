using UnityEngine;
using UnityEngine.UI;

public class DisplayHelpMessages : MonoBehaviour
{
    Text text;
    float waitTime = 8.0f;

    void Start()
    {
        text = GetComponent<Text>();
        GetNewHelpMessage();
    }

    void Update()
    {
        waitTime -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || waitTime <= 0.0f)
        {
            GetNewHelpMessage();
            waitTime = 8.0f;
        }
    }

    void GetNewHelpMessage()
    {
        text.text = LocalizationManager.GetRandomHelpMessage() + "\n\n<color=silver>Next Tip: [Space]</color>";
    }
}
