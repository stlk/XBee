// Copyright (c) 2009 http://grommet.codeplex.com
// This source is subject to the Microsoft Public License.
// See http://www.opensource.org/licenses/ms-pl.html
// All other rights reserved.

using System;

namespace STLK
{
    internal class ApiFrameBuilder
    {
        private const byte startDelimiter = 0x7E;
        private ApiFrame frame;
        private int position;
        private int length;
        private byte checksum;
        private byte[] buffer;
        private long lastReception;

        public bool IsComplete { get; private set; }

        public int BytesAppended
        {
            get { return position; }
        }

        public ApiFrameBuilder()
        {
            Reset();
        }

        public ApiFrame GetApiFrame()
        {
            if (IsComplete)
            {
                return frame;
            }
            return null;
        }

        public void Append(byte value)
        {
            long ticks = DateTime.Now.Ticks;
            if (value == startDelimiter && position != 0)
            {
                TimeSpan span = new TimeSpan(ticks - lastReception);
                if (span.Seconds > 1)
                {
                    // Timeout!
                    Reset();
                }
            }
            lastReception = ticks;

            switch (position)
            {
                case 0: // Start Delimitor
                    if (value == startDelimiter)
                    {
                        position++;
                    }
                    break;
                case 1: // MSB Length
                    if (value != 0)
                    {
                        // Invalid length
                        Reset();
                    }
                    else
                    {
                        length |= value << 8;
                        position++;
                    }
                    break;
                case 2: // LSB Length
                    length |= value;
                    buffer = new byte[length];
                    position++;
                    break;
                default: // Frame data
                    checksum += value;
                    if (position == 3 + length)
                    {
                        if (checksum == 0xFF)
                        {
                            IsComplete = true;
                            frame.FrameData = buffer;
                            break;
                        }
                        else
                        {
                            Reset();
                        }
                    }
                    buffer[position - 3] = value;
                    position++;
                    break;
            }
        }

        public void Reset()
        {
            position = 0;
            length = 0;
            checksum = 0;
            IsComplete = false;
            frame = new ApiFrame();
        }
    }
}
