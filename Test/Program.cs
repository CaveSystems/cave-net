using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Test
{
    class Program
    {
        static int Main(string[] args)
        {
            var errors = 0;
            Type[] types = typeof(Program).Assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.GetCustomAttributes(typeof(TestFixtureAttribute), false).Length == 0)
                {
                    continue;
                }

                var instance = Activator.CreateInstance(type);
                foreach (System.Reflection.MethodInfo method in type.GetMethods())
                {
                    if (method.GetCustomAttributes(typeof(TestAttribute), false).Length == 0)
                    {
                        continue;
                    }

                    GC.Collect(999, GCCollectionMode.Default);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{method.DeclaringType.Name}.cs: info TI0001: Start {method.Name}");
                    Console.ResetColor();
                    try
                    {
                        method.Invoke(instance, null);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{method.DeclaringType.Name}.cs: info TI0002: Success {method.Name}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{method.DeclaringType.Name}.cs: error TE0001: {ex.Message}");
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
                Console.WriteLine($"---: info TI9999: All tests successfully completed.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"---: error TE9999: {errors} tests failed!");
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
    }
}
