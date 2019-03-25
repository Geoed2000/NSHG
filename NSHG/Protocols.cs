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
