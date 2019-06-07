using System;

namespace SearchExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new Service();
            service.GetResults("lorem");
            Console.ReadKey();
        }
    }
}
