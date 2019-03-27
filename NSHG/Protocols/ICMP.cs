using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Protocols.ICMP
{
    public abstract class ICMPHeader : Header
    {
        public byte ICMPType;
        public byte Code;
        public UInt16 Checksum;

        public override Byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(ICMPType);
            bytes.Add(Code);
            bytes.AddRange(BitConverter.GetBytes(CalculateChecksum(bytes.ToArray())));
            return bytes.ToArray();
        }
    }

    public class ICMPEchoRequestReply : ICMPHeader
    {
        public UInt16 SequenceNumber;
        public UInt16 Identifier;

        public ICMPEchoRequestReply(byte type, UInt16 Identify, UInt16 Seq)
        {
            SequenceNumber = Seq;
            Identifier = Identify;

            ICMPType = type;
            Code = 0;

        }

        public ICMPEchoRequestReply(byte[] data)
        {
            if (data.Length != 8)
            {
                throw new ArgumentException("Data wrong length");
            }
            ICMPType = data[0];
            Code = data[1];
            SequenceNumber = BitConverter.ToUInt16(data, 4);
            Identifier = BitConverter.ToUInt16(data, 6);
        }

        public override byte[] ToBytes()
        {
            List<byte> b = new List<byte>();
            b.Add(ICMPType);
            b.Add(Code);
            b.AddRange(BitConverter.GetBytes((UInt16)0));
            b.AddRange(BitConverter.GetBytes(Identifier));
            b.AddRange(BitConverter.GetBytes(SequenceNumber));

            UInt16 checksum = CalculateChecksum(b.ToArray());

            byte[] checksumbytes = BitConverter.GetBytes(checksum);
            b[2] = checksumbytes[0];
            b[3] = checksumbytes[1];

            return b.ToArray();
        }





    }
}
