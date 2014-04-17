using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace X13 {
  public class Engine {
    static void Main(string[] args) {
      string path=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      Directory.SetCurrentDirectory(path);
      if(!Directory.Exists("../log")) {
        Directory.CreateDirectory("../log");
      }
      Log.Write+=new Action<LogLevel, DateTime, string>(Log_Write);

      Console.ForegroundColor=ConsoleColor.Green;
      Console.WriteLine("Engine running; press Enter to Exit");

      Console.Read();
      Console.ForegroundColor=ConsoleColor.Gray;
    }

    private static void Log_Write(LogLevel ll, DateTime dt, string msg) {
      switch(ll) {
      case LogLevel.Debug:
        Console.ForegroundColor=ConsoleColor.Gray;
        Console.WriteLine(dt.ToString("HH:mm:ss.ff")+"[D] "+msg);
        break;
      case LogLevel.Info:
        Console.ForegroundColor=ConsoleColor.White;
        Console.WriteLine(dt.ToString("HH:mm:ss.ff")+"[I] "+msg);
        break;
      case LogLevel.Warning:
        Console.ForegroundColor=ConsoleColor.Yellow;
        Console.WriteLine(dt.ToString("HH:mm:ss.ff")+"[W] "+msg);
        break;
      case LogLevel.Error:
        Console.ForegroundColor=ConsoleColor.Red;
        Console.WriteLine(dt.ToString("HH:mm:ss.ff")+"[E] "+msg);
        break;
      }
    }
  }
}
