using UnityEngine;
using UnityEngine.UI;

public class MessageLogUI : MonoBehaviour
{
    public GameObject messageObject;
    public Transform anchor;

    const int maxMessages = 7;
    Text[] messages;

    void Start()
    {
        anchor.DestroyChildren();

        messages = new Text[maxMessages];

        for (int i = 0; i < maxMessages; i++)
        {
            GameObject g = (GameObject)Instantiate(messageObject, anchor);
            Text t = g.GetComponent<Text>();
            t.text = "";
            messages[maxMessages - i - 1] = t;
        }
    }

    public void NewMessage(string message)
    {
        for (int i = maxMessages - 1; i > 0; i--)
        {
            messages[i].text = messages[i - 1].text;
        }

        messages[0].text = message;
    }
}
