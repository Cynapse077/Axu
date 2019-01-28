using UnityEngine;

public class WeaponHitEffect : MonoBehaviour
{
    public Transform childObject;

    void OnEnable()
    {
        StopAnimation();
    }

    void OnDisable()
    {
        StopAnimation();
    }

    void StopAnimation()
    {
        if (childObject.GetComponent<Animator>() != null)
        {
            childObject.GetComponent<Animator>().StopPlayback();
        }
        else
        {
            childObject.GetComponent<Animation>().Stop();
        }
    }

    public void FaceChildOtherDirection(int l, int x, int y, Item i)
    {
        StopAnimation();

        childObject.localScale = new Vector3(1, 1, 1);
        childObject.localRotation = Quaternion.Euler(0, 0, 0);

        x = Mathf.Clamp(x, -1, 1);
        y = Mathf.Clamp(y, -1, 1);
        Vector3 temp = childObject.localScale;
        int rotAmount = 0;

        if (x == 0)
        {
            temp.x = l;
            rotAmount = (y > 0) ? 90 : -90;
        }
        else
        {
            temp.x = x;

            if (y != 0)
            {
                if (y > 0)
                {
                    rotAmount = (x > 0) ? 45 : -45;
                }
                else
                {
                    rotAmount = (x > 0) ? -45 : 45;
                }
            }
        }

        if (temp.x < 0 && x == 0)
        {
            rotAmount += 180;
        }

        childObject.transform.localRotation = Quaternion.Euler(0, 0, rotAmount);
        childObject.localScale = temp;

        ChangeColor(i);
    }

    public void DiagonalSlashDirection(int x, int y, Item i)
    {
        StopAnimation();

        Vector3 temp = childObject.localScale;
        int rotAmount = 0;

        temp.x = x;

        if (y > 0)
        {
            rotAmount = (x > 0) ? 0 : 0;
        }
        else
        {
            rotAmount = (x > 0) ? -90 : 90;
        }

        childObject.transform.localRotation = Quaternion.Euler(0, 0, rotAmount);
        childObject.localScale = temp;

        ChangeColor(i);
    }

    void ChangeColor(Item i)
    {
        Color c = Color.white;

        if (i.HasProp(ItemProperty.Poison) || i.ContainsDamageType(DamageTypes.Venom))
        {
            c = Color.green;
        }
        else if (i.ContainsDamageType(DamageTypes.Cold))
        {
            c = Color.cyan;
        }
        else if (i.ContainsDamageType(DamageTypes.Heat))
        {
            c = Color.red;
        }
        else if (i.ContainsDamageType(DamageTypes.Energy))
        {
            c = Color.yellow;
        }

        childObject.GetComponent<SpriteRenderer>().color = c;
    }
}
