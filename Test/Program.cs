using System;
using System.Linq;
using NUnit.Framework;

namespace Test
{
    class Program
    {
        public static bool WarningsOnly { get; private set; }

        static int Main(string[] args)
        {
            WarningsOnly = true;
            var types = typeof(Program).Assembly.GetTypes();
            foreach (var type in types)
            {
                /*if (!type.GetCustomAttributes(typeof(TestAttribute), false).Any())
                {
                    continue;
                }*/

                var instance = Activator.CreateInstance(type);
                foreach (var method in type.GetMethods())
                {
                    if (!method.GetCustomAttributes(typeof(TestAttribute), false).Any())
                    {
                        continue;
                    }

                    var id = "T" + method.GetHashCode().ToString("x4");
                    Console.WriteLine($"Test : info {id}: {type} {method}");
                    try
                    {
                        method.Invoke(instance, new object[0]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Test : error T0002: {ex}");
                        return 1;
                    }
                }
            }
            return 0;
        }
    }
}
