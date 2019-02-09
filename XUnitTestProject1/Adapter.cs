using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NSHG;

namespace XUnitTests
{
    public class AdapterTests
    {
        [Fact]
        public void Equals1()
        {
            MAC mac = MAC.Parse("FF:FF:FF:FF:FF:FF");
            string name = "Adapter 1";
            IP localip = IP.Parse("192.168.1.2");
            IP Subnet = IP.Parse("255.255.255.0");
            IP DefaultG = IP.Parse("192.168.1.1");

            NSHG.Adapter a = new NSHG.Adapter(mac, name, localip, Subnet, DefaultG, 1, true);


            Assert.True(a.Equals(a));
        }

        [Fact]
        public void Equals2()
        {
            MAC mac = MAC.Parse("FF:FF:FF:FF:FF:FF");
            string name = "Adapter 1";
            IP localip = IP.Parse("192.168.1.2");
            IP Subnet = IP.Parse("255.255.255.0");
            IP DefaultG = IP.Parse("192.168.1.1");

            NSHG.Adapter a1 = new NSHG.Adapter(mac, name, localip, Subnet, DefaultG, 1, true);
            NSHG.Adapter a2 = new NSHG.Adapter(mac, name, localip, Subnet, DefaultG, 1, true);


            Assert.True(a1.Equals(a2));
        }

        [Fact]
        public void Equals3()
        {
            MAC mac1     = MAC.Parse("FF:FF:FF:FF:FF:FF");
            string name1 = "Adapter 1";
            IP localip1  = IP.Parse("192.168.1.2");
            IP Subnet1   = IP.Parse("255.255.255.0");
            IP DefaultG1 = IP.Parse("192.168.1.1");

            MAC mac2     = MAC.Parse("FF:FF:FF:FF:FF:FF");
            string name2 = "Adapter 1";
            IP localip2  = IP.Parse("192.168.1.2");
            IP Subnet2   = IP.Parse("255.255.255.0");
            IP DefaultG2 = IP.Parse("192.168.1.1");

            NSHG.Adapter a1 = new NSHG.Adapter(mac1, name1, localip1, Subnet1, DefaultG1, 1, true);
            NSHG.Adapter a2 = new NSHG.Adapter(mac2, name2, localip2, Subnet2, DefaultG2, 1, true);

            Assert.True(a1.Equals(a2));
        }
    }
}
