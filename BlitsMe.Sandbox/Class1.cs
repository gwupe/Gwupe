using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BlitsMe.Communication.P2P.RUDP.Utils;

namespace BlitsMe.Sandbox
{
    class Class1 : ApplicationContext
    {
        private ushort me = ushort.MaxValue - 10;
        private Queue<byte> validate;
        private bool run = true;

        public Class1()
        {
            validate = new Queue<byte>();
            CircularBuffer<byte> buffer = new CircularBuffer<byte>(1);

            Thread getter = new Thread(() => runGetter(buffer)) { IsBackground = true };
            getter.Start();
            runAdder(buffer);
            run = false;
            ExitThread();
        }

        private void runGetter(CircularBuffer<byte> buffer)
        {
            Random rand = new Random();
            byte[] otherBuff = new byte[20];
            while(run || buffer.Count > 0)
            {
                byte[] get = buffer.Get(rand.Next(19) + 1, 1000);
                foreach (var b in get)
                {
                    validateGet(b);
                }
                int count = buffer.Get(otherBuff, 1000);
                for(int i = 0; i < count;i++)
                {
                    validateGet(otherBuff[i]);
                }
            }
            if(validate.Count > 0)
            {
                throw new Exception("Oops, " + validate.Count + " still left in the queue.");
            }
            Console.WriteLine("Completed");
        }

        private void validateGet(byte b)
        {
            byte val = validate.Dequeue();
            if (b != val)
                throw new Exception("Got " + val + ", expected " + b);
            Console.WriteLine("Got " + b + ", it matches expected");
        }

        private void runAdder(CircularBuffer<byte> buffer)
        {
            byte[] input;
            Random rand = new Random();
            for (int i = 0; i < 100; i++)
            {
                input = new byte[rand.Next(19) + 1];
                rand.NextBytes(input);
                AddToBuffer(input, buffer);
                if(input.Length > 20)
                {
                    Thread.Sleep(20);
                }
            }
        }

        private void AddToBuffer(byte[] input, CircularBuffer<byte> buffer)
        {
            Console.WriteLine("Adding " + input.Length + " values.");
            foreach (var b in input)
            {
                Console.WriteLine("Added " + b);
                validate.Enqueue(b);
            }
            buffer.Add(input, 1000);
        }
    }
}
