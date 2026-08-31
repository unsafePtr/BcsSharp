using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace BcsSharp.Tests.RustParity;

/// <summary>
/// xUnit collection fixture that builds the Rust BCS Docker image and keeps an idle container running for the duration of the test session.
/// Tests retrieve baked-in fixture bytes via <see cref="ReadFixtureAsync(string, CancellationToken)"/>.
/// </summary>
public sealed class RustBcsContainer : IAsyncLifetime
{
    private const string ImageName = "bcssharp-rust-fixtures";
    private IContainer? _container;
    private readonly SemaphoreSlim _startGate = new(1, 1);

    // No-op: the actual image build and container start are lazy, triggered on the first
    // ReadFixtureAsync call. This keeps default test runs (where every parity test is
    // marked Explicit and skipped) free of any Docker work — xUnit still instantiates
    // the collection fixture but nothing happens until a test actually asks for bytes.
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        _startGate.Dispose();
    }

    /// <summary>
    /// Returns the bytes of a fixture file baked into the image at <c>/fixtures/</c>.
    /// Builds the image and starts the container on first call.
    /// </summary>
    public async Task<byte[]> ReadFixtureAsync(string filename, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);
        return await _container!.ReadFileAsync($"/fixtures/{filename}", ct);
    }

    private async Task EnsureStartedAsync(CancellationToken ct)
    {
        if (_container is not null)
        {
            return;
        }

        await _startGate.WaitAsync(ct);
        try
        {
            if (_container is not null)
            {
                return;
            }

            var image = new ImageFromDockerfileBuilder()
                .WithName($"{ImageName}:{FixturesTag()}")
                .WithDockerfile("Dockerfile")
                .WithDockerfileDirectory(DockerfileDirectory())
                .WithCleanUp(false)   // keep the image cached across test sessions
                .Build();
            await image.CreateAsync(ct);

            var container = new ContainerBuilder(image)
                .WithCleanUp(true)
                .Build();
            await container.StartAsync(ct);
            _container = container;
        }
        finally
        {
            _startGate.Release();
        }
    }

    /// <summary>
    /// Tag derived from the fixture sources.
    /// Keying the image name on their content means a change to the Rust code yields a new tag and forces a rebuild — with a fixed tag the cached image is reused and the new fixtures never appear in the container.
    /// </summary>
    private static string FixturesTag()
    {
        var dir = DockerfileDirectory();
        var files = new[] { "Dockerfile", "Cargo.toml", "Cargo.lock" }
            .Select(name => Path.Combine(dir, name))
            .Concat(Directory.EnumerateFiles(Path.Combine(dir, "src"), "*.rs", SearchOption.AllDirectories))
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.Ordinal);

        using var sha = SHA256.Create();
        foreach (var file in files)
        {
            var name = Encoding.UTF8.GetBytes(Path.GetFileName(file));
            sha.TransformBlock(name, 0, name.Length, null, 0);
            var content = File.ReadAllBytes(file);
            sha.TransformBlock(content, 0, content.Length, null, 0);
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!)[..12].ToLowerInvariant();
    }

    /// <summary>
    /// The path of the Dockerfile directory on the host, anchored to this source file's compile-time location (works regardless of the test runner's CWD).
    /// </summary>
    private static string DockerfileDirectory([CallerFilePath] string sourceFile = "")
    {
        var rustParityDir = Path.GetDirectoryName(sourceFile)!;
        var testsDir = Path.GetDirectoryName(rustParityDir)!;
        return Path.Combine(testsDir, "Fixtures", "rust-bcs");
    }
}

/// <summary>
/// xUnit collection definition — tests that need the Rust container live in this collection so the fixture is constructed exactly once per test session.
/// </summary>
[CollectionDefinition(nameof(RustBcsContainerCollection))]
public class RustBcsContainerCollection : ICollectionFixture<RustBcsContainer>;
