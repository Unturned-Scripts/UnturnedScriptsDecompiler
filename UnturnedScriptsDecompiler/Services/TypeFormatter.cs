namespace UnturnedScriptsDecompiler.Services
{
    public static class TypeFormatter
    {
        public static string FormatTypeName(ReadOnlySpan<char> typeName)
        {
            int nextIndex = 0;
            Span<char> formatted = stackalloc char[typeName.Length * 2];

            formatted[0] = typeName[0];
            for (int i = 1; i < typeName.Length - 1; i++)
            {
                char ch = typeName[i];
                if (ch == '_' || (char.IsUpper(ch) && !char.IsUpper(typeName[i - 1]) && typeName[i - 1] != '_'))
                    formatted[++nextIndex] = ' ';

                if (ch == '_') continue;
                formatted[++nextIndex] = ch;
            }
            formatted[++nextIndex] = typeName[^1];

            return formatted[..(nextIndex + 1)].ToString();
        }
    }
}
