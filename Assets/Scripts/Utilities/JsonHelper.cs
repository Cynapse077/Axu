﻿using LitJson;
using System.Text;
using System;

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

    public static bool TryGetValue(this JsonData dat, string key, out string o, string defaultValue = "")
    {
        if (dat.ContainsKey(key))
        {
            o = dat[key].ToString();
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetValue(this JsonData dat, string key, out int o, int defaultValue = 0)
    {
        if (dat.ContainsKey(key))
        {
            o = (int)dat[key];
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetValue(this JsonData dat, string key, out float o, float defaultValue = 0f)
    {
        if (dat.ContainsKey(key))
        {
            o = (float)((double)dat[key]);
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetValue(this JsonData dat, string key, out bool o, bool defaultValue = false)
    {
        if (dat.ContainsKey(key))
        {
            o = (bool)dat[key];
            return true;
        }

        o = defaultValue;
        return false;
    }

    public static bool TryGetValue(this JsonData dat, string key, out JsonData o)
    {
        if (dat.ContainsKey(key))
        {
            o = dat[key];
            return true;
        }

        o = false;
        return false;
    }

    public static bool TryGetValue(this JsonData dat, string key, out Coord o, Coord defaultValue = null)
    {
        if (dat.ContainsKey(key) && dat[key].Count == 2)
        {
            int x = 0;
            int y = 0;

            if (dat.TryGetValue("x", out x) && dat.TryGetValue("y", out y))
            {
                o = new Coord(x, y);
                return true;
            }
        }

        o = defaultValue;
        return false;
    }

    //For enums
    public static bool TryGetValue<T>(this JsonData dat, string key, out T o, bool parseEnum) where T : struct, IConvertible
    {
        if (parseEnum && !typeof(T).IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type");
        }

        if (dat.ContainsKey(key))
        {
            string e = dat[key].ToString();
            o = e.ToEnum<T>();
            return true;
        }

        o = default(T);
        return false;
    }
}