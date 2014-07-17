using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X13.plugin {
  public class PLC {
    static PLC() {
      instance=new PLC();
    }
    public static readonly PLC instance;

    private List<PiBlock> _blocks;
    private Dictionary<Topic, PiVar> _vars;
    public PLC() {
      _blocks=new List<PiBlock>();
      _vars=new Dictionary<Topic, PiVar>();
    }
    public void Init() {
    }
    public void Start() {
      Queue<PiVar> vQu=new Queue<PiVar>(_vars.Values);
      PiVar v1, v2;
      while(vQu.Count>0) {
        v1=vQu.Dequeue();
        if(v1._owner.vType==typeof(Topic)) {
          if(!_vars.TryGetValue(v1._owner.AsRef, out v2)) {
            v2=new PiVar(v1._owner.AsRef);
            _vars[v1._owner.AsRef]=v2;
            vQu.Enqueue(v2);
          }
          v1.gray=true;
          var l=new PiLink(v2, v1);
          v1._links.Add(l);
          v2._links.Add(l);
        } else if(v1.dir==true) {
          v1.gray=true;
        }
      }
      vQu=new Queue<PiVar>(_vars.Values.Where(z => z.gray==false));
      while(vQu.Count>0) {
        v1=vQu.Dequeue();
        if(v1.layer==-1) {
          v1.layer=0;
          v1.calcPath=new PiBlock[0];
        }
        foreach(var l in v1._links.Where(z => z.input==v1)) {
          l.layer=v1.layer;
          l.output.layer=l.layer;
          l.output.calcPath=v1.calcPath;
          vQu.Enqueue(l.output);
        }
        if(v1.dir==false && v1.block!=null) {
          if(v1.calcPath.Contains(v1.block)) {
            X13.lib.Log.Debug("{0} make loop", v1._owner.path);
            continue;
          }
          v1.block.layer=v1.block._pins.Where(z => z.dir==false).Max(z => z.layer)+1;
          v1.block.calcPath=v1.block.calcPath.Union(v1.calcPath).ToArray();
          foreach(var v3 in v1.block._pins.Where(z => z.dir==true)) {
            v3.layer=v1.block.layer;
            v3.calcPath=v1.block.calcPath;
            if(!vQu.Contains(v3)) {
              vQu.Enqueue(v3);
            }
          }
        }
      }

    }
    public void Stop() {
    }


    internal void AddBlock(PiBlock bl) {
      _blocks.Add(bl);
    }
    internal PiVar GetVar(Topic t) {
      PiVar v;
      if(!_vars.TryGetValue(t, out v)) {
        v=new PiVar(t);
        _vars[t]=v;
      }
      return v;
    }
  }
  internal class PiLink {
    public PiVar input;
    public PiVar output;
    public int layer;

    public PiLink(PiVar ip, PiVar op) {
      input=ip;
      output=op;
    }
  }

  internal class PiVar {
    internal Topic _owner;
    internal List<PiLink> _links;
    internal PiBlock[] calcPath;
    public PiBlock block;

    /// <summary>false - input, true - output, null - io</summary>
    public bool? dir { get { return pi==null?null:(bool?)pi.dir; } }
    public int layer;
    public   PinInfo pi;
    public bool gray;

    public PiVar(Topic src) {
      this._owner = src;
      _links=new List<PiLink>();
      layer=-1;
    }
  }

  [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
  public class PiBlock : ITenant {
    private Topic _owner;
    internal List<PiVar> _pins;
    internal PiBlock[] calcPath;
    private PDeclarer _decl;

    public PiBlock(string declarer) {
      this.declarer=declarer;
      _pins=new List<PiVar>();
      calcPath=new PiBlock[]{this};
      PLC.instance.AddBlock(this);
    }
    public Topic owner {
      get {
        return _owner;
      }
      set {
        if(_owner!=value) {
          if(_owner!=null) {
            _owner.children.changed-=children_changed;
          }
          _owner=value;
          if(_owner!=null) {
            _decl=PDeclarer.Get(declarer);
            _owner.children.changed+=children_changed;
          }
        }
      }
    }
    [Newtonsoft.Json.JsonProperty]
    public string declarer { get; private set; }

    private void children_changed(Topic src, TopicCmd p) {
      if(p.art==TopicCmd.Art.create || p.art==TopicCmd.Art.subscribe) {
        if(_decl==null) {
          return;
        }
        PinInfo pi;
        if(_decl.ExistPin(src.name, out pi)) {
          var pin=PLC.instance.GetVar(src);
          pin.pi=pi;
          pin.block=this;
          _pins.Add(pin);
        }
      }
    }
    public int layer;
  }

  [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
  public class PDeclarer : ITenant {
    private static Topic _droot;
    static PDeclarer() {
      _droot=Topic.root.Get("/etc/declarers", true);
    }
    internal static PDeclarer Get(string d) {
      Topic t;
      if(_droot.Exist(d, out t) && t.vType==typeof(PDeclarer)) {
        return t.As<PDeclarer>();
      }
      return null;
    }

    private Topic _owner;
    public Topic owner {
      get {
        return _owner;
      }
      set {
        if(_owner!=value) {
          if(_owner!=null) {
            _owner.children.changed-=children_changed;
          }
          _owner=value;
          if(_owner!=null) {
            _owner.children.changed+=children_changed;
          }
        }
      }
    }
    [Newtonsoft.Json.JsonProperty]
    private string info { get; set; }
    private void children_changed(Topic src, TopicCmd p) {
    }

    public bool ExistPin(string name, out PinInfo pi) {
      Topic t;
      if(_owner.Exist(name, out t) && t.vType==typeof(PinInfo)) {
        pi=t.As<PinInfo>();
        return true;
      }
      pi=null;
      return false;
    }
  }

  [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
  public class PinInfo {
    [Newtonsoft.Json.JsonProperty]
    public bool dir { get; set; }
  }

  /*
  public class PiGroup : PiBlock {
    private List<PiBlock> _blocks;

    public PiGroup()
      : base("schema") {
        _blocks=new List<PiBlock>();
    }
    protected override void children_changed(Topic src, TopicCmd p) {
      base.children_changed(src, p);
    }
  }*/
}
