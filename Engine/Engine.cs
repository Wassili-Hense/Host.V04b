using System;
using System.IO;
using System.Reflection;
using System.Text;
using X13.lib;

namespace X13 {
  public class Engine {
    static void Main(string[] args) {
      string path=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      Directory.SetCurrentDirectory(path);
      Log.Write+=new Action<LogLevel, DateTime, string>(Log_Write);

      Console.ForegroundColor=ConsoleColor.Green;
      Console.WriteLine("Engine running; press Enter to Exit");

      Console.Read();
      Console.ForegroundColor=ConsoleColor.Gray;
    }

    private static void Log_Write(LogLevel ll, DateTime dt, string msg) {
      string dts=dt.ToString("HH:mm:ss.ff");
      switch(ll) {
      case LogLevel.Debug:
        Console.ForegroundColor=ConsoleColor.Gray;
        Console.WriteLine(dts+"[D] "+msg);
        break;
      case LogLevel.Info:
        Console.ForegroundColor=ConsoleColor.White;
        Console.WriteLine(dts+"[I] "+msg);
        break;
      case LogLevel.Warning:
        Console.ForegroundColor=ConsoleColor.Yellow;
        Console.WriteLine(dts+"[W] "+msg);
        break;
      case LogLevel.Error:
        Console.ForegroundColor=ConsoleColor.Red;
        Console.WriteLine(dts+"[E] "+msg);
        break;
      }
    }
  }
}
