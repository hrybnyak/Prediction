using System;
using System.Net.Http;

namespace Prediction
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser();
            parser.GetMatchesForTheWeek();
        }
    }
}
