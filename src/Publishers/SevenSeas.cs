using HtmlAgilityPack;

public class SevenSeas : IPublisher
{
    public string Name => "Seven Seas Entertainment";

    private const string SevenSeasReleaseUrl = "https://sevenseasentertainment.com/release-dates/";

    public async Task<List<MangaRelease>> GetReleasesForDate(DateOnly date)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(SevenSeasReleaseUrl);

        var releasesTable = doc.DocumentNode.SelectSingleNode("""//table[@id="releasedates"]/tbody""");

        date = DateOnly.Parse("2022-07-12");
        var releases = new List<MangaRelease>();

        foreach (var row in releasesTable.SelectNodes("""tr"""))
        {
            var title = row.SelectSingleNode("""td[2]/a/strong""").InnerText;
            var releaseDate = DateOnly.Parse(row.SelectSingleNode("""td[1]""").InnerText);

            if (releaseDate != date)
            {
                // Releases are alphabetically ordered, so we have to keep looking.
                Console.WriteLine($"Skipping {title} because it is not for today");
                continue;
            }

            Console.WriteLine($"Processing title {title}");

            var releaseUrl = row.SelectSingleNode("""td[2]/a""").GetAttributeValue("href", null);
            var releaseDoc = await web.LoadFromWebAsync(releaseUrl);

            var author = releaseDoc.DocumentNode.SelectSingleNode("""//span[class="creator"]/a""").InnerText;
            var imageUrl = releaseDoc.DocumentNode.SelectSingleNode("""//*[@id="volume-cover"]/img""").GetAttributeValue("src", null);

            var allDetails = releaseDoc.DocumentNode
                .SelectSingleNode("""//div[@id="volume-meta"]/p[1]""")
                .InnerText
                .ReplaceLineEndings()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            var price = allDetails[2][(allDetails[2].IndexOf(": ") + 2)..];
            var description = releaseDoc.DocumentNode.SelectSingleNode("""//div[@id="volume-meta"]/p[6]""").InnerText.ReplaceLineEndings().Replace(Environment.NewLine, " ");

            releases.Add(new(title, author, description, releaseDate, price, new Uri(releaseUrl), new Uri(imageUrl)));
        }

        return releases;
    }
}
