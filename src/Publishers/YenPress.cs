using HtmlAgilityPack;
using OpenQA.Selenium;

public class YenPress : IPublisher
{
    public string Name => "Yen Press";

    private const string YenPressBaseUrl = "https://yenpress.com";
    private const string DigitalReleaseLink = $"{YenPressBaseUrl}/digital/";

    public async Task<List<MangaRelease>> GetReleasesForDate(DateOnly date, WebDriver driver)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(DigitalReleaseLink);

        var outThisMonth = doc.DocumentNode.SelectSingleNode("""//*[@id="yen-press-0"]""");

        var releases = new List<MangaRelease>();

        foreach (var bookLi in outThisMonth.SelectNodes("""li"""))
        {
            var bookLink = bookLi.SelectSingleNode($"""div/div[{ClassContains("book-detail-links")}]/a""").GetAttributeValue("href", null);
            if (bookLink is null)
            {
                Console.WriteLine($"Failed to retrieve book link for {bookLi}");
                continue;
            }

            bookLink = $"{YenPressBaseUrl}{bookLink}";

            var bookDoc = await web.LoadFromWebAsync(bookLink);
            var title = bookDoc.DocumentNode.SelectSingleNode("""//*[@id="book-title"]""").InnerText;

            Console.WriteLine($"Processing title {title}");

            const string FullDetailsXpath = """//div[@id="book-details"]/ul""";
            var releaseDate = DateOnly.Parse(bookDoc.DocumentNode.SelectSingleNode($"""{FullDetailsXpath}/li[last()]/span[last()]""").InnerText);

            if (releaseDate != date)
            {
                // Releases are alphabetically ordered, so we have to keep looking.
                Console.WriteLine($"Skipping {title} because it is not for today");
                continue;
            }

            var author = bookDoc.DocumentNode.SelectSingleNode("""//h3[@id="book-author"]""").InnerText;
            // For some reason, HtmlAgilityPack is seeing the h3 tag as having the description div as its child node. It's not, but just work around it.
            author = author[..author.IndexOf('\n')];
            var description = bookDoc.DocumentNode.SelectSingleNode("""//*[@id="book-description-full"]""").InnerText.Trim();
            var imageUrl = bookDoc.DocumentNode.SelectSingleNode("""//*[@id="main-cover"]/picture/img""").GetAttributeValue("src", null);
            var price = bookDoc.DocumentNode.SelectSingleNode($"""{FullDetailsXpath}/span[last()]""").InnerText;
            price = price[(price.IndexOf(": ") + 2)..];

            releases.Add(new(title, author, description, Name, releaseDate, price, new Uri(bookLink), new Uri(imageUrl)));
        }

        return releases;
    }
}
