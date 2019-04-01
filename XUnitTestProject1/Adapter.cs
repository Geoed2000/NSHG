using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NSHG;
using NSHG.NetworkInterfaces;

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
            IP DNS = IP.Parse("1.1.1.1");

            Adapter a = new Adapter(mac, 1, name, localip, Subnet, DefaultG, DNS, 1, true);


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
            IP DNS = IP.Parse("1.1.1.1");

            Adapter a1 = new Adapter(mac, 1, name, localip, Subnet, DefaultG, DNS, 1, true);
            Adapter a2 = new Adapter(mac, 1, name, localip, Subnet, DefaultG, DNS, 1, true);


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
            IP DNS1 = IP.Parse("1.1.1.1");

            MAC mac2     = MAC.Parse("FF:FF:FF:FF:FF:FF");
            string name2 = "Adapter 1";
            IP localip2  = IP.Parse("192.168.1.2");
            IP Subnet2   = IP.Parse("255.255.255.0");
            IP DefaultG2 = IP.Parse("192.168.1.1");
            IP DNS2 = IP.Parse("1.1.1.1");

            Adapter a1 = new Adapter(mac1, 1, name1, localip1, Subnet1, DefaultG1, DNS1, 1, true);
            Adapter a2 = new Adapter(mac2, 1, name2, localip2, Subnet2, DefaultG2, DNS2, 1, true);

            Assert.True(a1.Equals(a2));
        }
    }
}
