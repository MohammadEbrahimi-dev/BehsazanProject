namespace Behsazan.Presentation.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly TokenAccessor _tokenAccessor;

    public AuthHeaderHandler(TokenAccessor tokenAccessor)
    {
        _tokenAccessor = tokenAccessor;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        if (_tokenAccessor.HasToken)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenAccessor.CurrentToken);
        }
        return base.Send(request, cancellationToken);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        if (_tokenAccessor.HasToken)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenAccessor.CurrentToken);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
