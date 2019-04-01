using System;
using System.Collections.Generic;
using System.Xml;
using NSHG.Applications;
using NSHG.Protocols.ICMP;
using NSHG.Protocols.IPv4;
using NSHG.Protocols.TCP;
using NSHG.Protocols.UDP;
using NSHG.NetworkInterfaces;

namespace NSHG
{    
    public class System
    {
        public uint ID;

        public Dictionary<MAC,NetworkInterface> NetworkInterfaces;
        
        public List<String> Log;

        public bool respondToEcho;
        

        public event Action<Byte[],NetworkInterface> OnRecievedPacket;
        public event Action<Byte[],NetworkInterface> OnCorruptPacket; 
        public event Action<IPv4Header,NetworkInterface> OnNotForMe;  
        public event Action<IPv4Header, ICMPHeader, byte, NetworkInterface> OnICMPPacket;
        public event Action<IPv4Header, TCPHeader, UInt16, NetworkInterface> OnTCPPacket;
        public event Action<IPv4Header, UDPHeader, UInt16, NetworkInterface> OnUDPPacket;

        public System(uint ID, Dictionary<MAC,NetworkInterface> NetworkInterfaces = null, bool Respondtoecho = true, bool initapps = true)
        {
            this.ID = ID;
            if (NetworkInterfaces != null)
            {
                this.NetworkInterfaces = NetworkInterfaces;
            }
            else
            {
                NetworkInterfaces = new Dictionary<MAC, NetworkInterface>();
            }
            this.respondToEcho = Respondtoecho;
            OnTick += AdapterTick;
            if (initapps) AppsInit();
            Log = new List<string>();
        }

        public override bool Equals(object Obj)
        {
            System s;
            bool Equal = true;

            try
            {
                s = (System)Obj;
            }
            catch (InvalidCastException)
            {
                return false;
            }

            if (!((ID == s.ID) && (respondToEcho == s.respondToEcho))) Equal = false;

            foreach (Adapter a in NetworkInterfaces.Values)
            {
                if (!(s.NetworkInterfaces.ContainsValue(a))) Equal = false;//compares adapter a to every adapter in the s.adapters list until it
                                                           //finds an addapter where adapter 'a' is equal to it (runs a.equals(adapter) for each adapter)
            }
            foreach (Adapter a in s.NetworkInterfaces.Values)
            {
                if (!NetworkInterfaces.ContainsValue(a)) Equal = false; //compares adapter a to every adapter in the s.adapters list until it 
                                                          //finds an addapter where adapter 'a' is equal to it (runs a.equals(adapter) for each adapter)
            }
            return Equal;
        }

