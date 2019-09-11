using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.Text;

public struct IntRange
{
    public int min;
    public int max;

    const char seperator = '~';

    public IntRange(int min, int max)
    {
        this.min = min;
        this.max = max;
    }

    //Split with ~
    //Eg. 2~5
    public IntRange(string value)
    {
        min = 0;
        max = 0;

        FromString(value);
    }

    public IntRange(JsonData dat)
    {
        min = 0;
        max = 0;

        string value = dat.ToString();
        FromString(value);
    }

    public int GetRandom()
    {
        if (max <= min)
        {
            Log.Error("IntRange: max greater or equal to min. " + this.ToString());
            max = min + 1;
        }

        return SeedManager.combatRandom.Next(min, max);
    }

    public void FromString(string value)
    {
        if (value.NullOrEmpty())
        {
            return;
        }

        string[] values = value.Split(seperator);

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = values[i].Trim(' ');
        }

        if (values.Length < 2 || !int.TryParse(values[0], out min) || !int.TryParse(values[1], out max))
        {
            Log.Error("IntRange could not be parsed from \"" + value + "\"");
        }
    }

    public override string ToString()
    {
        return string.Format("({0} - {1})", min, max);
    }
}