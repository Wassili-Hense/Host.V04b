using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using X13.lib;
using X13.plugin;

namespace X13 {
  public class Engine {
    static void Main(string[] args) {
      string path=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      Directory.SetCurrentDirectory(path);

      PersistentStorage ps=new PersistentStorage();
      ps.Init();

      Timer tick=new Timer(TickPr, null, 0, 100);
      ps.Start();

      Console.ForegroundColor=ConsoleColor.Green;
      Console.WriteLine("Engine running; press Enter to Exit");

      Console.Read();
      Console.ForegroundColor=ConsoleColor.Gray;
      ps.Stop();
      tick.Change(Timeout.Infinite, Timeout.Infinite);
    }
    private static void TickPr(object o) {
      try {
        PLC.instance.Tick();
      }
      catch(Exception ex) {
        Log.Warning("PLC.instance.Tick() - "+ex.ToString());
      }
    }
  }
}
