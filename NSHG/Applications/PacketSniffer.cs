using System;
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
        private bool on = true;
        private bool ip = true;
        private bool udp = true;
        private bool dhcp = true;

        public PacketSniffer(Action<string> Log = null):base(Log)
        {
        }

        public void onPacket(byte[] datagram)
        {
            if (on)
            {
                IPv4Header iP = new IPv4Header(datagram);
                if (ip)
                {
                    Log("----------------IP----------------");
                    Log("Version:             " + iP.Version);
                    Log("HeaderLength:        " + iP.IHL);
                    Log("Type Of Service      " + iP.TOS);
                    Log("Total Length         " + iP.Length);
                    Log("Identification       " + iP.Identification);
                    Log("Reserved             " + iP.RES);
                    Log("Don't Fragment       " + iP.DF);
                    Log("More Fragments       " + iP.MF);
                    Log("Fragment Offset      " + iP.FragmentOffset);
                    Log("Time To Live         " + iP.TTL);
                    Log("Protocol             " + iP.Protocol);
                    Log("Checksum             " + iP.HeaderChecksum);
                    Log("Source Address       " + iP.SourceAddress);
                    Log("Destination Address  " + iP.DestinationAddress);
                    Log("----------------------------------------");
                }
                
                switch (iP.Protocol)
                {
                    case IPv4Header.ProtocolType.ICMP:
                        ICMPEchoRequestReply icmp = new ICMPEchoRequestReply(iP.Datagram);
                        break;

                    case IPv4Header.ProtocolType.UDP:
                        UDPHeader uDP = new UDPHeader(iP.Datagram);
                        if (udp)
                        {
                            Log("----------------UDP----------------");
                            Log("Source Port       " + uDP.SourcePort);
                            Log("Destination Port  " + uDP.DestinationPort);
                            Log("Length            " + uDP.Length);
                            Log("Checksum          " + uDP.Checksum);
                            Log("----------------------------------------");
                        }

                        DHCPDatagram dHCP = new DHCPDatagram(uDP.Datagram);
                        if (dhcp)
                        {
                            Log("----------------DHCP----------------");
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
                            foreach (DHCPOption option in dHCP.options)
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
                                        Log("    Lease Time         " + BitConverter.ToUInt16(option.data, 0));
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
                        }
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
