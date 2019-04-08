using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NSHG.NetworkInterfaces
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

        public Action<string> Log;


        protected NetworkInterface(MAC MACAddress, uint SysID, Action<string> Log = null)
        {
            this.Log = Log ?? Console.WriteLine;

            MyMACAddress = MACAddress;
            this.sysID = SysID;


            SendLock = new object();
            RecieveLock = new object();
            SendQueue = new Queue<byte[]>();
            RecieveQueue = new Queue<byte[]>();

        }

        public abstract bool isConnectedTo(uint id);
        
        protected void CallOnRecievedPacket(byte[] Data, NetworkInterface NetInterface)
        {
            OnRecievedPacket?.Invoke(Data, NetInterface);
        }

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

        public abstract XmlNode ToXML(XmlDocument doc);
        public abstract bool Connect(NetworkInterface a);
    }

    public class Adapter : NetworkInterface
    {
        public IP DefaultGateway;
        public IP DNS;
        public NetworkInterface OtherEnd;
        public uint OtherEndID;
        
        public Adapter(MAC MACAddress, uint sysID, string name = "", IP LocalIP = null, IP SubnetMask = null, IP DefaultGateway = null, IP DNS = null, uint OtherendID = 0, bool Connected = false, Action<string> Log = null) : base(MACAddress, sysID, Log: Log)
        {
            Name = name;
            if (Connected)
            {
                this.LocalIP = LocalIP;
                this.SubnetMask = SubnetMask;
                this.DefaultGateway = DefaultGateway;
                this.DNS = DNS;
                this.OtherEndID = OtherendID;
                _Connected = true;
            }
            else
            {
                _Connected = false;
            }
            Associated = false;
        }

        public override bool isConnectedTo(uint id)
        {
            return (OtherEndID == id);
        }

        public static Adapter FromXML(XmlNode Parent, Action<string> Log = null)
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

            Adapter a = new Adapter(MacAddress, sysID, Name, LocalIP, SubnetMask, DefaultGateway, DNS, OtherEndid, Connected, Log: Log);
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

            if (LocalIP != null)
            {
                XmlNode LocalIpNode = doc.CreateElement("LocalIP");
                LocalIpNode.InnerText = this.LocalIP.ToString();
                parent.AppendChild(LocalIpNode);
            }

            if (SubnetMask != null)
            {
                XmlNode SubnetMaskNode = doc.CreateElement("SubnetMask");
                SubnetMaskNode.InnerText = this.SubnetMask.ToString();
                parent.AppendChild(SubnetMaskNode);
            }

            if (DefaultGateway != null)
            {
                XmlNode DefaultGatewayNode = doc.CreateElement("DefaultGateway");
                DefaultGatewayNode.InnerText = this.DefaultGateway.ToString();
                parent.AppendChild(DefaultGatewayNode);
            }

            if(DNS != null)
            {
                XmlNode DNSNode = doc.CreateElement("DNS");
                DNSNode.InnerText = this.DNS.ToString();
                parent.AppendChild(DNSNode);
            }

            XmlNode OtherEndNode = doc.CreateElement("ConnectedSystem");
            OtherEndNode.InnerText = this.OtherEndID.ToString();
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
            

            if ((Name == a.Name)&&(OtherEndID == a.OtherEndID)&&(Connected == a.Connected)&&(Associated == a.Associated))
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
                Log("Packet sent from" + sysID + "to" + OtherEnd.sysID);
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

        public GroupAdapter(MAC MACAddress, uint SysID, string name = "", IP LocalIP = null, IP SubnetMask = null, List<uint> OtherEndIDs = null, bool Connected = false, Action<string> Log = null):base(MACAddress, SysID, Log: Log)
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


        public override bool isConnectedTo(uint id)
        {
            return OtherEndIDs.Contains(id);
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

            if (LocalIP != null)
            {
                XmlNode LocalIpNode = doc.CreateElement("LocalIP");
                LocalIpNode.InnerText = this.LocalIP.ToString();
                parent.AppendChild(LocalIpNode);
            }

            if (SubnetMask != null)
            {
                XmlNode SubnetMaskNode = doc.CreateElement("SubnetMask");
                SubnetMaskNode.InnerText = this.SubnetMask.ToString();
                parent.AppendChild(SubnetMaskNode);
            }

            XmlNode OtherEndsNode = doc.CreateElement("ConnectedSystems");

            foreach(uint s in OtherEndIDs)
            {
                XmlNode n = doc.CreateElement("ID");
                n.InnerText = s.ToString();
                OtherEndsNode.AppendChild(n);
            }
            parent.AppendChild(OtherEndsNode);

            XmlNode ConnectedNode = doc.CreateElement("Connected");
            ConnectedNode.InnerText = this.Connected.ToString();
            parent.AppendChild(ConnectedNode);

            return parent;
        }

        public static GroupAdapter FromXML(XmlNode Parent, Action<string> Log = null)
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

            GroupAdapter a = new GroupAdapter(MacAddress, sysID, Name, LocalIP, SubnetMask, OtherEndids, Connected, Log: Log);
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
            Log("Group adapt.Tick");
            if (SendQueue.Count != 0)
            {
                byte[] packet = SendQueue.Dequeue();
                foreach (NetworkInterface n in OtherEnds.Values)
                {
                    n.RecievePacket(packet);
                    Log("Paket sent from" + sysID + "to" + n.sysID);
                }

            }
            if (RecieveQueue.Count != 0)
            {
                CallOnRecievedPacket(RecieveQueue.Dequeue(), this);
            }
        }
    }
}
