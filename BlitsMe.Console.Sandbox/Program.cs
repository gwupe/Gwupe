using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlitsMe.Console.Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            byte _seq = 0;
            const byte _seqMask =(byte) 0x0F;
            for (byte i = 0; i < 300; i++)
            {
                System.Console.WriteLine("i: " + i + " seq : " + (i&0x0F));
            }
            System.Console.WriteLine("Done");
        }
    }
}
