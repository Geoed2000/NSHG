﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG
{
    public class IP
    {
        public static IP Loopback = new IP(new byte[4] { 127, 0, 0, 1});

        // 0.0.0.0 to 255.255.255.255
        private Byte[] Ip = new Byte[4];
        
        public IP(Byte[] IPAddress)
        {
            Ip = IPAddress;
        }

        public IP(Byte[] Array, int StartIndex )
        {
            ArraySegment<Byte> IPAddress = new ArraySegment<byte>(Array, StartIndex, 4);
            Ip = IPAddress.Array;
        }

        public static IP Parse(string IPAddress)
        {

            //Get rid of trailing and leading white space
            IPAddress = IPAddress.Trim();

            //Test if address is null
            if (IPAddress == null || IPAddress == "") throw new ArgumentNullException("address is null");

            //split into segments based on dot notation
            string[] ints = IPAddress.Split('.');

            //Test for correct amount of segments
            if (ints.Length != 4) throw new ArgumentOutOfRangeException("address not 4 segments long");

            //create Byte array to store IP in
            Byte[] byteAddress  = new Byte[4];

            //convert each string into a Byte
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    byteAddress[i] = Byte.Parse(ints[i].Trim());
                }
            }
            catch (ArgumentNullException)
            {
                //Test if one of the segments are null
                throw new ArgumentException("address segment null");
            }
            catch (OverflowException)
            {
                //Test if segment is too big
                throw new OverflowException("address segment overflow, segments must be < 255");
            }

            return new IP(byteAddress);
        }
        public static bool TryParse(string address, out IP IPaddress)
        {
            try {IPaddress = Parse(address);}
            catch
            {
                IPaddress = null;
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            string i = ((int)Ip[0]).ToString() + "." + ((int)Ip[1]).ToString() + "." + ((int)Ip[2]).ToString() + "." + ((int)Ip[3]).ToString();
            return i;
        }
        public override bool Equals(object obj)
        {

            IP ip;
            try
            {
                ip = (IP)obj;
            }
            catch
            {
                return false;
            }

            Byte[] bip = ip.ToBytes();
            for (int i = 0; i < 4; i++)
            {
                if (Ip[i] != bip[i]) return false;
            }

            return true;
        }
        public Byte[] ToBytes()
        {
            return Ip;
        }
    }

    public class MAC
    {
        // 00:00:00:00:00:00 to FF:FF:FF:FF:FF:FF

        private Byte[] Mac = new Byte[6];

        public MAC(Byte[] MACAddress)
        {
            Mac = MACAddress;
        }

        public static MAC Parse(String MACAddress)
        {
            //Get rid of trailing and leading white space
            MACAddress = MACAddress.Trim();

            //Test if address is null
            if (MACAddress == null || MACAddress == "") throw new ArgumentNullException("address is Null");

            List<Byte> Bytes = new List<Byte>();

            String[] hexs =  MACAddress.Split(':');

            if (hexs.Length != 6) throw new ArgumentOutOfRangeException("address not 6 segments long");
            try
            {
                foreach (string s in hexs)
                {
                    if (s == null || s == "") throw new ArgumentNullException("address segment is null");
                    Bytes.Add(Convert.ToByte(s, 16));
                }
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException("address segment overflow, segments must be < 255");
            }
            
            return new MAC(Bytes.ToArray());

        }
        public static bool TryParse(String MACAddressIn, out MAC MACAddressOut)
        {
            try
            {
                MAC Mac;
                Mac = Parse(MACAddressIn);
                MACAddressOut = Mac;
                return true;
            }
            catch
            {
                MACAddressOut = null;
                return false;
            }
        }

        public override string ToString()
        {
            string s = BitConverter.ToString(Mac);
            s.Replace('-', ':');
            return s;           
        }
        public override bool Equals(object obj)
        {
            MAC mac;
            try
            {
                mac = (MAC)obj;
            }
            catch
            {
                return false;
            }

            Byte[] bmac = mac.ToBytes();
            for (int i = 0; i < 12; i++)
            {
                if (Mac[i] != bmac[i]) return false;
            }

            return true;
        }
        public Byte[] ToBytes()
        {
            return Mac;
        }
    }
}
