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
        public bool respondToEcho;

        public List<String> Locallog;
        public Action<string> LocalLog;
        public Action<string> Log;

        public event Action<Byte[],NetworkInterface> OnRecievedPacket;
        public event Action<Byte[],NetworkInterface> OnCorruptPacket; 
        public event Action<IPv4Header,NetworkInterface> OnNotForMe;  
        public event Action<IPv4Header, ICMPHeader, byte, NetworkInterface> OnICMPPacket;
        public event Action<IPv4Header, TCPHeader, UInt16, NetworkInterface> OnTCPPacket;
        public event Action<IPv4Header, UDPHeader, UInt16, NetworkInterface> OnUDPPacket;


        public System(uint ID, Dictionary<MAC, NetworkInterface> NetworkInterfaces = null, bool Respondtoecho = true, int maxapps = 10, bool initapps = true, Action<string> Log = null)
        {
            Locallog = new List<string>();
            
            this.Log += Log ?? Console.WriteLine;
            this.LocalLog += Locallog.Add;
            this.Log += LocalLog;

            this.ID = ID;
            if (NetworkInterfaces != null)
            {
                this.NetworkInterfaces = NetworkInterfaces;
                foreach (NetworkInterface n in NetworkInterfaces.Values) n.OnRecievedPacket += Packet;
            }
            else
            {
                NetworkInterfaces = new Dictionary<MAC, NetworkInterface>();
            }
            this.respondToEcho = Respondtoecho;

            OnCorruptPacket += (n, a) => { LocalLog("Corrupt packet on: " + ID); };

            OnTick += AdapterTick;
            OnTick += ApplicationTick;
            Apps = new Application[maxapps];

            if (initapps) AppsInit();
        }
        
        
        // Network Layer
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
        public void Packet(byte[] datagram, NetworkInterface a)
        {
            OnRecievedPacket?.Invoke(datagram, a);
            IPv4Header Data;
            try
            {
                Data = new IPv4Header(datagram);
            }
            catch (Exception e)
            {
                OnCorruptPacket.BeginInvoke(datagram, a, null, null);
                return;
            }
            
            if(Data.DestinationAddress == a.LocalIP || (Data.DestinationAddress  & ~a.SubnetMask) == (IP.Broadcast & ~a.SubnetMask))
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
                        catch(Exception e)
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
                        catch(Exception e)
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
                LocalLog("Packet not for " + ID + " Addressed to: " + Data.DestinationAddress + " Recieved at:  " + a.LocalIP);
                OnNotForMe?.Invoke(Data, a);
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
        public Application[] Apps;
        public RoutingTable RoutingTable;
        private event Action<uint> OnTick;

        protected virtual void AppsInit()
        {
            DHCPClient c = new DHCPClient(this, new List<NetworkInterface>(NetworkInterfaces.Values));
            AddApp(c);
            RoutingTable = new RoutingTable(this);
            AddApp(RoutingTable);
        } 
        public bool AddApp(Application app, int start = 0)
        {
            for (int i = start; i < Apps.Length; i++)
            {
                if (Apps[i] == null ||  (Apps[i]?.closed ?? false))
                {
                    Apps[i] = app;
                    app.Log += s => LocalLog(i + "> " + s);
                    Log("Adding app " + app.GetType() + " To " + ID);
                    return true;
                }
            }
            return false;
        }
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
                a?.OnTick(tick);
            }
        }
        public void Command(string CommandString)
        {
            Locallog.Add(CommandString);
            string[] Command = CommandString.Split(' ');
            switch (Command[0].ToLower())
            {
                case "view":
                    if (Command.Length > 1)
                    {
                        switch (Command[1].ToLower())
                        {
                            case "networkinterfaces":
                                foreach (NetworkInterface n in NetworkInterfaces.Values)
                                {
                                    LocalLog("Name:------- " + n.Name);
                                    LocalLog("MacAddress:- " + n.MyMACAddress);
                                    LocalLog("Local IP:--- " + n.LocalIP);
                                    LocalLog("NetMask:---- " + n.SubnetMask);
                                    LocalLog("Type Of:---- " + n.GetType());
                                }
                                break;
                            case "apps":
                            case "applications":
                                for (int i = 0; i < Apps.Length; i++)
                                {
                                    if (Apps[i] != null)
                                    {
                                        LocalLog("   Slot: " + i + "  Type: " + Apps[i].GetType());
                                    }
                                }
                                break;
                            case "app":
                            case "application":
                                if (Command.Length > 2)
                                {
                                    try
                                    {
                                        int id = int.Parse(Command[2]);
                                        if (Apps[id]?.log != null)
                                        {
                                            foreach (string s in Apps[id]?.log)
                                            {
                                                LocalLog("   " + s);
                                            }
                                        }
                                        

                                    }
                                    catch (FormatException)
                                    {
                                        LocalLog("Error parsing Slot");
                                    }
                                    catch (OverflowException)
                                    {
                                        LocalLog("Error, Slot too large, overflow");
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        LocalLog("Error, Slot to large, out of range");
                                    }
                                }
                                break;
                            case "help":
                                LocalLog("view item");
                                LocalLog("    networkinterfaces  -Dispalys info of each network interface");
                                LocalLog("    apps               -Displays list of loaded application");
                                LocalLog("    app appNo          -Displays log of selected appliation");
                                LocalLog("    help               -Displays this help screen");
                                break;
                            default:
                                LocalLog("invalid parameter " + Command[1] + "  use 'view help' for more info");
                                break;
                        }
                    }
                    break;

                case "as":
                    if (Command.Length > 1)
                    {
                        switch (Command[1].ToLower())
                        {
                            case "app":
                            case "application":
                                if (Command.Length > 2)
                                {
                                    try
                                    {
                                        int id = int.Parse(Command[2]);

                                        string newcommand = "";
                                        for (int i = 3; i < Command.Length; i++)
                                        {
                                            newcommand += Command[i] + " ";
                                        }
                                        Apps[id]?.Command(newcommand);
                                    }
                                    catch (FormatException)
                                    {
                                        LocalLog("Error parsing Slot");
                                    }
                                    catch (OverflowException)
                                    {
                                        LocalLog("Error, Slot too large, overflow");
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        LocalLog("Error, Slot too large, out of range");
                                    }
                                }
                                else
                                {
                                    LocalLog("Must specify a app to act as");
                                }
                                break;
                            default:
                                LocalLog("Must specify a type to act as");
                                break;
                        }
                    }
                    break;

                case "start":
                    if (Command.Length > 1)
                    {
                        if (int.TryParse(Command[1],out int app))
                        {
                            bool isNull = Apps[app] == null;
                            bool isClosed = true;
                            if (!isNull) isClosed = Apps[app].closed;

                            if (isNull || isClosed)
                            {
                                if (Command.Length > 1)
                                {
                                    switch (Command[2].ToLower())
                                    {
                                        case "packetsniffer":
                                            PacketSniffer a = new PacketSniffer();
                                            OnRecievedPacket += (b, s) => { a.onPacket(b); };
                                            if (AddApp(a, app)) LocalLog("Successfully added App");
                                            else LocalLog("Failed to add App");
                                            break;
                                        default:
                                            LocalLog("Invalid application " + Command[2]);
                                            break;

                                    }
                                }
                                else
                                {
                                    Log("Pleace specify an application to start");
                                }
                            }
                            else
                            {
                                LocalLog("Invalid app slot, app already exists there");
                            }
                        }
                        else if(Command[1].ToLower() == "help")
                        {
                            LocalLog("start slot application");
                            LocalLog("    slot - intiger application slot, use 'view apps' to see available slots");
                            LocalLog("    application - name of application to start");
                            LocalLog("       packetsniffer");

                        }
                        else
                        {
                            LocalLog("Invalid application slot: " + Command[1]);
                        }
                    }
                    break;

                case "help":
                    LocalLog("view item otherparams   -shows infomation about selected item");
                    LocalLog("as item identifier      -runs a command as the selected item");
                    LocalLog("start slot application  -starts application in selected slot");
                    LocalLog("help                    -shows this help text");
                    LocalLog("use '[command] help' for more help with a spesific command");
                    break;
            }
        }

        // Higher Stuff
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
        public static System FromXML(XmlNode Parent, Action<string> log = null)
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
                            Adapter a = Adapter.FromXML(n, log);
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
                            GroupAdapter a = GroupAdapter.FromXML(n, log);
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

            return new System(ID, NetworkInterfaces, respondToEcho, Log: log);


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
                if (!NI.Connected || NI.GetType() == typeof(GroupAdapter))
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
                if (adapt.Connected == true && adapt.Associated == false && adapt.isConnectedTo(id))
                {
                    a = adapt;
                    return true;
                }
            }
            return (GetConectableAdapter(out a));
        }
    }

    public class Router : System
    {
        public Router(uint ID, Dictionary<MAC, NetworkInterface> NetworkInterfaces, int maxapps = 10, bool RespToEcho = true, bool initapps = true, Action<string> Log = null) : base(ID, NetworkInterfaces, RespToEcho, maxapps, false, Log)
        {
            if (initapps) AppsInit();
        }

        protected override void AppsInit()
        {
            
            foreach (NetworkInterface n in NetworkInterfaces.Values)
            {
                if (n.GetType() == typeof(GroupAdapter))
                {
                    DHCPServer s = new DHCPServer(this,n,n.LocalIP);
                    AddApp(s);
                }
            }

            RoutingTable = new SystemRoutingTable(this);
            AddApp(RoutingTable);
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
        public static Router FromXML(XmlNode Parent, Action<string> log = null)
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

            return new Router(ID, NetworkInterfaces, RespToEcho: respondToEcho, Log: log);


        }
    }
}
