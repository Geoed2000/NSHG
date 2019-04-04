using NSHG.NetworkInterfaces;
using NSHG.Protocols.DHCP;
using NSHG.Protocols.IPv4;
using NSHG.Protocols.UDP;
using System;
using System.Collections.Generic;

namespace NSHG.Applications
{
    public class DHCPClient : Application
    {
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

            Application Client;
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
            
            public session(Adapter a, Application client)
            {
                Client = client;
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
                                Client.Log(a.Name + "Request Ackgnolaged and lease of" + d.yiaddr.ToString() + " gained");
                                RequestResponce = d;
                                break;
                            case DHCPOption.MsgType.DHCPNAK:
                                Client.Log(a.Name + "Request Ackgnolaged and lease of" + d.yiaddr.ToString() + " gained");
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

                            T3 = 100;
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


        public DHCPClient(System s, List<NetworkInterface> adapters) : base()
        {
            s.OnUDPPacket += (packet);
            foreach (Adapter a in adapters)
            {
                session sesh = new session(a, this);
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
            Log("Adding new session on adapter " + a.Name + " " + a.MyMACAddress.ToString());
            session s = new session(a, this);
            sessions.Add(s.xid, s);
        }
        public void packet(IPv4Header ipv4, UDPHeader udp, UInt16 dest, NetworkInterface n)
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
            Log("DHCPClient Ticking");
            foreach (session s in sessions.Values) s.OnTick(tick);
        }
        public override void Command(string commandstring)
        { 

        }

    }

    public class DHCPServer : Application
    {
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

        static Random r = new Random();

        public NetworkInterface NetInterface;
        IP GatewayIP;
        readonly IP ThisIP;
        IP SubnetMask;
        IP DNS = new IP(new byte[4] { 1, 1, 1, 1 });
        
        public SortedList<IP, Lease> Leases = new SortedList<IP, Lease>();
        public SortedList<IP, Lease> Reserved = new SortedList<IP, Lease>();
        
        public uint currenttick = 0;

        public DHCPServer(System s, NetworkInterface a, IP Router) : base()
        {
            GatewayIP = Router;
            SubnetMask = a.SubnetMask;
            NetInterface = a;
            ThisIP = NetInterface.LocalIP;
            s.OnUDPPacket += packet;
        }

        
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
                                            ol.Add(new DHCPOption(Tag.router, GatewayIP.ToBytes()));
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
                                        Log("DHCPDescover recieved from " + dHCP.chaddr.ToString());
                                        newdHCP.yiaddr = NewAddress(dHCP.chaddr);
                                        if (newdHCP == null) break;
                                        ol.Add(new DHCPOption(Tag.dhcpMsgType, new byte[] { (byte)DHCPOption.MsgType.DHCPOFFER }));

                                        Log("Offered IP " + newdHCP.yiaddr + " to " + newdHCP.chaddr);
                                        break;
                                    case DHCPOption.MsgType.DHCPREQUEST:
                                        IP Request = new IP(dHCP.options.Find(match => match.tag == Tag.addressRequest).data,0);

                                        Log("DHCPRequest recieved from " + dHCP.chaddr.ToString());

                                        if (isAvailable(Request, dHCP.chaddr))
                                        {
                                            if (Reserved.ContainsKey(Request))
                                                Reserved.Remove(Request);
                                            if (Leases.ContainsKey(Request))
                                                Leases.Remove(Request);
                                            Lease l = new Lease(Request, dHCP.chaddr, currenttick, (uint)r.Next(1200, 1800));
                                            Leases.Add(l.ciaddr, l);
                                            ol.Add(new DHCPOption(Tag.dhcpMsgType, new byte[] { (byte)DHCPOption.MsgType.DHCPACK }));
                                            Log("Leased IP " + l.ciaddr + " to " + l.chaddr + " for " + l.LeaseLength + " ticks");
                                        }
                                        else
                                        {
                                            ol.Add(new DHCPOption(Tag.dhcpMsgType, new byte[] { (byte)DHCPOption.MsgType.DHCPNAK }));
                                            Log("denied lease of " + Request + " to " + dHCP.chaddr);
                                        }
                                            
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
            Log("DHCP Server Ticked");
            currenttick++;
            if (tick % 5 == 0)
            {
                foreach (Lease l in Leases.Values)
                {
                    if (l.LeaseStartTick + l.LeaseLength < tick && l.LeaseLength != 0) Leases.Remove(l.ciaddr);
                }
            }
        }
        public override void Command(string commandstring)
        {
            Log(commandstring);
            string[] command = commandstring.Split(' ');
            switch (command[0].ToLower())
            {
                case "echo":
                    if (command.Length > 1)
                    {
                        switch (command[1].ToLower())
                        {
                            case "all":
                                Log("networkinterface " + NetInterface?.Name + " with macaddress " + NetInterface?.MyMACAddress.ToString());
                                Log("Gateway IP Address " + GatewayIP?.ToString());
                                Log("DNS IP Address " + DNS?.ToString());
                                Log("Leases");
                                foreach (Lease L in Leases.Values)
                                {
                                    Log("    Lease of " + L.ciaddr + " to " + L.chaddr + " starting at tick " + L.LeaseStartTick + " for " + L.LeaseLength + "ticks");
                                }

                                break;
                            case "networkinterface":
                                Log("networkinterface " + NetInterface?.Name + " with macaddress " + NetInterface?.MyMACAddress.ToString());
                                break;
                            case "gateway":
                                Log("Gateway IP Address " + GatewayIP?.ToString());
                                break;
                            case "dns":
                                Log("DNS IP Address " + DNS?.ToString());
                                break;
                            case "leases":
                                Log("Leases");
                                foreach(Lease L in Leases.Values)
                                {
                                    Log("    Lease of " + L.ciaddr + " to " + L.chaddr + " starting at tick " + L.LeaseStartTick + " for " + L.LeaseLength + "ticks");
                                }
                                break;
                        }
                    }
                    else
                    {
                        Log("Please Specify what you would like to echo");
                    }
                    break;
            }
        }
    }
}
