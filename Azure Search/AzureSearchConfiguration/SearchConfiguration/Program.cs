using System;

namespace SearchConfiguration
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new Service();

            var serviceWorks = service.CreateService();

            if (serviceWorks)
                Console.WriteLine("Service is ready to use");
            else
                Console.WriteLine("Service isn't ready to use");

            Console.WriteLine("Click, to continue");
            Console.ReadKey();
        }
    }
}
