using System;
using Xunit;
using NSHG;
using NSHG.Protocols.IPv4;
using NSHG.Protocols.UDP;
using NSHG.Protocols.DHCP;

namespace XUnitTests
{
    public class HeadersTests
    {
        [Fact]
        public void Checksum()
        {
            UInt16 expected, actual;
            expected = 0b0111_0111_1101_0111;

            IPv4Header ipv4 = new IPv4Header(0x6E9C, true, false, 128, IPv4Header.ProtocolType.TCP, IP.Parse("10.144.227.100"), IP.Parse("40.67.254.36"), new byte[0], new byte[20]);
            actual = ipv4.HeaderChecksum;

            Assert.Equal(expected, actual);

        }
        [Fact]
        public void ToBytes()
        {
            byte[] expected, actual, tmp;

            expected = new byte[20]
            {
                0x45, 0x00, 0x00, 0x28, 0x6e, 0x9c, 0x40, 0x00, 0x80, 0x06, 0x77, 0xd7, 0x0a, 0x90, 0xe3, 0x64, 0x28, 0x43, 0xfe, 0x24
            };

            IPv4Header ipv4 = new IPv4Header(0x6E9C, true, false, 128, IPv4Header.ProtocolType.TCP, IP.Parse("10.144.227.100"), IP.Parse("40.67.254.36"), new byte[0], new byte[20]);

            tmp = ipv4.ToBytes();

            actual = new byte[20];
            Array.Copy(tmp, actual, 20);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ToBytesAndBack1()
        {
            Optionlist ol = new Optionlist();
            ol.Add(new DHCPOption(Tag.dhcpServerID, IP.Broadcast.ToBytes()));
            ol.Add(new DHCPOption(Tag.router, IP.Broadcast.ToBytes()));
            ol.Add(new DHCPOption(Tag.subnetmask, IP.Zero.ToBytes()));
            ol.Add(new DHCPOption(Tag.domainserver, IP.Broadcast.ToBytes()));




            DHCPDatagram DHCP1 = new DHCPDatagram(1, 78974564, 1, 10, 0, 0, true, IP.Broadcast, null, null, null, MAC.Random(), ol);
            UDPHeader UDP1 = new UDPHeader(100, 101, DHCP1.ToBytes());
            IPv4Header ipv41 = new IPv4Header(1, false, false, 30, IPv4Header.ProtocolType.UDP, IP.Zero, IP.Broadcast, new byte[0],UDP1.ToBytes());
            byte[] Bytes = ipv41.ToBytes();
            IPv4Header ipv42 = new IPv4Header(Bytes);
            UDPHeader UDP2 = new UDPHeader(ipv42.Datagram);
            DHCPDatagram DHCP2 = new DHCPDatagram(UDP2.Datagram);

            Assert.Equal(ipv41.ToBytes(), ipv42.ToBytes());
            Assert.Equal(UDP1.ToBytes(), UDP2.ToBytes());
            Assert.Equal(DHCP1.ToBytes(), DHCP2.ToBytes());

        }
    }
}
    