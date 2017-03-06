﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Factory;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            Console.WriteLine("Reading configuration");

            string fileName = "input.json";
            string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

            if(File.Exists(configFilePath) == false)
            {
                throw new FileNotFoundException($"{fileName} not found at {Directory.GetCurrentDirectory()}");
            }

            string jsonContent = File.ReadAllText(configFilePath);

            var algorithm = AlgorithmFactory.Create(jsonContent);

            Console.WriteLine($"{algorithm.Name} algorithm is running....");
            
            algorithm.Run();

            Console.WriteLine("Algorithm has completed");

            WaitKeyPress();
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception)e.ExceptionObject;

            Console.WriteLine($"ERROR! {exception.Message}");

            WaitKeyPress();

            Environment.Exit(1);
        }

        private static void WaitKeyPress()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}