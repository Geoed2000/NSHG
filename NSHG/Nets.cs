using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG
{
    public class IP:IComparable, ICloneable
    {
        public static IP Loopback = new IP(new byte[4] { 127, 0, 0, 1});

        public static IP Zero = new IP(new byte[4] { 0, 0, 0, 0 });
        public static IP Broadcast = new IP(new byte[4] {255,255,255,255});

        // 0.0.0.0 to 255.255.255.255
        private Byte[] Ip = new Byte[4];
        
        public IP(Byte[] IPAddress)
        {
            Ip = IPAddress;
        }

        public IP(Byte[] Array, int StartIndex )
        { 
            ArraySegment<Byte> IPAddress = new ArraySegment<byte>(Array, StartIndex, 4);
            Ip = new List<byte>(IPAddress).ToArray();
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

            if (isNull(ip)) return false;

            Byte[] bip = ip.ToBytes();
            for (int i = 0; i < 4; i++)
            {
                if (Ip[i] != bip[i]) return false;
            }

            return true;
        }
        public static bool isNull(IP iP)
        {
            try
            {
                byte[] b = iP.Ip;
                return false;
            }
            catch
            {
                return true;
            }
        }

        public static IP operator &(IP ip1, IP ip2)
        {
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = (byte)(ip1.Ip[i] & ip2.Ip[i]);
            }
            return new IP(bytes);
        }
        public static IP operator |(IP ip1, IP ip2)
        {
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = (byte)(ip1.Ip[i] | ip2.Ip[i]);
            }
            return new IP(bytes);
        }
        public static bool operator ==(IP ip1, IP ip2)
        {
            if (isNull(ip1))
                if (isNull(ip2)) return true;
                else return false;
            else return ip1.Equals(ip2);
        }
        public static bool operator !=(IP ip1, IP ip2)
        {
            return !(ip1 == ip2);
        }
        public static IP operator ++(IP ip1)
        {
            for(int i = 3; i >= 0; i--)
            {
                try
                {
                    ip1.Ip[i] = (byte)(ip1.Ip[i] + 1);
                    if (ip1.Ip[i] == 0) throw new OverflowException();
                    break;
                }
                catch(OverflowException e)
                {
                    ip1.Ip[i] = 0;
                }
            }
            return ip1;
        }
        public static IP operator ~(IP ip1)
        {
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = (byte)(~ip1.Ip[i]);
            }
            return new IP(bytes);
        }

        public int CompareTo(object o)
        {
            IP ComparedTo;
            try
            {
                ComparedTo = (IP)o;
            }catch(InvalidCastException ic)
            {
                return int.MaxValue;
            }

            for(int i = 0; i < 4; i++)
            {
                int comparedval = Ip[i].CompareTo(ComparedTo.Ip[i]);
                if (comparedval != 0)
                {
                    return comparedval;
                }
            }
            return 0;
        }
        public object Clone()
        {
            return new IP((byte[])Ip.Clone());
        }

        public Byte[] ToBytes()
        {
            return Ip;
        }
    }
    
    public class MAC
    {

        private Byte[] Mac = new Byte[6];

        private static Random rnd = new Random();
        // 00:00:00:00:00:00 to FF:FF:FF:FF:FF:FF
        public static MAC Random()
        {
            byte[] bytes = new byte[6];
            rnd.NextBytes(bytes);
            return new MAC(bytes);
        }

        

        public MAC(Byte[] MACAddress)
        {
            Mac = MACAddress;
        }

        public MAC(Byte[] Array, int StartIndex)
        {
            ArraySegment<Byte> MACAddress = new ArraySegment<byte>(Array, StartIndex, 6);
            Mac = new List<byte>(MACAddress).ToArray();
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
            return s.Replace('-', ':');         
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
            for (int i = 0; i < 6; i++)
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
