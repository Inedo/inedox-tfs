using System;
using System.Collections.Generic;
using System.Linq;

namespace Inedo.Extensions.TFS.Operations
{
    internal sealed class TfsArgumentBuilder
    {
        private List<TfsArg> arguments = new List<TfsArg>(16);

        public TfsArgumentBuilder()
        {
            if (arguments != null)
                this.arguments.AddRange(arguments);
        }

        public void AddRange(params TfsArg[] args) => this.arguments.AddRange(args);
        public void Append(string val) => this.arguments.Add(new TfsArg(val, false, false));
        public void AppendQuoted(string val) => this.arguments.Add(new TfsArg(val, true, false));
        public void AppendSensitive(string val) => this.arguments.Add(new TfsArg(val, true, true));
        
        public void Append(string arg, string val) => this.arguments.Add(new TfsArg(arg, val, false, false));
        public void AppendQuoted(string arg, string val) => this.arguments.Add(new TfsArg(arg, val, true, false));
        public void AppendSensitive(string arg, string val) => this.arguments.Add(new TfsArg(arg, val, true, true));

        public override string ToString() => string.Join(" ", this.arguments);
        public string ToSensitiveString() => string.Join(" ", this.arguments.Select(a => a.ToSensitiveString()));

        
    }
    public sealed class TfsArg
    {
        private bool quoted;
        private bool sensitive;
        private string arg;
        private string val;

        public TfsArg(string arg, string val, bool quoted, bool sensitive)
        {
            if (string.IsNullOrWhiteSpace(arg))
                throw new ArgumentNullException(nameof(arg));

            this.arg = arg;
            this.val = val ?? "";
            this.quoted = quoted;
            this.sensitive = sensitive;
        }public TfsArg(string val, bool quoted, bool sensitive)
        {
            this.val = val ?? "";
            this.quoted = quoted;
            this.sensitive = sensitive;
        }


        public override string ToString()
        {
            if (this.quoted)
                return this.arg == null
                    ? '"' + this.val.Replace("\"", @"\""") + '"'
                    : $"{arg}:{('"' + this.val.Replace("\"", @"\""") + '"')}";
            else
                return this.arg == null ? this.val : $"{arg}:{val}";
        }

        public string ToSensitiveString()
        {
            if (this.sensitive)
                return this.arg == null ? "(hidden)" : $"{arg}:\"(hidden)\"";
            else
                return this.ToString();
        }
    }
}
