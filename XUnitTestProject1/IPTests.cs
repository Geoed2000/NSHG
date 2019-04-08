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
        
        public class Operators
        {
            [Fact]
            public void Increment()
            {
                IP ip1 = IP.Zero;
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 0, 1 }));
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 0, 2 }));
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 0, 3 }));
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 0, 4 }));
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 0, 5 }));
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 0, 6 }));
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 0, 7 }));

                ip1 = new IP(new byte[4] { 0, 0, 0, 255 });
                ip1++;
                Assert.Equal(ip1, new IP(new byte[4] { 0, 0, 1, 0 }));
            }   
        }
        

    }
}
