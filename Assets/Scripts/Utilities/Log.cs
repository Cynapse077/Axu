using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Log
{
    public static void Message(object message)
    {
        Debug.Log(message);
    }

    public static void MessageConditional(object message, bool condition)
    {
        if (condition)
        {
            Debug.Log(message);
        }
    }

    public static void Error(object message)
    {
        Debug.LogError(message);
    }
}
