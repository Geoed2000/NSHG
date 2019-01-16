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
        public IP LocalIP;
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

        public event Action<byte[],Adapter> OnRecievedPacket;

        public Adapter(MAC MACAddress, string name = null, IP LocalIP = null ,IP SubnetMask = null, IP DefaultGateway = null, uint OtherendID = 0, bool Connected = false)
        {
            MyMACAddress = MACAddress;
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
        
        public static Adapter FromNode(XmlNode Parent,Dictionary<uint,NSHG.System> Systems)
        {
            string Name = null;
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
            if (MacAddress == null)
            {
                throw new ArgumentException("MacAddress not provided");
            }

            Adapter a = new Adapter(MacAddress, Name, LocalIP, SubnetMask, DefaultGateway, OtherEndid, Connected);
            return a;
        }

        public bool Connect(Adapter a)
        {
            if (!_Connected)
            {
                OtherEnd = a;
                _Connected = true;
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


        System(uint ID)
        {
            this.ID = ID;
            Adapters = new List<Adapter> { new Adapter(MAC.Random()), new Adapter(MAC.Random()) };

            OnICMPPacket += handleICMPPacket;
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

        // Packet Handeling
        public static System FromNode(XmlNode Parent)
        {
            XmlNodeList nodes = Parent.ChildNodes;
            uint ID = 0;
            foreach (XmlNode n in nodes)
            {
                switch (n.Name)
                {
                    case "ID":
                        uint.TryParse(n.InnerText,out ID);
                        break;
                }
            }
            if (ID == 0)
            {
                throw new Exception("Invalid System XML ID not Specified");
            }

            return new System(ID);


        }
 
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

        //Dictonary subscribed to for when a packed with spesific ID (Uint16) is recieved and needs to be processed
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
