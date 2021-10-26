using NiftyLaunchpad.Lib;
using System;
using System.Collections.Generic;

namespace NiftyLaunchpad.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var collectionRetriever = new CollectionRetriever();

            // Retrieve Active Collection + Mintable Tokens from Azure Table Storage
            var collection = collectionRetriever.GetCollectionAsync(Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae"));

            //var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            //while (await timer.WaitForNextTickAsync())
            //{
            //    
            //}
            Console.WriteLine("Hello World!");
        }
    }
}
