using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;

namespace XNAPerfStarter
{
class Program
{
    static int timesToLoop = 10000;

    static void Main(string[] args)
    {
        while (true)
        {
            XNAPerformanceChecker.CheckPerformance cp =
                new XNAPerformanceChecker.CheckPerformance();

            Stopwatch sw = new Stopwatch();

            //Call all methods once for any JIT-ing that needs to be done
            sw.Start();
            cp.InitializeTransformWithCalculation();
            cp.InitializeTransformWithConstant();
            cp.InitializeTransformWithDivision();
            cp.InitializeTransformWithConstantReferenceOut();
            cp.TransformVectorByReference();
            cp.TransformVectorByValue();
            cp.TransformVectorByReferenceAndOut();
            cp.TransformVectorByReferenceAndOutVectorAdd();
            cp.CreateCameraReferenceWithProperty();
            cp.CreateCameraReferenceWithValue();
            sw.Stop();
            sw.Reset();

            int i;
            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.InitializeTransformWithCalculation();
            sw.Stop();

            PrintPerformance("         Calculation", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.InitializeTransformWithConstant();
            sw.Stop();

            PrintPerformance("            Constant", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.InitializeTransformWithDivision();
            sw.Stop();

            PrintPerformance("            Division", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.InitializeTransformWithConstantReferenceOut();
            sw.Stop();

            PrintPerformance("ConstantReferenceOut", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.InitializeTransformWithPreDeterminedAspectRatio();
            sw.Stop();

            PrintPerformance("         AspectRatio", ref sw);
            sw.Reset();

            Console.WriteLine();
            Console.WriteLine("——————————————————————");
            Console.WriteLine();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.TransformVectorByReference();
            sw.Stop();

            PrintPerformance("      Reference", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.TransformVectorByValue();
            sw.Stop();

            PrintPerformance("          Value", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.TransformVectorByReferenceAndOut();
            sw.Stop();

            PrintPerformance("ReferenceAndOut", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.TransformVectorByReferenceAndOutVectorAdd();
            sw.Stop();

            PrintPerformance("RefOutVectorAdd", ref sw);
            sw.Reset();

            Console.WriteLine();
            Console.WriteLine("——————————————————————");
            Console.WriteLine();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.CreateCameraReferenceWithProperty();
            sw.Stop();

            PrintPerformance("Property", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.CreateCameraReferenceWithValue();
            sw.Stop();

            PrintPerformance("   Value", ref sw);
            sw.Reset();

            Console.WriteLine();
            Console.WriteLine("——————————————————————");
            Console.WriteLine();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.RotateWithMod();
            sw.Stop();

            PrintPerformance("   RotateWithMod", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.RotateWithoutMod();
            sw.Stop();

            PrintPerformance("RotateWithoutMod", ref sw);
            sw.Reset();

            sw.Start();
            for (i = 0; i < timesToLoop; i++)
                cp.RotateElseIf();
            sw.Stop();

            PrintPerformance("    RotateElseIf", ref sw);
            sw.Reset();

            string command = Console.ReadLine();

            if (command.ToUpper().StartsWith("E") ||
                command.ToUpper().StartsWith("Q"))
                break;
        }
    }

    static void PrintPerformance(string label, ref Stopwatch sw)
    {
        Console.WriteLine(label + " – Avg: " +
            ((float)((float)(sw.Elapsed.Ticks * 100) /
            (float)timesToLoop)).ToString("F") +
            " Total: " + sw.Elapsed.TotalMilliseconds.ToString());
    }
}
}
