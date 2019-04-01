using NSHG.Applications;
using NSHG.Protocols.ICMP;
using NSHG.Protocols.IPv4;
using NSHG.Protocols.TCP;
using NSHG.Protocols.UDP;
using System;
using System.Collections.Generic;
using System.Xml;

namespace NSHG
{
    public abstract class NetworkInterface
    {
        public string Name { get; set; }
        public uint sysID;
        public MAC MyMACAddress { get; set; }
        public IP LocalIP;
        public IP SubnetMask;
        protected bool _Connected;
        public bool Connected
        {
            get
            {
                return _Connected;
            }
        }
        public bool Associated;

        private readonly object SendLock;
        private Queue<byte[]> _SendQueue;
        public Queue<byte[]> SendQueue
        {
            get
            {
                lock (SendLock)
                {
                    return (_SendQueue);
                }
            }
            set
            {
                lock (SendLock)
                {
                    _SendQueue = value;
                }
            }
        }

        private readonly object RecieveLock;
        private Queue<byte[]> _RecieveQueue;
        public Queue<byte[]> RecieveQueue
        {
            get
            {
                lock (RecieveLock)
                {
                    return (_RecieveQueue);
                }
            }
            set
            {
                lock (RecieveLock)
                {
                    _RecieveQueue = value;
                }
            }
        }

        public event Action<byte[],NetworkInterface> OnRecievedPacket;

        protected void CallOnRecievedPacket(byte[] Data, NetworkInterface NetInterface)
        {
            OnRecievedPacket.Invoke(Data, NetInterface);
        }

        protected NetworkInterface(MAC MACAddress, uint SysID)
        {
            MyMACAddress = MACAddress;
            this.sysID = SysID;


            SendLock = new object();
            RecieveLock = new object();
            SendQueue = new Queue<byte[]>();
            RecieveQueue = new Queue<byte[]>();

        }
        
        public abstract XmlNode ToXML(XmlDocument doc);

        public abstract bool Connect(NetworkInterface a);

        public abstract void Reset();

        public virtual void SendPacket(byte[] datagram)
        {
            SendQueue.Enqueue(datagram);
        }

        public virtual void RecievePacket(byte[] datagram)
        {
            RecieveQueue.Enqueue(datagram);
        }

        public abstract void Tick(uint tick);
    }

    public class Adapter : NetworkInterface
    {
        public IP DefaultGateway;
        public IP DNS;
        public NetworkInterface OtherEnd;
        public uint OtherendID;
        
        public Adapter(MAC MACAddress, uint sysID, string name = "", IP LocalIP = null, IP SubnetMask = null, IP DefaultGateway = null, IP DNS = null, uint OtherendID = 0, bool Connected = false) : base(MACAddress, sysID)
        {
            Name = name;
            if (Connected)
            {
                this.LocalIP = LocalIP;
                this.SubnetMask = SubnetMask;
                this.DefaultGateway = DefaultGateway;
                this.DNS = DNS;
                this.OtherendID = OtherendID;
                _Connected = true;
            }
            else
            {
                _Connected = false;
            }
            Associated = false;
        }
        
