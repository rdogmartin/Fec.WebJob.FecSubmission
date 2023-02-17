using System.ServiceModel;

namespace Fec.WebJob.FecSubmission.WebServiceClient.Interfaces;

[ServiceContract(Namespace = "http://service.webload.efo.fec.gov/", ConfigurationName = "Fec.WebJob.FecSubmission.WebServiceClient.Interfaces.IUploadService")]
public interface IUploadService
{
    /// <summary>
    /// Method which takes an upload request and submits it to the FEC Submission Web Service
    /// </summary>
    /// <param name="uploadRequest">Details of what is to be uploaded</param>
    /// <returns>JSON string from the FEC with the results of the submission</returns>
    [OperationContract(Action = "", ReplyAction = "*")]
    [FaultContract(typeof(WebServiceClientException), Action = "", Name = "WebServiceClientException")]
    [XmlSerializerFormat(SupportFaults = true)]
    Task<UploadResponse> UploadAsync(UploadRequest uploadRequest);

    [OperationContractAttribute(Action="", ReplyAction="*")]
    [FaultContractAttribute(typeof(WebServiceClientException), Action="", Name="WebServiceClientException")]
    [XmlSerializerFormatAttribute(SupportFaults=true)]
    Task<StatusResponse> StatusAsync(StatusRequest statusRequest);
}
