namespace Kat.Test
{
    using System;
    using System.Text;

    public class TestTokenizer : Tokenizer<byte,string>
    {
        public TestTokenizer(IScanner<byte> scanner)
            : base(scanner)
        {
        }

        enum Mode
        {
            Default,
            CR,
            LF
        }

        Mode mode = Mode.Default;

        protected override Tuple<ScanResult<byte>, bool> Scan(
            ArraySegment<byte> source)
        {
            ScanResult<byte> result;
            switch(this.mode)
            {
                case Mode.CR:
                    result = this.scanner.Scan(source, x => x == '\r');
                    this.mode = Mode.LF;
                    return Tuple.Create(result, true);
                case Mode.LF:
                    result = this.scanner.Scan(source, x => x == '\n');
                    this.mode = Mode.Default;
                    return Tuple.Create(result, true);
                default:
                    result = this.scanner.Scan(source, x => x != '\r');
                    return Tuple.Create(result, false);
            }
        }

        protected override string Factory(ArraySegment<byte> source)
        {
            this.mode = Mode.CR;
            return Encoding.UTF8.GetString(
                source.Array,
                source.Offset,
                source.Count);
        }
    }
}
