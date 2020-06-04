namespace RSSViewer.RulesDb
{
    public enum MatchMode
    {
        None = 0,

        // match string:
        Contains = 1,
        StartsWith = 2,
        EndsWith = 3,
        Wildcard = 4,
        Regex = 5,
    }
}
