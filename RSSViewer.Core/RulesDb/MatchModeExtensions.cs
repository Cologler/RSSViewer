namespace RSSViewer.RulesDb
{
    public static class MatchModeExtensions
    {
        public static bool IsStringMode(this MatchMode matchMode)
        {
            var value = (int)matchMode;
            return (int)MatchMode.Contains <= value && value <= (int)MatchMode.Regex;
        }
    }
}
