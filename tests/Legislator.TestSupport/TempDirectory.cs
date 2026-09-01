namespace Legislator.TestSupport;

/// <summary>
/// A real directory on a real disk, removed when the test ends. Almost every test runs on
/// <c>MockFileSystem</c>; this is for the cases where the fake cannot express the behaviour
/// under test - a document the process is not allowed to read is one, because the fake returns
/// empty text where <c>System.IO</c> throws.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "legislator-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>The directory itself, with forward slashes - the form the engine joins and prints.</summary>
    public string Path { get; }

    /// <summary>Writes a file, creating whatever directories the relative path names.</summary>
    public TempDirectory With(string relative, string text)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
        return this;
    }

    /// <summary>
    /// Creates a directory where a document is expected. The bundle walk yields it exactly as
    /// the Python's <c>rglob</c> does, and reading it throws - which is what makes it the
    /// portable stand-in for the ruler's <c>chmod 0o000</c> fixture.
    /// </summary>
    public TempDirectory WithUnreadableDocument(string relative)
    {
        Directory.CreateDirectory(System.IO.Path.Combine(Path, relative));
        return this;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
