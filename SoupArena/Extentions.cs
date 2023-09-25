namespace SoupArena
{
    public static class Extentions
    {
        public static string ToString<T>(this IEnumerable<T> Source, Func<T, string> Selector, string Separator = "\n")
        {
            var Strings = Source.Select(Selector);
            return string.Join(Separator, Strings);
        }
        public static string IfNullOrEmpty(this string Source, string Replacement)
        {
            if (string.IsNullOrEmpty(Source) || string.IsNullOrWhiteSpace(Source))
                return Replacement;
            return Source;
        }
    }
}
