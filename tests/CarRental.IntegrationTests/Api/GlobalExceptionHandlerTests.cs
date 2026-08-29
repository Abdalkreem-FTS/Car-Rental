using CarRental.Api.Extensions;
using CarRental.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace CarRental.IntegrationTests.Api;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Handle_WhenTheClientHungUp_WritesNothing()
    {
        var problems = new RecordingProblemDetailsService();
        var context = AbortedContext();

        var handled = await Handler(problems).TryHandleAsync(
            context,
            new OperationCanceledException(context.RequestAborted),
            context.RequestAborted);

        handled.ShouldBeTrue();
        problems.Wrote.ShouldBeFalse("a client that hung up cannot be answered, and it is not a fault of ours");
    }

    [Fact]
    public async Task Handle_WhenTheRequestIsAliveAndSomethingFailed_StillWrites()
    {
        var problems = new RecordingProblemDetailsService();
        var context = new DefaultHttpContext();

        var handled = await Handler(problems).TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        handled.ShouldBeTrue();
        problems.Wrote.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problems.ErrorCode.ShouldBe(RequestErrors.Unexpected.Code);
    }

    [Fact]
    public async Task Handle_WhenCancelledButTheClientIsStillThere_IsTreatedAsAFailure()
    {
        var problems = new RecordingProblemDetailsService();

        var handled = await Handler(problems).TryHandleAsync(
            new DefaultHttpContext(),
            new OperationCanceledException("a timeout of our own"),
            CancellationToken.None);

        handled.ShouldBeTrue();
        problems.Wrote.ShouldBeTrue("only the client hanging up is silent");
    }

    private static GlobalExceptionHandler Handler(IProblemDetailsService problems) =>
        new(problems, new StubEnvironment(), NullLogger<GlobalExceptionHandler>.Instance);

    private static DefaultHttpContext AbortedContext()
    {
        var context = new DefaultHttpContext();
        var aborted = new CancellationTokenSource();
        aborted.Cancel();

        context.Features.Set<IHttpRequestLifetimeFeature>(new AbortedLifetime(aborted.Token));

        return context;
    }

    private sealed class AbortedLifetime(CancellationToken token) : IHttpRequestLifetimeFeature
    {
        public CancellationToken RequestAborted { get; set; } = token;

        public void Abort()
        {
        }
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public bool Wrote { get; private set; }

        public string? ErrorCode { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Record(context);

            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Record(context);

            return ValueTask.FromResult(true);
        }

        private void Record(ProblemDetailsContext context)
        {
            Wrote = true;
            ErrorCode = context.ProblemDetails.Extensions.TryGetValue("errorCode", out var code) ? code?.ToString() : null;
        }
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "CarRental.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
