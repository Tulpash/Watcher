using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;

//IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
//ShowWindow(h, 0);

//Программа
HttpClient client = new HttpClient();
List<Process> processes = new List<Process>();
Timer timer = new Timer();
if (CheckStart()) Run();

//Проводит настройку и запускает таймер
void Run()
{
    //Настройки клиента
    client.BaseAddress = new Uri("http://192.168.1.142:8088/");
    //client.BaseAddress = new Uri("http://localhost:5044/");
    client.DefaultRequestHeaders.Accept.Clear();

    //Настройки таймера для счиьывания
    timer.Interval = 2000;
    timer.AutoReset = true;
    timer.Elapsed += new ElapsedEventHandler(GetProcesses);
    timer.Start();

    //Что бы приложение не закрывалось
    Console.ReadLine();
}

//Считывает процессы и добавляет новые 
void GetProcesses(object sender, ElapsedEventArgs e)
{
    timer.Stop();
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("Find new");
    Console.ResetColor();
    var tmp = Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero && !String.IsNullOrEmpty(p.MainWindowTitle)).ToList();
    var except = new List<Process>();
    foreach (var proc in tmp)
    {
        if (processes.FindIndex(p => p.ProcessName == proc.ProcessName) == -1) except.Add(proc);
    }
    foreach (var proc in except)
    {
        string t = "";
        try
        {
            proc.EnableRaisingEvents = true;
            proc.Exited += new EventHandler(ProcExited);
        }
        catch (Exception exc)
        {
            t = exc.Message;
            Console.ForegroundColor = ConsoleColor.Red;
        }
        Console.WriteLine($"New: {proc.ProcessName} {t}");
        Console.ResetColor();
    }
    processes.AddRange(except);
    timer.Start();
}

//Происходит при выходе процесса
void ProcExited(object sender, EventArgs e)
{
    Process proc = (Process)sender;
    processes.Remove(proc);
    string endTime = "";
    HttpStatusCode code = HttpStatusCode.PartialContent;
    try
    {
        code = SendData(proc.ProcessName, proc.StartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"), proc.ExitTime.ToString("yyyy-MM-ddTHH:mm:ssZ"), proc.MainWindowTitle);
        endTime = proc.ExitTime.ToString();
        if ((int)code >= 200 && (int)code <= 299)
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else Console.ForegroundColor = ConsoleColor.Red;
    }
    catch (Exception exc)
    {
        endTime = exc.Message;
        Console.ForegroundColor = ConsoleColor.Red;
    }
    Console.WriteLine($"Dead: {proc.ProcessName} ({proc.StartTime} - {endTime}) {code} ({proc.MainWindowTitle})");
    Console.ResetColor();
}

//Отправка данных на сервер
HttpStatusCode SendData(string procName, string start, string stop, string windowTitle)
{
    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "put");
    string data = $"{{\"computer_name\": \"{Environment.MachineName}\", \"process_name\": \"{procName}\", \"main_window_title\": \"{windowTitle}\", \"time_start\": \"{start}\", \"time_stop\": \"{stop}\"}}";
    request.Content = new StringContent(data, Encoding.UTF8, "application/json");
    var response = client.SendAsync(request);
    return response.Result.StatusCode;
}

//
bool CheckStart()
{
    var processes = Process.GetProcessesByName("Watcher");
    if (processes.Length > 1) return false;
    return true;
}

//
//[DllImport("user32.dll")]
//static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);