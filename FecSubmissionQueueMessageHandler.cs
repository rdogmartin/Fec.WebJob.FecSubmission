using Fec.Core.Enums;
using Fec.Core.Interfaces;
using Fec.WebJob.FecSubmission.Interfaces;
using Fec.WebJobs.Core;
using Fec.WebJobs.Core.Config;
using Fec.WebJobs.Core.Data;
using Microsoft.Azure.WebJobs;

namespace Fec.WebJob.FecSubmission;

public class FecSubmissionQueueMessageHandler : QueueMessageHandler
{
    private readonly IFecSubmission _fecSubmission;
    private readonly IFileSystemService _fileSystemService;

    public FecSubmissionQueueMessageHandler(IFormBaseConnector formBaseConnector, IConfiguration configuration,
        ILogger<QueueMessageHandler> logger, IFecSubmission fecSubmission, IFileSystemService fileSystemService) : base(formBaseConnector, configuration, logger)
    {
        _fecSubmission = fecSubmission;
        _fileSystemService = fileSystemService;
    }

    /// <summary>
    /// This function will get triggered/executed when a new message is written to a
    /// Storage Queue specified by FecSubmission:QueueName
    /// </summary>
    /// <param name="submissionQueueMessage">Queue Message containing information about the FEC File and record to submit to the FEC</param>
    /// <param name="cloudStream">File Stream used to download the fec File to submit to the FEC</param>
    [FunctionName("ProcessFecSubmissionQueueMessage")]
    public async Task ProcessFecSubmissionQueueMessage([QueueTrigger("%FecSubmission:QueueName%")] SubmissionQueueMessage submissionQueueMessage,
        [Blob("%FecDataFilesContainerName%/{FileName}", FileAccess.Read)] Stream cloudStream)
    {
        _logger.LogInformation($"Webjob trigger: Submit FEC Form #{submissionQueueMessage.FormId} to the FEC. Processing queue message: {submissionQueueMessage with {FecIdPassword = "******"}}");

        //Provide a unique filename that we can save the file to locally
        string filePath = Path.Combine(Constants.RootPath, $"submission-{submissionQueueMessage.FormId}-{Guid.NewGuid()}.fec");

        try
        {
            //Update the status
            await _formBaseConnector.UpdateStatus(submissionQueueMessage.FormId, FecFormFilingStatus.FilingInProgress);

            //Use FileSystemService to copy the Azure stream down to a local file
            _logger.LogInformation($"Copying .fec file for form #{submissionQueueMessage.FormId} to local file {filePath}");
            await _fileSystemService.CopyToFileAsync(cloudStream, filePath);

            //Submit the FEC file and make sure there is a valid response
            _logger.LogInformation($"Beginning upload of .fec file for form #{submissionQueueMessage.FormId} to http://service.webload.efo.fec.gov");

            var fecSubmissionResponse = await _fecSubmission.SubmitFecFile(filePath, submissionQueueMessage.NotificationEmails, submissionQueueMessage.FecId, submissionQueueMessage.FecIdPassword);

            //Update the database with the Fec Response
            _logger.LogInformation($"Uploading to FEC is complete. fec.gov returned this data: {fecSubmissionResponse}");

            // If status is PROCESSING, then start polling the status API until it's done.
            if (fecSubmissionResponse.Status.Equals("Processing", StringComparison.OrdinalIgnoreCase))
            {
                const string fecValidatingMsg = "Please check in few minutes for status update";
                if (fecSubmissionResponse.Message.Equals(fecValidatingMsg, StringComparison.OrdinalIgnoreCase))
                {
                    // Save a better message than what the FEC provided.
                    fecSubmissionResponse = fecSubmissionResponse with { Message = "The FEC has received the report and is currently processing it. In most cases this completes in a few minutes but can take up to four hours during peak times. We'll periodically check with the FEC and update the status when it is done." };
                }

                await _formBaseConnector.UpdateFecSubmissionInfo(submissionQueueMessage.FormId, fecSubmissionResponse);

                _logger.LogInformation($"The FEC is processing the submission for form #{submissionQueueMessage.FormId}. Poll the status API until it is done.");
                var fecStatusResponse = await _fecSubmission.PollForSubmissionStatus(fecSubmissionResponse);

                if (fecStatusResponse.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) && fecStatusResponse.Message == fecValidatingMsg)
                {
                    // The message returned from the FEC is misleading, so replace it with the same message that would have been
                    // returned when using the async submission method (i.e. wait=true).
                    fecStatusResponse = fecStatusResponse with
                    {
                        Message = $"{fecStatusResponse.Status} {fecStatusResponse.ReportId}"
                    };
                }
                _logger.LogInformation($"The FEC has indicated it is done processing the file for form #{submissionQueueMessage.FormId}. fec.gov returned this data: {fecStatusResponse}");
                await _formBaseConnector.UpdateFecSubmissionInfo(submissionQueueMessage.FormId, fecStatusResponse);
            }
            else
            {
                await _formBaseConnector.UpdateFecSubmissionInfo(submissionQueueMessage.FormId, fecSubmissionResponse);
            }
        }

        catch (Exception ex)
        {
            //Log the error
            _logger.LogError(ex, $"Error while submitting FEC File. {submissionQueueMessage with { FecIdPassword = "******" }}");

            //Update the status
            await _formBaseConnector.UpdateStatus(submissionQueueMessage.FormId, FecFormFilingStatus.FilingFailed);
            await _formBaseConnector.UpdateFecSubmissionInfo(submissionQueueMessage.FormId, new FecSubmissionResponse("ERROR", string.Empty, $"{ex.GetType()}: {ex.Message}", string.Empty, false));
        }
        finally
        {
            //Remove the local fec file
            _fileSystemService.DeleteFiles(filePath);
        }
    }
}
