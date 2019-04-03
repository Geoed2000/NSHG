﻿using System;
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

        public override void Command(string CommandString)
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