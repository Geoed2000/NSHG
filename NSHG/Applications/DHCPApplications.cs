using System;
using System.Collections.Generic;
using System.Text;
using NSHG.Protocols.DHCP;
using NSHG.Protocols.UDP;
using NSHG.Protocols.IPv4;
using NSHG.NetworkInterfaces;

namespace NSHG.Applications
{
    public class DHCPClient : Application
    {
        public List<string> Log = new List<string>();
        
        public class session
        {
            public enum State
            {
                INIT = 0,
                SELECTING = 1,
                REQUESTING = 2,
                BOUND = 3,
                RENEWING = 4,
                REBINDING = 5,
                INITREBOOT = 6,
                REBOOTING = 7

            }
            
            public  UInt32 xid;
            private UInt32 T1;
            private UInt32 T2;
            private int T3;
            private int failures = 0;
            Adapter a;
            private State state;

            private DHCPDatagram Offer;

            private DHCPDatagram RequestResponce;

            List<byte> paramlist = new List<byte>
            {
                (byte)Tag.subnetmask,
                (byte)Tag.dhcpServerID,
                (byte)Tag.router,
                (byte)Tag.domainserver
            };


            IP offeredIP = null;
            IP DHCPServerIP = null;
            
            public session(Adapter a)
            {
                this.a = a;
                if (a.LocalIP != null)
                    state = State.INIT;
                else state = State.INITREBOOT;
                xid = (UInt32)new Random().Next();

            }

            public void Packet(DHCPDatagram d)
            {
                foreach (DHCPOption o in d.options)
                {
                    if(o.tag == Tag.dhcpMsgType)
                    {
                        switch ((DHCPOption.MsgType)o.data[0])
                        {
                            case DHCPOption.MsgType.DHCPACK:
                            case DHCPOption.MsgType.DHCPNAK:
                                RequestResponce = d;
                                break;
                            case DHCPOption.MsgType.DHCPOFFER:
                                Offer = d;
                                break;
                        }
                    }
                }
            }

