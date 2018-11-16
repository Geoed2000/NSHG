using System;
using NSHG;
using NSHG.Packet;

namespace ConsoleApp1
{
    class GeneralTests
    {
        static void Output(bool value, string test)
        {
            if (value)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.WriteLine( test + ": " + value);
            Console.ForegroundColor = ConsoleColor.White;
        }


        static void Main(string[] args)
        {

            {
                Console.WriteLine("NSHG.IP");
                IP ip0 = new IP(new Byte[] { 0, 0, 0, 0 });
                Output(ip0.Equals(ip0), "ip.Equals (Identical)");

                Console.WriteLine("NSHG.IP");
                IP ip1234 = new IP(new Byte[] { 1, 2, 3, 4 ,5},0);
                IP ip12342 = new IP(new Byte[] { 1, 2, 3, 4});
                Output(ip12342.Equals(ip1234), "ip.from array size > 4");


                IP ip255 = new IP(new Byte[] { 255, 255, 255, 255 });
                IP ip2552 = new IP(new Byte[] { 255, 255, 255, 255 });
                Output(ip255.Equals(ip2552), "ip.Equals (Non Identical)");

                IP ip3 = new IP(new Byte[] { 0, 0, 0, 0 });
                Output(ip0.Equals(ip3), "ip.Equals (Non Identical)");

                IP ip4 = new IP(new Byte[] { 0, 0, 0, 1 });
                Output(!ip0.Equals(ip4), "ip.Equals (Non Identical, Not Equal)");

                // Parse
                // Normal Parse's
                IP ip5 = IP.Parse("192.168.1.1");
                IP ip192 = new IP(new Byte[] { 192, 168, 1, 1 });
                Output(ip5.Equals(ip192), "ip.Parse(\"192.168.1.1\")");

                ip5 = IP.Parse("0.0.0.0");
                Output(ip5.Equals(ip0), "ip.Parse(\"0.0.0.0\")");

                ip5 = IP.Parse("255.255.255.255");
                Output(ip5.Equals(ip255), "ip.Parse(\"255.255.255.255\")");


                // Address Segments > 255
                bool result = false;
                try
                {
                    IP ip = IP.Parse("256.256.256.256");
                }
                catch (OverflowException)
                {
                    result = true;
                }
                finally
                {
                    Output(result, "IP.Parse (Overflow)");
                }

                // Null Input
                result = false;
                try
                {
                    IP ip = IP.Parse("");
                }
                catch (ArgumentNullException)
                {
                    result = true;
                }
                finally
                {
                    Output(result, "IP.Parse (Null)");
                }

                // Not Enough Segments
                result = false;
                try
                {
                    IP ip = IP.Parse("1");
                }
                catch (ArgumentOutOfRangeException)
                {
                    result = true;
                }
                finally
                {
                    Output(result, "IP.Parse (1)");
                }
                result = false;
                try
                {
                    IP ip = IP.Parse("1.1");
                }
                catch (ArgumentOutOfRangeException)
                {
                    result = true;
                }
                finally
                {
                    Output(result, "IP.Parse (1.1)");
                }
                result = false;
                try
                {
                    IP ip = IP.Parse("1.1.1");
                }
                catch (ArgumentOutOfRangeException)
                {
                    result = true;
                }
                finally
                {
                    Output(result, "IP.Parse (1.1.1)");
                }
            }// IP

            {
                MAC m = new MAC(new Byte[] { 255, 255, 255, 255, 255, 255 });

                m = MAC.Parse("FF:FF:FF:FF:FF:00");
                Console.WriteLine(m.ToString());
                foreach(Byte b in m.ToBytes())
                {
                    Console.WriteLine(b);
                }
            }// MAC

            {

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();

                byte[] expected, actual;

                expected = new byte[]
                {
                0x45, 0x00, 0x00, 0x28, 0x6e, 0x9c, 0x40, 0x00, 0x80, 0x06, 0x77, 0xd7, 0x0a, 0x90, 0xe3, 0x64, 0x28, 0x43, 0xfe, 0x24
                };

                IPv4Header ipv4 = new IPv4Header(0x6E9C, true, false, 128, (IPv4Header.ProtocolType)6, IP.Parse("10.144.227.100"), IP.Parse("40.67.254.36"), new byte[0], new byte[20]);

                


                actual = ipv4.ToBytes();

                foreach (byte b in expected)
                {
                    Console.Write(b.ToString("X"));
                    Console.Write(" ");
                }
                Console.WriteLine();


                foreach (byte b in actual)
                {
                    Console.Write(b.ToString("X"));
                    Console.Write(" ");
                }
                Console.WriteLine();
            }// IP  Header

            {

            }// TCP Header


            Console.ReadLine();
        }
    }
}
