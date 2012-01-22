// Copyright (c) 2009 http://grommet.codeplex.com
// This source is subject to the Microsoft Public License.
// See http://www.opensource.org/licenses/ms-pl.html
// All other rights reserved.

using System.IO;
using System;

namespace STLK
{
    public class ApiFrame
    {
        public const byte StartDelimiter = 0x7E;

        public byte Retries { get; set; }
        public DeliveryStatus DeliveryStatus { get; set; }
        public DiscoveryStatus DiscoveryStatus { get; set; }
        public XBeeCallback Callback { get; set; }

        public ushort Length 
        {
            get { return (ushort)FrameData.Length; }
        }

        public byte[] FrameData { get; set; }

        public byte Checksum
        {
            get
            {
                byte checksum = 0xFF;
                for (int i = 0; i < FrameData.Length; i++)
                {
                    checksum -= FrameData[i];
                }
                return checksum;
            }
        }

        public byte[] Serialize()
        {
            MemoryStream stream = new MemoryStream();
            stream.WriteByte(StartDelimiter);
            byte[] lengthBuffer = GetBytes(Length);
            stream.Write(lengthBuffer, 0, lengthBuffer.Length);
            stream.Write(FrameData, 0, Length);
            stream.WriteByte(Checksum);
            stream.Close();

            return stream.ToArray();
        }

        public static byte[] GetBytes(ushort value)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)value;
            return buffer;
        }

    }
}