            public void OnTick(uint tick)
            {
                switch (state)
                {
                    case State.INIT:
                        if (failures <= 10)
                        {
                            offeredIP = null;
                            DHCPServerIP = null;
                            Offer = null;
                            RequestResponce = null;
                            
                            
                            // Create DHCP Options
                            Optionlist ol = new Optionlist();
                            ol.Add(new DHCPOption(Tag.dhcpMsgType, new byte[1] { (byte)DHCPOption.MsgType.DHCPDISCOVER }));
                            // Requesting Spesific IP
                            if (a.LocalIP != null) ol.Add(new DHCPOption(Tag.addressRequest, a.LocalIP.ToBytes()));
                            // Asking for Spesific Params back
                            
                            ol.Add(new DHCPOption(Tag.paramaterList, paramlist.ToArray()));
                            
                            // create DHCP Datagram
                            DHCPDatagram DhcpDatagram = new DHCPDatagram(0, xid, ClientMAC: a.MyMACAddress, Broadcast: true, options: ol);

                            // UDP and IPv4 Headers
                            UDPHeader UDP = new UDPHeader(68, 67, DhcpDatagram.ToBytes());
                            IPv4Header IPv4 = IPv4Header.DefaultUDPWrapper(IP.Zero, IP.Broadcast, UDP.ToBytes(), 32);
                            state = State.SELECTING;

                            // Send IPv4 Packet
                            a.SendPacket(IPv4.ToBytes());

                            T3 = 500;
                        }
                        break;
                    case State.SELECTING:
                        if (Offer != null)
                        {
                            if (Offer.yiaddr != null)
                            {
                                offeredIP = Offer.yiaddr;
                                DHCPServerIP = null;

                                foreach (DHCPOption o in Offer.options)
                                {
                                    if (o.tag == Tag.dhcpServerID) DHCPServerIP = new IP(o.data);
                                }

                                Optionlist ol = new Optionlist()
                                {
                                    new DHCPOption(Tag.dhcpMsgType, new byte[1] { (byte)DHCPOption.MsgType.DHCPREQUEST }),
                                    new DHCPOption(Tag.addressRequest, offeredIP.ToBytes())
                                };

                                if (DHCPServerIP != null) ol.Add(new DHCPOption(Tag.dhcpServerID, DHCPServerIP.ToBytes()));
                                
                                ol.Add(new DHCPOption(Tag.paramaterList, paramlist.ToArray()));
                                


                                DHCPDatagram DhcpDatagram = new DHCPDatagram(0, xid, ClientMAC: a.MyMACAddress, options: ol);
                                UDPHeader UDP = new UDPHeader(68, 67, DhcpDatagram.ToBytes());
                                IP server = IP.Broadcast;
                                if (Offer.siaddr != null) server = Offer.siaddr;
                                IPv4Header IPv4 = IPv4Header.DefaultUDPWrapper(IP.Zero, server, UDP.ToBytes(), 32);

                                a.SendPacket(IPv4.ToBytes());

                                state = State.REQUESTING;
                            }
                        }else
                        if(T3-- <= 0)
                        {
                            failures++;
                            state = State.INIT;
                        }
                        break;
                    case State.REQUESTING:
                        if (RequestResponce != null)
                        {
                            foreach (DHCPOption o in RequestResponce.options)
                            {
                                if (o.tag == Tag.dhcpMsgType)
                                {
                                    if (o.data[0] == (byte)DHCPOption.MsgType.DHCPNAK)
                                    {
                                        Offer = null;
                                        state = State.INIT;
                                        break;
                                    }else 
                                    if (o.data[0] == (byte)DHCPOption.MsgType.DHCPACK)
                                    {
                                        foreach(DHCPOption op in RequestResponce.options)
                                        {
                                            switch (op.tag)
                                            {
                                                case Tag.addressTime:
                                                    T1 = BitConverter.ToUInt32(op.data, 0);
                                                    T2 = T1 - 500;
                                                    break;
                                                case Tag.subnetmask:
                                                    a.SubnetMask = new IP(op.data,0);
                                                    break;
                                                case Tag.router:
                                                    a.DefaultGateway = new IP(op.data, 0);
                                                    break;
                                                case Tag.domainserver:
                                                    a.DNS = new IP(op.data,0);
                                                    break;
                                            }
                                        }
                                        a.LocalIP = RequestResponce.yiaddr;
                                        state = State.BOUND;
                                        break;
                                    }
                                }
                            }
                                

                        }
                        break;
                    case State.BOUND:
                        if (T2-- <= 0)
                        {
                            T1--;
                            Optionlist ol = new Optionlist()
                                {
                                    new DHCPOption(Tag.dhcpMsgType, new byte[1] { (byte)DHCPOption.MsgType.DHCPREQUEST }),
                                    new DHCPOption(Tag.addressRequest, a.LocalIP.ToBytes())
                                };

                            if (DHCPServerIP != null) ol.Add(new DHCPOption(Tag.dhcpServerID, DHCPServerIP.ToBytes()));

                            ol.Add(new DHCPOption(Tag.paramaterList, paramlist.ToArray()));



                            DHCPDatagram DhcpDatagram = new DHCPDatagram(0, xid, ClientMAC: a.MyMACAddress, options: ol);
                            UDPHeader UDP = new UDPHeader(68, 67, DhcpDatagram.ToBytes());
                            IP server = IP.Broadcast;
                            if (Offer.siaddr != null) server = Offer.siaddr;
                            IPv4Header IPv4 = IPv4Header.DefaultUDPWrapper(a.LocalIP, server, UDP.ToBytes(), 32);

                            RequestResponce = null;

                            a.SendPacket(IPv4.ToBytes());

                            state = State.RENEWING;

                        }
                        break;
                    case State.RENEWING:
                        if (RequestResponce != null)
                        {
                            foreach (DHCPOption o in RequestResponce.options)
                            {
                                if (o.tag == Tag.dhcpMsgType)
                                {
                                    if (o.data[0] == (byte)DHCPOption.MsgType.DHCPNAK)
                                    {
                                        Offer = null;
                                        state = State.INIT;
                                        break;
                                    }
                                    else
                                    if (o.data[0] == (byte)DHCPOption.MsgType.DHCPACK)
                                    {
                                        foreach (DHCPOption op in RequestResponce.options)
                                        {
                                            switch (op.tag)
                                            {
                                                case Tag.addressTime:
                                                    T1 = BitConverter.ToUInt32(op.data, 0);
                                                    T2 = T1 - 200;
                                                    break;
                                                case Tag.subnetmask:
                                                    a.SubnetMask = new IP(op.data, 0);
                                                    break;
                                                case Tag.router:
                                                    a.DefaultGateway = new IP(op.data, 0);
                                                    break;
                                                case Tag.domainserver:
                                                    a.DNS = new IP(op.data, 0);
                                                    break;
                                            }
                                        }
                                        a.LocalIP = RequestResponce.yiaddr;
                                        state = State.BOUND;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        if (T1-- <= 0)
                        {
                            Optionlist ol = new Optionlist()
                            {
                                new DHCPOption(Tag.dhcpMsgType, new byte[1] { (byte)DHCPOption.MsgType.DHCPREQUEST }),
                                new DHCPOption(Tag.addressRequest, a.LocalIP.ToBytes())
                            };

                            if (DHCPServerIP != null) ol.Add(new DHCPOption(Tag.dhcpServerID, DHCPServerIP.ToBytes()));

                            ol.Add(new DHCPOption(Tag.paramaterList, paramlist.ToArray()));
                            
                            DHCPDatagram DhcpDatagram = new DHCPDatagram(0, xid, ClientMAC: a.MyMACAddress, options: ol);
                            UDPHeader UDP = new UDPHeader(68, 67, DhcpDatagram.ToBytes());
                            IP server = IP.Broadcast;
                            if (Offer.siaddr != null) server = Offer.siaddr;
                            IPv4Header IPv4 = IPv4Header.DefaultUDPWrapper(a.LocalIP, server, UDP.ToBytes(), 32);

                            state = State.REBINDING;
                            T3 = 200;

                            a.SendPacket(IPv4.ToBytes());                            
                        }
                        break;
                    case State.REBINDING:
                        if (RequestResponce != null)
                        {
                            foreach (DHCPOption o in RequestResponce.options)
                            {
                                if (o.tag == Tag.dhcpMsgType)
                                {
                                    if (o.data[0] == (byte)DHCPOption.MsgType.DHCPNAK)
                                    {
                                        Offer = null;
                                        state = State.INIT;
                                        break;
                                    }
                                    else
                                    if (o.data[0] == (byte)DHCPOption.MsgType.DHCPACK)
                                    {
                                        foreach (DHCPOption op in RequestResponce.options)
                                        {
                                            switch (op.tag)
                                            {
                                                case Tag.addressTime:
                                                    T1 = BitConverter.ToUInt32(op.data, 0);
                                                    T2 = T1 - 200;
                                                    break;
                                                case Tag.subnetmask:
                                                    a.SubnetMask = new IP(op.data, 0);
                                                    break;
                                                case Tag.router:
                                                    a.DefaultGateway = new IP(op.data, 0);
                                                    break;
                                                case Tag.domainserver:
                                                    a.DNS = new IP(op.data, 0);
                                                    break;
                                            }
                                        }
                                        a.LocalIP = RequestResponce.yiaddr;
                                        state = State.BOUND;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        if (T3-- <= 0)
                        {
                            state = State.INIT;
                        }
                        break;
                    case State.INITREBOOT:
                        state = State.INIT;
                        break;
                    case State.REBOOTING:
                        state = State.INIT;
                        break;
                }
            }
        }

        SortedList<uint,session> sessions = new SortedList<uint,session>();
        
        
        public DHCPClient(System s, List<NetworkInterface> adapters)
        {
            s.OnUDPPacket += (packet);
            foreach (Adapter a in adapters)
            {
                session sesh = new session(a);
                sessions.Add(sesh.xid, sesh);
            }
        }

        public void AddAdapter(NetworkInterface n)
        {
            Adapter a;
            try
            {
                a = (Adapter)n;
            }
            catch
            {
                return;
            }
            session s = new session(a);
            sessions.Add(s.xid, s);
        }

        public void packet(IPv4Header ipv4, UDPHeader udp,UInt16 dest, NetworkInterface n)
        {
            if(dest == 68)
            {
                try
                {
                    DHCPDatagram d = new DHCPDatagram(udp.Datagram);
                    if (sessions.ContainsKey(d.xid))
                        sessions[d.xid].Packet(d);
                }
                catch { }
            }
            return;
        }
        
        public override void OnTick(uint tick)
        {
            foreach (session s in sessions.Values) s.OnTick(tick);
        }
    }

    public class DHCPServer : Application
    {
        public List<string> Log = new List<string>();

        public NetworkInterface NetInterface;

        public uint currenttick = 0;

        public DHCPServer(System s, NetworkInterface a, IP Router)
        {
            RouterIP = Router;
            SubnetMask = a.SubnetMask;
            NetInterface = a;
            ThisIP = NetInterface.LocalIP;
            s.OnUDPPacket += packet;
        }

        public class Lease
        {
            public IP ciaddr;
            public MAC chaddr;
            public uint LeaseStartTick;
            public uint LeaseLength;
            public uint LeaseEndTick
            {
                get
                {
                    return LeaseStartTick + LeaseLength;

                }
            }

            public Lease(IP ClientIP, MAC ClientHardwareAddress, uint LeaseStartTick, uint LeaseLength)
            {
                ciaddr = ClientIP;
                chaddr = ClientHardwareAddress;
                this.LeaseStartTick = LeaseStartTick;
                this.LeaseLength = LeaseLength;
            }
        }

        public SortedList<IP, Lease> Leases = new SortedList<IP, Lease>();

        public SortedList<IP, Lease> Reserved = new SortedList<IP, Lease>();

        static Random r = new Random();

        IP RouterIP;
        readonly IP ThisIP;
        IP SubnetMask;
        IP DNS = new IP(new byte[4] { 1, 1, 1, 1 });

        public bool isAvailable(IP ip, MAC client)
        {
            if (Leases.ContainsKey(ip))
            {
                if (Leases[ip].chaddr == client) return true;
                else return false;
            }
            if (Reserved.ContainsKey(ip))
            {
                if (Reserved[ip].chaddr == client) return true;
                else return false;
            }
            else return true;
        }

        public IP NewAddress(MAC client)
        {
            IP checking = new IP(ThisIP.ToBytes());
            do
            {
                checking++;

            } while (isAvailable(checking, client));

            if ((checking & SubnetMask) != (ThisIP & SubnetMask))
            {
                return null;
            }
            return checking;
            
        }

        public void packet(IPv4Header ipv4, UDPHeader udp, UInt16 dest, NetworkInterface a)
        {
            if (dest == 67)
                try
                {
                    DHCPDatagram dHCP = new DHCPDatagram(udp.Datagram);
                    DHCPDatagram newdHCP = new DHCPDatagram(2, dHCP.xid, ClientMAC: dHCP.chaddr);
                    Optionlist ol = new Optionlist();
                   
                    foreach (DHCPOption o in dHCP.options)
                    {
                        switch (o.tag)
                        {
                            case Tag.paramaterList:
                                foreach(byte b in o.data)
                                {
                                    switch ((Tag)b)
                                    {
                                        case Tag.dhcpServerID:
                                            ol.Add(new DHCPOption(Tag.dhcpServerID, a.LocalIP.ToBytes()));
                                            break;
                                        case Tag.router:
                                            ol.Add(new DHCPOption(Tag.router, RouterIP.ToBytes()));
                                            break;
                                        case Tag.subnetmask:
                                            ol.Add(new DHCPOption(Tag.subnetmask, SubnetMask.ToBytes()));
                                            break;
                                        case Tag.domainserver:
                                            ol.Add(new DHCPOption(Tag.domainserver, DNS.ToBytes()));
                                            break;
                                    }
                                }
                                break;
                            case Tag.dhcpMsgType:
                                switch ((DHCPOption.MsgType)o.data[0])
                                {
                                    case DHCPOption.MsgType.DHCPDISCOVER:
                                        newdHCP.yiaddr = NewAddress(dHCP.chaddr);
                                        if (newdHCP == null) break;
                                        ol.Add(new DHCPOption(Tag.dhcpMsgType, new byte[] { (byte)DHCPOption.MsgType.DHCPOFFER }));
                                        break;
                                    case DHCPOption.MsgType.DHCPREQUEST:
                                        IP Request = new IP(dHCP.options.Find(match => match.tag == Tag.addressRequest).data,0);
                                        if (isAvailable(Request, dHCP.chaddr))
                                        {
                                            if (Reserved.ContainsKey(Request))
                                                Reserved.Remove(Request);
                                            if (Leases.ContainsKey(Request))
                                                Leases.Remove(Request);
                                            Lease l = new Lease(Request, dHCP.chaddr, currenttick, (uint)r.Next(1200, 1800));
                                            Leases.Add(l.ciaddr, l);
                                            ol.Add(new DHCPOption(Tag.dhcpMsgType, new byte[] { (byte)DHCPOption.MsgType.DHCPACK}));
                                        }
                                        else ol.Add(new DHCPOption(Tag.dhcpMsgType, new byte[] { (byte)DHCPOption.MsgType.DHCPACK }));
                                        break;

                                }
                                break;
                        }
                    }
                    newdHCP.options = ol;
                    UDPHeader newupd = new UDPHeader(67, 68, newdHCP.ToBytes());
                    IPv4Header newipv4 = IPv4Header.DefaultUDPWrapper(a.LocalIP, IP.Broadcast, newupd.ToBytes(), 32); 
                    a.SendPacket(newipv4.ToBytes());
                }
                catch
                {

                }
        }

        public override void OnTick(uint tick)
        {
            currenttick++;
            if (tick % 5 == 0)
            {
                foreach (Lease l in Leases.Values)
                {
                    if (l.LeaseStartTick + l.LeaseLength < tick && l.LeaseLength != 0) Leases.Remove(l.ciaddr);
                }
            }
        }
    }
}
