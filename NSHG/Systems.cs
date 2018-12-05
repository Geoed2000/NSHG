using System;
using System.Collections.Generic;
using System.Text;
using NSHG;
using NSHG.Packet;
using System.Xml;
namespace NSHG
{
    public class System
    {
        public readonly MAC MacAddress;

        public IP DefaultGateway;

        Dictionary<IP,MAC> Connections;
        
        
        private event Action<Byte[],MAC> OnRecievedPacket;
        private event Action<Byte[],MAC> OnCorruptPacket;
        private event Action<IPv4Header,MAC> OnICMPPacket;


        private bool respondToEcho;


        System(MAC MACAddress, IP DefaultGateway, MAC[] connections, IP[] LocalIPs)
        {
            this.MacAddress = MACAddress;
            this.DefaultGateway = DefaultGateway;
            

            OnICMPPacket += handleICMPPacket;
        }

        public static System FromNode(XmlNode Parent)
        {
            XmlNodeList nodes = Parent.ChildNodes;
            IP dg = null;
            MAC mac = null;
            foreach (XmlNode node in nodes)
            {
                switch (node.Name)
                {
                    case "MAC":
                        mac = MAC.Parse(node.InnerText);
                        break;
                    case "DefaultGateway":
                        dg = IP.Parse(node.InnerText);
                        break;
                }
            }
            if (dg == null || mac == null)
            {
                throw new Exception("Invalid System XML");
            }

            return new System(mac, dg);


        }
        
        public void Packet(byte[] datagram, MAC c)
        {
            OnRecievedPacket.BeginInvoke(datagram, c, null, null);
            IPv4Header Data;
            try
            {
                Data = new IPv4Header(datagram);
            }
            catch
            {
                OnCorruptPacket.BeginInvoke(datagram, c, null, null);
                return;
            }
           
            switch (Data.Protocol)
            {
                case IPv4Header.ProtocolType.ICMP:
                    OnICMPPacket.BeginInvoke(Data, c, null, null);
                    break;
                default:
                    break;
            }
            
            
        }
        
        private void handleICMPPacket(IPv4Header datagram, MAC c)
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
