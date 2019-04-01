﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NSHG
{
    public class Network
    {
        public Dictionary<uint, System> Systems;
        public List<Tuple<uint, uint>> Connections;
        public List<uint> OnlinePlayers;
        public List<uint> OfflinePlayers;
        public List<uint> UnallocatedPlayers;
        public List<MAC> TakenMacAddresses;

        public static Network NewNet()
        {
            Network n = new Network();
            n.Systems = new Dictionary<uint, System>();
            n.Connections = new List<Tuple<uint, uint>>();
            n.OnlinePlayers = new List<uint>();
            n.OfflinePlayers = new List<uint>();
            n.UnallocatedPlayers = new List<uint>();
            n.TakenMacAddresses = new List<MAC>();
            return n;
        }

        public bool Connect(uint sysA, uint sysB)
        {
            NetworkInterface a, b;
            if (Systems[sysA].GetConnectedUnassociatedAdapter(out a, sysB) && Systems[sysB].GetConnectedUnassociatedAdapter(out b, sysA))
            {
                a.Connect(b);
                b.Connect(a);
                Connections.Add(new Tuple<uint, uint>(sysA, sysB));
                return true;
            }
            else if (Systems[sysA].GetConectableAdapter(out a) && Systems[sysB].GetConectableAdapter(out b))
            {
                a.Connect(b);
                b.Connect(a);
                Connections.Add(new Tuple<uint, uint>(sysA, sysB));
                return true;
            }

            return false;
        }
        public bool Connect(XmlNode Parent)
        {

            uint sys1 = 0;
            uint sys2 = 0;
            List<uint> IDs = new List<uint>();
            foreach (XmlNode n in Parent.ChildNodes)
            {
                if (n.Name == "ID")
                {
                    if (sys1 != 0)
                    {
                        sys2 = uint.Parse(n.InnerText);
                    }
                    else
                    {
                        sys1 = uint.Parse(n.InnerText);
                    }
                }
            }
            return Connect(sys1, sys2);
        }

        public static Network LoadNetwork(string filepath, Action<String> log)
        {
            Network network = NewNet();
            
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);
            foreach (XmlNode node in doc.DocumentElement)
            {
                string s = node.Name;

                
                NSHG.System sys;

                switch (s)
                {
                    case "System":
                        try
                        {
                            sys = NSHG.System.FromXML(node);
                        }
                        catch
                        {
                            log("Reading System Failed");
                            break;
                        }
                        try
                        {
                            network.Systems.Add(sys.ID, sys);
                            foreach(Adapter a in sys.NetworkInterfaces.Values)
                            {
                                network.TakenMacAddresses.Add(a.MyMACAddress);
                            }
                            log("Added System \n    ID:" + sys.ID);
                        }
                        catch
                        {
                            log("failed adding system to network");
                        }
                        break;

                    case "Player":

                        break;
                    case "Connection":
                        if (network.Connect(node))
                        {
                            //success
                            log("Addedd connection\n    " + node.InnerText);
                        }
                        else
                        {
                            //failure
                            log("Failed to add connection\n    "+node.InnerText);
                        }
                        break;

                    default:
                        log("Invalid Identifier " + s);
                        break;
                }
            }
            return network;
        }
        public bool SaveNetwork(string filepath)
        {
            try
            {
                XmlDocument file = new XmlDocument();
                XmlNode rootNode = file.CreateElement("root");
                

                foreach (KeyValuePair<uint, NSHG.System> entry in Systems)
                {
                    rootNode.AppendChild(entry.Value.ToXML(file));
                }

                foreach (Tuple<uint, uint> connection in Connections)
                {
                    XmlNode c1 = file.CreateElement("ID");
                    c1.InnerText = connection.Item1.ToString();
                    XmlNode c2 = file.CreateElement("ID");
                    c2.InnerText = connection.Item2.ToString();
                    XmlNode Connection = file.CreateElement("Connection");
                    Connection.AppendChild(c1);
                    Connection.AppendChild(c2);
                    rootNode.AppendChild(Connection);
                }
                file.AppendChild(rootNode);
                file.Save(filepath);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
