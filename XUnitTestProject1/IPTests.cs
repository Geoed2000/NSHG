using System;
using Xunit;
using NSHG;

namespace XUnitTests
{
    public class IPTests
    {
       

        public class EqualsTests
        {
            [Fact]
            public void Identical()
            {
                IP ip0 = new IP(new byte[4] { 0, 0, 0, 0 });
                
                Assert.True(ip0.Equals(ip0));

            }

            [Fact]
            public void SameByteArray()
            {
                byte[] b = new byte[4] { 0, 0, 0, 0 };
                IP ip0 = new IP(b);
                IP ip1 = new IP(b);

                Assert.True(ip0.Equals(ip1));
            }

            [Fact]
            public void DifferentByteArray()
            {
                IP ip0 = new IP(new byte[4] { 0, 0, 0, 0 });
                IP ip1 = new IP(new byte[4] { 0, 0, 0, 0 });

                Assert.True(ip0.Equals(ip1));
            }
            [Fact]
            public void Equals3()
            {
                IP ip0 = new IP(new byte[4] { 0, 0, 0, 0 });
                IP ip1 = new IP(new byte[4] { 0, 0, 0, 1 });

                Assert.False(ip0.Equals(ip1));
            }
        }
        
        public class ToBytesTests
        {

        }
        

    }
}
