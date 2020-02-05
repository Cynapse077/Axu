using UnityEngine;

public class DestroyAfterTurns : MonoBehaviour
{
    public int lifeTime = 3;
    public GameObject childObject;
    public bool shrink;

    void Start()
    {
        World.turnManager.incrementTurnCounter += IncrementTurnCounter;
    }

    void OnDisable()
    {
        World.turnManager.incrementTurnCounter -= IncrementTurnCounter;
    }

    public void IncrementTurnCounter()
    {
        lifeTime--;

        if (lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }
}
