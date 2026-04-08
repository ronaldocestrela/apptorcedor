using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SocioTorcedor.Api.Middleware;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Api.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    private sealed class BrokenRule : IBusinessRule
    {
        public string Message => "Rule broken";
        public bool IsBroken() => true;
    }

    [Fact]
    public async Task InvokeAsync_BusinessRuleValidationException_Returns400ProblemJson()
    {
        var mw = new ExceptionHandlingMiddleware(
            _ => throw new BusinessRuleValidationException(new BrokenRule()),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await mw.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Contain("application/problem+json");

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Business rule violation");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("Rule broken");
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500ProblemJson()
    {
        var mw = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await mw.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Server error");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var nextCalled = false;
        var mw = new ExceptionHandlingMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();

        await mw.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
