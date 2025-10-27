using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serial_Com.Services.Cobs
{


    public sealed class Cobs
    {


        public static byte[] CobsEncode(byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                return new byte[] { 1, 0 };
            }

            var outBuf = new List<byte>(input.Length + 2);

            int codeIndex = 0;
            byte code = 1;
            outBuf.Add(0);

            foreach (byte currentByte in input)
            {
                if (currentByte != 0)
                {
                    outBuf.Add(currentByte);
                    code++;

                    if (code == 0xFF) // max run length reached
                    {
                        outBuf[codeIndex] = code;
                        code = 1;
                        codeIndex = outBuf.Count; // start new block
                        outBuf.Add(0);            // placeholder for next code
                    }
                }
                else
                {
                    outBuf[codeIndex] = code;
                    code = 1;
                    codeIndex = outBuf.Count; // start new block after the zero
                    outBuf.Add(0);            // placeholder for next code
                }
            }

            outBuf[codeIndex] = code; // finalize last block
            outBuf.Add(0);            // frame terminator (common on UART)

            return outBuf.ToArray();
        }

        public static byte[] CobsDecode(ReadOnlyMemory<byte> encoded)
        {
            ReadOnlySpan<byte> s = encoded.Span;
            if (s.Length == 0)
            {
                return Array.Empty<byte>();
            }

            int end = s.Length - 1;                  // last byte must be 0 (frame terminator)
            if (s[end] != 0)
            {
                throw new FormatException("Missing COBS frame terminator (0x00).");
            }

            var output = new List<byte>(end);        // worst case: all data

            int i = 0;
            while (i < end)
            {
                byte code = s[i++];
                if (code == 0)
                {
                    throw new FormatException("Invalid COBS code byte (0).");
                }

                int copyLen = code - 1;
                if (i + copyLen > end)
                {
                    throw new FormatException("COBS code exceeds frame length.");
                }

                // copy block bytes
                output.AddRange(s.Slice(i, copyLen).ToArray());
                i += copyLen;

                // reinsert a zero between blocks (unless code==0xFF or we're at end)
                if (code != 0xFF && i < end) output.Add(0);
            }

            return output.ToArray();
        }

        public static byte[] CobsDecodeNoTerm(ReadOnlyMemory<byte> encoded)
        {
            var s = encoded.Span;
            if (s.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var output = new List<byte>(s.Length);
            int i = 0;

            while (i < s.Length)
            {
                byte code = s[i++];
                if (code == 0) throw new FormatException("Invalid COBS code byte (0).");

                int copyLen = code - 1;
                if (i + copyLen > s.Length) throw new FormatException("COBS code exceeds frame length.");

                for (int k = 0; k < copyLen; k++) output.Add(s[i + k]);
                i += copyLen;

                // Reinsert a zero between blocks if code < 0xFF and not at end
                if (code != 0xFF && i < s.Length) output.Add(0);
            }

            return output.ToArray();
        }






    }







}
