namespace Kat.Test
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Text;

    [TestClass]
    public class ScannerTests
    {
        [TestMethod]
        public void Sandbox()
        {
            var s = new Scanner<byte>();
            ScanResult<byte> r;
            byte[] bytes;

            r = s.Scan(new ArraySegment<byte>(), x => x == 'x');
            Assert.IsTrue(r.HasFailed);

            bytes = Encoding.UTF8.GetBytes("x");
            r = s.Scan(new ArraySegment<byte>(bytes), x => x == 'x');
            Assert.AreEqual(1, r.Count);

            bytes = Encoding.UTF8.GetBytes("a");
            r = s.Scan(new ArraySegment<byte>(bytes), x => x == 'x');
            Assert.AreEqual("x", Encoding.UTF8.GetString(
                r.Token.Array, 
                r.Token.Offset, 
                r.Token.Count));
        }

        [TestMethod]
        public void ScanFooCrLf()
        {
            var s = new Scanner<byte>();
            ScanResult<byte> r;
            byte[] bytes;

            bytes = Encoding.UTF8.GetBytes("foo\r\n");
            r = s.Scan(new ArraySegment<byte>(bytes), x => x != '\r');
            Assert.AreEqual(3, r.Count);
            Assert.AreEqual("foo", Encoding.UTF8.GetString(
                r.Token.Array,
                r.Token.Offset,
                r.Token.Count));

            r = s.Scan(r.Rest, x => x == '\r');
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual((byte)'\r', r.Token.Array[r.Token.Offset]);

            r = s.Scan(r.Rest, x => x == '\n');
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.IsEmpty);

            bytes = Encoding.UTF8.GetBytes("bar\r\n");
            r = s.Scan(new ArraySegment<byte>(bytes), x => x == '\n');
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual((byte)'\n', r.Token.Array[r.Token.Offset]);
        }
    }
}
