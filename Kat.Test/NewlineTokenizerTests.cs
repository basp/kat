namespace Kat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NewlineTokenizerTests
    {
        [TestMethod]
        public void TokenizeFragmented()
        {
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
        }
    }
}
