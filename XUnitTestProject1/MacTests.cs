using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NSHG;

namespace XUnitTests
{
    public class MacTests
    {
        public class ToStringTests
        {
            [Fact]
            public void SameObject()
            {
                Byte[] b = new byte[]
                {
                    255,
                    255,
                    255,
                    255,
                    255,
                    255
                };

                MAC m = new MAC(b);

                Assert.Equal("FF:FF:FF:FF:FF:FF",m.ToString());
            }
        }

        public class EqualsTests
        {
            [Fact]
            public void SameObject()
            {
                Byte[] b = new byte[]
                {
                    255,
                    255,
                    255,
                    255,
                    255,
                    255
                };

                MAC m = new MAC(b);

                Assert.True(m.Equals(m));
            }

            [Fact]
            public void SameBytearray()
            {
                Byte[] b = new byte[]
                {
                    255,
                    255,
                    255,
                    255,
                    255,
                    255
                };

                MAC m1 = new MAC(b);
                MAC m2 = new MAC(b);

                Assert.True(m1.Equals(m2));
            }

            [Fact]
            public void Identicalbytearray()
            {
                Byte[] b1 = new byte[]
                {
                    255,
                    255,
                    255,
                    255,
                    255,
                    255
                };

                Byte[] b2 = new byte[]
                {
                    255,
                    255,
                    255,
                    255,
                    255,
                    255
                };


                MAC m1 = new MAC(b1);
                MAC m2 = new MAC(b2);

                Assert.True(m1.Equals(m2));
            }

        }

        public class Parseyests
        {

        }
    }
}
