using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Harmonie.Application.Features.Auth.Register;
using Harmonie.Application.Features.Uploads.UploadFile;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Harmonie.API.IntegrationTests;

public sealed class UploadsLocalFileSystemE2ETests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _tempDir;

    public UploadsLocalFileSystemE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _tempDir = Path.Combine(Path.GetTempPath(), $"harmonie-uploads-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task UploadFile_WithLocalFileSystemProvider_ShouldWriteFileToDisk()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var user = await RegisterAsync(client);
        using var multipart = CreateMultipartContent("hello.txt", "text/plain", "hello from filesystem");

        var response = await SendAuthorizedMultipartAsync(client, "/api/uploads", multipart, user.AccessToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(payload);
        Assert.Equal("hello.txt", payload!.Filename);
        Assert.Equal("text/plain", payload.ContentType);
        Assert.StartsWith("/api/files/", payload.Url);

        // Verify file exists on disk by downloading through the authorized endpoint
        var downloadResponse = await SendAuthorizedGetAsync(client, payload.Url, user.AccessToken);
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

        var diskContent = await downloadResponse.Content.ReadAsStringAsync();
        Assert.Equal("hello from filesystem", diskContent);
    }

    [Fact]
    public async Task DownloadFile_WithLocalFileSystemProvider_ShouldServeFileViaAuthenticatedEndpoint()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var user = await RegisterAsync(client);
        using var multipart = CreateMultipartContent("image.png", "image/png", "fake-png-content");

        var uploadResponse = await SendAuthorizedMultipartAsync(client, "/api/uploads", multipart, user.AccessToken);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var payload = await uploadResponse.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(payload);

        var fileResponse = await SendAuthorizedGetAsync(client, payload!.Url, user.AccessToken);

        Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);
        Assert.Equal("image/png", fileResponse.Content.Headers.ContentType?.MediaType);

        var fileContent = await fileResponse.Content.ReadAsStringAsync();
        Assert.Equal("fake-png-content", fileContent);
    }

    [Fact]
    public async Task DownloadFile_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var user = await RegisterAsync(client);
        using var multipart = CreateMultipartContent("secret.txt", "text/plain", "secret content");

        var uploadResponse = await SendAuthorizedMultipartAsync(client, "/api/uploads", multipart, user.AccessToken);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var payload = await uploadResponse.Content.ReadFromJsonAsync<UploadFileResponse>();
        Assert.NotNull(payload);

        var fileResponse = await client.GetAsync(payload!.Url);

        Assert.Equal(HttpStatusCode.Unauthorized, fileResponse.StatusCode);
    }

    [Fact]
    public async Task UploadFile_WithLocalFileSystemProvider_ShouldReturnCorrectMetadata()
    {
        using var factory = BuildFactory();
        using var client = factory.CreateClient();

        var user = await RegisterAsync(client);
        var bytes = System.Text.Encoding.UTF8.GetBytes("test content");
        using var multipart = CreateMultipartContent("doc.txt", "text/plain", "test content");

        var response = await SendAuthorizedMultipartAsync(client, "/api/uploads", multipart, user.AccessToken);
        var payload = await response.Content.ReadFromJsonAsync<UploadFileResponse>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.FileId));
        Assert.Equal("doc.txt", payload.Filename);
        Assert.Equal("text/plain", payload.ContentType);
        Assert.Equal(bytes.Length, payload.SizeBytes);
        Assert.StartsWith("/api/files/", payload.Url);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private WebApplicationFactory<Program> BuildFactory()
        => _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ObjectStorage:LocalBasePath"] = _tempDir,
                    ["ObjectStorage:LocalBaseUrl"] = "http://localhost/files"
                });
            });
        });

    private static async Task<RegisterResponse> RegisterAsync(HttpClient client)
    {
        var request = new RegisterRequest(
            Email: $"test{Guid.NewGuid():N}@harmonie.chat",
            Username: $"user{Guid.NewGuid():N}"[..20],
            Password: "Test123!@#");

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(payload);
        return payload!;
    }

    private static MultipartFormDataContent CreateMultipartContent(
        string fileName,
        string contentType,
        string content)
    {
        var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(fileContent, "file", fileName);
        return multipart;
    }

    private static async Task<HttpResponseMessage> SendAuthorizedMultipartAsync(
        HttpClient client,
        string uri,
        MultipartFormDataContent content,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendAuthorizedGetAsync(
        HttpClient client,
        string uri,
        string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }
}
