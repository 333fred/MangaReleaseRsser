global using static Util;

using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;

if (args is not [string path, ..])
{
    throw new Exception("Must provide a path to the RSS file.");
}

var allReleases = new Dictionary<string, List<MangaRelease>>();

try
{
    using var xmlReader = XmlReader.Create(path);
    var feed = SyndicationFeed.Load(xmlReader);

    var groupedItems = feed.Items.Select(MangaRelease.ToMangaRelease).GroupBy(x => x.Title);

    foreach (var group in groupedItems)
    {
        allReleases.Add(group.Key, group.ToList());
    }
}
catch (FileNotFoundException)
{ }

var publishers = new IPublisher[] {
    new YenPress(),
    // TODO: They block scrapers
    // new SevenSeas(),
    new Viz(),
    new Kodansha(),
    new SquareEnix(),
};

var today = args.Length == 2 ? DateOnly.Parse(args[1]) : DateOnly.FromDateTime(DateTime.Today);
var thirtyDaysAgo = today.AddDays(-30);

foreach (var publisher in publishers)
{
    Console.WriteLine($"Processing {publisher.Name}");

    try
    {

        var releases = await publisher.GetReleasesForDate(today);
        if (allReleases.TryGetValue(publisher.Name, out var existingReleases))
        {
            allReleases[publisher.Name].AddRange(releases);
        }
        else
        {
            allReleases.Add(publisher.Name, releases);
        }

        for (int i = 0; i < releases.Count; i++)
        {
            if (releases[i].ReleaseDate < thirtyDaysAgo)
            {
                releases.RemoveAt(i);
                i--;
                continue;
            }
            break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Encountered error with {publisher.Name}:\n {ex.Message}");
        allReleases.Add(publisher.Name, new() {
            new("Error", "Error", ex.Message, publisher.Name, today, "Error", new Uri("Error"), new Uri("Error"))
        });
    }
}

Console.WriteLine("Finished all publishers");

var items = allReleases.SelectMany(x => x.Value).OrderBy(x => x.ReleaseDate).Select(MangaRelease.ToSyndicationItem).ToList();
var syndicationFeed = new SyndicationFeed(
    "Official Manga Releases",
    "Publication information for new official manga publications",
    new Uri("https://manga.silberberg.xyz/rss.xml"),
    items);

using var writer = XmlWriter.Create(path);
var rssFormatter = new Rss20FeedFormatter(syndicationFeed);
rssFormatter.WriteTo(writer);

interface IPublisher
{
    Task<List<MangaRelease>> GetReleasesForDate(DateOnly date);
    string Name { get; }
}

public record MangaRelease(string Title, string Author, string Description, string Publisher, DateOnly ReleaseDate, string Price, Uri ReleaseUrl, Uri ImageUrl)
{
    public static SyndicationItem ToSyndicationItem(MangaRelease release)
        => new(
            title: release.Title,
            content: "",
            itemAlternateLink: release.ReleaseUrl)
        {
            Summary = new TextSyndicationContent($"""
                     Title: {release.Title}
                     Author: {release.Author}
                     Description: {release.Description.ReplaceLineEndings().Replace(Environment.NewLine, " ")}
                     Publisher: {release.Publisher}
                     Release Date: {release.ReleaseDate:d}
                     Price: {release.Price}
                     """),
            ElementExtensions = {
                new XElement(((XNamespace)"media") + "thumbnail", new XAttribute("url", release.ImageUrl))
            }
        };

    public static MangaRelease ToMangaRelease(SyndicationItem item)
    {
        var title = item.Title.Text;
        var descriptionLines = item.Summary.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var author = descriptionLines[1]["Author: ".Length..].Trim();
        var description = descriptionLines[2]["Description: ".Length..].Trim();
        var publisher = descriptionLines[3];
        var releaseDate = DateOnly.Parse(descriptionLines[4]["Release Date: ".Length..].Trim());
        var price = descriptionLines[5]["Price: ".Length..].Trim();
        var releaseUrl = item.BaseUri;
        var imageUrl = new Uri(item.ElementExtensions.FirstOrDefault(x => x.OuterName == "thumbnail")!.GetReader().GetAttribute("url")!);
        return new MangaRelease(title, author, description, publisher, releaseDate, price, releaseUrl, imageUrl);
    }
}
