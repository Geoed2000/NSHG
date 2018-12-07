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
        public string Name;
        public readonly MAC MyMACAddress;
        public IP LocalIP;
        public IP SubnetMask;
        public IP DefaultGateway;
        public Adapter OtherEnd;
        public bool Connected;

        public Adapter(MAC MACAddress)
        {
            MyMACAddress = MACAddress;
        }

        public bool Connect(Adapter a)
        {
            if (!Connected)
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
        private event Action<IPv4Header,Adapter> OnICMPPacket;


        private bool respondToEcho;


        System(uint ID)
        {
            this.ID = ID;
            

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
           
            switch (Data.Protocol)
            {
                case IPv4Header.ProtocolType.ICMP:
                    OnICMPPacket.BeginInvoke(Data, a, null, null);
                    break;
                default:
                    break;
            }
            
            
        }
        
        private void handleICMPPacket(IPv4Header datagram, Adapter a)
        {
            switch(datagram.Datagram[0])
            {
                case 0:

                    break;
                case 8:

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
