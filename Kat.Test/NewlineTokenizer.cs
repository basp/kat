namespace Kat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class NewlineTokenizer : Tokenizer<byte, string>
    {
        public NewlineTokenizer(IScanner<byte> scanner)
            : base(scanner)
        {
        }

        enum Mode
        {
            Default,
            LF
        }

        Mode mode = Mode.Default;
                
        protected override Tuple<ScanResult<byte>, bool> Scan(ArraySegment<byte> source)
        {
            ScanResult<byte> result;

            switch (this.mode)
            {
                case Mode.LF:
                    result = this.scanner.Scan(source, x => x == '\n');
                    this.mode = Mode.Default;
                    return Tuple.Create(result, true);
                default:
                    result = this.scanner.Scan(source, x => x != '\n');
                    return Tuple.Create(result, false);
            }
        }

        protected override string Factory(ArraySegment<byte> source)
        {
            this.mode = Mode.LF;
            return Encoding.UTF8.GetString(
                source.Array,
                source.Offset,
                source.Count);
        }
    }
}
