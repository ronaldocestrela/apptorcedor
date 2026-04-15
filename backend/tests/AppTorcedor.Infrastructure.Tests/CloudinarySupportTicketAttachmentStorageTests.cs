using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using AppTorcedor.Infrastructure.Services.Support;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class CloudinarySupportTicketAttachmentStorageTests
{
    [Fact]
    public async Task SaveAsync_uses_image_resource_for_image_content_type()
    {
        var gateway = new FakeCloudinaryGateway
        {
            NextUploadResult = new CloudinaryUploadResult("https://cdn.example/att.png"),
        };
        var sut = new CloudinarySupportTicketAttachmentStorage(
            gateway,
            Microsoft.Extensions.Options.Options.Create(
                new SupportTicketAttachmentStorageOptions
                {
                    MaxBytesPerFile = 1024,
                    Cloudinary = new SupportAttachmentCloudinaryOptions { Folder = "support-attachments" },
                }));

        var key = await sut.SaveAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            [1, 2, 3],
            "dot.png",
            "image/png");

        Assert.False(string.IsNullOrWhiteSpace(key));
        Assert.Single(gateway.UploadCalls);
        var call = gateway.UploadCalls[0];
        Assert.Equal(CloudinaryAssetResourceType.Image, call.ResourceType);
        Assert.False(call.Overwrite);
    }

    [Fact]
    public async Task SaveAsync_uses_raw_resource_for_pdf()
    {
        var gateway = new FakeCloudinaryGateway
        {
            NextUploadResult = new CloudinaryUploadResult("https://cdn.example/doc.pdf"),
        };
        var sut = new CloudinarySupportTicketAttachmentStorage(
            gateway,
            Microsoft.Extensions.Options.Options.Create(new SupportTicketAttachmentStorageOptions { MaxBytesPerFile = 1024 }));

        var key = await sut.SaveAsync(Guid.NewGuid(), Guid.NewGuid(), [1, 2, 3], "doc.pdf", "application/pdf");

        Assert.False(string.IsNullOrWhiteSpace(key));
        Assert.Single(gateway.UploadCalls);
        Assert.Equal(CloudinaryAssetResourceType.Raw, gateway.UploadCalls[0].ResourceType);
    }

    [Fact]
    public async Task OpenReadAsync_uses_gateway_url_from_storage_key()
    {
        var gateway = new FakeCloudinaryGateway();
        var sut = new CloudinarySupportTicketAttachmentStorage(gateway, Microsoft.Extensions.Options.Options.Create(new SupportTicketAttachmentStorageOptions()));

        var stream = await sut.OpenReadAsync("cloudinary|image|support-attachments/id|https%3A%2F%2Fcdn.example%2Fx.png");

        Assert.NotNull(stream);
        Assert.Equal("https://cdn.example/x.png", gateway.LastOpenReadUrl);
    }

    [Fact]
    public async Task DeleteAsync_deletes_using_storage_key_parts()
    {
        var gateway = new FakeCloudinaryGateway();
        var sut = new CloudinarySupportTicketAttachmentStorage(gateway, Microsoft.Extensions.Options.Options.Create(new SupportTicketAttachmentStorageOptions()));

        await sut.DeleteAsync("cloudinary|raw|support-attachments/id|https%3A%2F%2Fcdn.example%2Fx.pdf");

        Assert.Single(gateway.DeleteCalls);
        var call = gateway.DeleteCalls[0];
        Assert.Equal("support-attachments/id", call.PublicId);
        Assert.Equal(CloudinaryAssetResourceType.Raw, call.ResourceType);
    }

    private sealed class FakeCloudinaryGateway : ICloudinaryAssetGateway
    {
        public CloudinaryUploadResult? NextUploadResult { get; set; }
        public List<CloudinaryUploadRequest> UploadCalls { get; } = [];
        public List<CloudinaryDeleteRequest> DeleteCalls { get; } = [];
        public string? LastOpenReadUrl { get; private set; }

        public Task<CloudinaryUploadResult?> UploadAsync(CloudinaryUploadRequest request, CancellationToken cancellationToken = default)
        {
            UploadCalls.Add(request);
            return Task.FromResult(NextUploadResult);
        }

        public Task<bool> DeleteAsync(CloudinaryDeleteRequest request, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(request);
            return Task.FromResult(true);
        }

        public Task<Stream?> OpenReadAsync(string url, CancellationToken cancellationToken = default)
        {
            LastOpenReadUrl = url;
            return Task.FromResult<Stream?>(new MemoryStream([1]));
        }
    }
}
