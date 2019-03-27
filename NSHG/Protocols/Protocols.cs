using System;
using System.Collections.Generic;
using System.Text;

namespace NSHG
{
    namespace Protocols
    {
        public abstract class Header
        {

            /// <summary>
            /// Calculates the one's compliment of the one's compliment sum in 16 bit words of the byte array provided
            /// </summary>
            /// <param name="data">Byte array containing the data to be included in the check sum</param>
            /// <param name="start">Starting byte in the array</param>
            /// <param name="length">Amount of bytes to read, must be divisible by 2, start + length >= data.length</param>
            /// <returns>16bit unsinged one's comliment sum</returns>
            public static UInt16 CalculateChecksum(byte[] data)
            {
                if (data.Length % 2 == 1)
                {
                    byte[] tmp = new byte[data.Length + 1];
                    data.CopyTo(tmp, 0);
                    tmp[tmp.Length - 1] = 0;
                    data = tmp;
                }
                UInt16 current;
                UInt32 total = 0;

                for (int i = 0; i < data.Length; i += 2)
                {
                    current = (UInt16)((data[i] << 8) + data[i + 1]);
                    total += current;
                    while ((total >> 16) > 0) // if the value is > 65536(2^16) then remove the 256 
                    {
                        total = (total & 0xFFFF) + (total >> 16);
                    }
                }
                return (UInt16)~total;

            }

            public abstract byte[] ToBytes();
        }


        public class DSPDatagram
        {

        }

        public class FTPDatagram
        {

        }

        public class HTTPDatagram
        {

        }

        public class HTTPSDatagram
        {

        }

        public class POP3Datagram
        {

        }

        public class SMTPDatagram
        {

        }

        public class SSHDatagram
        {

        }
    }
}
