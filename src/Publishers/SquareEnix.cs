using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

public class SquareEnix : IPublisher
{
    public string Name => "Square Enix";

    private const string SquareEnixBaseUrl = "https://squareenixmangaandbooks.square-enix-games.com";
    private const string SquareEnixJsonUrl = $"{SquareEnixBaseUrl}/locale/en-us";

    public async Task<List<MangaRelease>> GetReleasesForDate(DateOnly date)
    {
        var jsonText = "";
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync("https://squareenixmangaandbooks.square-enix-games.com/locale/en-us");
            jsonText = await response.Content.ReadAsStringAsync();
        }

        var json = JObject.Parse(jsonText);
        var releases = new List<MangaRelease>();
        foreach (var series in json.Value<JArray>("series")!)
        {
            foreach (var volume in series.Value<JArray>("volumes") ?? new JArray())
            {
                var title = volume.Value<string>("title")!;
                var releaseDate = DateOnly.Parse(volume.Value<string>("releaseDate")!);

                if (releaseDate > date)
                {
                    // We've passed the date we're looking for, no releases found for this manga
                    Console.WriteLine($"{title} is releasing after {date}, skipping manga.");
                    break;
                }

                if (releaseDate < date)
                {
                    // Skipping all previous volumes
                    Console.WriteLine($"Skipping {title} because it is not for {date}");
                    continue;
                }

                Console.WriteLine($"Processing title {title}");

                var author = series.Value<string>("author")!;
                var isbn = volume.Value<string>("isbn")!;
                var releaseUrl = $"{SquareEnixBaseUrl}/en-us/product/{isbn}";
                var imageUrl = volume.Value<JObject>("cover")!.Value<string>("image")!;

                var releaseDoc = await new HtmlWeb().LoadFromWebAsync(releaseUrl);
                var description = releaseDoc.DocumentNode.SelectSingleNode($"""//div[{ClassContains("synopsis-text")}]""").InnerText.Trim();
                var price = releaseDoc.DocumentNode.SelectSingleNode($"""//div[{ClassContains("price-header")}]""").InnerText.Trim();

                releases.Add(new(title, author, description, Name, releaseDate, price, new Uri(releaseUrl), new Uri(imageUrl)));
            }
        }

        return releases;
    }
}
