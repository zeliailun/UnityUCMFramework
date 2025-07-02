using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnknownCreator.Modules
{

    /// <summary>
    /// 拓展方法
    /// </summary>
	public static class JsonExts
    {


        public static void WriteProperty(this JsonWriter w, string name, long value)
        {
            w.WritePropertyName(name);
            w.Write(value);
        }

        public static void WriteProperty(this JsonWriter w, string name, string value)
        {
            w.WritePropertyName(name);
            w.Write(value);
        }

        public static void WriteProperty(this JsonWriter w, string name, bool value)
        {
            w.WritePropertyName(name);
            w.Write(value);
        }

        public static void WriteProperty(this JsonWriter w, string name, double value)
        {
            w.WritePropertyName(name);
            w.Write(value);
        }

        public static string GetFullTypeName(this Type type)
        {
            if (type.IsGenericType)
            {
                // Get the generic type definition
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                // Get the number of generic arguments
                int genericArgumentCount = genericTypeDefinition.GetGenericArguments().Length;

                // Construct the base generic type name without the backtick and count
                string typeName = genericTypeDefinition.FullName.Remove(genericTypeDefinition.FullName.IndexOf('`'));

                // Get the generic arguments formatted as needed
                var argumentNames = type.GetGenericArguments()
                                        .Select(arg => GetFullTypeName(arg))
                                        .ToArray();

                // Construct the full name with arguments, adding backtick and argument count only once
                return $"{typeName}`{genericArgumentCount}[[{string.Join("],[", argumentNames)}]]";
            }
            else if (type.IsArray)
            {
                return $"{GetFullTypeName(type.GetElementType())}[]";
            }
            else
            {
                return type.FullName;
            }
        }

        private static readonly Regex _simplifyRegex = new Regex(
@",\s*(mscorlib|System\.Core|System),\s*Version=\d+\.\d+\.\d+\.\d+, Culture=neutral, PublicKeyToken=\w+",
RegexOptions.Compiled);

        public static string SimplifyFullName(this string fullName)
        {
            return _simplifyRegex.Replace(fullName, string.Empty);
        }

        // 将中文字符转换为 UTF-8 编码的字节形式
        public static string ChineseToUtf8(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            int index = 0;
            while (index < input.Length)
            {
                if (input[index] == '\\' &&
                    index + 1 < input.Length &&
                    (input[index + 1] == 'u' || input[index + 1] == 'U'))
                {
                    int codeLength = input[index + 1] == 'u' ? 4 : 8;
                    if (index + codeLength + 1 <= input.Length)
                    {
                        string hexCode = input.Substring(index + 2, codeLength);
                        if (int.TryParse(hexCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int unicodeValue))
                        {
                            sb.Append((char)unicodeValue);
                            index += codeLength + 2;
                            continue;
                        }
                    }
                }
                sb.Append(input[index]);
                index++;
            }
            return sb.ToString();
        }


    }
}