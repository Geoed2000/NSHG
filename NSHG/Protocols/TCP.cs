using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Protocols.TCP
{
    public class TCPHeader : Header
        {
            static UInt16 HlenMask = (UInt16) 0b1111000000000000;
            static UInt16 URGMask  = (UInt16) 0b0000000000100000;
            static UInt16 ACKMask  = (UInt16) 0b0000000000010000;
            static UInt16 PSHMask  = (UInt16) 0b0000000000001000;
            static UInt16 RSTMask  = (UInt16) 0b0000000000000100;
            static UInt16 SYNMask  = (UInt16) 0b0000000000000010;
            static UInt16 FINMask  = (UInt16) 0b0000000000000001;

            public UInt16 SourcePort;
            public UInt16 DestinationPort;
            public UInt32 SequenceNumber;
            public UInt32 AcknowledgementNumber;
            private UInt16 Flags;
            private UInt16 HlenFlags
            {
                get
                {
                    return (UInt16)(Flags + (Hlen << 12));
                }
            }
            public byte Hlen
            {
                get
                {
                    return (byte)(5 + Options.Length / 4);
                }
            }
            public bool URG
            {
                get
                {
                    return ((Flags & URGMask) == URGMask);
                }
                set
                {
                    Flags = (UInt16)(Flags & ~HlenMask);
                    if (value) Flags += 0b000001;
                }
            }
            public bool ACK
            {
                get
                {
                    return ((Flags & ACKMask) == ACKMask);
                }
                set
                {
                    Flags = (UInt16)(Flags & ~HlenMask);
                    if (value) Flags += 0b000010;
                }
            }
            public bool PSH
            {
                get
                {
                    return ((Flags & PSHMask) == PSHMask);
                }
                set
                {
                    Flags = (UInt16)(Flags & ~HlenMask);
                    if (value) Flags += 0b000100;
                }
            }
            public bool RST
            {
                get
                {
                    return ((Flags & RSTMask) == RSTMask);
                }
                set
                {
                    Flags = (UInt16)(Flags & ~HlenMask);
                    if (value) Flags += 0b001000;
                }
            }
            public bool SYN
            {
                get
                {
                    return ((Flags & SYNMask) == SYNMask);
                }
                set
                {
                    Flags = (UInt16)(Flags & ~HlenMask);
                    if (value) Flags += 0b010000;
                }
            }
            public bool FIN
            {
                get
                {
                    return ((Flags & FINMask) == FINMask);
                }
                set
                {
                    Flags = (UInt16)(Flags & ~HlenMask);
                    if (value) Flags += 0b100000;
                }
            }

            public UInt16 Window;
            public UInt16 Checksum
            {
                get
                {
                    List<byte> bytes = new List<byte>();
                    bytes.AddRange(_ToBytesNoChecksumNoDatagram());
                    bytes.AddRange(Datagram);
                    return (CalculateChecksum(bytes.ToArray()));
                }
            }
            public UInt16 UrgentPointer;
            public byte[] Options;
            public byte[] Datagram;


            public TCPHeader(UInt16 SourcePort, UInt16 DestinationPort, UInt32 SequenceNumber, UInt32 AcknowledgementNumber,
                bool URG, bool ACK, bool PSH, bool RST, bool SYN, bool FIN, 
                UInt16 Window, UInt16 UrgentPointer, byte[] Options, byte[] Datagram)
            {
                this.SourcePort = SourcePort;
                this.DestinationPort = DestinationPort;
                this.SequenceNumber = SequenceNumber;
                this.AcknowledgementNumber = AcknowledgementNumber;
                this.URG = URG;
                this.ACK = ACK;
                this.PSH = PSH;
                this.RST = RST;
                this.SYN = SYN;
                this.FIN = FIN;
                this.Window = Window;
                this.UrgentPointer = UrgentPointer;
            }

            public TCPHeader(Byte[] bytes)
            {
                byte len = (byte)(bytes[12] >> 4);
                byte[] options = new byte[(len - 5) * 4];
                Datagram = new byte[bytes.Length - (len * 4)];
                bytes.CopyTo(Datagram, len * 4);
                SourcePort = BitConverter.ToUInt16(bytes, 0);
                DestinationPort = BitConverter.ToUInt16(bytes, 2);
                SequenceNumber = BitConverter.ToUInt32(bytes, 4);
                AcknowledgementNumber = BitConverter.ToUInt32(bytes, 8);
                Flags = (UInt16)bytes[13];
                Window = BitConverter.ToUInt16(bytes, 14);
                UrgentPointer = BitConverter.ToUInt16(bytes, 18);
                bytes.CopyTo(Options, len * 4);
                if (Checksum != BitConverter.ToUInt16(bytes, 16)) throw new Exception("Checksum is invalid");

            }


            private byte[] _ToBytesNoChecksumNoDatagram()
            {
                List<byte> bytes = new List<byte>();

                bytes.AddRange(BitConverter.GetBytes(SourcePort));
                bytes.AddRange(BitConverter.GetBytes(DestinationPort));
                bytes.AddRange(BitConverter.GetBytes(SequenceNumber));
                bytes.AddRange(BitConverter.GetBytes(AcknowledgementNumber));
                bytes.AddRange(BitConverter.GetBytes(HlenFlags));
                bytes.AddRange(BitConverter.GetBytes(Window));
                bytes.AddRange(BitConverter.GetBytes(0b0000000000000000));
                bytes.AddRange(BitConverter.GetBytes(UrgentPointer));
                bytes.AddRange(Options);

                return bytes.ToArray();
            }

            public override byte[] ToBytes()
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(_ToBytesNoChecksumNoDatagram());
                bytes.AddRange(Datagram);
                byte[] Bytes = bytes.ToArray();
                BitConverter.GetBytes(CalculateChecksum(Bytes)).CopyTo(Bytes, 16);

                return Bytes;
            }


        }
}
