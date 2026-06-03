namespace API.LoadTests.Infrastructure.Http;

public sealed class QueryApiClient
{
    private readonly HttpClient _httpClient;

    public QueryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        _httpClient.GetAsync("/api/workflows/dashboard", cancellationToken);

    public Task<HttpResponseMessage> GetWorkflowDetailsAsync(int workflowId, CancellationToken cancellationToken = default) =>
        _httpClient.GetAsync($"/api/workflows/{workflowId}/dashboard", cancellationToken);
}