        public static Adapter FromXML(XmlNode Parent)
        {
            string Name = null;
            uint sysID = 0;
            MAC MacAddress = null;
            IP LocalIP = null;
            IP SubnetMask = null;
            IP DefaultGateway = null;
            IP DNS = null;
            uint OtherEndid = 0;
            bool Connected = false;

            foreach (XmlNode n in Parent.ChildNodes)
            {
                switch (n.Name.ToLower())
                {
                    case "name":
                        Name = n.InnerText;
                        break;
                    case "sysid":
                        try
                        {
                            sysID = uint.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read Adapter system ID, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "mac":
                    case "macaddress":
                        try
                        {
                            MacAddress = MAC.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read MAC address, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "localip":
                        try
                        {
                            LocalIP = IP.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read IP address, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "subnetmask":
                        try
                        {
                            SubnetMask = IP.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read SubnetMask, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "defaultgateway":
                        try
                        {
                            DefaultGateway = IP.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read DefaultGateway, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "dns":
                        try
                        {
                            DNS = IP.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read DNS, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "connectedsystem":
                    case "otherend":
                        try
                        {
                            OtherEndid = uint.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read Connected System(otherend), invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "connected":
                        try
                        {
                            Connected = bool.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read Connected status, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                }
            }
            if (MacAddress == null||sysID == 0)
            {
                throw new ArgumentException("MacAddress or SYSID not provided");
            }

            Adapter a = new Adapter(MacAddress, sysID, Name, LocalIP, SubnetMask, DefaultGateway, DNS, OtherEndid, Connected);
            return a;
        }

        public override XmlNode ToXML(XmlDocument doc)
        {
            XmlNode parent = doc.CreateElement("Adapter");

            XmlNode nameNode = doc.CreateElement("Name");
            nameNode.InnerText = this.Name;
            parent.AppendChild(nameNode);

            XmlNode SysIDNode = doc.CreateElement("SysID");
            SysIDNode.InnerText = this.sysID.ToString();
            parent.AppendChild(SysIDNode);

            XmlNode MacNode = doc.CreateElement("MAC");
            MacNode.InnerText = this.MyMACAddress.ToString();
            parent.AppendChild(MacNode);

            XmlNode LocalIpNode = doc.CreateElement("LocalIP");
            LocalIpNode.InnerText = this.LocalIP.ToString();
            parent.AppendChild(LocalIpNode);

            XmlNode SubnetMaskNode = doc.CreateElement("SubnetMask");
            SubnetMaskNode.InnerText = this.SubnetMask.ToString();
            parent.AppendChild(SubnetMaskNode);

            XmlNode DefaultGatewayNode = doc.CreateElement("DefaultGateway");
            DefaultGatewayNode.InnerText = this.DefaultGateway.ToString();
            parent.AppendChild(DefaultGatewayNode);

            XmlNode DNSNode = doc.CreateElement("DNS");
            DNSNode.InnerText = this.DNS.ToString();
            parent.AppendChild(DNSNode);

            XmlNode OtherEndNode = doc.CreateElement("ConnectedSystem");
            OtherEndNode.InnerText = this.OtherendID.ToString();
            parent.AppendChild(OtherEndNode);

            XmlNode ConnectedNode = doc.CreateElement("Connected");
            ConnectedNode.InnerText = this.Connected.ToString();
            parent.AppendChild(ConnectedNode);

            return parent;
        }

        public override bool Connect(NetworkInterface a)
        {
            if (!_Connected)
            {
                OtherEnd = a;
                _Connected = true;
                Associated = true;
                return true;
            }else if (Associated == false)
            {
                OtherEnd = a;
                Associated = true;
                return true;
            }
            
            return false;
        }

        public override void Reset()
        {
            LocalIP = null;
            SubnetMask = null;
            DefaultGateway = null;
            DNS = null;
        }

        public override bool Equals(object obj)
        {
            Adapter a;
            try
            {
                a = (Adapter)obj;
            }
            catch (InvalidCastException)
            {
                return false;
            }

            if (MyMACAddress == null)
            {
                if (a.MyMACAddress != null) return false;
            }
            else if (!MyMACAddress.Equals(a.MyMACAddress)) return false;

            if (LocalIP == null)
            {
                if (a.LocalIP != null) return false;
            }
            else if (!LocalIP.Equals(a.LocalIP)) return false;

            if (SubnetMask == null)
            {
                if (a.SubnetMask != null) return false;
            }
            else if (!SubnetMask.Equals(a.SubnetMask)) return false;
            
            if (DefaultGateway == null)
            {
                if (a.DefaultGateway != null) return false;
            }
            else if (!DefaultGateway.Equals(a.DefaultGateway)) return false;

            if (DNS == null)
            {
                if (a.DNS != null) return false;
            }
            else if (!DNS.Equals(a.DNS)) return false;
            

            if ((Name == a.Name)&&(OtherendID == a.OtherendID)&&(Connected == a.Connected)&&(Associated == a.Associated))
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(MyMACAddress.ToBytes(), 0);
        }
        
        public override void Tick(uint tick)
        {
            if (SendQueue.Count != 0)
            {
                OtherEnd.RecievePacket(SendQueue.Dequeue());
            }
            if (RecieveQueue.Count != 0)
            {
                CallOnRecievedPacket(RecieveQueue.Dequeue(), this);
            }
        }
    }

    public class GroupAdapter:NetworkInterface
    {
        List<uint> OtherEndIDs;
        SortedList<uint, NetworkInterface> OtherEnds;

        public GroupAdapter(MAC MACAddress, uint SysID, string name = "", IP LocalIP = null, IP SubnetMask = null, List<uint> OtherEndIDs = null, bool Connected = false):base(MACAddress, SysID)
        {
            Name = name;
            if (Connected)
            {
                this.OtherEndIDs = OtherEndIDs;
                this.LocalIP = LocalIP;
                this.SubnetMask = SubnetMask;
                _Connected = true;
            }
            else
            {
                OtherEndIDs = new List<uint>();
                _Connected = false;
            }
            OtherEnds = new SortedList<uint, NetworkInterface>();
            if (OtherEndIDs == null)
            {
                OtherEndIDs = new List<uint>();
                _Connected = false;
            }
            Associated = false;
        }

        public override XmlNode ToXML(XmlDocument doc)
        {
            XmlNode parent = doc.CreateElement("GroupAdapter");

            XmlNode nameNode = doc.CreateElement("Name");
            nameNode.InnerText = this.Name;
            parent.AppendChild(nameNode);

            XmlNode SysIDNode = doc.CreateElement("SysID");
            SysIDNode.InnerText = this.sysID.ToString();
            parent.AppendChild(SysIDNode);

            XmlNode MacNode = doc.CreateElement("MAC");
            MacNode.InnerText = this.MyMACAddress.ToString();
            parent.AppendChild(MacNode);

            XmlNode LocalIpNode = doc.CreateElement("LocalIP");
            LocalIpNode.InnerText = this.LocalIP.ToString();
            parent.AppendChild(LocalIpNode);

            XmlNode SubnetMaskNode = doc.CreateElement("SubnetMask");
            SubnetMaskNode.InnerText = this.SubnetMask.ToString();
            parent.AppendChild(SubnetMaskNode);
            
            XmlNode OtherEndsNode = doc.CreateElement("ConnectedSystems");

            foreach(uint s in OtherEndIDs)
            {
                XmlNode n = doc.CreateElement("ID");
                n.InnerText = s.ToString();
            }
            parent.AppendChild(OtherEndsNode);

            XmlNode ConnectedNode = doc.CreateElement("Connected");
            ConnectedNode.InnerText = this.Connected.ToString();
            parent.AppendChild(ConnectedNode);

            return parent;
        }

        public static GroupAdapter FromXML(XmlNode Parent)
        {
            string Name = null;
            uint sysID = 0;
            MAC MacAddress = null;
            IP LocalIP = null;
            IP SubnetMask = null;
            List<uint> OtherEndids = new List<uint>();
            bool Connected = false;

            foreach (XmlNode n in Parent.ChildNodes)
            {
                switch (n.Name.ToLower())
                {
                    case "name":
                        Name = n.InnerText;
                        break;
                    case "sysid":
                        try
                        {
                            sysID = uint.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read Adapter system ID, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "mac":
                    case "macaddress":
                        try
                        {
                            MacAddress = MAC.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read MAC address, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "localip":
                        try
                        {
                            LocalIP = IP.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read IP address, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "subnetmask":
                        try
                        {
                            SubnetMask = IP.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read SubnetMask, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "connectedsystems":
                    case "otherends":
                        try
                        {
                            foreach(XmlNode node in n.ChildNodes)
                            OtherEndids.Add(uint.Parse(node.InnerText));
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read Connected System(otherend), invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "connected":
                        try
                        {
                            Connected = bool.Parse(n.InnerText);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read Connected status, invalid formatting");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                }
            }
            if (MacAddress == null||sysID == 0)
            {
                throw new ArgumentException("MacAddress or SYSID not provided");
            }

            GroupAdapter a = new GroupAdapter(MacAddress, sysID, Name, LocalIP, SubnetMask, OtherEndids, Connected);
            return a;
        }

        public override bool Connect(NetworkInterface a)
        {
            OtherEnds.Add(a.sysID, a);
            _Connected = true;

            if (!OtherEndIDs.Contains(a.sysID))
            {
                OtherEndIDs.Add(a.sysID);
            }
            else
            {
                foreach (uint id in OtherEndIDs)
                {
                    if (!OtherEnds.ContainsKey(id))
                    {
                        Associated = false;
                        return true;
                    }
                }
                Associated = true;
            }
            return true;
        }

        public override void Reset()
        {
            LocalIP = null;
            SubnetMask = null;

        }

        public override void Tick(uint tick)
        {
            if (SendQueue.Count != 0)
            {
                foreach(NetworkInterface n in OtherEnds.Values) n.RecievePacket(SendQueue.Dequeue());
            }
            if (RecieveQueue.Count != 0)
            {
                CallOnRecievedPacket(RecieveQueue.Dequeue(), this);
            }
        }

    }
    
    public class System
    {
        public uint ID;

        public Dictionary<MAC,NetworkInterface> NetworkInterfaces;
        
        public List<String> Log;

        public bool respondToEcho;
        

        private event Action<Byte[],NetworkInterface> OnRecievedPacket;
        private event Action<Byte[],NetworkInterface> OnCorruptPacket; 
        private event Action<IPv4Header,NetworkInterface> OnNotForMe;  
        private event Action<IPv4Header,NetworkInterface> OnICMPPacket;
        private event Action<IPv4Header, NetworkInterface> OnTCPPacket;
        private event Action<IPv4Header, NetworkInterface> OnUDPPacket;

        protected System()
        {
            OnICMPPacket += handleICMPPacket;
            OnTCPPacket += handleTCPPacket;
            OnUDPPacket += handleUDPPacket;
            OnTick += AdapterTick;

            Log = new List<string>();
        }

        public System(uint ID):this()
        {
            this.ID = ID;
            NetworkInterfaces = new Dictionary<MAC, NetworkInterface>();
            respondToEcho = false;
        }

        public System(uint ID, Dictionary<MAC,NetworkInterface> NetworkInterfaces = null, bool Respondtoecho = true):this()
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

            foreach (Adapter a in NetworkInterfaces.Values)
            {
                parent.AppendChild(a.ToXML(doc));
            }

            return parent;
        }

        public bool GetConectableAdapter(out NetworkInterface a)
        {
            foreach (NetworkInterface NI in NetworkInterfaces.Values)
            {
                if (!NI.Connected || NI.GetType().ToString() == "NSHG.GroupAdapter")
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
                        OnICMPPacket.Invoke(Data, a);
                        break;
                    case IPv4Header.ProtocolType.TCP:

                    case IPv4Header.ProtocolType.UDP:
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
                case 0: // Echo Reply
                    {
                        ICMPEchoRequestReply header = new ICMPEchoRequestReply(datagram.Datagram);
                        if (ICMPEcholistner.ContainsKey(header.Identifier))
                        {
                            ICMPEcholistner[header.Identifier].BeginInvoke(datagram, header, a, null, null);
                        }
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
        
        protected virtual void handleTCPPacket(IPv4Header datagram, NetworkInterface a)
        {
            TCPHeader TCP;
            try
            {
                TCP = new TCPHeader(datagram.Datagram);
            }
            catch
            {
                OnCorruptPacket.Invoke(datagram.ToBytes(), a);
                return;
            }

            TCPListner[TCP.DestinationPort].Invoke(datagram, TCP, a);
        }
        
        protected virtual void handleUDPPacket(IPv4Header datagram, NetworkInterface a)
        {
            UDPHeader UDP;
            try
            {
                UDP = new UDPHeader(datagram.Datagram);
            }
            catch
            {
                OnCorruptPacket.Invoke(datagram.ToBytes(), a);
                return;
            }

            UDPListner[UDP.DestinationPort].Invoke(datagram, UDP, a);
        }

        // Dictonary subscribed to for when a packed with spesific ID (Uint16) is recieved and needs to be processed
        // Used to pass from network to Application layer
        protected Dictionary<UInt16, Action<IPv4Header, ICMPEchoRequestReply, NetworkInterface>>   ICMPEcholistner = new Dictionary<UInt16, Action<IPv4Header, ICMPEchoRequestReply, NetworkInterface>>();
        
        protected Dictionary<UInt16, Action<IPv4Header, Protocols.TCP.TCPHeader, NetworkInterface>> TCPListner = new Dictionary<UInt16, Action<IPv4Header, Protocols.TCP.TCPHeader, NetworkInterface>>();

        protected Dictionary<UInt16, Action<IPv4Header,Protocols.UDP.UDPHeader, NetworkInterface>> UDPListner = new Dictionary<UInt16, Action<IPv4Header, Protocols.UDP.UDPHeader, NetworkInterface>>();

        // Application Layer
        protected List<Application> Apps = new List<Application>();
        
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

    public class PC : System
    {
        protected override void AppsInit()
        {
            Apps.Add(new DHCPClient(ref UDPListner, new List<NetworkInterface>(NetworkInterfaces.Values)));
            RoutingTable = new SystemRoutingTable(this);
            Apps.Add(RoutingTable);
        }
    }

    public class Router : System
    {
        protected override void AppsInit()
        {
            Apps.Add(new DHCPServer(ref UDPListner));
            RoutingTable = new SystemRoutingTable(this);
            Apps.Add(RoutingTable);
        }
    }

    //public class EchoServer : System
    //{

    //}
}
