Kat
===
A collection of simple scanning utilities designed to make scanning tokens across boundaries of data chunks. It was primarily designed to scan bytes but it can work with higher level primitives too. 

Scanner
=======
Kat was born out of a need to parse (possibly) very large tokens across any number of socket read operations in a memory and CPU efficient way. It has the following features:

* Scan operations operate on a `Func<T,bool>` predicate.
* A scanner will only return a complete token when it finds a primitive that doesn't match the specified predicate.
* Scanners operate on an externally managed array of input primitives, this means they don't need any memory for their input stream.
* Scanners _do_ need memory to build up their token but once one is found, it's immediately available to the external application via an array pointer to the internal scanner memory so no copy operations are required to do the actual processing.
* Scanners are forward only.

Tokenizer
=========
You can use a scanner directly but often it's more convenient to implemented the included `Tokenizer<T,U>` base class. This contains all the boiler plate code to read from a `IScanner<T>` and convert it to an `IEnumerable<U>`. It's still up to you to implement the actual _protocol_ and the `ArraySegment<T>` to `U` conversion. 

A tokenizer that uses a Kat `IScanner<T>` usually operates in the following way:

1. Perform a scan with the scanner
2. If the result has failed we break or return early
3. If we found a token and we don't have to skip it we use the tokenizer's factory function to wrap up the found token segment
4. If the result was not empty, we repeat from 1 using the rest of our result as input (there might still be more tokens we can fetch from the segment)

This is exactly how the included `Tokenizer<T,U>` operates. With one added convenience: it will call an `OnFail` callback before breaking the loop.

 