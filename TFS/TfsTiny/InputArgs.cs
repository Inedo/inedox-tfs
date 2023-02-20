using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Inedo.TFS
{
    internal sealed class InputArgs
    {
        public InputArgs(ImmutableArray<string> positional, ImmutableDictionary<string, string> named)
        {
            this.Positional = positional;
            this.Named = named;
        }

        public ImmutableArray<string> Positional { get; set; }
        public ImmutableDictionary<string, string> Named { get; set; }

        public static InputArgs Parse(string[] args)
        {
            var positional = new List<string>();
            var options = ImmutableDictionary<string, string>.Empty.ToBuilder();

            foreach (var a in args)
            {
                if (!a.StartsWith("-"))
                {
                    positional.Add(a);
                    continue;
                }

                try
                {
                    var trimmed = a.TrimStart('-');
                    int equalsIndex = trimmed.IndexOf('=');
                    if (equalsIndex < 0)
                    {
                        options.Add(trimmed.ToString(), string.Empty);
                        continue;
                    }

                    var name = trimmed.Substring(0, equalsIndex);
                    var value = equalsIndex < trimmed.Length - 1 ? trimmed.Substring(equalsIndex + 1) : default;
                    options.Add(name.ToString(), AH.NullIf(value.ToString().Trim(), string.Empty));
                }
                catch (ArgumentException)
                {
                    throw new ConsoleException($"Duplicate argument: {a}");
                }
            }

            return new InputArgs(positional.ToImmutableArray(), options.ToImmutable());
        }
    }

    internal static class ImmutableHelpers
    {
        public static string TryGetValue(this ImmutableDictionary<string, string> Named, string key)
        {
            if (Named.TryGetValue(key, out var val))
                return val;
            return null;
        }
    }
}
