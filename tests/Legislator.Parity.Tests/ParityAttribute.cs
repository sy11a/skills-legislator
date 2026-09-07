namespace Legislator.Parity.Tests;

/// <summary>
/// Names the parity-ruler assertion a .NET test twins (R-8206): the ruler
/// (<c>engine</c> or <c>hooks</c>) and the label exactly as
/// <c>evals/parity_labels.py</c> prints it. A Python job may be removed only
/// once every label it covers carries one of these.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ParityAttribute(string ruler, string label) : Attribute
{
    public string Ruler { get; } = ruler;

    public string Label { get; } = label;
}
