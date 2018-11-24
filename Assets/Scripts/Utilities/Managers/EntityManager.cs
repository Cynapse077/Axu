using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager
{
    public static EntityManager instance;
    public List<Entity> onScreenNPCObjects;
    public List<NPC> npcClasses;

    public EntityManager()
    {
        instance = this;
        npcClasses = new List<NPC>();
        onScreenNPCObjects = new List<Entity>();
    }
}
