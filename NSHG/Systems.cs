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
        readonly MAC MacAddress;
        
        IP DefaultGateway;

        MAC[] Connections;

        System(MAC MACAddress, IP DefaultGateway, MAC[] Connections)
        {
            this.MacAddress = MACAddress;
            this.DefaultGateway = DefaultGateway;
            this.Connections = Connections;
        }

        public void Packet(Byte[] datagram)
        {
            
        }

        public System FromNode(XmlNode Parent)
        {
            XmlNodeList nodes = Parent.ChildNodes;
            IP dg = null;
            MAC mac = null;
            List<MAC> connections = new List<MAC>();
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
                    case "Connections":
                        foreach (XmlNode n in node.ChildNodes)
                        {
                            if (n.Name == "MAC")
                            {
                                connections.Add(MAC.Parse(n.InnerText));
                            }
                        }
                        break;
                }
            }
            if (dg == null || mac == null || connections.Count == 0)
            {
                throw new Exception("Invalid System XML");
            }
            
            return new System(mac,dg, connections.ToArray());


        }
    }

    public class EchoServer : System
    {

    }


}
