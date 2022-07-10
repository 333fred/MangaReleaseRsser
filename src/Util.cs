public static class Util
{
    public static string ClassContains(string className) => TagContains("class", className);
    public static string TagContains(string tag, string toSearch) => $"""contains(concat(' ',normalize-space(@{tag}),' '),' {toSearch} ')""";
}
