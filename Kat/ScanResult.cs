namespace Kat
{
    using System;

    public class ScanResult<T>
    {
        public ScanResult(
            ArraySegment<T> token,
            ArraySegment<T> rest,
            int count = 0)
        {
            this.Token = token;
            this.Rest = rest;
            this.Count = count;
        }

        public ArraySegment<T> Token { get; private set; }
        public ArraySegment<T> Rest { get; private set; }
        public int Count { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return this.Token.Count == 0
                    && this.Rest.Count == 0;
            }
        }

        public bool HasFailed
        {
            get
            {
                return this.Count == 0;
            }
        }

        public static ScanResult<T> Create(
            ArraySegment<T> token,
            ArraySegment<T> rest)
        {
            return new ScanResult<T>(token, rest, token.Count);
        }

        public static ScanResult<T> Fail()
        {
            return new ScanResult<T>(
                new ArraySegment<T>(),
                new ArraySegment<T>(),
                0);
        }

        public static ScanResult<T> Empty(int count)
        {
            return new ScanResult<T>(
                new ArraySegment<T>(),
                new ArraySegment<T>(),
                count);
        }
    }
}
