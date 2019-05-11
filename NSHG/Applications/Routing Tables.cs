using System;
using System.Collections.Generic;
using System.Text;
using NSHG.NetworkInterfaces;

namespace NSHG.Applications
{
    public class RoutingTable : Application
    {
        public struct Entry
        {
            public IP Destination;
            public IP Netmask;
            public IP Gateway;
            public MAC Interface;
            public uint Metric;
        }

        // used to sort the entries in a sorted list and allow duplicate keys.
        public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
        {
            public int Compare(TKey x, TKey y)
            {
                int result = x.CompareTo(y);

                if (result == 0)
                    return 1;   // Handle equality as beeing greater
                else
                    return result;
            }
        }

        public SortedList<uint, Entry> Entries;
        public SortedList<uint, Entry> StaticEntries;
        protected System sys;

        public RoutingTable(System s)
        {
            Entries = new SortedList<uint, Entry>(new DuplicateKeyComparer<uint>());
            StaticEntries = new SortedList<uint, Entry>(new DuplicateKeyComparer<uint>());
            sys = s;

        }

        public void NewStaticEntry(Entry e)
        {
            StaticEntries.Add(e.Metric, e);
        }

        public virtual void Route(NSHG.Protocols.IPv4.IPv4Header datagram)
        {
            List<Entry> entries = (List<Entry>)Entries.Values;
            
            for (int i = entries.Count - 1; i < 0; i--)
            {
                Entry e = entries[i];
                if ((e.Destination & e.Netmask) == (datagram.DestinationAddress & e.Netmask))
                    if (sys.SendPacket(e.Interface, datagram))
                        return;

            }
        }

        public override void OnTick(uint tick)
        {

        }

        public override void Command(string CommandString)
        {
            string[] Commands = CommandString.Split(' ');
            if (Commands.Length > 0)
            {
                switch (Commands[0])
                {
                    case "add":
                        if (Commands.Length == 6)
                        {
                            Entry e = new Entry();
                            try
                            {
                                e.Destination = IP.Parse(Commands[1]);
                                e.Netmask = IP.Parse(Commands[2]);
                                if(Commands[2].ToLower() != "null")
                                {
                                    e.Gateway = IP.Parse(Commands[3]);
                                }
                                e.Interface = MAC.Parse(Commands[4]);
                                e.Metric = uint.Parse(Commands[5]);
                                NewStaticEntry(e);
                            }
                            catch
                            {
                                Log("Unable to Parse Parameters");
                            }
                        }
                        if ((Commands?[1] ?? "") == "help")
                        {
                            Log("add [Destination IP] [Subnetmask] [Gateway] [Interface] [Metric]");
                            Log("    [Destination IP]  Destination packet will be compared with");
                            Log("    [Subnet Mask]     Subnetmask that will be applied to packet when comparing with destination IP");
                            Log("    [Gateway]         Gateway Packet could be forwarded to be, use 'null' if no gateway is used");
                            Log("    [Interface]       The Mac address of the Network Interface that the packet will be sent on");
                            Log("    [Metric]          The Metric of the rule aka its weight, if it is higher it will be picked over other rules that accept the destination");
                        }
                        else
                        {
                            Log("Invalid parameters use 'add help' for more info");
                        }
                        break;
                    case "table":
                        Log("Destination    Netmask       Gateway    interface       metric"); 
                        foreach(Entry e in Entries.Values)
                        {
                            Log(e.Destination + "   " + e.Netmask + "   " + e.Gateway ?? "null" + "   " + e.Interface + "   " + e.Metric);
                        }

                        Log(" ");
                        Log("Static Entries");
                        foreach(Entry e in StaticEntries.Values)
                        {
                            Log(e.Destination + "   " + e.Netmask + "   " + e.Gateway + "   " + e.Interface + "   " + e.Metric);
                        }
                        break;
                    case "help":
                        Log("add [Destination IP] [Subnetmask] [Gateway] [Interface] [Metric]  -Adds a new static route");
                        Log("Table  -displays routing table");
                        break;
                    
                }
            }
        }
    }

    public class SystemRoutingTable : RoutingTable
    {

        private static Random rand = new Random();
        uint nextUpdateTick = 0;
        
        public SystemRoutingTable(System s) : base(s)
        {
            GenerateTable();
        }

        private void GenerateTable()
        {
            Entries = new SortedList<uint, Entry>(new DuplicateKeyComparer<uint>());
            foreach (NetworkInterface ni in sys.NetworkInterfaces.Values)
            {
                if (ni.Connected)
                {
                    Entries.Add(50, new Entry
                    {
                        Destination = ni.LocalIP | ni.SubnetMask,
                        Netmask = ni.SubnetMask,
                        Gateway = null,
                        Interface = ni.MyMACAddress,
                        Metric = 50
                    });
                    Entries.Add(225, new Entry
                    {
                        Destination = ni.LocalIP,
                        Netmask = IP.Broadcast,
                        Gateway = null,
                        Interface = ni.MyMACAddress,
                        Metric = 225
                    });
                    try
                    {
                        Adapter a = (Adapter)ni;
                        if (a.DefaultGateway != null)
                        {
                            Entries.Add(1, new Entry
                            {
                                Destination = IP.Zero,
                                Netmask = IP.Zero,
                                Gateway = a.DefaultGateway,
                                Interface = a.MyMACAddress,
                                Metric = 1
                            });
                        }
                    }
                    catch (InvalidCastException)
                    {

                    }
                }
            }
            if (StaticEntries.Count != 0)
            {
                foreach (Entry e in StaticEntries.Values)
                {
                    Entries.Add(e.Metric, e);
                }
            }
        }

        public override void OnTick(uint tick)
        {
            if (tick >= nextUpdateTick)
            {
                GenerateTable();
                nextUpdateTick += (uint)rand.Next(50, 200);
            }
        }

        public override void Command(string CommandString)
        {


            base.Command(CommandString);
        }
    }
}
