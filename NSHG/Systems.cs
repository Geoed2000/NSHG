using System;
using System.Collections.Generic;
using System.Text;
using NSHG;
using NSHG.Packet;
using System.Xml;
namespace NSHG
{
    
    public class Adapter
    {
        private string Name;
        private readonly MAC MyMACAddress;
        private IP LocalIP;
        private IP SubnetMask;
        private IP DefaultGateway;
        private Adapter OtherEnd;
        private uint OtherendID;
        private bool _Connected;
        public  bool Connected
        {
            get
            {
                return _Connected;
            }
        }
        public  bool Associated;

        public Adapter(MAC MACAddress)
        {
            MyMACAddress = MACAddress;
        }

        public Adapter(string Name,MAC MACAddress,IP LocalIP,IP SubnetMask, bool Connected)
        {
            this.Name = Name;
            this.MyMACAddress = MACAddress;
            this.LocalIP = LocalIP;
            this.SubnetMask = SubnetMask;
            this._Connected = Connected;
            Associated = false;
        }

        public Adapter(string Name,MAC MACAddress,IP LocalIP,IP SubnetMask, uint otherend, bool Connected) : this(Name, MACAddress, LocalIP, SubnetMask, Connected)
        {
            OtherendID = otherend;
        }

        public static Adapter FromNode(XmlNode Parent,Dictionary<uint,NSHG.System> Systems)
        {
            string Name;
            MAC MacAddress;
            IP LocalIP;
            IP SubnetMask;
            uint OtherEndid;
            bool Connected;

            foreach (XmlNode n in Parent.ChildNodes)
            {
                switch (n.Name.ToLower())
                {
                    case "name":
                        Name = n.InnerText;
                        break;
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

            Adapter a = new Adapter
        }

        public bool Connect(Adapter a)
        {
            if (!_Connected)
            {
                OtherEnd = a;
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

        public event Action<byte[],Adapter> OnRecievedPacket;

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
        public readonly uint ID;

        private List<Adapter> Adapters;
        
        private event Action<Byte[],Adapter> OnRecievedPacket;
        private event Action<Byte[],Adapter> OnCorruptPacket; 
        private event Action<IPv4Header,Adapter> OnNotForMe;  
        private event Action<IPv4Header,Adapter> OnICMPPacket;


        private bool respondToEcho;

        System()
        {
            OnICMPPacket += handleICMPPacket;
        }

        System(uint ID):this()
        {
            this.ID = ID;
            respondToEcho = false;
            Adapters = new List<Adapter> { new Adapter(MAC.Random()), new Adapter(MAC.Random()) };

            OnICMPPacket += handleICMPPacket;
        }

        System(uint ID, List<Adapter> Adapters = null, bool Respondtoecho = true):this()
        {
            this.ID = ID;
            if (Adapters != null)
            {
                this.Adapters = Adapters;
            }else
            {
                Adapters = new List<Adapter> { new Adapter(MAC.Random()), new Adapter(MAC.Random()) };
            }
            this.respondToEcho = Respondtoecho;
        }

        public static System FromNode(XmlNode Parent, Dictionary<uint,System> Systems)
        {
            uint ID = 0;
            List<Adapter> Adapters = new List<Adapter>();
            bool respondToEcho = true;

            foreach (XmlNode n in Parent.ChildNodes)
            {
                switch (n.Name.ToLower())
                {
                    case "id":
                        if (uint.TryParse(n.InnerText, out ID))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("failed to read ID");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "adapter":
                        try
                        {
                            Adapters.Add(Adapter.FromNode(n, Systems));
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

        public bool GetFreeAdapter(out Adapter a)
        {
            foreach (Adapter adapt in Adapters)
            {
                if (!adapt._Connected)
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
        public bool GetFreeAdapter(out Adapter a, uint id)
        {
            foreach (Adapter adapt in Adapters)
            {
                if (adapt.Connected == true && adapt.Associated == false)
                {
                    a = adapt;
                    return true;
                }
            }
            return GetFreeAdapter(out a);
        }

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

        protected Dictionary<UInt16, Action<IPv4Header, ICMPEchoRequestReply, Adapter>> ICMPlistner = new Dictionary<UInt16, Action<IPv4Header, ICMPEchoRequestReply, Adapter>>();

        private void handleICMPPacket(IPv4Header datagram, Adapter a)
        {
            switch(datagram.Datagram[0])
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

        private void ping()
        {

        }
    }


    //public class EchoServer : System
    //{

    //}


}
