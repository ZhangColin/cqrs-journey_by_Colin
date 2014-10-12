using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRoleCommandProcessor {
    class Program {
        static void Main(string[] args) {
            DatabaseSetup.Initialize();

            using(var processor = new ConferenceProcessor()) {
                processor.Start();

                Console.WriteLine("Host started");
                Console.WriteLine("Press enter to finish");

                Console.ReadLine();

                processor.Stop();
            }
        }
    }
}
