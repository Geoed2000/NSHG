using System;
using Xunit;
using NSHG;

namespace XUnitTests
{
    public class IPTests
    {
       

        public class Constructors
        {
            [Fact]
            public void Equals1()
            {
                IP ip0 = new IP(new byte[4] { 0, 0, 0, 0 });
                
                Assert.True(ip0.Equals(ip0));

            }

            [Fact]
            public void Equals2()
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
        
        public class ToBytes
        {

        }
        

    }
}
