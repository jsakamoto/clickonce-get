using System.ComponentModel.DataAnnotations;

namespace ClickOnceGet.Shared.Models;

public class ClickOnceAppInfo
{
    public string Name { get; set; }

    public string OwnerId { get; set; }

    public DateTime RegisteredAt { get; set; }

    [StringLength(140)]
    public string Title { get; set; }

    [StringLength(140)]
    public string Description { get; set; }

    [Url]
    public string ProjectURL { get; set; }

    public string PublisherName { get; set; }

    public string PublisherAvatorImageURL { get; set; }

    public string PublisherURL { get; set; }

    public int NumberOfDownloads { get; set; }

    public bool? HasCodeSigning { get; set; }

    public bool SignedByPublisher { get; set; }

    public string GetTitleOrName()
    {
        return string.IsNullOrEmpty(this.Title) ? this.Name : this.Title;
    }
}
