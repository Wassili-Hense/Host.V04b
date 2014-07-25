using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace X13.plugin {
  internal class VW {

  }
  public class PiConst {
    private static Dictionary<Type, Delegate> _getters;
    static PiConst() {
      _getters=new Dictionary<Type, Delegate>();
      _getters.Add(typeof(long), (Delegate)new Func<Topic.VT, object, Topic.PriDT, long>(GetLong));
      _getters.Add(typeof(double), (Delegate)new Func<Topic.VT, object, Topic.PriDT, double>(GetDouble));
    }
    private static long GetLong(Topic.VT vt, object o, Topic.PriDT dt) {
      long ret;
      switch(vt) {
      case Topic.VT.Bool:
      case Topic.VT.Integer:
      case Topic.VT.DateTime:
        ret=dt.l;
        break;
      case Topic.VT.Float:
        ret=(long)Math.Truncate(dt.d);
        break;
      case Topic.VT.String:
        if(!long.TryParse((string)o, out ret)) {
          double tmp;
          if(double.TryParse((string)o, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tmp)) {
            ret=(long)Math.Truncate(tmp);
          } else {
            ret=0;
          }
        }
        break;
      case Topic.VT.Ref: {
          Topic r=o as Topic;
          ret=r==null?0:r.AsLong;
        }
        break;
      default:
        ret=0;
        break;
      }
      return ret;

    }
    private static double GetDouble(Topic.VT vt, object o, Topic.PriDT dt) {
      double ret;
      switch(vt) {
      case Topic.VT.Bool:
      case Topic.VT.Integer:
        ret=dt.l;
        break;
      case Topic.VT.Float:
        ret=dt.d;
        break;
      case Topic.VT.DateTime:
        ret=dt.dt.ToOADate();
        break;
      case Topic.VT.String:
        if(!double.TryParse((string)o, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ret)) {
          ret=0;
        }
        break;
      case Topic.VT.Ref: {
          Topic r=o as Topic;
          ret=r==null?0:r.AsDouble;
        }
        break;
      default:
        ret=0;
        break;
      }
      return ret;

    }
    internal static T GetVal<T>(Topic.VT vt, object o, Topic.PriDT dt) {
      if(typeof(T).IsClass) {
        if(o==null) {
          switch(vt) {
          case Topic.VT.Bool:
            o=dt.l!=0;
            break;
          case Topic.VT.Integer:
            o=dt.l;
            break;
          case Topic.VT.DateTime:
            o=dt.dt;
            break;
          case Topic.VT.Float:
            o=dt.d;
            break;
          case Topic.VT.Ref: {
              Topic r=o as Topic;
              return r==null?default(T):(T)r.AsObject;
            }
          }
        }
        return (T)o;
      }
      Delegate d;
      if(_getters.TryGetValue(typeof(T), out d)) {
        Func<Topic.VT, object, Topic.PriDT, T> f;
        if((f=d as Func<Topic.VT, object, Topic.PriDT, T>)!=null) {
          return f(vt, o, dt);
        }
      }
      return default(T);
    }

    private Topic.PriDT _dt;
    private object _o;
    private Topic.VT _vt;
    public PiConst(long l) {
      _dt=new Topic.PriDT() { l=l };
      _o=null;
      _vt=Topic.VT.Integer;
    }
    public void Set(long v) {
      _dt.l=v;
      _o=null;
      _vt=Topic.VT.Integer;
    }
    public T As<T>() {
      return GetVal<T>(_vt, _o, _dt);
    }
  }
}