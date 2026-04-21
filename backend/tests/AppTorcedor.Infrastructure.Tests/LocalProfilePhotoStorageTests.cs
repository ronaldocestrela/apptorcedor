using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Account;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class LocalProfilePhotoStorageTests
{
    [Fact]
    public void ShouldDeletePreviousAfterReplace_different_paths_returns_true()
    {
        var env = new FakeHostEnvironment();
        var sut = new LocalProfilePhotoStorage(
            env,
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions { MaxBytes = 1024 }));

        const string prev = "/uploads/profile-photos/abc/old.jpg";
        const string next = "/uploads/profile-photos/abc/new.jpg";
        Assert.True(sut.ShouldDeletePreviousAfterReplace(prev, next));
    }

    [Fact]
    public void ShouldDeletePreviousAfterReplace_same_path_returns_false()
    {
        var env = new FakeHostEnvironment();
        var sut = new LocalProfilePhotoStorage(
            env,
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions { MaxBytes = 1024 }));

        const string u = "/uploads/profile-photos/abc/one.jpg";
        Assert.False(sut.ShouldDeletePreviousAfterReplace(u, u));
    }

    [Fact]
    public void ShouldDeletePreviousAfterReplace_empty_previous_returns_false()
    {
        var env = new FakeHostEnvironment();
        var sut = new LocalProfilePhotoStorage(
            env,
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions { MaxBytes = 1024 }));

        Assert.False(sut.ShouldDeletePreviousAfterReplace(null, "/uploads/x.jpg"));
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Path.GetTempPath());
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
    }
}
