namespace Kat
{
    using System;
    using System.Collections.Generic;

    public interface IScanner<T>
    {
        ArraySegment<T> Buffer { get; }

        ScanResult<T> Scan(
            ArraySegment<T> segment, 
            Func<T, bool> pred);

        void Reset();
    }
}
