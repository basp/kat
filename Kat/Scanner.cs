namespace Kat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Scanner<T> : IScanner<T>
    {
        T[] buffer;
        int tip;

        static int MaxBuffer = 512 * 1024 * 1024;

        public Scanner(int initialBufferSize = 1024)
        {
            this.buffer = new T[initialBufferSize];
            this.tip = 0;
        }

        public ArraySegment<T> Buffer
        {
            get
            {
                return new ArraySegment<T>(this.buffer, 0, this.tip);
            }
        }

        public ScanResult<T> Scan(
            ArraySegment<T> segment,
            Func<T, bool> pred)
        {
            // No way to make progress with empty segment and worse,
            // the underlying byte array may be null too.
            if(segment.Count == 0)
            {
                return ScanResult<T>.Fail();
            }

            var count = segment.TakeWhile(x => pred(x)).Count();

            // No progress, nothing buffered so we fail.
            if ((this.Buffer.Count + count) == 0)
            {
                return ScanResult<T>.Fail();
            }

            if (count == segment.Count)
            {
                // Our predicate is pretty successful. We still have not 
                // encountered anything that fails it so we have to keep
                // on trucking.
                this.Append(segment);

                // Progress but we are not there yet but we can return 
                // the number of  characters we processed.
                return ScanResult<T>.Empty(count);
            }

            // Real progress at last! We found something that we can return.
            // Now we need to wrap up our result.
            var found = new ArraySegment<T>(
                segment.Array,
                segment.Offset,
                count);

            this.Append(found);

            var rest = new ArraySegment<T>(
                segment.Array,
                segment.Offset + count,
                segment.Count - found.Count);

            var result = ScanResult<T>.Create(this.Buffer, rest);
            
            this.tip = 0;
            return result;
        }

        private void Append(ArraySegment<T> segment)
        {
            if (segment.Count > this.buffer.Length - this.tip)
            {
                var required = segment.Count + this.tip;
                var size = ((required / 1024) + 1) * 1024;
                if(size > MaxBuffer)
                {
                    throw new InvalidOperationException();
                }

                Array.Resize(ref this.buffer, size);
            }

            System.Buffer.BlockCopy(
                segment.Array,
                segment.Offset,
                this.buffer,
                tip,     
                segment.Count);

            this.tip += segment.Count;
        }  
    }
}
