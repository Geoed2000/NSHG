﻿using System;
using System.Collections.Generic;
using System.Text;
using NSHG.Protocols.IPv4;
using NSHG.Protocols.UDP;
using NSHG.Protocols.ICMP;
using NSHG.Protocols.DHCP;

namespace NSHG.Applications
{
    public class PacketSniffer : Application
    {
        bool on = false;
        bool ip = true;
        bool udp = true;
        bool dhcp = true;

        public PacketSniffer(Action<string> Log = null):base(Log)
        {

        }

        public void onPacket(byte[] datagram)
        {
            if (on)
            {
                IPv4Header ip = new IPv4Header(datagram);
                Log("Version:             " + ip.Version);
                Log("HeaderLength:        " + ip.IHL);
                Log("Type Of Service      " + ip.TOS);
                Log("Total Length         " + ip.Length);
                Log("Identification       " + ip.Identification);
                Log("Reserved             " + ip.RES);
                Log("Don't Fragment       " + ip.DF);
                Log("More Fragments       " + ip.MF);
                Log("Fragment Offset      " + ip.FragmentOffset);
                Log("Time To Live         " + ip.TTL);
                Log("Protocol             " + ip.Protocol);
                Log("Checksum             " + ip.HeaderChecksum);
                Log("Source Address       " + ip.SourceAddress);
                Log("Destination Address  " + ip.DestinationAddress);
                Log("----------------------------------------");

                switch (ip.Protocol)
                {
                    case IPv4Header.ProtocolType.ICMP:
                        ICMPEchoRequestReply icmp = new ICMPEchoRequestReply(ip.Datagram);
                        break;

                    case IPv4Header.ProtocolType.UDP:
                        UDPHeader udp = new UDPHeader(ip.Datagram);
                        Log("Source Port       " + udp.SourcePort);
                        Log("Destination Port  " + udp.DestinationPort);
                        Log("Length            " + udp.Length);
                        Log("Checksum          " + udp.Checksum);
                        Log("----------------------------------------");
                        DHCPDatagram dHCP = new DHCPDatagram(udp.Datagram);
                        Log("Op                ");
                        Log("htype             ");
                        Log("hlen              ");
                        Log("hops              ");
                        Log("transaction ID    ");
                        Log("Secconds Elapsed  ");
                        Log("Flags");
                        Log("    Boradcast     ");
                        Log("Client IP         ");
                        Log("Your IP           ");
                        Log("Server IP         ");
                        Log("Router IP         ");
                        Log("Client MAC        ");
                        Log("Server Host Name  ");
                        Log("Boot File Name    ");
                        Log("Options");
                        foreach(DHCPOption option in dHCP.options)
                        {
                            switch (option.tag)
                            {
                                case Tag.subnetmask:
                                    Log("    SubnetMask         " + new IP(option.data).ToString());
                                    break;
                                case Tag.defaultIPTTL:
                                    Log("    Default TTL        " + option.data[0]);
                                    break;
                                case Tag.addressRequest:
                                    Log("    Requested IP       " + new IP(option.data).ToString());
                                    break;
                                case Tag.addressTime:
                                    Log("    Lease Time         " + BitConverter.ToUInt16(option.data,0));
                                    break;
                                case Tag.router:
                                    Log("    DHCP Message Type  " + option.data[0]);
                                    break;
                                case Tag.paramaterList:
                                    Log("    Paramater List  ");
                                    foreach (byte b in option.data) Log("        " + b);
                                    break;
                            }
                        }
                        Log("----------------------------------------");
                        break;
                }
            }
        }
        
        public override void Command(string commandstring)
        {
            string[] commands = commandstring.Split(' ');
            if (commands.Length >= 1)
            {
                switch (commands[0].ToLower())
                {
                    case "start":
                        on = true;
                        break;
                    case "stop":
                        on = false;
                        break;
                    case "ip":
                        ip = !ip;
                        Log("IP log status: " + ip);
                        break;
                    case "udp":
                        udp = !udp;
                        Log("UDP log status: " + udp);
                        break;
                    case "dhcp":
                        dhcp = !dhcp;
                        Log("DHCP log status: " + dhcp);
                        break;
                    case "exit":
                        closed = true;
                        break;
                    case "help":
                        Log("start  -starts listening to packets");
                        Log("stop   -stops listening to packets");
                        Log("ip     -toggles logging IP datagram");
                        Log("udp    -toggles logging UDP datagram");
                        Log("dhcp   -toggles logging DHCP datagram");
                        Log("exit   -closes this application");
                        Log("help   - Shows this help window");

                        break;


                }
            }
        }

        public override void OnTick(uint tick)
        {

        }
    }
}
