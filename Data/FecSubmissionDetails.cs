using System.Text.Json.Serialization;

namespace Fec.WebJob.FecSubmission.Data;

/// <summary>
/// Credentials and target emails for making a submission to the FEC. JsonPropertyName is used so we can specify the JSON field names required to be submitted to the FEC from our Pascal cased properties
/// </summary>
/// <param name="CommitteeId">This is the FEC committee ID for the organization that is filing</param>
/// <param name="Password">The password for the FEC committee.</param>
/// <param name="ApiKey">This is the API key assigned to a vendor (Aegis) in order to submit an FEC report.</param>
/// <param name="Email1">Email of person to receive results from the FEC</param>
/// <param name="Email2">Optional second email of person to receive results from the FEC</param>
/// <param name="AgencyId">Vendor ID Number. For submitting to test servers, "VEND" is used. </param>
/// <param name="AmendmentId">If the report being submitted is an amended report, you need to include the FEC ID for the original report here.</param>
public record class FecSubmissionDetails(
    [property: JsonPropertyName("committee_id")] string CommitteeId,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("api_key")] string ApiKey,
    [property: JsonPropertyName("email_1")] string Email1,
    [property: JsonPropertyName("email_2")] string Email2,
    [property: JsonPropertyName("agency_id")] string AgencyId,
    [property: JsonPropertyName("amendment_id")] string AmendmentId)
{
    /// <summary>
    /// Indicates whether to keep the HTTP connection open until fec.gov has completed processing the .fec file. When <c>false</c>, the
    /// HTTP request will return immediately after the file has finished uploading and will return JSON similar to this.
    /// <code>{"status":"PROCESSING","report_id":"","message":"Please check in few minutes for status update","submission_id":"YPPJ230125534867","success":true}</code>
    /// When <c>true</c>, the HTTP request returns JSON similar to this:
    /// <code>{"status":"ACCEPTED","report_id":"FEC-3697","message":"ACCEPTED FEC-3697  \r\n","submission_id":"HHGR230125534873","success":true}</code>
    /// </summary>
    [JsonPropertyName("wait")]
    public bool Wait { get; init; } = false;
}
