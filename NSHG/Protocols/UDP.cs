using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Protocols.UDP
{
    public class UDPHeader : Header
    {
        public UInt16 SourcePort;
        public UInt16 DestinationPort;
        public UInt16 Length
        {
            get
            {
                return (UInt16)(8 + Datagram.Length);
            }
        }
        public UInt16 Checksum
        {
            get
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(_ToBytesNoChecksumNoDatagram());
                bytes.AddRange(Datagram);
                return CalculateChecksum(bytes.ToArray());
            }
        }
        public byte[] Datagram;

        public UDPHeader(UInt16 SourcePort, UInt16 DestinationPort, byte[] Datagram)
        {
            this.SourcePort = SourcePort;
            this.DestinationPort = DestinationPort;
            this.Datagram = Datagram;
        }

        public UDPHeader(byte[] bytes)
        {
            SourcePort = BitConverter.ToUInt16(bytes, 0);
            DestinationPort = BitConverter.ToUInt16(bytes, 2);
            UInt16 len = BitConverter.ToUInt16(bytes, 4);
            Datagram = new List<byte>(new ArraySegment<byte>(bytes, 8, len - 8)).ToArray();
            if (Checksum != BitConverter.ToUInt16(bytes, 6)) throw new Exception("Checksum is invalid");
            
        }

        private byte[] _ToBytesNoChecksumNoDatagram()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(SourcePort));
            bytes.AddRange(BitConverter.GetBytes(DestinationPort));
            bytes.AddRange(BitConverter.GetBytes(Length));
            bytes.AddRange(BitConverter.GetBytes((UInt16)0b0000000000000000));

            return bytes.ToArray(); 
        }

        public override byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(_ToBytesNoChecksumNoDatagram());
            bytes.AddRange(Datagram);
            byte[] Bytes = bytes.ToArray();
            BitConverter.GetBytes(CalculateChecksum(Bytes)).CopyTo(Bytes, 6);

            return Bytes;

        }
    }
}
