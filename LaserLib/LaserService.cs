using LaserLib.Models;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace LaserLib
{
    public class LaserService
    {
        public string Address { get; set; }
        public LaserStatus Status { get; set; }
        public event Action<LaserStatus> StatusChanged;
        private string _baseUrl => $"http://{Address}";

        private static Dictionary<int, string> statusDescriptions = new Dictionary<int, string>
        {
            { 0, "Перепол. буф. ком." },
            { 1, "Перегрев" },
            { 2, "Излучение ВКЛ" },
            { 3, "Обратное отражение" },
            { 4, "Аналог.управление" },
            { 5, "Резерв" },
            { 6, "Резерв" },
            { 7, "Резерв" },
            { 8, "Пилот. лазер включен" },
            { 9, "Длит. импульса мала" },
            { 10, "Непрерывный режим (CW)" },
            { 11, "ИП (ВЫКЛ/ВКЛ)" },
            { 12, "Модуляция" },
            { 13, "Резерв" },
            { 14, "Резерв" },
            { 15, "Резерв" },
            { 16, "Режим Gate" },
            { 17, "Резерв" },
            { 18, "HW управл. излучением" },
            { 19, "Неисправность ИП" },
            { 20, "Резерв" },
            { 21, "Резерв" },
            { 22, "Резерв" },
            { 23, "Резерв" },
            { 24, "Низкая температура" },
            { 25, "Ошибка ИП" },
            { 26, "Резерв" },
            { 27, "HW управл. пилот. лазером" },
            { 28, "Предупр. пилот. лазера" },
            { 29, "Критическая ошибка" },
            { 30, "Обрыв волокна" },
            { 31, "Резерв" }
        };

        public LaserService(string address)
        {
            Address = address;
            Status = new LaserStatus();
            Status.IsSuccess = false;
        }

        public void SetAddress(string address)
        {
            Address = address;
        }

        public string GetByteName(int byteIndex)
        {
            if (byteIndex > 31 || byteIndex < 0) return "null";
            return statusDescriptions[byteIndex];
        }

        public async Task<LaserStatus> GetStatus()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"{_baseUrl}/monitoring");
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        LaserStatus status = JsonConvert.DeserializeObject<LaserStatus>(jsonResponse);
                        if (status != null)
                        {
                            Status = status;
                            Status.IsSuccess = true;
                            StatusChanged?.Invoke(Status);
                        }
                        else
                        {
                            Status.IsSuccess = false;
                            return Status;
                        }


                        int statusBits = Status.STA;

                        Console.WriteLine($"Статус лазера (STA): 0x{statusBits.ToString("X")} (в шестнадцатеричном формате)");

                        //// Для удобства выводим состояние каждого бита (как на вашем изображении)
                        //Console.WriteLine("\nДетализация статуса:");
                        //Console.WriteLine("Бит | Состояние");
                        //Console.WriteLine("------------------");

                        for (int i = 0; i < 32; i++)
                        {
                            bool state = (statusBits & (1 << i)) != 0;
                            Status.STAStates[i] = state;
                            //Console.WriteLine($"{GetByteName(i)} | {state}");
                        }

                        // Также можно вывести другие интересующие вас параметры
                        //Console.WriteLine($"\nРежим работы: {Status.RMT}");
                        //Console.WriteLine($"Средняя мощность: {Status.ROP} W");
                        //Console.WriteLine($"Температура корпуса: {Status.RCT} °C");
                        //Console.WriteLine($"Температура платы: {Status.RBT} °C");
                        //Console.WriteLine($"ID устройства: {Status.RID}");
                        //Console.WriteLine($"Версия прошивки: {Status.RFV}");
                        return Status;
                    }
                    else
                    {
                        Status.IsSuccess = false;
                        return Status;
                    }
                }
                catch (Exception ex)
                {
                    Status.IsSuccess = false;
                    StatusChanged?.Invoke(Status);
                }


            }

            Status.IsSuccess = false;
            return Status;
        }

        public async Task<bool> SendCommandAsync(string commandName)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Создаем тело запроса как form-encoded
                    var formData = new Dictionary<string, string>
                    {
                        { "ver", "1" },
                        { "cmd", commandName }
                    };

                    var content = new FormUrlEncodedContent(formData);

                    // Добавляем заголовки, которые отправляет веб-интерфейс
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")
                    {
                        CharSet = "UTF-8"
                    };

                    // Создаем запрос
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/setcmd");
                    request.Content = content;

                    // Добавляем необходимые заголовки
                    request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    request.Headers.Add("Origin", _baseUrl);
                    request.Headers.Add("Referer", $"{_baseUrl}/");

                    // Отправляем запрос
                    var response = await client.SendAsync(request);
                    Console.WriteLine(await response.Content.ReadAsStringAsync());

                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки команды {commandName}: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
