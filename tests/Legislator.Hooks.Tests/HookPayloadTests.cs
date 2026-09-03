using Xunit;

namespace Legislator.Hooks.Tests;

/// <summary>
/// The hook payload as data rather than as a type (the `ManifestFile` precedent): Claude Code's
/// JSON is read as a node and asked questions, because every hook's contract is to DECIDE on a
/// surprising shape, not to trip over it. A typed model would turn "tool_input is a string" into
/// an exception where the Python turns it into an allow.
/// </summary>
public sealed class HookPayloadTests
{
    [Fact]
    public void A_well_formed_payload_carries_its_fields()
    {
        var payload = HookPayload.Parse(
            """{"tool_name": "Bash", "tool_input": {"command": "git status"}, "cwd": "/r", "stop_hook_active": true}""");

        Assert.True(payload.Usable);
        Assert.Equal("Bash", payload.ToolName);
        Assert.Equal("git status", payload.Command);
        Assert.Equal("/r", payload.Cwd);
        Assert.True(payload.StopHookActive);
    }

    [Fact]
    public void The_edited_path_comes_from_either_key()
    {
        Assert.Equal("/a.cs", HookPayload.Parse("""{"tool_input": {"file_path": "/a.cs"}}""").FilePath);
        Assert.Equal("/a.ipynb", HookPayload.Parse("""{"tool_input": {"notebook_path": "/a.ipynb"}}""").FilePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n")]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("\"a string\"")]
    public void Anything_that_is_not_a_json_object_is_unusable(string raw)
    {
        Assert.False(HookPayload.Parse(raw).Usable);
    }

    /// <summary>An unusable payload answers every question with nothing - a hook reads it without a null check of its own.</summary>
    [Fact]
    public void An_unusable_payload_answers_nothing_rather_than_throwing()
    {
        var payload = HookPayload.Parse("not json");

        Assert.Null(payload.ToolName);
        Assert.Null(payload.Command);
        Assert.Null(payload.Cwd);
        Assert.Null(payload.FilePath);
        Assert.False(payload.StopHookActive);
    }

    /// <summary>Fields of the wrong type are absent fields, not faults: the Python asks `isinstance` and moves on.</summary>
    [Fact]
    public void A_field_of_the_wrong_type_reads_as_absent()
    {
        var payload = HookPayload.Parse("""{"tool_name": 7, "tool_input": "nonsense", "cwd": [], "stop_hook_active": "yes"}""");

        Assert.True(payload.Usable);
        Assert.Null(payload.ToolName);
        Assert.Null(payload.Command);
        Assert.Null(payload.Cwd);
        Assert.False(payload.StopHookActive);
    }

    /// <summary>A path that is not a string is not a path - the guard must not resolve `42` against a directory.</summary>
    [Fact]
    public void A_non_string_path_reads_as_absent()
    {
        Assert.Null(HookPayload.Parse("""{"tool_input": {"file_path": 42}}""").FilePath);
    }
}
