Kat
===
Kat is a small collection of simple scanning utilities designed to make scanning tokens across boundaries of data chunks more pleasant. It was primarily designed to scan bytes for tokens across multiple socket receive operations but as it turns out, it is suitable to work with any kind of stream of primitives that needs to be scanned. 

* Primities are the items that a scanner operates on
* Tokens are made up of any number of primitives
* A scanner by itself doesn't produce tokens, it is only there to facilitate

Scanner
=======
Kat was born out of a need to parse (possibly) very large tokens across any number of socket read operations in a memory and CPU efficient way. You can use scanners to perform operations such as:

* Enforce a particular primitive
* Accepting any number of acceptable primitive
* Filtering

Scanners have the following features:

* Scan operations operate on a `Func<T,bool>` predicate (although we might soon offer a `Func<ArraySegment<T>,bool>` predicate too)
* A scanner will only return a complete token when it finds a primitive that doesn't match the specified predicate (it is _very_ eager)
* Scanners operate on an externally managed array of input primitives, this means that the only memory in use is the externally managed buffer(s) which can be as large or small as you want depending on your use-case(s)
* Scanners _do_ need memory to build up their token but once one is found, it's immediately available to the external application via an array pointer to the internal scanner memory so no copy operations are required to do the actual processing
* Scanners will automatically increase their internal buffer to fit the token that they are building up
* Scanners are forward only

Tokenizer
=========
You can use a scanner directly but often it's more convenient to use the `Tokenizer<T,U>` base class. This contains all the boiler plate code to read from a `IScanner<T>` and convert it to an `IEnumerable<U>`. It's still up to you to implement the actual _protocol_ and the `ArraySegment<T>` to `U` conversion. 

A tokenizer that uses a Kat `IScanner<T>` usually operates in the following way:

1. Perform a scan with the scanner
2. If the result has failed we break or return early
3. If we found a token and we don't have to skip it we use the tokenizer's factory function to wrap up the found token segment. We need to be sure to pass any trailing tokens (not part of the current result) for continued processing.
4. If the result was not empty, we repeat from 1 using the rest of our result as input (there might still be more tokens we can fetch from the segment, this might be some tokens we passed in #3 that where not part of the command)

This is exactly how the included `Tokenizer<T,U>` operates. With one added convenience: it will call an `OnFail` callback before breaking the loop.

Scanner Example
=======
Let's take a look at how we can use a scanner to implemented a filtering operation on a possible infinite stream of bytes. First we'll need a scanner.

	var s = new Scanner<byte>();
	
Now we need some bytes to scan:

	var bs1 = Encoding.UTF8.GetBytes("foo");
	var bs2 = Encoding.UTF8.GetBytes("\nba");
	var bs3 = Encoding.UTF8.GetBytes("r\nquux\n");	

That's three sets of bytes that make up `foo\nbar\nquux\n` which is a sequence of tokens that we need to break up into using the `\n` character as a separator. Maybe we are in kind of some REPL that allows commands to span multiple lines? Or maybe we are just reading from a socket as chunks are coming in. The important thing is that we really don't know and those chunks might be coming in for (in theory) forever.

When we start to read the initial command we wanna start reading everything until we encounter a `\n` character. That's easy enough:

	var result = s.Scan(new ArraySegment<byte>(bs1), b => b != '\n');

This will return with a result but unfortunately it's empty:

	result.IsEmpty
	=> true

	result.Token.Count
	=> 0

Fortunately it didn't fail:

	result.HasFailed
	=> false

And we can also see that we made progress (if `result.Count` would be `0` then `HasFailed` would be `true` too):

	result.Count
	=> 3

This means it _did_ successfully accept the primitives. The reason we didn't get `"foo"` back yet is because the scanner hasn't found a primitive yet that didn't match the predicate `b != '\n'`. What it did instead is remember the `"foo"` for now because there might be additional characters for this token coming in (there aren't but it doesn't know that yet).

This presents us with a bit of a conundrum though because if the scanner doesn't know? How can we know? Or how do we even know how to proceed? Well fortunately, we can check the `ScanResult<T>` and see how to proceed.

If it failed (the `HasFailed` property is `true`) then we know that it made absolutely no progress on our last `Scan` invocation. In other words, it failed at the very first byte of the request. This will also happen if you feed it an empty request. How to proceed is usually to `Reset` the scanner and try on the next bit of data. You might wanna also log it or throw an exception instead. 

`HasFailed` will always mean that _zero_ characters where scanned. The scanner assumes you always feed it something that at least will make some kind of progress, if we don't we fail. Usually the problem lies with a client sending erroneous input.

In our case though it didn't fail, but instead we got an empty result. When dealing with a Kat scanner, you can easily check for an empty result:

	result.IsEmpty
	=> true

When you get an empty result this means that everything is _ok_ but the scanner just needs more data so lets try that:

	result = s.Scan(new ArraySegment<byte>(bs2), x => x != '\n');

So what are we doing? Well just feed the scanner with the next set of bytes using __the same__ predicate (we are still looking for that `\n`). 

This time we _finally_ get back our token:

	var s = result.Token;
	Encoding.UTF8.GetString(s.Array, s.Offset, s.Count);
	=> "foo"

However, this time we cannot go around and feed the scanner a new set of bytes, it still has to deal with the `\n`. And who knows what else is hiding behind that `\n`? There still might be some tokens left for us to process. 

One way to check this is to use the `result.Rest` property which is an `ArraySegment<T>` that points to the unprocessed range of items in the last `Scan` invocation. In this case we have to loop around before obtaining any new data and finish with our current request first:

	result = s.Scan(result.Rest, x => x == '\n');
	
This time we are feeding it `result.Rest` and revert the predicate and say we are only interested in the `\n` instead of everything else. 

	result.IsEmpty
	=> false

Now the result we get back is still not empty because that `\n` is in there. We are not interested in that one, we want to know what is behind it. So we need to loop around again still using `result.Rest` as our input. Except, this time we __reverse our predicate__ again:

	result = s.Scan(result.Rest, x => x != '\n');

Finally we will get back an empty result again because the scanner is trying to parse `ba` but it succeeded all the way to the end. Again, it remembered our `ba` so far and is waiting on that `\n` for confirmation that the next token is complete. 

Scan Pattern
============
By now a pattern is emerging from our operations:

* We get an empty result, in this case we got no token but data ran out. We'll just have to wait or obtain more data and feed that as new input into the scanner.
* We get a non empty result, in this case we found something that didn't match our predicate so we where able to return something. We still might have more stuff to handle in `Rest` so either check for that or blindly pass in `Rest` to another scan and check the next result.
* We get a failed result, in this case we passed the scanner some input and a predicate that failed at the first item. In this case, either the input was horribly invalid or the tokenizer is in some weird state. You can reset and try again, log errors and throw exceptions.
* Also we need to know what to look for next, this is usually implemented as a sort of state machine (often even using a simple `enum`) but you can be creative here.

Tokenizer Example
=================
The `Tokenizer<T, U>` is an abstract class that we cannot use directly. We'll have to implement it but once we do it will abstract away a lot of the tedium from the scanning pattern. Fortunately, implementing this abstract class is pretty easy:

	using System;

	public class NewlineTokenizer : Tokenizer<byte,string>
	{
		protected override Tuple<ScanResult<byte>, bool> Scan(
            ArraySegment<byte> source)
        {
			throw new NotImplementedExeption();
        }

        protected override string Factory(ArraySegment<byte> source)
        {
			throw new NotImplementedExeption();
        }
	}

So now we are left with implementing these two methods. Let's tackle `Scan` first because that's usually the more difficult one. This will need to return a `Tuple<ScanResult<byte>, bool>`. We can obtain a `ScanResult<byte>` by using a scanner (the `Tokenizer<byte,string>` class comes with it) and the `bool` will signal whether the token (if any) we produce should be skipped.

To properly implement our newline protocol we need to know if we are looking for a the newline character or not. We can use a simple `enum` but you might need something more complex depending on the protocol you're trying to parse.

Let's implement this simple protocol:

	enum Mode 
	{
		Default,
		LF
	}

	Mode mode = Mode.Default;

	protected override Tuple<ScanResult<byte>, bool> Scan(
        ArraySegment<byte> source)
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
	
We basically have two modes, either reading stuff until we find a linefeed character or handling that single linefeed. If we find a linefeed we return `true` for the `skipToken` value and make sure this is not skipped. For all other tokens in between those linefeed characters we return the token and `false` so it isn't skipped.

We also need to implement the `Factory` method. This creates our final token out of the raw primitives we scanned:

    protected override string Factory(ArraySegment<byte> source)
    {
        this.mode = Mode.LF;
        return Encoding.UTF8.GetString(
            source.Array,
            source.Offset,
            source.Count);
    }

The factory method will be called once an actual token to be returned is found (and `skipToken` equals `false`) so we can conveniently switch modes here.

When using a `Tokenizer<T, U>` you don't have to worry about storing bytes or remembering stuff. It will make sure you get the correct piece of input passed into your `Scan` implementation. How to handle the behavior and state of that tokenizer is mostly up to the implementation.

This tokenizer can be used to tokenize fragmented byte streams using a newline as a separator:

	var s = new Scanner<byte>();
    var t = new NewlineTokenizer(s);

    var bs1 = Encoding.UTF8.GetBytes("foo\nbar");
    var bs2 = Encoding.UTF8.GetBytes("\nquux\nzoz\n");

    IList<string> tokens;

    tokens = t.Tokenize(new ArraySegment<byte>(bs1)).ToList();
    Assert.AreEqual(1, tokens.Count());
    Assert.AreEqual("foo", tokens[0]);

    tokens = t.Tokenize(new ArraySegment<byte>(bs2)).ToList();
    Assert.AreEqual(3, tokens.Count());
    Assert.AreEqual("bar", tokens[0]);
    Assert.AreEqual("quux", tokens[1]);
    Assert.AreEqual("zoz", tokens[2]);