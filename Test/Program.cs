using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Test
{
    class Program
    {
        #region Private Methods

        static int Main(string[] args)
        {
            Verbose = args.Contains("-v") || args.Contains("--verbose");
            var errors = 0;
            var types = typeof(Program).Assembly.GetTypes();
            foreach (var type in types.OrderBy(t => t.Name))
            {
                if (!type.GetCustomAttributes(typeof(TestFixtureAttribute), false).Any())
                {
                    continue;
                }

                var instance = Activator.CreateInstance(type);
                foreach (var method in type.GetMethods())
                {
                    if (!method.GetCustomAttributes(typeof(TestAttribute), false).Any())
                    {
                        continue;
                    }

                    GC.Collect(999, GCCollectionMode.Default);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{method.DeclaringType.Name}.cs: info TI0001: Start {method.Name} framework {Environment.Version}");
                    Console.ResetColor();
                    try
                    {
                        method.Invoke(instance, null);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{method.DeclaringType.Name}.cs: info TI0002: Success {method.Name} framework {Environment.Version}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{method.DeclaringType.Name}.cs: error TE0001: {ex.Message} framework {Environment.Version}");
                        Console.WriteLine(ex);
                        Console.ResetColor();
                        errors++;
                    }

                    Console.WriteLine("---");
                }
            }

            if (errors == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"---: info TI9999: All tests successfully completed at framework {Environment.Version}.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"---: error TE9999: {errors} tests failed framework {Environment.Version}!");
            }

            Console.ResetColor();
            if (Debugger.IsAttached)
            {
                WaitExit();
            }

            return errors;
        }

        static void WaitExit()
        {
            Console.Write("--- press enter to exit ---");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter)
            {
                ;
            }
        }

        public static bool Verbose { get; private set; }

        #endregion Private Methods
    }
}
