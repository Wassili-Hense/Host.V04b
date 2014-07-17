using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using X13.lib;

namespace X13 {
  public sealed class Topic : IComparable<Topic> {
    public static readonly Topic root;
    private static Queue<TopicCmd> _prIp;
    private static Queue<TopicCmd> _prTp;
    private static SortedList<string, TopicCmd> _prOp;
    private static int _busyFlag;
    private static JsonSerializer _jser;

    static Topic() {
      _prIp=new Queue<TopicCmd>();
      _prTp=new Queue<TopicCmd>();
      _prOp=new SortedList<string, TopicCmd>();
      root=new Topic(null, "/");
      _jser=JsonSerializer.Create(new JsonSerializerSettings() { DateFormatHandling=DateFormatHandling.IsoDateFormat, DateParseHandling=DateParseHandling.DateTime, MissingMemberHandling=MissingMemberHandling.Ignore, DefaultValueHandling=DefaultValueHandling.Include, NullValueHandling=NullValueHandling.Include, TypeNameHandling=TypeNameHandling.Objects, PreserveReferencesHandling=PreserveReferencesHandling.None });
      _jser.ReferenceResolver=new RefResolver();
      _busyFlag=1;
    }
    public static void Process() {
      if(Interlocked.CompareExchange(ref _busyFlag, 2, 1)!=1) {
        return;
      }
      lock(root) {
        _prTp=Interlocked.Exchange(ref _prIp, _prTp);
      }
      TopicCmd c;
      Action<Topic, TopicCmd> func;
      Topic t;
      while(_prTp.Count>0) {
        c=_prTp.Dequeue();
        if(_prTp.Count==64) {
          _prTp.TrimExcess();
        }
        if(c==null || c.src==null) {
          continue;
        }
        switch(c.art) {
        case TopicCmd.Art.create:
          if((t=c.src.parent)!=null) {
            if(t._subRecords!=null) {
              foreach(var sr in t._subRecords.Where(z => z.ma!=null && z.ma.Length==1 && z.ma[0]==Bill.maskChildren)) {
                c.src.Subscribe(new SubRec() { mask=sr.mask, ma=new string[0], f=sr.f });
              }
            }
            while(t!=null) {
              if(t._subRecords!=null) {
                foreach(var sr in t._subRecords.Where(z => z.ma!=null && z.ma.Length==1 && z.ma[0]==Bill.maskAll)) {
                  c.src.Subscribe(new SubRec() { mask=sr.mask, ma=new string[0], f=sr.f });
                }
              }
              t=t.parent;
            }
          }
          goto case TopicCmd.Art.set;

        case TopicCmd.Art.subscribe:
        case TopicCmd.Art.unsubscribe:
          if((func=c.o as Action<Topic, TopicCmd>)!=null) {
            if(c.dt.l==0) {
              if(c.art==TopicCmd.Art.subscribe) {
                c.src.Subscribe(new SubRec() { mask=c.src.path, ma=Bill.curArr, f=func });
              } else {
                c.src.Unsubscribe(c.src.path, func);
              }
              goto case TopicCmd.Art.set;
            } else {
              SubRec sr;
              Bill b;
              if(c.dt.l==1) {
                sr=new SubRec() { mask=c.prim.path+"/+", ma=Bill.curArr, f=func };
                if(c.art==TopicCmd.Art.subscribe) {
                  c.src.Subscribe(new SubRec() { mask=sr.mask, ma=Bill.childrenArr, f=func });
                } else {
                  c.src.Unsubscribe(sr.mask, func);
                }
                b=c.src.children;
              } else {
                sr=new SubRec() { mask=c.prim.path+"/#", ma=Bill.allArr, f=func };
                b=c.src.all;
              }
              foreach(Topic tmp in b) {
                if(c.art==TopicCmd.Art.subscribe) {
                  tmp.Subscribe(sr);
                } else {
                  c.src.Unsubscribe(sr.mask, func);
                }
                AssignCmd(tmp, c.art, c.src);
              }
            }
          }
          break;

        case TopicCmd.Art.remove:
          foreach(Topic tmp in c.src.all) {
            AssignCmd(tmp, c.art, c.prim);
          }
          break;
        case TopicCmd.Art.move:
          if((t=c.o as Topic)!=null) {
            string oPath=c.src.path;
            string nPath=t.path;
            t._children=c.src._children;
            c.src._children=null;
            t._vt=c.src._vt;
            c.src._vt=VT.Undefined;
            t._dt=c.src._dt;
            t._o=c.src._o;
            c.src._o=null;
            if(c.src._subRecords!=null) {
              foreach(var sr in c.src._subRecords) {
                if(sr.mask.StartsWith(oPath)) {
                  t.Subscribe(new SubRec() { mask=sr.mask.Replace(oPath, nPath), ma=sr.ma, f=sr.f });
                }
              }
            }
            foreach(var t1 in t.children) {
              t1._parent=t;
            }
            foreach(var t1 in t.all) {
              if(t1._subRecords!=null) {
                for(int i=t1._subRecords.Count-1; i>=0; i--) {
                  if(t1._subRecords[i].mask.StartsWith(oPath)) {
                    t1._subRecords[i]=new SubRec() { mask=t1._subRecords[i].mask.Replace(oPath, nPath), ma=t1._subRecords[i].ma, f=t1._subRecords[i].f };
                  } else if(!t1._subRecords[i].mask.StartsWith(nPath)) {
                    t1._subRecords.RemoveAt(i);
                  }
                }
              }
              t1._path=t1.parent==root?string.Concat("/", t1.name):string.Concat(t1.parent.path, "/", t1.name);
              AssignCmd(t1, TopicCmd.Art.create, c.prim);
            }
            TopicCmd c1;
            if(_prOp.TryGetValue(c.src.path, out c1) && c1!=null && c1.art==TopicCmd.Art.set) {
              _prOp[t.path]=new TopicCmd(t, TopicCmd.Art.set, c1.prim) { vt=c1.vt, dt=c1.dt, o=c1.o };
            }
            goto case TopicCmd.Art.set;
          }
          break;
        case TopicCmd.Art.changed:
        case TopicCmd.Art.set: {
            TopicCmd c1;
            if(!_prOp.TryGetValue(c.src.path, out c1) || c1==null || ((int)c.art<=(int)c1.art)) {
              _prOp[c.src.path]=c;
            }
          }
          break;
        }
      }

      foreach(var cmd in _prOp.Values) {
        if(cmd.art==TopicCmd.Art.set || cmd.art==TopicCmd.Art.remove) {
          if(cmd.art!=TopicCmd.Art.set 
        || cmd.src._vt!=cmd.vt 
        || ((cmd.src._vt==VT.Object || cmd.src._vt==VT.Ref || cmd.src._vt==VT.String) && !object.Equals(cmd.src._o, cmd.o))
        || ((cmd.src._vt==VT.Bool || cmd.src._vt==VT.Integer) && cmd.src._dt.l!=cmd.dt.l)
        || (cmd.src._vt==VT.Float && cmd.src._dt.d!=cmd.dt.d)
        || (cmd.src._vt==VT.DateTime && cmd.src._dt.dt!=cmd.dt.dt)) {
            cmd.old_vt=cmd.src._vt;
            cmd.old_dt=cmd.src._dt;
            cmd.old_o=cmd.src._o;
            if(cmd.vt==VT.Json) {
              cmd.src._json=cmd.o as string;
              if(string.IsNullOrEmpty(cmd.src._json)) {
                cmd.src._vt=VT.Undefined;
              } else {
                using(JsonTextReader reader = new JsonTextReader(new StringReader(cmd.src._json))) {
                  if(reader.Read()) {
                    switch(reader.TokenType) {
                    case JsonToken.Boolean:
                      cmd.src._vt=VT.Bool;
                      cmd.src._dt.l=((bool)reader.Value)?1:0;
                      cmd.src._o=null;
                      break;
                    case JsonToken.Integer:
                      cmd.src._vt=VT.Integer;
                      cmd.src._dt.l=(long)reader.Value;
                      cmd.src._o=null;
                      break;
                    case JsonToken.Float:
                      cmd.src._vt=VT.Float;
                      cmd.src._dt.d=(double)reader.Value;
                      cmd.src._o=null;
                      break;
                    case JsonToken.Date:
                      cmd.src._vt=VT.DateTime;
                      cmd.src._dt.dt=(DateTime)reader.Value;
                      cmd.src._o=null;
                      break;
                    case JsonToken.String:
                      cmd.src._vt=VT.String;
                      cmd.src._dt.l=0;
                      cmd.src._o=reader.Value;
                      break;
                    default:
                      cmd.src._o=_jser.Deserialize(new JsonTextReader(new StringReader(cmd.src._json)));
                      if(cmd.src._o==null) {
                        cmd.src._vt=VT.Null;
                      } else if(cmd.src._o is Topic) {
                        cmd.src._vt=VT.Ref;
                      } else {
                        cmd.src._vt=VT.Object;
                      }
                      break;
                    }
                  }
                }
              }
            } else {
              cmd.src._vt=cmd.vt;
              cmd.src._dt=cmd.dt;
              cmd.src._o=cmd.o;
              cmd.src._json=null;
            }
            if(cmd.art==TopicCmd.Art.set) {
              cmd.art=TopicCmd.Art.changed;
            }
          }
        }
        if(cmd.art==TopicCmd.Art.remove || cmd.art==TopicCmd.Art.move) {
          cmd.src._flags[2]=true;
          if(cmd.src.parent!=null) {
            cmd.src.parent._children.Remove(cmd.src.name);
          }
        }
        //TODO: save for undo/redo
        /*IHistory h;
        if(cmd.prim!=null && cmd.prim._vt==VT.Object && (h=cmd.prim._o as IHistory)!=null) {
          h.Add(cmd);
        }*/
      }

      foreach(var cmd in _prOp.Values) {
        if(cmd.art==TopicCmd.Art.changed || cmd.art==TopicCmd.Art.remove) {
          if(cmd.old_o!=null) {
            Topic r;
            ITenant it;
            if(cmd.old_vt==VT.Ref && (r=cmd.old_o as Topic)!=null) {
              r.Unsubscribe(r.path, cmd.src.RefChanged);
              //TODO: X13.plugin.PLC.instance.DelVar(cmd.src);
            } else if(cmd.old_vt==VT.Object && (it=cmd.old_o as ITenant)!=null) {
              it.owner=null;
            }
          }
        }
        if(cmd.art==TopicCmd.Art.changed || cmd.art==TopicCmd.Art.create) {
          if(cmd.src._o!=null && !cmd.src._flags[2]) {
            ITenant tt;
            Topic r;
            if(cmd.src._vt==VT.Ref && (r=cmd.src._o as Topic)!=null) {
              r.Subscribe(new SubRec() { mask=r.path, ma=Bill.curArr, f=cmd.src.RefChanged });
              X13.plugin.PLC.instance.GetVar(cmd.src);
            } else if(cmd.src._vt==VT.Object &&  (tt=cmd.src._o as ITenant)!=null) {
              tt.owner=cmd.src;
            }
          }
        }
        if(cmd.art!=TopicCmd.Art.set) {
          cmd.src.Publish(cmd);
        }
        if(cmd.src._flags[2]) {
          cmd.src._flags[3]=true;
        }
      }
      _prOp.Clear();
      _busyFlag=1;
    }
    private static void AssignCmd(Topic src, TopicCmd.Art art, Topic prim) {
      TopicCmd c1;
      if((!_prOp.TryGetValue(src.path, out c1) || c1==null ||  ((int)art<=(int)c1.art))) {
        _prOp[src.path]=new TopicCmd(src, art, prim);
      }
    }

