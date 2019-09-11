using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MessageBus<T>
{
    static Queue<Message<T>> messages;

    public static void Broadcast(Message<T> message)
    {
        messages.Enqueue(message);

        while (messages.Count > 0)
        {
            Message<T> m = messages.Dequeue();
            m.Broadcast();
        }
    }
}

public class Message<T>
{
    public T source;

    public virtual void Broadcast()
    {
        throw new System.NotImplementedException();
    }
}

public class Message_Targeted : Message<Entity>
{
    public Entity target;

    public override void Broadcast()
    {
        throw new System.NotImplementedException();
    }
}

public interface IEventSubscriber
{
    void Subscribe<T>();
}