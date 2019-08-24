using LitJson;
using System;
using System.Text;
using System.Collections.Generic;

public static class JsonHelper {
	public static string Indent = "    ";
	
	public static string PrettyPrint(string input)
	{
		var output = new StringBuilder(input.Length * 2);
		char? quote = null;
		int depth = 0;
		
		for(int i=0; i<input.Length; ++i)
		{
			char ch = input[i];
			
			switch (ch)
			{
			case '{':
			case '[':
				output.Append(ch);
				if (!quote.HasValue)
				{
					output.AppendLine();
					output.Append(Indent.Repeat(++depth));
				}
				break;
			case '}':
			case ']':
				if (quote.HasValue)  
					output.Append(ch);
				else
				{
					output.AppendLine();
					output.Append(Indent.Repeat(--depth));
					output.Append(ch);
				}
				break;
			case '"':
			//case '\'':
				output.Append(ch);
				if (quote.HasValue)
				{
					if (!output.IsEscaped(i))
						quote = null;
				}
				else quote = ch;
				break;
			case ',':
				output.Append(ch);
				if (!quote.HasValue)
				{
					output.AppendLine();
					output.Append(Indent.Repeat(depth));
				}
				break;
			case ':':
				if (quote.HasValue) output.Append(ch);
				else output.Append(" : ");
				break;
			default:
				if (quote.HasValue || !char.IsWhiteSpace(ch)) 
					output.Append(ch);
				break;
			}
		}
		
		return output.ToString();
	}
	public static string Repeat(this string str, int count)
	{
		return new StringBuilder().Insert(0, str, count).ToString();
	}
	
	public static bool IsEscaped(this string str, int index)
	{
		bool escaped = false;
		while (index > 0 && str[--index] == '\\') escaped = !escaped;
		return escaped;
	}
	
	public static bool IsEscaped(this StringBuilder str, int index)
	{
		return str.ToString().IsEscaped(index);
	}

    public static bool ContainsKey(this JsonData data, string key)
    {
        return (data.Keys.Contains(key) && data[key] != null);
    }

    public static bool TryGetString(this JsonData dat, string key, out string o, string defaultValue = "")
    {
        if (dat.ContainsKey(key))
        {
            o = dat[key].ToString();
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetInt(this JsonData dat, string key, out int o, int defaultValue = 0)
    {
        if (dat.ContainsKey(key))
        {
            o = (int)dat[key];
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetFloat(this JsonData dat, string key, out float o, float defaultValue = 0f)
    {
        if (dat.ContainsKey(key))
        {
            o = (float)((double)dat[key]);
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetDouble(this JsonData dat, string key, out double o, double defaultValue = 0.0)
    {
        if (dat.ContainsKey(key))
        {
            o = (double)dat[key];
            return true;
        }

        o = 0.0;
        return false;
    }

    public static bool TryGetBool(this JsonData dat, string key, out bool o, bool defaultValue = false)
    {
        if (dat.ContainsKey(key))
        {
            o = (bool)dat[key];
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetJsonData(this JsonData dat, string key, out JsonData o)
    {
        if (dat.ContainsKey(key))
        {
            o = dat[key];
            return true;
        }

        o = false;
        return false;
    }

    public static bool TryGetCoord(this JsonData dat, string key, out Coord o, Coord defaultValue = null)
    {
        if (dat.ContainsKey(key) && dat[key].Count == 2)
        {
            int x = 0;
            int y = 0;

            if (dat.TryGetInt("x", out x) && dat.TryGetInt("y", out y))
            {
                o = new Coord(x, y);
                return true;
            }
        }

        o = defaultValue;
        return false;
    }

    //For enums
    public static bool TryGetEnum<T>(this JsonData dat, string key, out T o, T defaultValue) where T : struct, IConvertible
    {
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("<T> must be an enumerated type");
        }

        if (dat.ContainsKey(key))
        {
            string e = dat[key].ToString();
            o = e.ToEnum<T>();
            return true;
        }

        o = defaultValue;
        return false;
    }

    //For lists of strings
    public static bool TryGetValue(this JsonData dat, string key, out List<string> o)
    {
        o = new List<string>();

        if (dat.ContainsKey(key))
        {
            for (int i = 0; i < dat[key].Count; i++)
            {
                o.Add(dat[key][i].ToString());
            }

            return true;
        }

        return false;
    }
}