    public static void Clear() {
      lock(root) {
        _prIp.Clear();
        _prTp.Clear();
        _prOp.Clear();
        foreach(var t in root.all) {
          t._flags[2]=true;
          if(t._children!=null) {
            t._children.Clear();
            t._children=null;
          }
        }
        _busyFlag=1;
      }
      root._flags[2]=false;
    }
    #region variables
    private SortedList<string, Topic> _children;
    private List<SubRec> _subRecords;
    private Topic _parent;
    private string _name;
    private string _path;
    /// <summary>[0] - saved, [1] - local, [2] - disposed, [3] - disposed fin. </summary>
    private System.Collections.BitArray _flags;

    private VT _vt;
    private PriDT _dt;
    private object _o;
    private string _json;
    #endregion variables

    private Topic(Topic parent, string name) {
      _flags=new System.Collections.BitArray(5);
      _flags[0]=true;  // saved
      _name=name;
      _parent=parent;
      _vt=VT.Undefined;
      _dt.l=0;
      _o=null;

      if(parent==null) {
        _path="/";
      } else if(parent==root) {
        _path="/"+name;
      } else {
        _path=parent.path+"/"+name;
        _flags[1]=parent.local;
      }
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
    public Type vType {
      get {
        switch(_vt) {
        case VT.Null:
        case VT.Undefined:
          return null;
        case VT.Bool:
          return typeof(bool);
        case VT.Integer:
          return typeof(long);
        case VT.Float:
          return typeof(double);
        case VT.DateTime:
          return typeof(DateTime);
        case VT.String:
          return _o==null?null:typeof(string);
        case VT.Ref:
          return _o==null?null:typeof(Topic);
        case VT.Object:
          return _o==null?null:_o.GetType();
        }
        return null;
      }
    }
    public Bill all { get { return new Bill(this, true); } }
    public Bill children { get { return new Bill(this, false); } }
    /// <summary>save value in persistent storage</summary>
    public bool saved {
      get { return _flags[0]; }
      set {
        if(_flags[0]!=value) {
          _flags[0]=value;
          var c=new TopicCmd(this, TopicCmd.Art.changed, null);
          lock(root) {
            _prIp.Enqueue(c);
          }
        }
      }
    }
    /// <summary>only for this instance</summary>
    public bool local { get { return _flags[1]; } set { _flags[1]=value; } }
    /// <summary>removed</summary>
    public bool disposed { get { return _flags[2]; } }
    /// <summary>save value only in config file</summary>
    public bool config {
      get { return _flags[4]; }
      set {
        if(_flags[4]!=value) {
          _flags[4]=value;
          var c=new TopicCmd(this, TopicCmd.Art.changed, null);
          lock(root) {
            _prIp.Enqueue(c);
          }
        }
      }
    }

    /// <summary> Get item from tree</summary>
    /// <param name="path">relative or absolute path</param>
    /// <param name="create">true - create, false - check</param>
    /// <returns>item or null</returns>
    public Topic Get(string path, bool create=true, Topic prim=null) {
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
        } else if(home._children.TryGetValue(pt[i], out next)) {
          home=next;
        }
        if(next==null) {
          if(create) {
            next=new Topic(home, pt[i]);
            home._children.Add(pt[i], next);
            var c=new TopicCmd(next, TopicCmd.Art.create, prim);
            lock(root) {
              _prIp.Enqueue(c);
            }
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
    public void Remove(Topic prim=null) {
      var cmd=new TopicCmd(this, TopicCmd.Art.remove, prim);
      lock(root) {
        _prIp.Enqueue(cmd);
      }
    }
    public Topic Move(Topic nParent, string nName, Topic prim=null) {
      if(nParent==null) {
        nParent=this.parent;
      }
      if(string.IsNullOrEmpty(nName)) {
        nName=this.name;
      }
      if(nParent.Exist(nName)) {
        throw new ArgumentException(string.Concat(this.path, ".Move(", nParent.path, "/", nName, ") - destination already exist"));
      }
      Topic dst=new Topic(nParent, nName);
      nParent._children.Add(nName, dst);
      var c=new TopicCmd(this, TopicCmd.Art.move, prim);
      c.o=dst;
      lock(root) {
        _prIp.Enqueue(c);
      }
      return dst;
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
    public void SetJson(string json, Topic prim=null) {
      var c=new TopicCmd(this, TopicCmd.Art.set, prim);
      c.o=json;
      c.vt=VT.Json;
      lock(root) {
        _prIp.Enqueue(c);
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
            _o=_dt.l!=0;
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
    public T As<T>() where T : class {
      return _vt==VT.Object?(_o as T):default(T);
    }
    public Topic AsRef { get { return _vt==VT.Ref?(_o as Topic):null; } }
    public string ToJson() {
      if(_json==null) {
        lock(this) {
          if(_json==null) {
            switch(_vt) {
            case VT.Null:
              _json=JsonConvert.Null;
              break;
            case VT.Undefined:
              _json=JsonConvert.Undefined;
              break;
            case VT.Bool:
              _json=_dt.l==0?JsonConvert.False:JsonConvert.True;
              break;
            case VT.Integer:
              _json=JsonConvert.ToString(_dt.l);
              break;
            case VT.Float:
              _json=JsonConvert.ToString(_dt.d);
              break;
            case VT.DateTime:
              _json=JsonConvert.ToString(_dt.dt);
              break;
            case VT.String:
              _json=JsonConvert.ToString(_o as string);
              break;
            case VT.Ref:
              _json=string.Concat("{\"$ref\":", JsonConvert.ToString((_o as Topic).path), "}");
              break;
            default:
              using(var tw=new StringWriter()) {
                _jser.Serialize(tw, this.AsObject);
                _json=tw.ToString();
              }
              break;
            }
          }
        }
      }
      return _json;
    }
    public event Action<Topic, TopicCmd> changed {
      add {
        var c=new TopicCmd(this, TopicCmd.Art.subscribe, this);
        c.o=value;
        _dt.l=0;
        lock(root) {
          _prIp.Enqueue(c);
        }
      }
      remove {
        var c=new TopicCmd(this, TopicCmd.Art.unsubscribe, this);
        c.o=value;
        _dt.l=0;
        lock(root) {
          _prIp.Enqueue(c);
        }
      }
    }

    private void Publish(TopicCmd cmd) {
      Action<Topic, TopicCmd> func;
      if(cmd.art==TopicCmd.Art.subscribe && (func=cmd.o as Action<Topic, TopicCmd>)!=null) {
        try {
          func(this, cmd);
        }
        catch(Exception ex) {
          Log.Warning("{0}.{1}({2}, {4}) - {3}", func.Method.DeclaringType.Name, func.Method.Name, this.path, ex.ToString(), cmd.art.ToString());
        }
      } else {
        if(_subRecords!=null) {
          for(int i=0; i<_subRecords.Count; i++) {
            if((func=_subRecords[i].f)!=null && (_subRecords[i].ma.Length==0 || _subRecords[i].ma[0]==Bill.maskAll)) {
              try {
                func(this, cmd);
              }
              catch(Exception ex) {
                Log.Warning("{0}.{1}({2}, {4}) - {3}", func.Method.DeclaringType.Name, func.Method.Name, this.path, ex.ToString(), cmd.art.ToString());
              }
            }
          }
        }
      }
    }
    private void RefChanged(Topic t, TopicCmd c) {
      if(_vt==VT.Ref && (_o as Topic)==c.src) {
        if(c.art==TopicCmd.Art.move) {
          Topic dst;
          if((dst=c.o as Topic)!=null) {
            _o=dst;
            dst.Subscribe(new SubRec() { f=this.RefChanged, mask=dst.path, ma=Bill.curArr });
            var cmd=new TopicCmd(this, dst, c.prim);
            cmd.art=TopicCmd.Art.changed;
            this.Publish(cmd);
          }
        } else if(c.art==TopicCmd.Art.changed) {
          this.Publish(c);
        } else if(c.art==TopicCmd.Art.remove) {
          var cmd=new TopicCmd(this, TopicCmd.Art.changed, c.prim);
          cmd.old_vt=_vt;
          _vt=c.old_vt;
          cmd.vt=_vt;
          cmd.old_dt=_dt;
          _dt=c.old_dt;
          cmd.dt=_dt;
          cmd.old_o=_o;
          _o=c.old_o;
          cmd.o=_o;
          this.Publish(cmd);
        }
      }
    }
    private void Subscribe(SubRec sr) {
      if(this._subRecords==null) {
        this._subRecords=new List<SubRec>();
      }
      if(!this._subRecords.Exists(z => z.f==sr.f && z.mask==sr.mask)) {
        this._subRecords.Add(sr);
      }
    }
    private void Unsubscribe(string mask, Action<Topic, TopicCmd> f) {
      if(this._subRecords!=null) {
        this._subRecords.RemoveAll(z => z.f==f && z.mask==mask);
      }
    }

    #region nested types
    public class Bill : IEnumerable<Topic> {
      public const char delmiter='/';
      public const string delmiterStr="/";
      public const string maskAll="#";
      public const string maskChildren="+";
      public static readonly char[] delmiterArr=new char[] { delmiter };
      public static readonly string[] curArr=new string[0];
      public static readonly string[] allArr=new string[] { maskAll };
      public static readonly string[] childrenArr=new string[] { maskChildren };

      private Topic _home;
      private bool _deep;

      public Bill(Topic home, bool deep) {
        _home=home;
        _deep=deep;
      }
      public IEnumerator<Topic> GetEnumerator() {
        if(!_deep) {
          if(_home._children!=null) {
            Topic[] ch=_home._children.Values.ToArray();
            for(int i=ch.Length-1; i>=0; i--) {
              yield return ch[i];
            }
          }
        } else {
          var hist=new Stack<Topic>();
          Topic[] ch;
          Topic cur;
          hist.Push(_home);
          do {
            cur=hist.Pop();
            yield return cur;
            if(cur._children!=null) {
              ch=cur._children.Values.ToArray();
              for(int i=ch.Length-1; i>=0; i--) {
                hist.Push(ch[i]);
              }
            }
          } while(hist.Any());
        }
      }
      public event Action<Topic, TopicCmd> changed {
        add {
          TopicCmd c=new TopicCmd(_home, TopicCmd.Art.subscribe, _home);
          c.o=value;
          c.dt.l=_deep?2:1;
          lock(root) {
            _prIp.Enqueue(c);
          }
        }
        remove {
          TopicCmd c=new TopicCmd(_home, TopicCmd.Art.unsubscribe, _home);
          c.o=value;
          c.dt.l=_deep?2:1;
          lock(root) {
            _prIp.Enqueue(c);
          }
        }
      }
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
    }

    private class RefResolver : Newtonsoft.Json.Serialization.IReferenceResolver {
      public void AddReference(object context, string reference, object value) {
      }

      public string GetReference(object context, object value) {
        Topic t=value as Topic;
        return (t!=null)?t.path:string.Empty;
      }

      public bool IsReferenced(object context, object value) {
        Topic t=value as Topic;
        return t!=null;
      }

      public object ResolveReference(object context, string path) {
        return Topic.root.Get(path);
      }
    }

    internal enum VT {
      Undefined = 0,
      Null,
      Bool,
      Integer,
      Float,
      DateTime,
      String,
      Object,
      //Array,
      //Record,
      //Binary,
      Ref,
      Json,
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

    private struct SubRec {
      public string mask;
      public string [] ma;
      public  Action<Topic, TopicCmd> f;
    }
    #endregion nested types
  }

  public class TopicCmd {
    internal Topic.VT vt;
    internal Topic.PriDT dt;
    internal object o;
    internal Topic.VT old_vt;
    internal Topic.PriDT old_dt;
    internal object old_o;

    public readonly Topic src;
    public readonly Topic prim;
    public Art art { get; internal set; }

    internal TopicCmd(Topic src, Art art, Topic prim) {
      this.src=src;
      this.art=art;
      vt=Topic.VT.Undefined;
      o=null;
      this.prim=prim;
    }
    internal TopicCmd(Topic src, bool val, Topic prim) {
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
      create=4,
      set=3,
      changed=5,
      subscribe=6,
      unsubscribe=7,
      move=2,
      remove=1
    }

    public bool Visited(Topic snd, bool add) {
      return false;
    }
  }

  public interface ITenant {
    Topic owner { get; set; }
  }
}
