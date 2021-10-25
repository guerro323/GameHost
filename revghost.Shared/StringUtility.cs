using System.Text;

namespace revghost.Shared;

public static class StringUtility
{
    // from https://stackoverflow.com/a/63055998
    public static string ToSnakeCase(this string str)
    {
        if (str is null)
            throw new ArgumentNullException(nameof(str));
        if (str.Length < 2)
            return str;

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(str[0]));
        for (var i = 1; i < str.Length; ++i)
        {
            var c = str[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}