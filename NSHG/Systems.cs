using NSHG.Packet;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
namespace NSHG
{
    public class Adapter
    {
        public string Name { get; set; }
        public uint sysID;
        public MAC MyMACAddress { get; set; }
        public IP LocalIP;
        public IP SubnetMask;
        public IP DefaultGateway;
        public Adapter OtherEnd;
        public uint OtherendID;
        private bool _Connected;
        public  bool Connected
        {
            get
            {
                return _Connected;
            }
        }
        public  bool Associated;

        public event Action<byte[],Adapter> OnRecievedPacket;

        public Adapter(MAC MACAddress, uint sysID, string name = null, IP LocalIP = null ,IP SubnetMask = null, IP DefaultGateway = null, uint OtherendID = 0, bool Connected = false)
        {
            MyMACAddress = MACAddress;
            this.sysID = sysID;
            if (Connected)
            {
                Name = name;
                this.LocalIP = LocalIP;
                this.SubnetMask = SubnetMask;
                this.DefaultGateway = DefaultGateway;
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

            Adapter a = new Adapter(MacAddress, sysID, Name, LocalIP, SubnetMask, DefaultGateway, OtherEndid, Connected);
            return a;
        }

        public XmlNode ToXML(XmlDocument doc)
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

            XmlNode OtherEndNode = doc.CreateElement("ConnectedSystem");
            OtherEndNode.InnerText = this.OtherendID.ToString();
            parent.AppendChild(OtherEndNode);

            XmlNode ConnectedNode = doc.CreateElement("Connected");
            ConnectedNode.InnerText = this.Connected.ToString();
            parent.AppendChild(ConnectedNode);

            return parent;
        }

        public bool Connect(Adapter a)
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

        public void Reset()
        {
            Name = null;
            LocalIP = null;
            SubnetMask = null;
            DefaultGateway = null;
            OtherEnd = null;
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

            if ((Name == a.Name)&&(MyMACAddress.Equals(a.MyMACAddress))&&(LocalIP.Equals(a.LocalIP))&&(SubnetMask.Equals(a.SubnetMask))&&(DefaultGateway.Equals(a.DefaultGateway))
                &&(OtherendID == a.OtherendID)&&(Connected == a.Connected)&&(Associated == a.Associated))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(MyMACAddress.ToBytes(), 0);
        }

        public void SendPacket(byte[] datagram)
        {
            OtherEnd.RecievePacket(datagram);
        }

        public void RecievePacket(byte[] datagram)
        {
            OnRecievedPacket.BeginInvoke(datagram, this, null, null);
        }
    }

    public class System
    {
        public uint ID;

        public List<Adapter> Adapters;
        
        private event Action<Byte[],Adapter> OnRecievedPacket;
        private event Action<Byte[],Adapter> OnCorruptPacket; 
        private event Action<IPv4Header,Adapter> OnNotForMe;  
        private event Action<IPv4Header,Adapter> OnICMPPacket;


        public bool respondToEcho;

        System()
        {
            OnICMPPacket += handleICMPPacket;
        }

        public System(uint ID):this()
        {
            this.ID = ID;
            Adapters = new List<Adapter>();
            respondToEcho = false;

            OnICMPPacket += handleICMPPacket;
        }

        public System(uint ID, List<Adapter> Adapters = null, bool Respondtoecho = true):this()
        {
            this.ID = ID;
            if (Adapters != null)
            {
                this.Adapters = Adapters;
            }else
            {
                Adapters = new List<Adapter>();
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

            foreach (Adapter a in Adapters)
            {
                if (!s.Adapters.Contains(a)) Equal = false;//compares adapter a to every adapter in the s.adapters list until it
                                                           //finds an addapter where adapter 'a' is equal to it (runs a.equals(adapter) for each adapter)
            }
            foreach (Adapter a in s.Adapters)
            {
                if (!Adapters.Contains(a)) Equal = false; //compares adapter a to every adapter in the s.adapters list until it 
                                                          //finds an addapter where adapter 'a' is equal to it (runs a.equals(adapter) for each adapter)
            }
            return Equal;
        }

        public static System FromXML(XmlNode Parent)
        {
            uint ID = 0;
            List<Adapter> Adapters = new List<Adapter>();
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
                            Adapters.Add(Adapter.FromXML(n));
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to read adapter");
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

            return new System(ID,Adapters,respondToEcho);


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

            foreach (Adapter a in Adapters)
            {
                parent.AppendChild(a.ToXML(doc));
            }

            return parent;
        }

        public bool GetFreeAdapter(out Adapter a)
        {
            foreach (Adapter adapt in Adapters)
            {
                if (!adapt.Connected)
                {
                    a = adapt;
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
        public bool GetConnectedUnassociatedAdapter(out Adapter a, uint id)
        {
            foreach (Adapter adapt in Adapters)
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
        public void Packet(byte[] datagram, Adapter a)
        {
            OnRecievedPacket.BeginInvoke(datagram, a, null, null);
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
                        OnICMPPacket.BeginInvoke(Data, a, null, null);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                OnNotForMe.BeginInvoke(Data, a, null, null);
            }
        }

        private void handleICMPPacket(IPv4Header datagram, Adapter a)
        {
            switch (datagram.Datagram[0])
            {
                case 0: // Echo Reply
                    {
                        ICMPEchoRequestReply header = new ICMPEchoRequestReply(datagram.Datagram);
                        if (ICMPlistner.ContainsKey(header.Identifier))
                        {
                            ICMPlistner[header.Identifier].BeginInvoke(datagram, header, a, null, null);
                        }
                    }
                    break;
                case 8: // Echo Request
                    if (respondToEcho)
                    {
                        ICMPEchoRequestReply header = new ICMPEchoRequestReply(datagram.Datagram);
                        ICMPEchoRequestReply icmp = new ICMPEchoRequestReply(0, header.Identifier, (UInt16)(header.Sequencenumber + 1));
                        IPv4Header ipv4 = new IPv4Header(datagram.Identification, false, false, 255, IPv4Header.ProtocolType.ICMP, a.LocalIP, datagram.SourceAddress, null, icmp.ToBytes());

                        a.SendPacket(ipv4.ToBytes());
                    }
                    break;
            }
        }

        // Dictonary subscribed to for when a packed with spesific ID (Uint16) is recieved and needs to be processed
        // Used to pass from network to Application layer
        protected Dictionary<UInt16, Action<IPv4Header, ICMPEchoRequestReply, Adapter>> ICMPlistner = new Dictionary<UInt16, Action<IPv4Header, ICMPEchoRequestReply, Adapter>>();

        
        // Application Layer
        public void ping(IP Recipient)
        {

        }
        
    }


    //public class EchoServer : System
    //{

    //}


}
