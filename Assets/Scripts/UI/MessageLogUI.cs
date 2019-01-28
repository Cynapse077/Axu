using UnityEngine;
using UnityEngine.UI;

public class MessageLogUI : MonoBehaviour
{
    public GameObject messageObject;
    public Transform anchor;

    const int maxMessages = 7;
    Text[] messages;
    string[] messageInfo;
    int repetitions = 1;

    void Start()
    {
        anchor.DestroyChildren();

        messages = new Text[maxMessages];
        messageInfo = new string[maxMessages];

        for (int i = 0; i < maxMessages; i++)
        {
            GameObject g = (GameObject)Instantiate(messageObject, anchor);
            Text t = g.GetComponent<Text>();
            t.text = "";
            messages[maxMessages - i - 1] = t;
            messageInfo[maxMessages - i - 1] = t.text;
        }
    }

    public void NewMessage(string message)
    {
        if (message == messageInfo[0])
        {
            repetitions++;
            messages[0].text = message + string.Format(" (x{0})", repetitions);
        }
        else
        {
            for (int i = maxMessages - 1; i > 0; i--)
            {
                messages[i].text = messages[i - 1].text;
            }

            messages[0].text = message;
            messageInfo[0] = message;
            repetitions = 1;
        }        
    }
}
