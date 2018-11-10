using System;
using Xunit;
using NSHG;
using NSHG.Packet;

namespace XUnitTestProject1
{
    public class Headers
    {
        [Fact]
        public void Checksum()
        {
            UInt16 expected, actual;
            expected = 0b1011_1101_1110_0001;

            IPv4Header ipv4 = new IPv4Header(0, 0, false, false, false, 0, IPv4Header.ProtocolType.TCP, IP.Loopback, IP.Loopback, new byte[0], new byte[0]);
            actual = ipv4.HeaderChecksum;

            Assert.Equal(expected, actual);

        }
        [Fact]
        public void ToBytes()
        {
            Byte[] expected, actual;

            expected = new Byte[]
            {
                0x45, 0x00, 0x00, 0x28, 0x6e, 0x9c, 0x40, 0x00, 0x80, 0x06, 0x77, 0xd7, 0x0a, 0x90, 0xe3, 0x64, 0x28, 0x43, 0xfe, 0x24
            };

            IPv4Header ipv4 = new IPv4Header(true, 128, (IPv4Header.ProtocolType)6, IP.Parse("10.144.227.100"), IP.Parse("40.67.254.36"), new Byte[0], new Byte[20]);

            actual = ipv4.ToBytes();

            Assert.Equal(expected, actual);
        }
    }
}
    