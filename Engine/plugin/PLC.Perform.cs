using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.plugin {
  public class Perform : IComparable<Perform> {
    private static int[] _prio;

    static Perform() {
      //create=1,         1
      //subscribe=2,      2  
      //unsubscribe=3,    2
      //set=4,            2
      //changed=5,        2
      //move=6,           3
      //remove=7          3
      _prio=new int[] { 0, 1, 2, 2, 2, 2, 3, 3 };
    }

    internal Topic.VT vt;
    internal Topic.PriDT dt;
    internal object o;
    internal Topic.VT old_vt;
    internal Topic.PriDT old_dt;
    internal object old_o;

    public readonly Topic src;
    public readonly Topic prim;
    public readonly int layer;
    public Art art { get; internal set; }

    private Perform(Art art, Topic src, Topic prim) {
      this.src=src;
      this.art=art;
      this.prim=prim;

      PiBlock b;
      PiVar v;
      if(vt==Topic.VT.Object && (b=o as PiBlock)!=null) {
        this.layer=b.layer;
      } else if((v=PLC.instance.GetVar(src, false))!=null) {
        this.layer=v.layer;
      } else {
        this.layer=int.MinValue;
      }
    }
    internal Perform(Topic src, Art art, Topic prim)
      : this(art, src, prim) {
      vt=Topic.VT.Undefined;
      o=null;
    }
    internal Perform(Topic src, bool val, Topic prim)
      : this(Art.set, src, prim) {
      vt=Topic.VT.Bool;
      dt.l=val?1:0;
      o=null;
    }
    internal Perform(Topic src, long val, Topic prim)
      : this(Art.set, src, prim) {
      vt=Topic.VT.Integer;
      dt.l=val;
      o=null;
    }
    internal Perform(Topic src, double val, Topic prim)
      : this(Art.set, src, prim) {
      vt=Topic.VT.Float;
      dt.d=val;
      o=null;
    }
    internal Perform(Topic src, DateTime val, Topic prim)
      : this(Art.set, src, prim) {
      vt=Topic.VT.DateTime;
      dt.dt=val;
      o=null;
    }
    internal Perform(Topic src, object val, Topic prim)
      : this(Art.set, src, prim) {
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
    }

    public int CompareTo(Perform other) {
      if(other==null) {
        return -1;
      }
      if(this.layer!=other.layer) {
        return this.layer.CompareTo(other.layer);
      }
      if(this.src.path!=other.src.path) {
        return this.src.path.CompareTo(other.src.path);
      }
      return _prio[((int)this.art)].CompareTo(_prio[(int)(other.art)]);    }

    public enum Art {
      create=1,
      subscribe=2,
      unsubscribe=3,
      set=4,
      changed=5,
      move=6,
      remove=7
    }

  }
}
