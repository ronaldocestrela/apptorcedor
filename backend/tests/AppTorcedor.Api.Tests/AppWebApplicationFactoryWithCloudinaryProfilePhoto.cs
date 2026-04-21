using AppTorcedor.Infrastructure.Services.Cloudinary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppTorcedor.Api.Tests;

/// <summary>
/// Uses Cloudinary profile photo storage with a recording in-memory gateway (no real Cloudinary calls).
/// </summary>
public sealed class AppWebApplicationFactoryWithCloudinaryProfilePhoto : AppWebApplicationFactoryWithLegalSeed
{
    public RecordingCloudinaryProfilePhotoGateway CloudinaryGateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ProfilePhotos:Provider", "Cloudinary");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(ICloudinaryAssetGateway));
            services.AddSingleton<ICloudinaryAssetGateway>(CloudinaryGateway);
        });
    }
}
