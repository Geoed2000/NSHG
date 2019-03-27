using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG
{
    namespace Protocols
    {
        public class DHCPDatagram
        {
            static UInt16 BMask = 0b1000000000000000;
            public byte op;
            public byte htype;
            public byte hlen;
            public byte hops;
            public UInt32 xid;
            public UInt16 secs;
            private UInt16 flags;
            bool B
            {
                get
                {
                    return (UInt16)(flags & BMask) == BMask;
                }
                set
                {
                    flags = (UInt16)(flags & ~BMask);
                    if (value) flags += BMask;
                }
            }
            public IP ciaddr;
            public IP yiaddr;
            public IP siaddr;
            public IP giaddr;
            public MAC chaddr;
            public byte[] sname = new byte[64];
            public byte[] file = new byte[128];
            public byte[] options;

            public DHCPDatagram(byte op, UInt32 xid,  byte htype = 1, byte hlen = 10, byte hops = 0, UInt16 secs = 0, bool Broadcast = false, IP ClientIP = null, IP YourIP = null, IP ServerIP = null, IP RelayIP = null,
                MAC ClientMAC = null, byte[] options = null)
            {
                this.op = op;
                this.xid = xid;
                this.htype = htype;
                this.hlen = hlen;
                this.hops = hops;
                this.secs = secs;
                this.B = Broadcast;
                this.ciaddr = ClientIP;
                this.yiaddr = YourIP;
                this.siaddr = ServerIP;
                this.giaddr = RelayIP;
                this.chaddr = ClientMAC;
                if (options == null) this.options = new byte[0];
                else this.options = options;
            }

            public byte[] ToBytes()
            {
                List<byte> bytes = new List<byte>();
                bytes.Add(op);
                bytes.Add(htype);
                bytes.Add(hlen);
                bytes.Add(hops);
                bytes.AddRange(BitConverter.GetBytes(xid));
                bytes.AddRange(BitConverter.GetBytes(secs));
                bytes.AddRange(BitConverter.GetBytes(flags));
                bytes.AddRange(ciaddr.ToBytes());
                bytes.AddRange(yiaddr.ToBytes());
                bytes.AddRange(siaddr.ToBytes());
                bytes.AddRange(giaddr.ToBytes());
                bytes.AddRange(chaddr.ToBytes());
                bytes.AddRange(sname);
                bytes.AddRange(file);
                bytes.AddRange(options);
                return bytes.ToArray();

            }

            public class DHCPOption
            {
                Tag tag;
                byte[] data;

                public enum Tag : byte
                {
                    pad = 0,
                    subnetmask = 1,
                    router = 3,
                    domainserver = 6,
                    defaultIPTTL = 23,
                    addressRequest = 50,
                    addressTime = 51,
                    dhcpMsgType = 53,
                    dhcpServerID = 54,
                    paramaterList = 55,

                    
                    endMark = 255
                }

                public enum MsgType : byte
                {
                    DHCPDISCOVER = 1,
                    DHCPOFFER = 2,
                    DHCPREQUEST = 3,
                    DHCPDECLINE = 4,
                    DHCPACK = 5,
                    DHCPNAK = 6,
                    DHCPRELEASE = 7,
                    DHCPINFORM = 8          
                }

                public byte[] ToBytes()
                {
                    byte len = (byte)(data.Length + 2);
                    byte[] b = new byte[len];
                    switch ((byte)tag)
                    {
                        //tag, no length, no data
                        case 0:
                            return new byte[1] { (byte)tag };

                        //tag, 1 byte data
                        case 23:
                        case 53:
                            return new byte[2] { (byte)tag, data[0] };

                        //tag, no length, 4 bytes data
                        case 1:
                        case 50:
                        case 51:
                        case 54:
                            b = new byte[5];
                            b[0] = (byte)tag;
                            data.CopyTo(b, 1);
                            return b;

                        //tag, length length, N byte(s) data
                        case 3:
                        case 6:
                        default:
                            b[0] = (byte)tag;
                            b[1] = len;
                            data.CopyTo(b, 2);
                            return b;
                    }
                }
            }

            public class Optionlist : List<DHCPOption>
            {
                public byte[] ToBytes()
                {
                    List<byte> bytes = new List<byte>();
                    foreach(DHCPOption o in this)
                    {
                        bytes.AddRange(o.ToBytes());
                    }
                    bytes.Add(255);
                    if (bytes.Count % 2 == 1)
                    {
                        bytes.Add(0);
                    }
                }
            }


        }

        public class DSPDatagram
        {

        }

        public class FTPDatagram
        {

        }

        public class HTTPDatagram
        {

        }

        public class HTTPSDatagram
        {

        }

        public class POP3Datagram
        {

        }

        public class SMTPDatagram
        {

        }

        public class SSHDatagram
        {

        }
    }
}
