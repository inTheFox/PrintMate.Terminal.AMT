using System;
using System.Threading.Tasks;
using PrintMate.Terminal.Hans;

namespace TestHansClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Hans Scanner Client Test ===\n");

            // Создаем клиент
            var client = new HansScannerClient("172.18.34.227", "test1");

            // Запускаем
            Console.WriteLine("Starting client...");
            bool started = await client.StartAsync();

            if (!started)
            {
                Console.WriteLine("❌ Failed to start client");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("✅ Client started successfully!\n");

            // Ждём немного
            await Task.Delay(2000);

            // Проверяем статус
            Console.WriteLine("Getting status...");
            var status = await client.GetStatusAsync();

            if (status != null)
            {
                Console.WriteLine($"✅ Status received:");
                Console.WriteLine($"   IsConnected: {status.IsConnected}");
                Console.WriteLine($"   IsMarking: {status.IsMarking}");
                Console.WriteLine($"   BoardIndex: {status.BoardIndex}");
                Console.WriteLine($"   WorkStatus: {status.WorkStatus}");
            }
            else
            {
                Console.WriteLine("❌ Failed to get status");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();

            // Закрываем
            client.Dispose();
            Console.WriteLine("Client disposed");
        }
    }
}
