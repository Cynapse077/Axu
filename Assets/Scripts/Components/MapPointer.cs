using UnityEngine;
using System.Collections;

public class MapPointer : MonoBehaviour
{
    public GameObject questPointer;
    Vector3 Difference = Vector3.zero;
    Journal journal;
    PlayerInput playerInput;

    Vector3 Diff(Coord questPos)
    {
        Difference.x = (questPos.x + 50) - questPointer.transform.position.x + 0.5f;
        Difference.y = (questPos.y - 200) - questPointer.transform.position.y + 0.5f;
        return Difference.normalized;
    }

    void Start()
    {
        if (journal == null && ObjectManager.playerJournal != null)
        {
            journal = ObjectManager.playerJournal;
        }
    }

    public void OnChangeWorldMapPosition()
    {
        if (ObjectManager.player == null)
        {
            return;
        }

        if (journal == null && ObjectManager.playerJournal != null)
        {
            journal = ObjectManager.playerJournal;
        }

        if (playerInput == null)
        {
            playerInput = ObjectManager.player.GetComponent<PlayerInput>();
        }

        if (journal != null && journal.quests != null && journal.quests.Count > 0 && journal.trackedQuest != null && journal.trackedQuest.ActiveGoal != null)
        {
            Coord destination = journal.trackedQuest.ActiveGoal.Destination();
            bool show = (journal.trackedQuest != null && destination != null);

            questPointer.SetActive(show);

            if (show)
            {
                if (destination == World.tileMap.WorldPosition)
                {
                    questPointer.SetActive(false);
                }
                else
                {
                    Vector3 diff = Diff(destination);
                    float rot_z = (Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg) - 135;
                    questPointer.transform.localRotation = Quaternion.Euler(0f, 0f, rot_z);
                }
            }

        }
        else
        {
            questPointer.SetActive(false);
        }
    }
}
