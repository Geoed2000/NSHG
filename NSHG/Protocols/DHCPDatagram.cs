using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG.Protocols.DHCP
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
        public Optionlist options;



        public DHCPDatagram(byte op, UInt32 xid, byte htype = 1, byte hlen = 10, byte hops = 0, UInt16 secs = 0, bool Broadcast = false, IP ClientIP = null, IP YourIP = null, IP ServerIP = null, IP RelayIP = null,
            MAC ClientMAC = null, Optionlist options = null)
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
            if (options == null) this.options = new Optionlist();
            else this.options = options;
        }

        public DHCPDatagram(byte[] bytes)
        {
            op = bytes[0];
            htype = bytes[1];
            hlen = bytes[2];
            hops = bytes[3];
            xid = BitConverter.ToUInt32(bytes, 4);
            secs = BitConverter.ToUInt16(bytes, 12);
            flags = BitConverter.ToUInt16(bytes, 16);
            ciaddr = new IP (bytes, 20);
            yiaddr = new IP (bytes, 24);
            siaddr = new IP (bytes, 28);
            giaddr = new IP (bytes, 32);
            chaddr = new MAC(bytes, 36);
            bytes.CopyTo(sname, 42);
            bytes.CopyTo(file, 100);
            byte[] optionbytes = new byte[bytes.Length-228];
            bytes.CopyTo(optionbytes, 228);
            options = new Optionlist(optionbytes);

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
            if (ciaddr != null)
                bytes.AddRange(ciaddr.ToBytes());
            else
                bytes.AddRange(IP.Zero.ToBytes());
            if (yiaddr != null)
                bytes.AddRange(yiaddr.ToBytes());
            else
                bytes.AddRange(IP.Zero.ToBytes());
            if (siaddr != null)
                bytes.AddRange(siaddr.ToBytes());
            else
                bytes.AddRange(IP.Zero.ToBytes());
            if (giaddr != null)
                bytes.AddRange(giaddr.ToBytes());
            else
                bytes.AddRange(IP.Zero.ToBytes());
            if (chaddr != null)
                bytes.AddRange(chaddr.ToBytes());
            else
                bytes.AddRange(IP.Zero.ToBytes());
            bytes.AddRange(sname);
            bytes.AddRange(file);
            bytes.AddRange(options.ToBytes());
            return bytes.ToArray();

        }
    }

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

    public class DHCPOption
    {
        public Tag tag;
        public byte[] data;

        public DHCPOption(Tag tag, byte[] data = null)
        {
            this.tag = tag;
            this.data = data;
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
            byte len = (byte)data.Length;
            byte[] b = new byte[len + 2];
            switch ((byte)tag)
            {
                //tag, no length, no data
                case 0:
                    return new byte[1] { (byte)tag };

                //tag, 1 byte data
                case 23:
                case 53:
                    return new byte[3] { (byte)tag, 1, data[0] };

                //tag, no length, 4 bytes data
                case 1:
                case 50:
                case 51:
                case 54:
                    b[0] = (byte)tag;
                    b[1] = 4;
                    data.CopyTo(b, 2);
                    return b;

                //tag, length N + 2, N byte(s) data
                case 3:
                case 6:
                case 55:
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
        public Optionlist(byte[] b): base()
        {
            List<byte> bytes = new List<byte>(b);

            while (bytes.Count != 0)
            {
                Tag tag = (Tag)bytes[0];
                if (tag == Tag.endMark) break;
                byte len = bytes[1];
                byte[] data = new byte[len];

                bytes.CopyTo(0, data, 0, len);

                bytes.RemoveRange(0, len + 2);
                Add(new DHCPOption(tag, data));
            }

        }

        public Optionlist() : base()
        {

        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            foreach (DHCPOption o in this)
            {
                bytes.AddRange(o.ToBytes());
            }
            bytes.Add(255);
            if (bytes.Count % 2 == 1)
            {
                bytes.Add(0);
            }
            return bytes.ToArray();
        }


    }
}