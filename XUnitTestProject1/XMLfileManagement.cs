﻿using System;
using Xunit;
using NSHG;
using System.Xml;
using System.Collections.Generic;

namespace XUnitTests
{
    public class XMLfileManagement
{
        public class AdapterTests
        {
            [Fact]
            public void Read1()
            {
                MAC mac = MAC.Parse("FF:FF:FF:FF:FF:FF");
                string name = "Adapter 1";
                IP localip = IP.Parse("192.168.1.2");
                IP Subnet = IP.Parse("255.255.255.0");
                IP DefaultG = IP.Parse("192.168.1.1");

                NSHG.Adapter a = new NSHG.Adapter(mac, name, localip, Subnet, DefaultG, 1, true);
                NSHG.Adapter a2 = null;
                XmlDocument doc = new XmlDocument();
                doc.Load("XMLFile.xml");
                
                foreach (XmlNode node in doc.DocumentElement)
                {
                    if(node.Name == "AdapterRead1")
                    {
                        foreach (XmlNode n in node.ChildNodes)
                        {
                            if (n.Name == "Adapter")
                            {
                                a2 = NSHG.Adapter.FromXML(n);
                            }
                        }
                    }
                }
                
                Assert.True(a.Equals(a2));

            }
        }

        public class SystemTests
        {
            [Fact]
            public void Read1()
            {
                NSHG.System s1;
                NSHG.System s2 = null;
                List<NSHG.Adapter> a = new List<NSHG.Adapter>();

                MAC mac = MAC.Parse("FF:FF:FF:FF:FF:FF");
                string name = "Adapter 1";
                IP localip = IP.Parse("192.168.1.2");
                IP Subnet = IP.Parse("255.255.255.0");
                IP DefaultG = IP.Parse("192.168.1.1");

                a.Add(new NSHG.Adapter(mac, name, localip, Subnet, DefaultG, 1, true));

                s1 = new NSHG.System(1, a, true);


                XmlDocument doc = new XmlDocument();
                doc.Load("XMLFile.xml");

                foreach (XmlNode node in doc.DocumentElement)
                {
                    if (node.Name == "SystemRead1")
                    {
                        foreach (XmlNode n in node.ChildNodes)
                        {
                            if (n.Name == "System")
                            {
                                s2 = NSHG.System.FromXML(n);
                            }
                        }
                    }
                }

                Assert.True(s1.Equals(s2));

            }
        }
}
}