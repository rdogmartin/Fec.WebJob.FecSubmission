using System.Text.Json;
using Fec.WebJob.FecSubmission.Data;
using Fec.WebJob.FecSubmission.Interfaces;
using Fec.WebJob.FecSubmission.WebServiceClient;
using Fec.WebJobs.Core;

namespace Fec.WebJob.FecSubmission;


/// <inheritdoc />
public class FecSubmission : IFecSubmission
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FormBaseConnector> _logger;

    public FecSubmission(IConfiguration configuration, ILogger<FormBaseConnector> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<FecSubmissionResponse> SubmitFecFile(string fecFilePath, string notificationEmails, string fecId, string fecIdPassword)
    {
        //Read file into a byte array
        byte[] bytes = await File.ReadAllBytesAsync(path: fecFilePath);

        //Up to two emails can be provided, split by a semicolon. If only one email is present, it will be in notificationEmails[0].
        string[] notificationEmailsSplit = notificationEmails.Split(';');

        //Create record class instance loaded with our settings
        FecSubmissionDetails fecSubmissionDetails = new(
            CommitteeId: fecId,
            Password: fecIdPassword,
            ApiKey: _configuration["FecSubmission:APIKey"],
            Email1: notificationEmailsSplit[0],
            Email2: notificationEmailsSplit.Length > 1 ? notificationEmailsSplit[1] : "",
            AgencyId: _configuration["FecSubmission:AgencyId"],
            AmendmentId: "");

        //Call Web Service passing in json settings and the fec file as a byte array.
        _logger.LogInformation($"HTTP payload containing .fec file being sent to fec.gov. arg0: {fecSubmissionDetails with {Password = "******"}}; arg1.length: {bytes.Length} (arg1 is the base64 representation of the .fec file)");
        UploadResponse uploadResponse = await new UploadServiceClient().UploadAsync(JsonSerializer.Serialize(fecSubmissionDetails), bytes);

        return uploadResponse.FecSubmissionResponse ?? new FecSubmissionResponse("ERROR", string.Empty,
            "The FEC web service returned with an empty HTTP response body, so the status of the submission is unknown.",
            string.Empty, false);
    }

    public async Task<FecSubmissionResponse> PollForSubmissionStatus(FecSubmissionResponse fecSubmissionResponse)
    {
        const int statusCheckTimeoutHours = 8; // Don't poll for longer than 8 hours
        var fecStatusResponse = new FecSubmissionResponse("ERROR", string.Empty, "The call to the status endpoint was cancelled before the first check was made.", fecSubmissionResponse.SubmissionId, false);
        try
        {
            // To prevent an infinite polling situation, we'll set a timeout of 8 hours.
            CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromHours(statusCheckTimeoutHours));

            // Set up a timer to execute every five seconds
            using PeriodicTimer periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(5000));

            // Run the PeriodicTimer continually until cancelled
            while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
            {
                fecStatusResponse = await GetFecSubmissionStatus(fecSubmissionResponse.SubmissionId);

                // If the submission is anything other than Processing, cancel the timer
                if (!fecStatusResponse.Status.Equals("Processing", StringComparison.OrdinalIgnoreCase))
                {
                    cancellationTokenSource.Cancel();
                }
            }

            // We get here when the status endpoint returns a status of anything other than "Processing".
            return fecStatusResponse;
        }
        catch (TaskCanceledException)
        {
            // We get here when the PeriodicTimer is cancelled in the previous while loop. This is a valid scenario so just absorb the exception.
        }
        catch (OperationCanceledException)
        {
            // We get here when the PeriodicTimer hits the timeout we defined in the CancellationTokenSource.
            fecStatusResponse = fecStatusResponse with { Message = $"Giving up checking the status of the FEC submission. fec.gov continued to report a status of Processing after {statusCheckTimeoutHours} hours." };
        }
        catch (Exception ex)
        {
            fecStatusResponse = fecStatusResponse with { Status = "ERROR", Message = "An unhandled exception occurred while polling the status of the FEC submission." };
            _logger.LogError(ex, $"An error occurred while polling the status of the submission. {fecStatusResponse}");
        }

        return fecStatusResponse;
    }

    private async Task<FecSubmissionResponse> GetFecSubmissionStatus(string submissionId)
    {
        _logger.LogInformation($"Calling fec.gov web service to request status of FEC submission #{submissionId}");
        StatusResponse statusResponse = await new UploadServiceClient().StatusAsync(submissionId);
        _logger.LogInformation($"fec.gov reported this status for FEC submission #{submissionId}: {statusResponse.FecSubmissionResponse}");

        return statusResponse.FecSubmissionResponse ?? new FecSubmissionResponse("ERROR", string.Empty,
            "The FEC web service returned with an empty HTTP response body, so the status of the submission is unknown.",
            string.Empty, false);
    }
}
