using System.IO.Abstractions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Legislator.Core.Options;

/// <summary>
/// Reads one YAML configuration layer as flat key → raw string: top-level scalars, and sequences of
/// scalars joined by <see cref="LegislatorOptions.ListSeparator"/>. Anything nested, or a document that
/// is not a mapping, is an <see cref="OptionsError"/> of the given layer. Driven by the event stream -
/// no object deserialization, no reflection (AOT-first, C-04).
/// </summary>
public static class YamlLayerReader
{
    public static IReadOnlyDictionary<string, string> Read(IFileSystem fs, string path, OptionsLayer layer)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new List<OptionsError>();
        try
        {
            using var text = new StringReader(fs.File.ReadAllText(path));
            var parser = new Parser(text);
            parser.Consume<StreamStart>();
            if (!parser.TryConsume<StreamEnd>(out _))
            {
                parser.Consume<DocumentStart>();
                if (parser.TryConsume<MappingStart>(out _))
                {
                    ReadMapping(parser, layer, values, errors);
                }
                else
                {
                    errors.Add(new(layer, string.Empty, "the document is not a mapping of key: value"));
                }
            }
        }
        catch (YamlException ex)
        {
            errors.Add(new(layer, string.Empty, $"not valid YAML - {ex.Message}"));
        }

        return errors.Count == 0 ? values : throw new OptionsException(errors);
    }

    private static void ReadMapping(Parser parser, OptionsLayer layer, Dictionary<string, string> values, List<OptionsError> errors)
    {
        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>().Value;
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                values[key] = scalar.Value;
            }
            else if (parser.TryConsume<SequenceStart>(out _))
            {
                values[key] = string.Join(LegislatorOptions.ListSeparator, ReadScalarSequence(parser, layer, key, errors));
            }
            else
            {
                errors.Add(new(layer, key, "nested value not allowed - a value is a scalar or a sequence of scalars"));
                parser.SkipThisAndNestedEvents();
            }
        }
    }

    private static List<string> ReadScalarSequence(Parser parser, OptionsLayer layer, string key, List<OptionsError> errors)
    {
        var items = new List<string>();
        while (!parser.TryConsume<SequenceEnd>(out _))
        {
            if (parser.TryConsume<Scalar>(out var item))
            {
                items.Add(item.Value);
            }
            else
            {
                errors.Add(new(layer, key, "nested value not allowed - a sequence holds scalars only"));
                parser.SkipThisAndNestedEvents();
            }
        }

        return items;
    }
}
