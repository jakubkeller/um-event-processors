namespace Models.Event;

public class SendEmail
{
    /// <summary>
    /// Navitus episode Id associated with the outbound fax
    /// </summary>
    public string NavitusEpisodeId { get; set; } = default!;

    /// <summary>
    /// Subject line of the email
    /// </summary>
    public string Subject { get; set; } = default!;

    /// <summary>
    /// Body of the email
    /// </summary>
    public string HtmlBody { get; set; } = default!;

    /// <summary>
    /// Sensitivity of the email
    /// </summary>
    public int Sensitivity { get; set; } = 0;

    /// <summary>
    /// List of To email addresses to send email to
    /// </summary>
    public IEnumerable<string> ToEmailaddress { get; set; } = default!;

    /// <summary>
    /// List of CC email addresses to send email to
    /// </summary>
    public IEnumerable<string> CcEmailaddress { get; set; } = default!;

    /// <summary>
    /// List of BCC email addresses to send email to
    /// </summary>
    public IEnumerable<string> BccEmailaddress { get; set; } = default!;

    /// <summary>
    /// List of Document Id - S3 Object Key
    /// </summary>
    public IEnumerable<string> DocumentId { get; set; } = default!;
}
