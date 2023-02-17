using System.ServiceModel;
using System.Text.Json;
using System.Xml.Schema;
using System.Xml.Serialization;
using Fec.WebJobs.Core;

namespace Fec.WebJob.FecSubmission.WebServiceClient;

[MessageContract(WrapperName = "statusResponse", WrapperNamespace = "http://service.webload.efo.fec.gov/", IsWrapped = true)]
public class StatusResponse
{
    [MessageBodyMember(Namespace = "http://service.webload.efo.fec.gov/", Order = 0)]
    [XmlElement(Form = XmlSchemaForm.Unqualified)]
    public string @return = string.Empty;

    //Deserialize @return (JSON) into a FecSubmissionResponse record
    public FecSubmissionResponse? FecSubmissionResponse => JsonSerializer.Deserialize<FecSubmissionResponse>(@return);
        
    public StatusResponse()
    {
    }
        
    public StatusResponse(string @return)
    {
        this.@return = @return;
    }
}
