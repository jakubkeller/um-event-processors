namespace Models.Event;

public class TcnInformationEvent
{
    public string? ExternalEpisodeId { get; set; }
    public long EpisodeNumber { get; set; }
    public string PriorAuthStatus { get; set; } = default!;
    public string? MemberFirstName { get; set; }
    public string? MemberLastName { get; set; }
    public string? UniqueId { get; set; } = default!;
    public string? DateOfBirth { get; set; }
    public string? CarrierId { get; set; }
    public string? DrugDescription { get; set; }
    public string? MemberZipCode { get; set; }
    public string? MemberHomePhone { get; set; }
    public string? MemberWorkPhone { get; set; }
    public string? MemberCellPhone { get; set; }
    public string? MemberLanguageCode { get; set; }
    public string? EpisodeSourceType { get; set; }
    public string? CallerInfoPhone { get; set; }
    public string? SubscriberNumber { get; set; }
    public string? PersonCode { get; set; }
}
