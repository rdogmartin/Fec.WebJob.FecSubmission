using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using Fec.WebJob.FecSubmission.MTOM;
using Fec.WebJob.FecSubmission.WebServiceClient.Interfaces;

namespace Fec.WebJob.FecSubmission.WebServiceClient;

public partial class UploadServiceClient : ClientBase<IUploadService>, IUploadService
{
    public UploadServiceClient() : base(GetDefaultBinding(), GetDefaultEndpointAddress())
    {
        Endpoint.Name = EndpointConfiguration.UploadServiceImplPort.ToString();
        ConfigureEndpoint(Endpoint);
    }

    /// <summary>
    /// Configure the Endpoint with MTOM support
    /// </summary>
    /// <param name="serviceEndpoint">Endpoint to be updated with MTOM support</param>
    static void ConfigureEndpoint(ServiceEndpoint serviceEndpoint)
    {
        Type messageEncodingBindingElementType = typeof(MessageEncodingBindingElement);
        BindingElementCollection bindingElementCollection = serviceEndpoint.Binding.CreateBindingElements();

        IEnumerable<BindingElement> bindingElementsWithoutEncodingElement = bindingElementCollection.Where(item => !messageEncodingBindingElementType.IsAssignableFrom(item.GetType()));
        MessageEncodingBindingElement existingBindingElement = (MessageEncodingBindingElement)bindingElementCollection.Where(item => messageEncodingBindingElementType.IsAssignableFrom(item.GetType())).First();

        // Encoding is before transport, so we prepend the MTOM message encoding binding element
        // https://docs.microsoft.com/en-us/dotnet/framework/wcf/extending/custom-bindings
        serviceEndpoint.Binding = new CustomBinding(bindingElementsWithoutEncodingElement.Prepend(new MtomMessageEncoderBindingElement(existingBindingElement)))
        {
            SendTimeout = new TimeSpan(0, 30, 0) // 30 minutes
        };
    }

    public Task<UploadResponse> UploadAsync(string arg0, byte[] arg1)
    {
        return ((IUploadService)this).UploadAsync(new UploadRequest() { arg0 = arg0, arg1 = arg1 });
    }

    /// <summary>
    /// Calls WebService via Channel (ClientBase)
    /// </summary>
    /// <param name="request">Details of the upload request</param>
    /// <returns>An UploadResponse object containing the answer from the FEC Web Service</returns>
    Task<UploadResponse> IUploadService.UploadAsync(UploadRequest request)
    {
        return Channel.UploadAsync(request);
    }

    public Task<StatusResponse> StatusAsync(string submissionId)
    {
        return ((IUploadService)this).StatusAsync(new StatusRequest() { arg0 = submissionId });
    }

    Task<StatusResponse> IUploadService.StatusAsync(StatusRequest request)
    {
        return Channel.StatusAsync(request);
    }

    private static Binding GetDefaultBinding()
    {
        return new BasicHttpBinding()
        {
            MaxBufferSize = int.MaxValue,
            ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
            MaxReceivedMessageSize = int.MaxValue,
            AllowCookies = true,
            Security = new BasicHttpSecurity { Mode = BasicHttpSecurityMode.Transport }
        };
    }

    private static EndpointAddress GetDefaultEndpointAddress() => new("https://efoservices.fec.gov/webload/services/upload");

    public enum EndpointConfiguration { UploadServiceImplPort }
}
