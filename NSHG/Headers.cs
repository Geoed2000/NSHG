using System;
using System.Net;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace NSHG
{
    namespace Packet
    {
        /// <summary>
        /// This class defines the IPV4 header bit by bit accurate to a real life example
        /// </summary>
        public class IPv4Header
        {
            // Depreciated
            public class Option
            {

                public enum OptionType : byte
                {
                    EndOfOptionList = 0,
                    NoOperation = 1,
                    Security = 130,
                    LooseSourceRecordRoute = 131,
                    StrictSourceRecordRoute = 137,
                    RecordRoute = 7,
                    InternetTimestamp = 68
                }
                public interface IOption
                {
                    //IOption Frombytes(byte[] bytes,uint pointer);
                    byte[] ToBytes();
                    OptionType type
                    {
                        get;
                    }
                }
                public class EndOfOptionList : IOption
                {
                    public OptionType type
                    {
                        get
                        {
                            return OptionType.EndOfOptionList;
                        }
                    }
                    public byte[] ToBytes()
                    {
                        return new byte[1] { (byte)type };
                    }
                }
                public class NoOperation : IOption
                {
                    public OptionType type
                    {
                        get
                        {
                            return OptionType.NoOperation;
                        }
                    }

                    public byte[] ToBytes()
                    {
                        return new byte[1] { (byte)type };
                    }
                }
                public class Security : IOption
                {
                    public enum SecurityType : UInt16
                    {
                        Unclassified = 0b00000000_00000000,
                        Confidential = 0b11110001_00110101,
                        EFTO = 0b01111000_10011010,
                        MMMM = 0b10111100_01001101,
                        PROG = 0b01011110_00100110,
                        Restricted = 0b10101111_00010011,
                        Secret = 0b11010111_10001000,
                        TopSecret = 0b01101011_11000101
                    }

                    public OptionType type
                    {
                        get
                        {
                            return OptionType.Security;
                        }
                    }
                    public SecurityType SecType;
                    public UInt16 Compartments;
                    public UInt16 HandlingRestrictions;

                    private UInt32 transmissionControlCode;
                    public UInt32 TransmissionControlCode
                    {
                        get
                        {
                            return transmissionControlCode;
                        }
                        set
                        {
                            if (value > 0b00000000_11111111_11111111_11111111) throw new OverflowException();
                            else
                            {
                                transmissionControlCode = value;
                            }
                        }
                    }


                    public byte[] ToBytes()
                    {
                        List<byte> bytes = new List<byte>
                    {
                        (byte)type,
                        11, // Length
                    };
                        bytes.AddRange(BitConverter.GetBytes((UInt16)SecType));
                        bytes.AddRange(BitConverter.GetBytes((UInt16)Compartments));

                        byte b = (byte)(TransmissionControlCode >> 16);
                        bytes.Add(b);
                        bytes.AddRange(BitConverter.GetBytes(TransmissionControlCode));


                        return bytes.ToArray();
                    }

                }
                public abstract class SourceRecordRoute : IOption
                {
                    private OptionType oType;
                    public OptionType type
                    {
                        get
                        {
                            return oType;
                        }
                    }
                    public byte Length;
                    public byte Pointer;
                    public byte[] RouteData;

                    public SourceRecordRoute(OptionType type, IP[] RouteData)
                    {
                        this.oType = type;

                        List<byte> bytes = new List<byte>();

                        foreach (IP ip in RouteData)
                        {
                            bytes.AddRange(ip.ToBytes());
                        }

                        Length = (byte)(RouteData.Length * 4);
                        Pointer = 4;
                    }


                    public IP NextIp()
                    {
                        byte[] ip = new byte[4];

                        RouteData.CopyTo(ip, Pointer - 4);
                        Pointer += 4;

                        return new IP(ip);
                    }

                    public byte[] ToBytes()
                    {

                        List<byte> bytes = new List<byte>()
                        {
                            (byte)type,
                            Length,
                            Pointer
                        };
                        bytes.AddRange(RouteData);

                        return bytes.ToArray();
                    }
                }
                public class LooseSourceRecordRoute : SourceRecordRoute
                {
                    public LooseSourceRecordRoute(IP[] RouteData) : base(OptionType.LooseSourceRecordRoute, RouteData)
                    {

                    }

                }
                public class StrictSourceRecordRoute : SourceRecordRoute
                {
                    public StrictSourceRecordRoute(IP[] RouteData) : base(OptionType.StrictSourceRecordRoute, RouteData)
                    {

                    }

                }
                public class RecordRoute : IOption
                {
                    public OptionType type
                    {
                        get
                        {
                            return OptionType.RecordRoute;
                        }
                    }
                    public byte Length;
                    public byte Pointer;
                    public byte[] RouteData;

                    public bool AddIp(IP ip)
                    {
                        if (Pointer >= Length) return false;
                        ip.ToBytes().CopyTo(RouteData, Pointer - 4);
                        return true;
                    }

                    public byte[] ToBytes()
                    {
                        List<byte> bytes = new List<byte>()
                        {
                            (byte)type,
                            Length,
                            Pointer
                        };
                        bytes.AddRange(RouteData);

                        return bytes.ToArray();
                    }
                }
                public class InternetTimestamp : IOption
                {
                    public OptionType type
                    {
                        get
                        {
                            return OptionType.InternetTimestamp;
                        }
                    }

                    public byte[] ToBytes()
                    {
                        List<byte> bytes = new List<byte>()
                        {
                            (byte)type,
                        };

                        return bytes.ToArray();
                        
                    }
                }
            }

            // Static(s)
            /// <summary>
            /// Calculates the one's compliment of the one's compliment sum in 16 bit words of the byte array provided
            /// </summary>
            /// <param name="data">Byte array containing the data to be included in the check sum</param>
            /// <param name="start">Starting byte in the array</param>
            /// <param name="length">Amount of bytes to read, must be divisible by 2, start + length >= data.length</param>
            /// <returns>16bit unsinged one's comliment sum</returns>
            private static UInt16 CalculateChecksum (byte[] data)
            {
                UInt16 current;
                UInt32 total = 0;
                for (int i = 0; i < data.Length ; i += 2)
                {
                    current = (UInt16)((data[i] << 8) + data[i + 1]);
                    total += current;
                    while((total >> 16) > 0) // if the value is > 65536(2^16) then remove the 256 
                    {
                        total = (total & 0xFFFF) + (total >> 16);
                    }    
                }
                return (UInt16)~total;

            }

            public enum ProtocolType : byte
            {
                ICMP = 1,
                TCP = 6

            }

            private const byte IHLbyteMask = 0b00001111;
            private const byte VersionbyteMask = 0b11110000;
            private const UInt16 FOMask =  0b0001111111111111;
            private const UInt16 RESMask = 0b1000000000000000;
            private const UInt16 DFMask =  0b0100000000000000;
            private const UInt16 MFMask =  0b0010000000000000;
            
            
            // Attribute(s) 
            private byte _VersionIHL
            {
                get
                {
                    return (byte)(0x40 + IHL);
                }
            }
            public  byte Version
            {
                get
                {
                    return 4;
                }
            }
            public  byte IHL
            {
                get
                {
                    byte Ihl = (byte)(5 + (Options.Length / 4));                 
                    return Ihl;
                }
            }
            public  byte TOS;
            public  UInt16 Length
            {
                get
                {
                    return (UInt16)((IHL * 4) + Datagram.Length);
                }
            }
            public  UInt16 Identification;
            private UInt16 _FlagsFragmentOffset;
            public  bool RES
            {
                get
                {
                    UInt16 v = (UInt16)(_FlagsFragmentOffset & RESMask);
                    if (v > 0) return true;
                    else return false;
                }
                set
                {
                    UInt16 v = (UInt16)(_FlagsFragmentOffset & ~RESMask);
                    if (value) _FlagsFragmentOffset = (UInt16)(_FlagsFragmentOffset | RESMask);
                }
            }
            public  bool DF
            {
                get
                {
                    UInt16 v = (UInt16)(_FlagsFragmentOffset & DFMask);
                    if (v > 0) return true;
                    else return false;
                }
                set
                {
                    UInt16 v = (UInt16)(_FlagsFragmentOffset & ~DFMask);
                    if (value) _FlagsFragmentOffset = (UInt16)(_FlagsFragmentOffset | DFMask);
                }
            }
            public  bool MF
            {
                get
                {
                    UInt16 v = (UInt16)(_FlagsFragmentOffset & MFMask);
                    if (v > 0) return true;
                    else return false;
                }
                set
                {
                    UInt16 v = (UInt16)(_FlagsFragmentOffset & ~MFMask);
                    if (value) _FlagsFragmentOffset = (UInt16)(_FlagsFragmentOffset | MFMask);
                }
            }
            public  UInt16 FragmentOffset
            {
                get
                {
                    return (UInt16)(_FlagsFragmentOffset & FOMask);
                }
                set
                {
                    if (value > 8191)
                    {
                        throw new OverflowException();
                    }
                    _FlagsFragmentOffset = (UInt16)((_FlagsFragmentOffset & ~IHLbyteMask) + value);
                }
            }
            public  byte TTL;
            public  ProtocolType Protocol;
            public  UInt16 HeaderChecksum
            {
                get
                {
                    byte[] header = _ToBytesNoChecksumNoDatagram().ToArray();
                    return CalculateChecksum(header);
                }
            }
            public  IP SourceAddress;
            public  IP DestinationAddress;
            public  byte[] Options;             
            public byte[] Datagram;


            // Constructor(s)
            public IPv4Header(byte TOS, UInt16 Identification,
                bool Reserved, bool DontFragment, bool MoreFragments, UInt16 FragmentOffset, byte TTL,
                ProtocolType Protocol, IP Source, IP Destination, byte[] Options, 
                byte[] Data)
            {                
                this.TOS = TOS;
                this.Identification = Identification;
                this.RES = Reserved;
                this.DF = DontFragment;
                this.MF = MoreFragments;
                this.FragmentOffset = FragmentOffset;
                this.TTL = TTL;
                this.Protocol = Protocol;
                this.SourceAddress = Source;
                this.DestinationAddress = Destination;
                this.Options = Options;
                this.Datagram = Data;
            }
            public IPv4Header(UInt16 Identification, bool DontFragment, bool MoreFragments, byte TTL, ProtocolType Protocol, IP Source, IP Destination, byte[] Options, byte[] Datagram):
                this(0, Identification, false, DontFragment, MoreFragments, 0, TTL, Protocol, Source, Destination, Options, Datagram)
            {

            }
            public IPv4Header(byte[] data)
            {
                byte Version = (byte)(data[0] >> 4);
                if (Version != 0b0100_0000)
                {
                    throw new ArgumentException("Incorrect IP Version");
                } // Check for valid IP Version

                byte IHL = (byte)(data[0] - 0b0100_0000);
                byte[] header = new byte[IHL * 4];
                Array.Copy(data, header, IHL * 4);

                TOS = header[1];
                UInt16 TotalLength = BitConverter.ToUInt16(header, 2);
                if (TotalLength < 20) throw new ArgumentException("Header Total length < 20");
                Identification = BitConverter.ToUInt16(header, 4);
                _FlagsFragmentOffset =  BitConverter.ToUInt16(header, 6);
                TTL = header[8];
                Protocol = (ProtocolType)header[9];
                SourceAddress = new IP(header, 12);
                DestinationAddress = new IP(header, 16);
                if (IHL*4 - 20 > 0) Options = new ArraySegment<byte>(header, 20, IHL*4 - 20).Array;
                Datagram = new ArraySegment<byte>(header,IHL*4,TotalLength - IHL*4).Array;

                if (HeaderChecksum != BitConverter.ToUInt16(header, 10))
                {
                    throw new Exception("Checksum is invalid");
                }
            }

            public static IPv4Header DefaultTCPWrapper(IP Source, IP Destination, byte[] Datagram)
            {
                return new IPv4Header(0, false, false, 255, ProtocolType.TCP, Source, Destination, new byte[0], Datagram);
            }

            // Method(s)

            private List<byte> _ToBytesNoChecksumNoDatagram()
            {
                byte[] b = new byte[2];
                List<byte> bytes = new List<byte>();
                bytes.Add(_VersionIHL); // Byte 0
                bytes.Add(TOS); // Byte 1

                b = BitConverter.GetBytes(Length);
                Array.Reverse(b);
                bytes.AddRange(b); // Byte 2,3
                b = BitConverter.GetBytes(Identification);
                Array.Reverse(b);
                bytes.AddRange(b); //Byte 4,5
                b = BitConverter.GetBytes(_FlagsFragmentOffset);
                Array.Reverse(b);
                bytes.AddRange(b); //Byte 6,7
                bytes.Add(TTL); // Byte 8
                bytes.Add((byte)Protocol); // Byte 9
                bytes.AddRange(new byte[]{0,0}); // bytes 10,11 / set to zero for the purposes of calculationg the checksum later
                bytes.AddRange(SourceAddress.ToBytes()); // Byte 12,13,14,15
                bytes.AddRange(DestinationAddress.ToBytes()); // Byte 16,17,18,19
                bytes.AddRange(Options); // Byte 20 - x
                return bytes;
            }
            public byte[] ToBytes()
            {
                List<byte> bytes = _ToBytesNoChecksumNoDatagram();

                UInt16 checksum = CalculateChecksum(bytes.ToArray());

                byte[] checksumBytes = BitConverter.GetBytes(checksum);

                bytes[10] = checksumBytes[1];
                bytes[11] = checksumBytes[0];

                bytes.AddRange(Datagram); // x - end
                
                return bytes.ToArray();
            }
            
        }
        
        public class ICMPHeader
        {
            byte Type;
            byte Code;
            UInt16 Checksum;
            
        }
    }
}
