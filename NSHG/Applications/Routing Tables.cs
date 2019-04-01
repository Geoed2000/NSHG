using System;
using System.Collections.Generic;
using System.Text;

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

        public SortedList<uint, Entry> Entries;
        public SortedList<uint, Entry> StaticEntries;
        protected System sys;

        public RoutingTable(System s)
        {
            Entries = new SortedList<uint, Entry>();

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
    }

    public class SystemRoutingTable : RoutingTable
    {

        private static Random rand = new Random();

        uint nextUpdateTick = 0;
        
        public SystemRoutingTable(System s) : base(s)
        {

        }

        private void GenerateTable()
        {
            foreach (Adapter a in sys.NetworkInterfaces.Values)
            {
                if (a.Connected)
                {
                    Entries.Add(50, new Entry
                    {
                        Destination = a.LocalIP | a.SubnetMask,
                        Netmask = a.SubnetMask,
                        Gateway = null,
                        Interface = a.MyMACAddress,
                        Metric = 50
                    });
                    Entries.Add(225, new Entry
                    {
                        Destination = a.LocalIP,
                        Netmask = IP.Broadcast,
                        Gateway = null,
                        Interface = a.MyMACAddress,
                        Metric = 225
                    });
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
            }

            foreach (Entry e in StaticEntries.Values)
            {
                Entries.Add(e.Metric, e);
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
    }
}
