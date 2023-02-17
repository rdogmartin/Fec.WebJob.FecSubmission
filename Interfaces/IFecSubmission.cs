using Fec.WebJobs.Core;

namespace Fec.WebJob.FecSubmission.Interfaces;

/// <summary>
/// Contains functionality for submitting an FEC File to fec.gov and checking its status.
/// </summary>
public interface IFecSubmission
{
    /// <summary>
    /// Uploads an FEC file to the FEC online submission system at fec.gov.
    /// </summary>
    /// <param name="fecFilePath">Path to the FEC file to be submitted</param>
    /// <param name="notificationEmails">Up to two emails can be provided, split by a semicolon. These emails will receive a copy of the submission results from the FEC</param>
    /// <param name="fecId">The organization's committee ID assigned by the FEC. Ex: "C00363168"</param>
    /// <param name="fecIdPassword">The password used to authenticate against the FEC online submission system.</param>
    /// <returns>An instance of <see cref="FecSubmissionResponse" /> containing details about the submission.</returns>
    Task<FecSubmissionResponse> SubmitFecFile(string fecFilePath, string notificationEmails, string fecId, string fecIdPassword);

    /// <summary>
    /// Periodically check the status of a previously submitted FEC file until the endpoint no longer returns a status of "Processing".
    /// </summary>
    /// <param name="fecSubmissionResponse">The FEC submission response from when the .fec file was first uploaded.</param>
    /// <returns>An instance of <see cref="FecSubmissionResponse" /> returned from polling the status endpoint. This is different
    /// than the one passed in to the method.</returns>
    Task<FecSubmissionResponse> PollForSubmissionStatus(FecSubmissionResponse fecSubmissionResponse);
}
