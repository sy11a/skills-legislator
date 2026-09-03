using System.Text.Json.Nodes;
using Legislator.Engine.Runs;
using Xunit;
using static Legislator.Engine.Tests.Apply.ApplyFixture;

namespace Legislator.Engine.Tests.Runs;

/// <summary>
/// Where the run record lives and what refuses to write it there. The parity twin pins the
/// derived path for one repository; these tests pin the flag that overrides it and the rule
/// that guards the repository itself - a record inside the tree would be picked up by the very
/// audit that reads the tree.
/// </summary>
public sealed class RecordPathTests
{
    [Fact]
    public void Given_an_explicit_record_When_the_path_is_derived_Then_the_flag_wins()
    {
        var fs = Repo();

        Assert.Equal("/elsewhere/run.json", RecordPath.Of(fs, Options, Root, "/elsewhere/run.json"));
    }

    [Fact]
    public void Given_no_flag_When_the_path_is_derived_Then_it_sits_under_the_run_record_directory_in_the_system_temp_dir()
    {
        var fs = Repo();

        var path = RecordPath.Of(fs, Options, Root, null);

        Assert.StartsWith(fs.Path.GetTempPath().TrimEnd('/'), path, StringComparison.Ordinal);
        Assert.Contains($"/{Options.RunRecordDir.Value}/", path, StringComparison.Ordinal);
        Assert.EndsWith(".json", path, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_two_repositories_of_the_same_name_When_the_path_is_derived_Then_the_two_records_do_not_collide()
    {
        var fs = Repo();

        Assert.NotEqual(
            RecordPath.Of(fs, Options, "/one/r", null),
            RecordPath.Of(fs, Options, "/two/r", null));
    }

    [Fact]
    public void Given_a_record_path_inside_the_repository_When_it_is_written_Then_the_run_stops_naming_the_path()
    {
        var fs = Repo();

        var thrown = Assert.Throws<InvalidOperationException>(
            () => RunRecord.Write(fs, Root, $"{Root}/docs/run.json", new JsonObject()));

        Assert.Contains($"{Root}/docs/run.json", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_no_record_at_the_path_When_it_is_read_Then_the_run_stops_saying_apply_has_not_run()
    {
        var fs = Repo();

        var thrown = Assert.Throws<InvalidOperationException>(() => RunRecord.Read(fs, "/tmp/absent.json"));

        Assert.Contains("apply", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Given_a_record_that_does_not_parse_When_it_is_read_Then_the_run_stops_naming_the_file()
    {
        var fs = Repo();
        fs.AddFile("/tmp/broken.json", new System.IO.Abstractions.TestingHelpers.MockFileData("{oops"));

        var thrown = Assert.Throws<InvalidOperationException>(() => RunRecord.Read(fs, "/tmp/broken.json"));

        Assert.Contains("/tmp/broken.json", thrown.Message, StringComparison.Ordinal);
    }
}