        public static System FromXML(XmlNode Parent)
        {
            uint ID = 0;
            Dictionary<MAC, NetworkInterface> NetworkInterfaces = new Dictionary<MAC, NetworkInterface>();
            bool respondToEcho = true;

            foreach (XmlNode n in Parent.ChildNodes)
            {
                switch (n.Name.ToLower())
                {
                    case "id":
                        if (!uint.TryParse(n.InnerText, out ID))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("failed to read ID");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "adapter":
                        try
                        {
                            Adapter a = Adapter.FromXML(n);
                            NetworkInterfaces.Add(a.MyMACAddress,a);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read adapter");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "groupadapter":
                        try
                        {
                            GroupAdapter a = GroupAdapter.FromXML(n);
                            NetworkInterfaces.Add(a.MyMACAddress, a);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read groupadapter");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "respondtoecho":
                        try
                        {
                            respondToEcho = bool.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read flag: respondtoecho");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                }
            }
            if (ID == 0)
            {
                throw new Exception("Invalid System XML ID not Specified");
            }

            return new System(ID,NetworkInterfaces,respondToEcho);


        }

        public XmlNode ToXML(XmlDocument doc)
        {
            XmlNode parent = doc.CreateElement("System");

            XmlNode IDNode = doc.CreateElement("ID");
            IDNode.InnerText = this.ID.ToString();
            parent.AppendChild(IDNode);

            XmlNode RespondToEchoNode = doc.CreateElement("RespondToEcho");
            RespondToEchoNode.InnerText = this.respondToEcho.ToString();
            parent.AppendChild(RespondToEchoNode);

            foreach (NetworkInterface a in NetworkInterfaces.Values)
            {
                parent.AppendChild(a.ToXML(doc));
            }

            return parent;
        }

        public bool GetConectableAdapter(out NetworkInterface a)
        {
            foreach (NetworkInterface NI in NetworkInterfaces.Values)
            {
                if (!NI.Connected || NI.GetType().ToString() == "NSHG.NetworkInterfaces.GroupAdapter")
                {
                    a = NI;
                    return true;
                }
            }
            a = null;
            return false;
        }

        /// <summary>
        /// will return an adapter that is connected but not associated to another adapter if not found will instead return a new adapter that isn't connected
        /// </summary>
        /// <param name="a">adapter that is free to connect to</param>
        /// <param name="id">id of the system to be connected to</param>
        /// <returns> if an adapter was found</returns>
        public bool GetConnectedUnassociatedAdapter(out NetworkInterface a, uint id)
        {
            foreach (NetworkInterface adapt in NetworkInterfaces.Values)
            {
                if (adapt.Connected == true && adapt.Associated == false)
                {
                    a = adapt;
                    return true;
                }
            }
            a = null;
            return false;
        }

        // Network Layer


        // Packet Handeling
        public void Packet(byte[] datagram, NetworkInterface a)
        {
            OnRecievedPacket.Invoke(datagram, a);
            IPv4Header Data;
            try
            {
                Data = new IPv4Header(datagram);
            }
            catch
            {
                OnCorruptPacket.BeginInvoke(datagram, a, null, null);
                return;
            }
            
            if(Data.DestinationAddress == a.LocalIP)
            {
                switch (Data.Protocol)
                {
                    case IPv4Header.ProtocolType.ICMP:
                        handleICMPPacket(Data, a);
                        break;
                    case IPv4Header.ProtocolType.TCP:
                        TCPHeader TCP;
                        try
                        {
                            TCP = new TCPHeader(Data.Datagram);
                        }
                        catch
                        {
                            OnCorruptPacket?.Invoke(datagram, a);
                            break;
                        }

                        OnTCPPacket?.Invoke(Data, TCP, TCP.DestinationPort, a);
                        break;
                    case IPv4Header.ProtocolType.UDP:
                        UDPHeader UDP;
                        try
                        {
                            UDP = new UDPHeader(Data.Datagram);
                        }
                        catch
                        {
                            OnCorruptPacket?.Invoke(datagram, a);
                            break;
                        }
                        OnUDPPacket?.Invoke(Data, UDP, UDP.DestinationPort, a);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                OnNotForMe.Invoke(Data, a);
            }
        }

        public bool SendPacket(MAC adapter, IPv4Header data)
        {
            if (NetworkInterfaces.ContainsKey(adapter))
            {
                NetworkInterfaces[adapter].SendPacket(data.ToBytes());
                return true;
            }
            else
            {
                return false;
            }
        }

        protected virtual void handleICMPPacket(IPv4Header datagram, NetworkInterface a)
        {
            switch (datagram.Datagram[0])
            {
                case 0: // Echo Replys
                    {
                        ICMPEchoRequestReply header = new ICMPEchoRequestReply(datagram.Datagram);
                        OnICMPPacket?.Invoke(datagram, header, header.Code, a);
                    }
                    break;
                case 8: // Echo Request
                    if (respondToEcho)
                    {
                        ICMPEchoRequestReply header = new ICMPEchoRequestReply(datagram.Datagram);
                        ICMPEchoRequestReply icmp = new ICMPEchoRequestReply(0, header.Identifier, (UInt16)(header.SequenceNumber + 1));
                        IPv4Header ipv4 = new IPv4Header(datagram.Identification, false, false, 255, IPv4Header.ProtocolType.ICMP, a.LocalIP, datagram.SourceAddress, null, icmp.ToBytes());

                        a.SendPacket(ipv4.ToBytes());
                    }
                    break;
            }
        }
        
        // Application Layer
        public List<Application> Apps = new List<Application>();
        
        public RoutingTable RoutingTable;

        protected virtual void AppsInit()
        { 
            RoutingTable = new RoutingTable(this);
            Apps.Add(RoutingTable);
        } 
        // AI

        private event Action<uint> OnTick;

        public void Tick(uint tick )
        {
            //Other AI Logic
            OnTick.Invoke(tick);
        }

        public void AdapterTick(uint tick)
        {
            foreach (NetworkInterface A in NetworkInterfaces.Values)
            {
                A.Tick(tick);
            }
        }

        public void ApplicationTick(uint tick)
        {
            foreach (Application a in Apps)
            {
                a.OnTick(tick);
            }
        }
    }

    //public class PC : System
    //{
    //    public new XmlNode ToXML(XmlDocument doc)
    //    {
    //        XmlNode parent = doc.CreateElement("PC");

    //        XmlNode IDNode = doc.CreateElement("ID");
    //        IDNode.InnerText = this.ID.ToString();
    //        parent.AppendChild(IDNode);

    //        XmlNode RespondToEchoNode = doc.CreateElement("RespondToEcho");
    //        RespondToEchoNode.InnerText = this.respondToEcho.ToString();
    //        parent.AppendChild(RespondToEchoNode);

    //        foreach (NetworkInterface a in NetworkInterfaces.Values)
    //        {
    //            parent.AppendChild(a.ToXML(doc));
    //        }

    //        return parent;
    //    }

    //    public new static PC FromXML(XmlNode Parent)
    //    {
    //        uint ID = 0;
    //        Dictionary<MAC, NetworkInterface> NetworkInterfaces = new Dictionary<MAC, NetworkInterface>();
    //        bool respondToEcho = true;

    //        foreach (XmlNode n in Parent.ChildNodes)
    //        {
    //            switch (n.Name.ToLower())
    //            {
    //                case "id":
    //                    if (!uint.TryParse(n.InnerText, out ID))
    //                    {
    //                        Console.ForegroundColor = ConsoleColor.Red;
    //                        Console.WriteLine("failed to read ID");
    //                        Console.ForegroundColor = ConsoleColor.White;
    //                    }
    //                    break;
    //                case "adapter":
    //                    try
    //                    {
    //                        Adapter a = Adapter.FromXML(n);
    //                        NetworkInterfaces.Add(a.MyMACAddress, a);
    //                    }
    //                    catch
    //                    {
    //                        Console.ForegroundColor = ConsoleColor.Red;
    //                        Console.WriteLine("Failed to read adapter");
    //                        Console.ForegroundColor = ConsoleColor.White;
    //                    }
    //                    break;
    //                case "groupadapter":
    //                    try
    //                    {
    //                        GroupAdapter a = GroupAdapter.FromXML(n);
    //                        NetworkInterfaces.Add(a.MyMACAddress, a);
    //                    }
    //                    catch
    //                    {
    //                        Console.ForegroundColor = ConsoleColor.Red;
    //                        Console.WriteLine("Failed to read groupadapter");
    //                        Console.ForegroundColor = ConsoleColor.White;
    //                    }
    //                    break;
    //                case "respondtoecho":
    //                    try
    //                    {
    //                        respondToEcho = bool.Parse(n.InnerText);
    //                    }
    //                    catch
    //                    {
    //                        Console.ForegroundColor = ConsoleColor.Red;
    //                        Console.WriteLine("Failed to read flag: respondtoecho");
    //                        Console.ForegroundColor = ConsoleColor.White;
    //                    }
    //                    break;
    //            }
    //        }
    //        if (ID == 0)
    //        {
    //            throw new Exception("Invalid System XML ID not Specified");
    //        }

    //        return new PC(ID, NetworkInterfaces, respondToEcho);


    //    }

    //    protected override void AppsInit()
    //    {
    //        Apps.Add(new DHCPClient(this, new List<NetworkInterface>(NetworkInterfaces.Values)));
    //        RoutingTable = new SystemRoutingTable(this);
    //        Apps.Add(RoutingTable);
    //    }
    //}

    public class Router : System
    {

        public Router(uint ID, Dictionary<MAC,NetworkInterface> NetworkInterfaces, bool RespToEcho = true, bool initapps = false) : base(ID,NetworkInterfaces, RespToEcho, true)
        {

        }

        public new XmlNode ToXML(XmlDocument doc)
        {
            XmlNode parent = doc.CreateElement("Router");

            XmlNode IDNode = doc.CreateElement("ID");
            IDNode.InnerText = this.ID.ToString();
            parent.AppendChild(IDNode);

            XmlNode RespondToEchoNode = doc.CreateElement("RespondToEcho");
            RespondToEchoNode.InnerText = this.respondToEcho.ToString();
            parent.AppendChild(RespondToEchoNode);

            foreach (NetworkInterface a in NetworkInterfaces.Values)
            {
                parent.AppendChild(a.ToXML(doc));
            }

            return parent;
        }

        public new static Router FromXML(XmlNode Parent)
        {
            uint ID = 0;
            Dictionary<MAC, NetworkInterface> NetworkInterfaces = new Dictionary<MAC, NetworkInterface>();
            bool respondToEcho = true;

            foreach (XmlNode n in Parent.ChildNodes)
            {
                switch (n.Name.ToLower())
                {
                    case "id":
                        if (!uint.TryParse(n.InnerText, out ID))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("failed to read ID");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "adapter":
                        try
                        {
                            Adapter a = Adapter.FromXML(n);
                            NetworkInterfaces.Add(a.MyMACAddress, a);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read adapter");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "groupadapter":
                        try
                        {
                            GroupAdapter a = GroupAdapter.FromXML(n);
                            NetworkInterfaces.Add(a.MyMACAddress, a);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read groupadapter");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "respondtoecho":
                        try
                        {
                            respondToEcho = bool.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read flag: respondtoecho");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                }
            }
            if (ID == 0)
            {
                throw new Exception("Invalid System XML ID not Specified");
            }

            return new Router(ID, NetworkInterfaces, respondToEcho);


        }

        protected override void AppsInit()
        {
            foreach (NetworkInterface n in NetworkInterfaces.Values)
            {
                if (n.GetType().ToString() == "NSHG.NetworkInterfaces.GroupAdapter")
                {
                    DHCPServer s = new DHCPServer(this,n,n.LocalIP);
                    Apps.Add(s);

                }
            }

            RoutingTable = new SystemRoutingTable(this);
            Apps.Add(RoutingTable);
        }
    }

    //public class EchoServer : System
    //{

    //}
}
