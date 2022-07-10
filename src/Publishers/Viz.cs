using HtmlAgilityPack;

public class Viz : IPublisher
{
    public string Name => "Viz";

    private const string VizBaseUrl = "https://www.viz.com";
    private const string VizReleaseBaseUrl = $"{VizBaseUrl}/calendar/";

    public async Task<List<MangaRelease>> GetReleasesForDate(DateOnly date)
    {
        var releaseCalendarUrl = $"{VizReleaseBaseUrl}{date.Year}/{date.Month}";

        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(releaseCalendarUrl);

        var mangaGrid = doc.DocumentNode.SelectSingleNode("""//div[@id="m-grid"]""");

        var releases = new List<MangaRelease>();

        foreach (var article in mangaGrid.SelectNodes("article"))
        {
            var aTag = article.SelectSingleNode("""div/a""");
            var releaseUrl = aTag.GetAttributeValue("href", null).Replace("paperback", "digital");
            releaseUrl = $"{VizBaseUrl}{releaseUrl}";
            var name = aTag.InnerText;

            var releaseDoc = await web.LoadFromWebAsync(releaseUrl);

            var ReleaseDatePath = $"""//div[{ClassContains("o_release-date")}]""";
            var releaseDateString = releaseDoc.DocumentNode.SelectSingleNode(ReleaseDatePath).InnerText;
            releaseDateString = releaseDateString[(releaseDateString.IndexOf("Release ") + "Release ".Length)..];
            var releaseDate = DateOnly.Parse(releaseDateString);

            if (releaseDate > date)
            {
                // Releases are ordered by date, so we are done if the date is after today.
                Console.WriteLine($"{name} is releasing after {date}, ending search.");
                break;
            }
            else if (releaseDate != date)
            {
                Console.WriteLine($"Skipping {name} because it is not for {date}");
                continue;
            }

            Console.WriteLine($"Processing title {name}");

            var price = releaseDoc.DocumentNode.SelectSingleNode("""//div[@id="buy-digital"]/div/table/tbody/tr[1]/td[2]""").InnerText.Trim();
            var author = releaseDoc.DocumentNode.SelectSingleNode(ReleaseDatePath).ParentNode.SelectSingleNode("div[1]").InnerText;
            var image = releaseDoc.DocumentNode.SelectSingleNode($"""//div[{ClassContains("product-image")}]/img""").GetAttributeValue("src", null);
            var description = releaseDoc.DocumentNode.SelectSingleNode("""//div[@id="product_row"]/div[2]/div[1]/p""").InnerText;

            releases.Add(new(name, author, description, Name, releaseDate, price, new Uri(releaseUrl), new Uri(image)));
        }

        return releases;
    }
}
