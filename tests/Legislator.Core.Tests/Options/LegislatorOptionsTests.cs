using Legislator.Core.Options;
using Xunit;

namespace Legislator.Core.Tests.Options;

public sealed class LegislatorOptionsTests
{
    // Reflection is test-side only (IsAotCompatible=false here); Core's KeyMap and
    // Enumerate stay hand-written (C-03).
    [Fact]
    public void Every_member_is_reachable_by_key()
    {
        var optionMembers = typeof(LegislatorOptions).GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition() == typeof(OptionValue<>))
            .Select(p => p.Name)
            .ToHashSet();

        Assert.Equal(optionMembers, LegislatorOptions.KeyMap.Values.ToHashSet());
    }

    [Fact]
    public void Defaults_carry_the_Defaults_source()
    {
        var options = new LegislatorOptions();

        var entries = options.Enumerate().ToList();

        Assert.Equal(LegislatorOptions.KeyMap.Keys.ToHashSet(), entries.Select(e => e.Key).ToHashSet());
        Assert.All(entries, e => Assert.Equal(OptionsLayer.Defaults, e.Source));
    }

    [Fact]
    public void Enumerate_renders_lists_comma_joined()
    {
        var options = new LegislatorOptions();

        Assert.Contains(options.Enumerate(),
            e => e.Key == "human_class_docs" && e.Value == "glossary.md,log.md");
    }

    [Fact]
    public void Integer_keys_census_matches_the_int_members()
    {
        var intKeys = typeof(LegislatorOptions).GetProperties()
            .Where(p => p.PropertyType == typeof(OptionValue<int>))
            .Select(p => LegislatorOptions.KeyMap.Single(kv => kv.Value == p.Name).Key)
            .ToHashSet();

        Assert.Equal(intKeys, LegislatorOptions.IntegerKeys.ToHashSet());
    }
}
