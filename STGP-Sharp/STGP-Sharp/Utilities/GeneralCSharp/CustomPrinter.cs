using System;

namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public static class CustomPrinter
    {
        public static void PrintLine(object? toPrint)
        {
            Console.WriteLine(toPrint);
        }

        public static void PrintBreak()
        {
            PrintLine("/////////////////////////////////////////");
        }
    }
}