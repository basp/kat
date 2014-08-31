namespace Kat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TokenizerTests
    {
        [TestMethod]
        public void ChunkTokenizing()
        {
            var s = new Scanner<byte>();
            var t = new CRLFTokenizer(s);

            byte[] bytes;
            IList<string> tokens;

            bytes = Encoding.UTF8.GetBytes("foobar\r");
            tokens = t.Tokenize(new ArraySegment<byte>(bytes)).ToList();
            Assert.AreEqual(1, tokens.Count);

            bytes = Encoding.UTF8.GetBytes("\nquux");
            tokens = t.Tokenize(new ArraySegment<byte>(bytes)).ToList();
            Assert.AreEqual(0, tokens.Count);

            bytes = Encoding.UTF8.GetBytes("\r\nzoz\r\n");
            tokens = t.Tokenize(new ArraySegment<byte>(bytes)).ToList();
            Assert.AreEqual(2, tokens.Count);
        }
    }
}
