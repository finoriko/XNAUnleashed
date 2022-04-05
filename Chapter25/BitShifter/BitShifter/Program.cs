using System;

namespace BitShifter
{
    class Program
    {

        enum State
        {
            Awake,
            Asleep,
            Random,
            Searching,
            Fleeing,
            Attacking
        }
        
        static bool isInventoryFull = true;
        static bool isAbleToRun = false;
        static State state = State.Random;
        
        static int health = 15;
        static int status = 1;

        static byte firstByte = 0;
        static byte secondByte = 0;

        static void Main(string[] args)
        {
            int bitfield = 0;
            AddToBitfield(ref bitfield, 1, isInventoryFull ? 1 : 0);
            AddToBitfield(ref bitfield, 1, isAbleToRun ? 1 : 0);
            AddToBitfield(ref bitfield, 4, (int)state);

            firstByte = (byte)bitfield; //packetWriter.Write((byte)bitfield);
            Console.WriteLine("firstByte: " + Convert.ToString(firstByte, 2));
            Console.WriteLine();

            bitfield = 0;
            AddToBitfield(ref bitfield, 4, health);
            AddToBitfield(ref bitfield, 4, status);

            secondByte = (byte)bitfield; //packetWriter.Write((byte)bitfield);
            Console.WriteLine("secondByte: " + Convert.ToString(secondByte, 2));
            Console.WriteLine();


            bitfield = firstByte; //packetReader.ReadByte();
            state = (State)ReadFromBitfield(ref bitfield, 4);
            isAbleToRun = ReadFromBitfield(ref bitfield, 1) != 0;
            isInventoryFull = ReadFromBitfield(ref bitfield, 1) != 0;

            bitfield = secondByte; //packetReader.ReadByte();
            status = ReadFromBitfield(ref bitfield, 4);
            health = ReadFromBitfield(ref bitfield, 4);

            Console.WriteLine("isInventoryFull: " + isInventoryFull.ToString());
            Console.WriteLine("isAbleToRun: " + isAbleToRun.ToString());
            Console.WriteLine("state: " + state.ToString());
            Console.WriteLine("health: " + health.ToString());
            Console.WriteLine("status: " + status.ToString());
            
            Console.ReadLine();
        }

        static void AddToBitfield(ref int bitfield, int bitCount, int value)
        {
            bitfield <<= bitCount;
            bitfield |= value;
        }

        static int ReadFromBitfield(ref int bitfield, int bitCount)
        {
            int value = bitfield & ((1 << bitCount) - 1);
            bitfield >>= bitCount;

            return value;
        }
        
    }
}
