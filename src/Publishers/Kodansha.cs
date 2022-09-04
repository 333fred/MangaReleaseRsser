using HtmlAgilityPack;
using OpenQA.Selenium;

public class Kodansha : IPublisher
{
    public string Name => nameof(Kodansha);

    public const string KodanshaReleaseUrl = "https://kodansha.us/manga/calendar/";

    public async Task<List<MangaRelease>> GetReleasesForDate(DateOnly date, WebDriver driver)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(KodanshaReleaseUrl);

        var calendarDays = doc.DocumentNode.SelectNodes($"""//li[{ClassContains("calendar__day")}]""");
        var releases = new List<MangaRelease>();

        foreach (var day in calendarDays)
        {
            var releaseDate = DateOnly.Parse(day.SelectSingleNode("h3").InnerText);

            if (releaseDate == date)
            {

                var mangaList = day.SelectNodes("""ol/li/div/div""");
                foreach (var manga in mangaList)
                {
                    var aNode = manga.SelectSingleNode("""div[1]/h4/a""");
                    var name = aNode.InnerText;
                    var releaseUrl = aNode.GetAttributeValue("href", null);

                    Console.WriteLine($"Processing title {name}");

                    var releaseDoc = await web.LoadFromWebAsync(releaseUrl);
                    var author = releaseDoc.DocumentNode.SelectSingleNode($"""//p[{ClassContains("byline")}]""").InnerText.Trim();
                    var description = releaseDoc.DocumentNode.SelectSingleNode($"""//div[{ClassContains("product-detail-hero__synopsis")}]""").InnerText.Trim();
                    var image = releaseDoc.DocumentNode.SelectSingleNode($"""//div[{ClassContains("product-image")}]/img""").GetAttributeValue("src", null);

                    releases.Add(new(name, author, description, Name, releaseDate, "Unknown", new Uri(releaseUrl), new Uri(image)));
                }

                break;
            }
            else if (releaseDate < date)
            {
                // Skipping all previous days
                continue;
            }
            else
            {
                break;
            }
        }

        // We've gone past the date we're looking for, no manga found
        return releases;
    }
}
