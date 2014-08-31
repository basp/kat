namespace Kat
{
    using System;
    using System.Collections.Generic;

    public interface ITokenizer<T, U>
    {
        IEnumerable<U> Tokenize(ArraySegment<T> source);
    }

    public abstract class Tokenizer<T, U> : ITokenizer<T, U>
    {
        protected readonly IScanner<T> scanner;

        public Tokenizer(IScanner<T> scanner)
        {
            this.scanner = scanner;
        }

        protected abstract Tuple<ScanResult<T>, bool> Scan(
            ArraySegment<T> source);

        protected abstract U Factory(ArraySegment<T> source);

        protected virtual void OnFail(
            ArraySegment<T> source,
            ScanResult<T> result)
        {
            this.scanner.Reset();
        }

        public IEnumerable<U> Tokenize(ArraySegment<T> source)
        {
            var result = ScanResult.Create(
                new ArraySegment<T>(),
                source);

            bool skipToken = false;

            do
            {
                if (result.Rest.Count == 0)
                {
                    break;
                }

                var t = this.Scan(result.Rest);
                result = t.Item1;
                skipToken = t.Item2;

                if (result.HasFailed)
                {
                    this.OnFail(source, result);
                    break;
                }

                if (result.Token.Count > 0 && !skipToken)
                {
                    yield return this.Factory(result.Token);
                }
            }
            while (!result.IsEmpty);
        }
    }
}
