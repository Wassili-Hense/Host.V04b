using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace X13 {
  public sealed class Topic: IComparable<Topic> {
    public static readonly Topic root;
    private static Queue<TopicCmd> _prIp;
    private static Queue<TopicCmd> _prTp;
    private static SortedList<string, TopicCmd> _prOp;
    private static int _busyFlag;

    static Topic() {
      _prIp=new Queue<TopicCmd>();
      _prTp=new Queue<TopicCmd>();
      _prOp=new SortedList<string, TopicCmd>();
      root=new Topic(null, "/");
      _busyFlag=1;
    }
    public static void Process() {
      if(Interlocked.CompareExchange(ref _busyFlag, 2, 1)!=1) {
        return;
      }
      lock(root) {
        _prTp=Interlocked.Exchange(ref _prIp, _prTp);
      }
      TopicCmd c, c1;
      while(_prTp.Count>0){
        c=_prTp.Dequeue();
        if(_prTp.Count==64){
          _prTp.TrimExcess();
        }
        if(c!=null && (!_prOp.ContainsKey(c.src.path) || (c1=_prOp[c.src.path])==null ||  ((int)c.art<=(int)c1.art))) {
          _prOp[c.src.path]=c;
        }
      }
      
      foreach(var cmd in _prOp.Values) {
        cmd.src.SetValue(cmd);
        //TODO: save for undo/redo
        /*IHistory h;
        if(cmd.prim!=null && cmd.prim._vt==VT.Object && (h=cmd.prim._o as IHistory)!=null) {
          h.Add(cmd);
        }*/
      }

      foreach(var cmd in _prOp.Values) {
        if(cmd.art!=TopicCmd.Art.set) {
          if(cmd.src._o!=null && cmd.src._disposed<1) {
            ITenant tt;
            Topic r;
            if(cmd.src._vt==VT.Ref && (r=cmd.src._o as Topic)!=null) {
              //r.Subscribe("", RefChanged);
            } else if(cmd.src._vt==VT.Object &&  (tt=cmd.src._o as ITenant)!=null) {
              tt.owner=cmd.src;
            }
          }

          cmd.src.Publish(cmd, null);
          if(cmd.src._disposed==1) {
            if(cmd.src.parent!=null) {
              cmd.src.parent._children.Remove(cmd.src.name);
            }
            cmd.src._disposed=2;
          }
        }
      }
      _prOp.Clear();
      _busyFlag=1;
    }

    #region variables
    private SortedList<string, Topic> _children;
    private Topic _parent;
    private string _name;
    private string _path;
    private int _disposed;

    private VT _vt;
    private PriDT _dt;
    private object _o;
    #endregion variables

    private Topic(Topic parent, string name) {
      _name=name;
      _parent=parent;
      _vt=VT.Undefined;
      _dt=new PriDT();
      _o=null;

      if(parent==null) {
        _path="/";
      } else if(parent==root) {
        _path="/"+name;
      } else {
        _path=parent.path+"/"+name;
      }
      _disposed=0;
    }

    public Topic parent {
      get { return _parent; }
    }
    public string name {
      get { return _name; }
    }
    public string path {
      get { return _path; }
    }
    public Bill all { get { return new Bill(this, true); } }
    public Bill children { get { return new Bill(this, false); } }
    public bool disposed { get { return _disposed>0; } }

    /// <summary> Get item from tree</summary>
    /// <param name="path">relative or absolute path</param>
    /// <param name="create">true - create, false - check</param>
    /// <returns>item or null</returns>
    public Topic Get(string path, bool create=true) {
      if(string.IsNullOrEmpty(path)) {
        return this;
      }
      Topic home=this, next;
      if(path[0]==Bill.delmiter) {
        if(path.StartsWith(this._path)) {
          path=path.Substring(this._path.Length);
        } else {
          home=Topic.root;
        }
      }
      var pt=path.Split(Bill.delmiterArr, StringSplitOptions.RemoveEmptyEntries);
      for(int i=0; i<pt.Length; i++) {
        if(pt[i]==Bill.maskAll || pt[i]==Bill.maskChildren) {
          throw new ArgumentException(string.Format("{0}[{1}] dont allow wildcard", this.path, path));
        }
        next=null;
        if(home._children==null) {
          home._children=new SortedList<string, Topic>();
        }
        if(home._children.TryGetValue(pt[i], out next)) {
          home=next;
        } else if(create==true) {
          home._children.TryGetValue(pt[i], out next);
        }
        if(next==null) {
          if(create!=false) {
            next=new Topic(home, pt[i]);
            home._children.Add(pt[i], next);
          } else {
            return null;
          }
        }
        home=next;
      }
      return home;
    }
    public bool Exist(string path) {
      return Get(path, false)!=null;
    }
    public bool Exist(string path, out Topic topic) {
      return (topic=Get(path, false))!=null;
    }
    public void Remove() {
      foreach(var t in this.all) {
        lock(root) {
          _prIp.Enqueue(new TopicCmd(this, TopicCmd.Art.remove));
        }
      }
    }
    public void Move(Topic nParent, string nName) {
      throw new NotImplementedException();
    }
    public override string ToString() {
      return _path;
    }
    public int CompareTo(Topic other) {
      if(other==null) {
        return 1;
      }
      return string.Compare(this._path, other._path);
    }

    public void Set(bool val, Topic prim=null) {
      Topic r;
      if(_vt==VT.Ref && (r=_o as Topic)!=null) {
        r.Set(val, prim);
      } else {
        var c=new TopicCmd(this, val, prim);
        lock(root) {
          _prIp.Enqueue(c);
        }
      }
    }
    public void Set(long val, Topic prim=null) {
      Topic r;
      if(_vt==VT.Ref && (r=_o as Topic)!=null) {
        r.Set(val, prim);
      } else {
        var c=new TopicCmd(this, val, prim);
        lock(root) {
          _prIp.Enqueue(c);
        }
      }
    }
    public void Set(double val, Topic prim=null) {
      Topic r;
      if(_vt==VT.Ref && (r=_o as Topic)!=null) {
        r.Set(val, prim);
      } else {
        var c=new TopicCmd(this, val, prim);
        lock(root) {
          _prIp.Enqueue(c);
        }
      }
    }
    public void Set(DateTime val, Topic prim=null) {
      Topic r;
      if(_vt==VT.Ref && (r=_o as Topic)!=null) {
        r.Set(val, prim);
      } else {
        var c=new TopicCmd(this, val, prim);
        lock(root) {
          _prIp.Enqueue(c);
        }
      }
    }
    public void Set(object val, Topic prim=null) {
      Topic r;
      if(val!=null && _vt==VT.Ref && (r=_o as Topic)!=null) {
        r.Set(val, prim);
      } else {
        TopicCmd c;
        switch(Type.GetTypeCode(val==null?null:val.GetType())) {
        case TypeCode.Boolean:
          c=new TopicCmd(this, (bool)val, prim);
          break;
        case TypeCode.Byte:
        case TypeCode.SByte:
        case TypeCode.Int16:
        case TypeCode.Int32:
        case TypeCode.Int64:
        case TypeCode.UInt16:
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          c=new TopicCmd(this, Convert.ToInt64(val), prim);
          break;
        case TypeCode.Single:
        case TypeCode.Double:
        case TypeCode.Decimal:
          c=new TopicCmd(this, Convert.ToDouble(val), prim);
          break;
        case TypeCode.DateTime:
          c=new TopicCmd(this, (DateTime)val, prim);
          break;
        case TypeCode.Empty:
        default:
          c=new TopicCmd(this, val, prim);
          break;
        }
        lock(root) {
          _prIp.Enqueue(c);
        }
      }
    }
    public bool AsBool {
      get {
        bool ret;
        switch(_vt) {
        case VT.Bool:
        case VT.Integer:
        case VT.DateTime:
          ret=_dt.l!=0;
          break;
        case VT.Float:
          ret=_dt.d!=0;
          break;
        case VT.String:
          if(!bool.TryParse((string)_o, out ret)) {
            ret=false;
          }
          break;
        case VT.Ref: {
            Topic r=_o as Topic;
            ret=(r!=null) && r.AsBool;
          }
          break;
        default:
          ret=false;
          break;
        }
        return ret;
      }
    }
    public long AsLong {
      get {
        long ret;
        switch(_vt) {
        case VT.Bool:
        case VT.Integer:
        case VT.DateTime:
          ret=_dt.l;
          break;
        case VT.Float:
          ret=(long)Math.Truncate(_dt.d);
          break;
        case VT.String:
          if(!long.TryParse((string)_o, out ret)) {
            double tmp;
            if(double.TryParse((string)_o, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tmp)) {
              ret=(long)Math.Truncate(tmp);
            } else {
              ret=0;
            }
          }
          break;
        case VT.Ref: {
            Topic r=_o as Topic;
            ret=r==null?0:r.AsLong;
          }
          break;
        default:
          ret=0;
          break;
        }
        return ret;
      }
    }
    public double AsDouble {
      get {
        double ret;
        switch(_vt) {
        case VT.Bool:
        case VT.Integer:
          ret=_dt.l;
          break;
        case VT.Float:
          ret=_dt.d;
          break;
        case VT.DateTime:
          ret=_dt.dt.ToOADate();
          break;
        case VT.String:
          if(!double.TryParse((string)_o, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ret)) {
            ret=0;
          }
          break;
        case VT.Ref: {
            Topic r=_o as Topic;
            ret=r==null?0:r.AsDouble;
          }
          break;
        default:
          ret=0;
          break;
        }
        return ret;
      }
    }
    public DateTime AsDateTime {
      get {
        DateTime ret;
        switch(_vt) {
        case VT.DateTime:
          ret=_dt.dt;
          break;
        case VT.Bool:
        case VT.Integer:
          ret=new DateTime(_dt.l);
          break;
        case VT.Float:
          ret=DateTime.FromOADate(_dt.d);
          break;
        case VT.String:
          DateTime.TryParse((string)_o, out ret);
          break;
        case VT.Ref: {
            Topic r=_o as Topic;
            ret=r==null?DateTime.MinValue:r.AsDateTime;
          }
          break;
        default:
          ret=DateTime.MinValue;
          break;
        }
        return ret;
      }
    }
    public string AsString {
      get {
        string ret;
        switch(_vt) {
        case VT.Bool:
          ret=_dt.l==0?bool.FalseString:bool.TrueString;
          break;
        case VT.Integer:
          ret=_dt.l.ToString();
          break;
        case VT.DateTime:
          ret=_dt.dt.ToString();
          break;
        case VT.Float:
          ret=_dt.d.ToString();
          break;
        case VT.String:
          ret=(string)_o;
          break;
        case VT.Ref: {
            Topic r=_o as Topic;
            ret=r==null?string.Empty:r.AsString;
          }
          break;
        case VT.Object:
          ret=_o==null?string.Empty:_o.ToString();
          break;
        default:
          ret=string.Empty;
          break;
        }
        return ret;
      }
    }
    public object AsObject {
      get {
        if(_o==null) {
          switch(_vt) {
          case VT.Bool:
            _o=_dt.l==0;
            break;
          case VT.Integer:
            _o=_dt.l;
            break;
          case VT.DateTime:
            _o=_dt.dt;
            break;
          case VT.Float:
            _o=_dt.d;
            break;
          case VT.Ref: {
              Topic r=_o as Topic;
              return r==null?null:r.AsObject;
            }
          }
        }
        return _o;
      }
    }
    public T As<T>() where T: class {
      return _vt==VT.Object?(_o as T):default(T);
    }
    public Topic AsRef { get { return _vt==VT.Ref?(_o as Topic):null; } }

    private void SetValue(TopicCmd v) {
      if(v.art!=TopicCmd.Art.set 
        || _vt!=v.vt 
        || ((_vt==VT.Object || _vt==VT.Ref || _vt==VT.String) && !object.Equals(_o, v.o))
        || ((_vt==VT.Bool || _vt==VT.Integer) && _dt.l!=v.dt.l)
        || (_vt==VT.Float && _dt.d!=v.dt.d)
        || (_vt==VT.DateTime && _dt.dt!=v.dt.dt)) {
        if(_o!=null) {
          Topic r;
          ITenant t;
          if(_vt==VT.Ref && (r=_o as Topic)!=null) {
            //r.Unsubscribe("", RefChanged);
          } else if(_vt==VT.Object && (t=_o as ITenant)!=null) {
            t.owner=null;
          }
        }
        //TODO: for undo/redo
        /*
         v.oldvt=_vt;
         
         */
        _vt=v.vt;
        _dt=v.dt;
        _o=v.o;
        if(v.art==TopicCmd.Art.set) {
          v.art=TopicCmd.Art.changed;
        }
      }
      if(v.art==TopicCmd.Art.remove) {
        _disposed=1;
      }
    }
    private void Publish(TopicCmd cmd, Action<Topic, TopicCmd> func) {
      //if(func!=null) {
      //  try {
      //    func(this, cmd);
      //  }
      //  catch(Exception ex) {
      //    Log.Warning("{0}.{1}({2}, ) - {3}", func.Method.DeclaringType.Name, func.Method.Name, this.path, ex.ToString());
      //  }
      //} else {

      //}
    }
    private void RefChanged(Topic t, TopicCmd c) {
      throw new NotImplementedException();
    }

    #region nested types
    [Flags]
    public enum MaskType {
      None=0,
      value=1,
      children=2,
      all=4,
      changed=8,
      remove=16,
      saved=32,
    }

    public class Bill: IEnumerable<Topic> {
      public const char delmiter='/';
      public const string maskAll="#";
      public const string maskChildren="+";
      public static readonly char[] delmiterArr=new char[] { delmiter };
      public static readonly string[] allArr=new string[] { maskAll };
      public static readonly string[] childrenArr=new string[] { maskChildren };

      private Topic _home;
      private MaskType _mask;

      public Bill(Topic home, bool deep) {
        _home=home;
        _mask=deep?MaskType.all:MaskType.children;
      }
      public IEnumerator<Topic> GetEnumerator() {
        if(_mask==MaskType.children) {
          if(_home._children==null) {
            _home._children=new SortedList<string, Topic>();
          }
          foreach(var t in _home._children.Values) {
            yield return t;
          }
        } else if(_mask==MaskType.all) {
          var hist=new Stack<Topic>();
          Topic[] ch;
          Topic cur;
          hist.Push(_home);
          do {
            cur=hist.Pop();
            yield return cur;
            if(cur._children==null) {
              cur._children=new SortedList<string, Topic>();
            }
            ch=cur._children.Values.ToArray();
            for(int i=ch.Length-1; i>=0; i--) {
              hist.Push(ch[i]);
            }
          } while(hist.Any());
        } else {
          yield return _home;
        }
      }
      //public event Action<Topic, TopicCmd> changed {
      //  add {
      //    _home.Subscribe(_mask, value);
      //    foreach(var t in this) {
      //      t.Publish(value);
      //    }
      //  }
      //  remove {
      //    _home.Unsubscribe(_mask, value);
      //  }
      //}
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
    }

    internal enum VT {
      Undefined = 0,
      Null,
      Integer,
      DateTime,
      Bool,
      String,
      Float,
      Object,
      //Array,
      //Record,
      //Binary,
      Ref
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct PriDT {
      [FieldOffset(0)]
      public Int64 l;
      [FieldOffset(0)]
      public double d;
      [FieldOffset(0)]
      public DateTime dt;
    }
    #endregion nested types
  }

  public class TopicCmd {
    internal Topic.VT vt;
    internal Topic.PriDT dt;
    internal object o;

    public readonly Topic src;
    public readonly Topic prim;
    public Art art { get; internal set; }

    internal TopicCmd(Topic src, Art art) {
      this.src=src;
      this.art=art;
      vt=Topic.VT.Undefined;
      o=null;
    }
    public TopicCmd(Topic src, bool val, Topic prim) {
      this.src=src;
      vt=Topic.VT.Bool;
      dt.l=val?1:0;
      o=null;
      this.prim=prim;
      art=Art.set;
    }
    internal TopicCmd(Topic src, long val, Topic prim) {
      this.src=src;
      vt=Topic.VT.Integer;
      dt.l=val;
      o=null;
      this.prim=prim;
      art=Art.set;
    }
    internal TopicCmd(Topic src, double val, Topic prim) {
      this.src=src;
      vt=Topic.VT.Float;
      dt.d=val;
      o=null;
      this.prim=prim;
      art=Art.set;
    }
    internal TopicCmd(Topic src, DateTime val, Topic prim) {
      this.src=src;
      vt=Topic.VT.DateTime;
      dt.dt=val;
      o=null;
      this.prim=prim;
      art=Art.set;
    }
    internal TopicCmd(Topic src, object val, Topic prim) {
      this.src=src;
      if(val==null) {
        vt=Topic.VT.Null;
      } else if(val is Topic) {
        vt=Topic.VT.Ref;
      } else if(val is string) {
        vt=Topic.VT.String;
      } else {
        vt=Topic.VT.Object;
      }
      o=val;
      this.prim=prim;
      art=Art.set;
    }


    public enum Art {
      create=3,
      set=2,
      changed=4,
      suback=5,
      remove=1
    }
  }

  public interface ITenant {
    Topic owner { get; set; }
  }
}
