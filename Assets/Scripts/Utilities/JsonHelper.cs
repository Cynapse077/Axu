using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

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
}

