global using static Util;

using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;

if (args is not [string path, ..])
{
    throw new Exception("Must provide a path to the RSS file.");
}

var allReleases = new Dictionary<string, List<MangaRelease>>();

var publishers = new IPublisher[] {
    new YenPress(),
    // TODO: They block scrapers
    // new SevenSeas(),
    new Viz(),
    new Kodansha(),
    // TODO: They block scrapers
    // new SquareEnix(),
};

var today = args.Length == 2 ? DateOnly.Parse(args[1]) : DateOnly.FromDateTime(DateTime.Today);

foreach (var publisher in publishers)
{
    Console.WriteLine($"Processing {publisher.Name}");

    try
    {

        var releases = await publisher.GetReleasesForDate(today);
        allReleases.Add(publisher.Name, releases);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Encountered error with {publisher.Name}:\n {ex.Message}");
        allReleases.Add(publisher.Name, new() {
            new("Error", "Error", ex.Message, publisher.Name, today, "Error", null, null)
        });
    }
}

Console.WriteLine("Finished all publishers");

if (allReleases.Count == 0)
{
    Console.WriteLine("No releases found.");
    return;
}

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

public record MangaRelease(string Title, string Author, string Description, string Publisher, DateOnly ReleaseDate, string Price, Uri? ReleaseUrl, Uri? ImageUrl)
{
    public static SyndicationItem ToSyndicationItem(MangaRelease release)
        => new(
            title: release.Title,
            content: "",
            itemAlternateLink: release.ReleaseUrl)
        {
            Id = release.ReleaseUrl?.ToString() ?? Guid.NewGuid().ToString(),
            PublishDate = DateTimeOffset.Now,
            Summary = new TextSyndicationContent($"""
                     Title: {release.Title}
                     Author: {release.Author}
                     Description: {release.Description.ReplaceLineEndings().Replace(Environment.NewLine, " ")}
                     Publisher: {release.Publisher}
                     Release Date: {release.ReleaseDate:d}
                     Price: {release.Price}
                     """),
            ElementExtensions = {
                new XElement(((XNamespace)"media") + "thumbnail", new XAttribute("url", release.ImageUrl?.ToString() ?? ""))
            }
        };
}
