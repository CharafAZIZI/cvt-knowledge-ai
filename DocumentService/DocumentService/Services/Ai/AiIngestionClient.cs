using System.Net.Http.Headers;

namespace DocumentService.Services.Ai;

public class AiIngestionClient : IAiIngestionClient
{
    private readonly HttpClient _httpClient;

    public AiIngestionClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task NotifyIngestionAsync(
        int documentId,
        string title,
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(documentId.ToString()), "document_id" },
            { new StringContent(title), "title" },
            { new StringContent(fileName), "file_name" }
        };

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType)
                    ? "application/octet-stream"
                    : contentType);

        content.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync("/ingest", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task NotifyDeletionAsync(
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/ingest/{documentId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
