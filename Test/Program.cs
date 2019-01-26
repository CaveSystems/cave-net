using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cave;
using NUnit.Framework;

namespace Test
{
    class Program
    {
        static readonly object consoleLock = new object();

        static readonly ParallelOptions parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
        };

        static bool Test(MethodInfo method)
        {
            try
            {
                object obj = Activator.CreateInstance(method.DeclaringType);
                method.Invoke(obj, new object[0]);
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Success ");
                    Console.ResetColor();
                    Console.WriteLine($"{method.DeclaringType}.{method.Name}");
                }
                return true;
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Error ");
                    Console.ResetColor();
                    Console.WriteLine($"{method.DeclaringType}.{method.Name}");
                    Console.WriteLine(ex.ToString());
                }
                return false;
            }
        }


        public static void Main()
        {
            System.Collections.Generic.IEnumerable<MethodInfo> methods = typeof(Program)
                .Assembly
                .GetTypes()
                //.Where(t => t
                //.HasAttribute<TestClassAttribute>())
                .SelectMany(t => t
                .GetMethods()
                .Where(m => m
                //.HasAttribute<TestMethodAttribute>()));
                .HasAttribute<TestAttribute>()));
#if DEBUG
            foreach (MethodInfo method in methods)
            {
                Test(method);
            }
#else
            Parallel.ForEach(methods, parallelOptions, (method) => Test(method));
#endif
            if (Debugger.IsAttached)
            {
                Console.WriteLine("--- press enter to exit ---");
                Console.ReadLine();
            }
        }
    }
}
