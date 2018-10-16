using System;
using System.Net;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace NSHG
{
    namespace Packet
    {
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
                    byte[] Tobytes();
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
                    public byte[] Tobytes()
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

                    public byte[] Tobytes()
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


                    public byte[] Tobytes()
                    {
                        List<byte> bytes = new List<byte>
                    {
                        (byte)type,
                        11, // Length
                    };
                        bytes.AddRange(BitConverter.Getbytes((UInt16)SecType));
                        bytes.AddRange(BitConverter.Getbytes((UInt16)Compartments));

                        byte b = (byte)(TransmissionControlCode >> 16);
                        bytes.Add(b);
                        bytes.AddRange(BitConverter.Getbytes(TransmissionControlCode));


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
                            bytes.AddRange(ip.Tobytes());
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

                    public byte[] Tobytes()
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
                        ip.Tobytes().CopyTo(RouteData, Pointer - 4);
                        return true;
                    }

                    public byte[] Tobytes()
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

                    public byte[] Tobytes()
                    {
                        List<byte> bytes = new List<byte>()
                        {
                            (byte)type,
                        };

                        return bytes.ToArray();
                        
                    }
                }
            }

            public enum ProtocolType : byte
            {
                ICMP = 1,
                TCP = 6,

            }

            private const byte IHLbyteMask = 0b00001111;
            private const byte VersionbyteMask = 0b11110000;
            private const UInt16 FOMask =  0b0001111111111111;
            private const UInt16 RESMask = 0b1000000000000000;
            private const UInt16 DFMask =  0b0100000000000000;
            private const UInt16 MFMask =  0b0010000000000000;
            
            
            // Attribute(s) 
            private byte VersionIHL = 0;
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
                    byte Ihl = (byte)(5 + Options.Length / 4);
                    VersionIHL = (byte)(Ihl + 0b0100_0000);
                    return Ihl;
                }
            }
            public  byte TOS;
            public  UInt16 Length
            {
                get
                {
                    return (UInt16)(IHL * 4 + Datagram.Length);
                }
            }
            public  UInt16 Identification;
            private UInt16 FlagsFragmentOffset;
            public  bool RES
            {
                get
                {
                    UInt16 v = (UInt16)(FlagsFragmentOffset & RESMask);
                    if (v > 0) return true;
                    else return false;
                }
                set
                {
                    UInt16 v = (UInt16)(FlagsFragmentOffset & ~RESMask);
                    if (value) FlagsFragmentOffset = (UInt16)(FlagsFragmentOffset | RESMask);
                }
            }
            public  bool DF
            {
                get
                {
                    UInt16 v = (UInt16)(FlagsFragmentOffset & DFMask);
                    if (v > 0) return true;
                    else return false;
                }
                set
                {
                    UInt16 v = (UInt16)(FlagsFragmentOffset & ~DFMask);
                    if (value) FlagsFragmentOffset = (UInt16)(FlagsFragmentOffset | DFMask);
                }
            }
            public  bool MF
            {
                get
                {
                    UInt16 v = (UInt16)(FlagsFragmentOffset & MFMask);
                    if (v > 0) return true;
                    else return false;
                }
                set
                {
                    UInt16 v = (UInt16)(FlagsFragmentOffset & ~MFMask);
                    if (value) FlagsFragmentOffset = (UInt16)(FlagsFragmentOffset | MFMask);
                }
            }
            public  UInt16 FragmentOffset
            {
                get
                {
                    return (UInt16)(FlagsFragmentOffset & FOMask);
                }
                set
                {
                    if (value > 8191)
                    {
                        throw new OverflowException();
                    }
                    FlagsFragmentOffset = (UInt16)((FlagsFragmentOffset & ~IHLbyteMask) + value);
                }
            }
            public  byte TTL;
            public  ProtocolType Protocol;
            public  UInt16 HeaderChecksum;
            public  IP SourceAddress;
            public  IP DestinationAddress;
            public  byte[] Options;
             
            public byte[] Datagram;


            // Constructor(s)
            public IPv4Header(byte TOS, UInt16 Identification,
                bool Reserved, bool DontFragment, bool MoreFragments, UInt16 FragmentOffset,
                ProtocolType Protocol, IP Source, IP Destination, byte[] Options, 
                byte[] Data)
            {                
                this.TOS = TOS;
                this.Identification = Identification;
                this.RES = Reserved;
                this.DF = DontFragment;
                this.MF = MoreFragments;
                this.FragmentOffset = FragmentOffset;
                this.Protocol = Protocol;
                this.SourceAddress = Source;
                this.DestinationAddress = Destination;
                this.Options = Options;
                this.Datagram = Data;
            }

            public IPv4Header(bool DontFragment, byte TTL, ProtocolType Protocol, IP Source, IP Destination, byte[] Options, byte[] Datagram):
                this(0, 0, false, false, false, 0, Protocol, Source, Destination, Options, Datagram)
            {

            }

            public IPv4Header(byte[] data)
            {
                byte Version = (byte)(data[0] >> 4);
                if (Version != 0b0100_0000)
                {
                    throw new ArgumentException("Incorrect IP Version");
                }

                byte IHL = (byte)(data[0] - 0b0100_0000);
                byte[] header = new byte[IHL * 4];
                Array.Copy(data, header, IHL * 4);

                byte TypeOfService = header[1];
                UInt16 TotalLength = BitConverter.ToUInt16(header, 2);
                Identification = BitConverter.ToUInt16(header, 4);

            }


            static IPv4Header DefaultTCPWrapper(IP Source, IP Destination, byte[] Datagram)
            {
                return new IPv4Header(false, 255, ProtocolType.TCP, Source, Destination, new byte[0], Datagram);
            }
            // Method(s)
            public byte[] Tobytes()
            {
                List<byte> bytes = new List<byte>();
                

                bytes.Add(VersionIHL);
                bytes.Add(TOS);
                bytes.AddRange(BitConverter.Getbytes(Length));
                bytes.AddRange(BitConverter.Getbytes(Identification));
                bytes.AddRange(BitConverter.Getbytes(FlagsFragmentOffset));
                bytes.Add(TTL);
                bytes.Add((byte)Protocol);
                bytes.AddRange(BitConverter.Getbytes(HeaderChecksum));
                bytes.AddRange(SourceAddress.Tobytes());
                bytes.AddRange(DestinationAddress.Tobytes());
                bytes.AddRange(Options);
                bytes.AddRange(Datagram);

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
