using System;
using System.Collections.Generic;
using System.Text;
using Cave.Net;
using NUnit.Framework;

namespace Test
{
    [TestFixture]
    class HttpConnectionTest
    {
        [Test]
        public void GetGoogle()
        {
            var google = HttpConnection.GetString("http://google.de");

        }
    }
}
