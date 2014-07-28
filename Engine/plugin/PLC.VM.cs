using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using X13.lib;

namespace X13.plugin {
  internal class VM {
    private List<IPiValue> _s;
    private List<IPiValue> _mem;
    private int _sp;
    private int _sfp;
    private List<byte> _prg;
    private int _pc;

    public VM(){
      _s=new List<IPiValue>();
      _mem=new List<IPiValue>();
      _prg=new List<byte>();
      _sp=-1;
      _sfp=-1;
      _pc=0;
    }
    public void Load(byte[] prg) {
      _prg=new List<byte>(prg);
      _sp=-1;
      _sfp=-1;
      _pc=0;
    }
    public bool Step() {
      if(_prg==null || _pc>=_prg.Count) {
        return false;
      }
      OP cmd=(OP)_prg[_pc++];
      IPiValue tmp, tmp2;
      switch(cmd) {
        /*
      #region PUSH/POP
      case OP.DUP:
        PushS(_s[_sp]);
        break;
      case OP.DROP:
        PopS();
        break;
      case OP.NIP:    // (a b - b )
        tmp=PopS();
        _s[_sp]=tmp;
        break;
      case OP.SWAP:  // (b a - a b)
        tmp=_s[_sp];
        _s[_sp]=_s[_sp-1];
        _s[_sp-1]=tmp;
        break;
      case OP.PUSH_S1:
      case OP.PUSH_S2:
      case OP.PUSH_S4:
        PushS(LoadProgramMemory(ref _pc, (VarType)(((byte)cmd)&0x07)));
        break;
      case OP.PUSH_U1:
      case OP.PUSH_U2:
        PushS(LoadProgramMemory(ref _pc, (VarType)((-1+(byte)cmd)&0x07)));
        break;
      case OP.PUSH_ZERO:
        PushS(0);
        break;
      case OP.PUSH_TRUE:
        PushS(1);
        break;
      case OP.PUSH_P0:
      case OP.PUSH_P1:
      case OP.PUSH_P2:
      case OP.PUSH_P3:
      case OP.PUSH_P4:
      case OP.PUSH_P5:
      case OP.PUSH_P6:
      case OP.PUSH_P7:
      case OP.PUSH_P8:
      case OP.PUSH_P9:
      case OP.PUSH_PA:
      case OP.PUSH_PB:
      case OP.PUSH_PC:
      case OP.PUSH_PD:
      case OP.PUSH_PE:
      case OP.PUSH_PF:
        PushS(_s[_sfp-2-(((byte)cmd) & 0x0f)]);
        break;
      case OP.PUSH_L0:
      case OP.PUSH_L1:
      case OP.PUSH_L2:
      case OP.PUSH_L3:
      case OP.PUSH_L4:
      case OP.PUSH_L5:
      case OP.PUSH_L6:
      case OP.PUSH_L7:
      case OP.PUSH_L8:
      case OP.PUSH_L9:
      case OP.PUSH_LA:
      case OP.PUSH_LB:
      case OP.PUSH_LC:
      case OP.PUSH_LD:
      case OP.PUSH_LE:
      case OP.PUSH_LF:
        PushS(_s[_sfp+1+(((byte)cmd) & 0x0f)]);
        break;
      case OP.POP_P0:
      case OP.POP_P1:
      case OP.POP_P2:
      case OP.POP_P3:
      case OP.POP_P4:
      case OP.POP_P5:
      case OP.POP_P6:
      case OP.POP_P7:
      case OP.POP_P8:
      case OP.POP_P9:
      case OP.POP_PA:
      case OP.POP_PB:
      case OP.POP_PC:
      case OP.POP_PD:
      case OP.POP_PE:
      case OP.POP_PF:
        _s[_sfp-2-(((byte)cmd) & 0x0f)]=PopS();
        break;
      case OP.POP_L0:
      case OP.POP_L1:
      case OP.POP_L2:
      case OP.POP_L3:
      case OP.POP_L4:
      case OP.POP_L5:
      case OP.POP_L6:
      case OP.POP_L7:
      case OP.POP_L8:
      case OP.POP_L9:
      case OP.POP_LA:
      case OP.POP_LB:
      case OP.POP_LC:
      case OP.POP_LD:
      case OP.POP_LE:
      case OP.POP_LF:
        _s[_sfp+1+(((byte)cmd) & 0x0f)]=PopS();
        break;
      case OP.PUSHM_B1_S16: // memory -> stack
      case OP.PUSHM_B1_CS8:
      case OP.PUSHM_B1_CS16:
      case OP.PUSHM_B1_C16:
      case OP.PUSHM_S1_S16:
      case OP.PUSHM_S1_CS8:
      case OP.PUSHM_S1_CS16:
      case OP.PUSHM_S1_C16:
      case OP.PUSHM_S2_S16:
      case OP.PUSHM_S2_CS8:
      case OP.PUSHM_S2_CS16:
      case OP.PUSHM_S2_C16:
      case OP.PUSHM_S4_S16:
      case OP.PUSHM_S4_CS8:
      case OP.PUSHM_S4_CS16:
      case OP.PUSHM_S4_C16:
      case OP.PUSHM_U1_S16:
      case OP.PUSHM_U1_CS8:
      case OP.PUSHM_U1_CS16:
      case OP.PUSHM_U1_C16:
      case OP.PUSHM_U2_S16:
      case OP.PUSHM_U2_CS8:
      case OP.PUSHM_U2_CS16:
      case OP.PUSHM_U2_C16: {
          int addr;
          switch(((byte)cmd)&0x03) {
          case 0:
            addr=PopS();
            break;
          case 1:
            addr=PopS()+LoadProgramMemory(ref _pc, VarType.S1);
            break;
          case 2:
            addr=PopS()+LoadProgramMemory(ref _pc, VarType.S2);
            break;
          case 3:
            addr=LoadProgramMemory(ref _pc, VarType.U2);
            break;
          default:
            addr=-1;
            break;
          }
          PushS(LoadData(addr, (VarType)((((byte)cmd)>>2)&0x07)));
        }
        break;
      case OP.POPM_B1_S16: // stack -> memory
      case OP.POPM_B1_CS8:
      case OP.POPM_B1_CS16:
      case OP.POPM_B1_C16:
      case OP.POPM_S1_S16:
      case OP.POPM_S1_CS8:
      case OP.POPM_S1_CS16:
      case OP.POPM_S1_C16:
      case OP.POPM_S2_S16:
      case OP.POPM_S2_CS8:
      case OP.POPM_S2_CS16:
      case OP.POPM_S2_C16:
      case OP.POPM_S4_S16:
      case OP.POPM_S4_CS8:
      case OP.POPM_S4_CS16:
      case OP.POPM_S4_C16:
      case OP.POPM_U1_S16:
      case OP.POPM_U1_CS8:
      case OP.POPM_U1_CS16:
      case OP.POPM_U1_C16:
      case OP.POPM_U2_S16:
      case OP.POPM_U2_CS8:
      case OP.POPM_U2_CS16:
      case OP.POPM_U2_C16: {
          int addr;
          switch(((byte)cmd)&0x03) {
          case 0:
            addr=PopS();
            break;
          case 1:
            addr=PopS()+LoadProgramMemory(ref _pc, VarType.S1);
            break;
          case 2:
            addr=PopS()+LoadProgramMemory(ref _pc, VarType.S2);
            break;
          case 3:
            addr=LoadProgramMemory(ref _pc, VarType.U2);
            break;
          default:
            addr=-1;
            break;
          }
          StoreData(addr, (VarType)((((byte)cmd)>>2)&0x07), PopS());
        }
        break;
      #endregion PUSH/POP
        */
      #region ALU
      case OP.ADD:
        tmp=PopS();
        tmp2=PopS(); 
        {
          PiConst r=new PiConst();
          r.Set<double>(tmp.As<double>()+tmp2.As<double>());
          PushS(r);
        }
        //_s[_sp]+=tmp;
        break;
        /*
      case OP.SUB:
        tmp=PopS();
        _s[_sp]-=tmp;
        break;
      case OP.MUL:
        tmp=PopS();
        _s[_sp]*=tmp;
        break;
      case OP.DIV:
        tmp=PopS();
        _s[_sp]/=tmp;
        break;
      case OP.MOD:
        tmp=PopS();
        _s[_sp]%=tmp;
        break;
      case OP.SHL:
        tmp=PopS();
        _s[_sp]<<=tmp;
        break;
      case OP.SHR:
        tmp=PopS();
        _s[_sp]>>=tmp;
        break;
      case OP.AND:
        tmp=PopS();
        _s[_sp]&=tmp;
        break;
      case OP.OR:
        tmp=PopS();
        _s[_sp]|=tmp;
        break;
      case OP.XOR:
        tmp=PopS();
        _s[_sp]^=tmp;
        break;
      case OP.NOT:
        _s[_sp]=~_s[_sp];
        break;
      case OP.NEG:
        _s[_sp]=-_s[_sp];
        break;
      case OP.INC:
        _s[_sp]++;
        break;
      case OP.DEC:
        _s[_sp]--;
        break;
      case OP.CEQ:
        tmp=PopS();
        _s[_sp]=_s[_sp]==tmp?1:0;
        break;
      case OP.CNE:
        tmp=PopS();
        _s[_sp]=_s[_sp]!=tmp?1:0;
        break;
      case OP.CGT:
        tmp=PopS();
        _s[_sp]=_s[_sp]>tmp?1:0;
        break;
      case OP.CGE:
        tmp=PopS();
        _s[_sp]=_s[_sp]>=tmp?1:0;
        break;
      case OP.CLT:
        tmp=PopS();
        _s[_sp]=_s[_sp]<tmp?1:0;
        break;
      case OP.CLE:
        tmp=PopS();
        _s[_sp]=_s[_sp]<=tmp?1:0;
        break;
      case OP.NOT_L:
        _s[_sp]=_s[_sp]==0?1:0;
        break;
      case OP.AND_L:
        tmp=PopS();
        _s[_sp]=_s[_sp]!=0 && tmp!=0?1:0;
        break;
      case OP.OR_L:
        tmp=PopS();
        _s[_sp]=_s[_sp]!=0 || tmp!=0?1:0;
        break;
      case OP.XOR_L:
        tmp=PopS();
        _s[_sp]=_s[_sp]!=0 ^ tmp!=0?1:0;
        break;
        */
      #endregion ALU
        /*
      case OP.JMP:
        tmp=LoadProgramMemory(ref _pc, VarType.U2);
        _pc=(int)tmp.As<long>();
        break;
      case OP.CALL:
        tmp=LoadProgramMemory(ref _pc, VarType.U2);
        PushS(_pc);
        PushS(_sfp);
        _sfp=_sp;
        _pc=tmp;
        break;
      case OP.JZ:
        tmp2=PopS();
        if(tmp2==0) {
          tmp=LoadProgramMemory(ref _pc, VarType.U2);
          _pc=tmp;
        } else {
          _pc+=2;
        }
        break;
      case OP.JNZ:
        tmp2=PopS();
        if(tmp2!=0) {
          tmp=LoadProgramMemory(ref _pc, VarType.U2);
          _pc=tmp;
        } else {
          _pc+=2;
        }
        break;

      case OP.TEST_EQ:
        _testNr++;
        tmp=PopS();
        tmp2=LoadProgramMemory(ref _pc, VarType.S4);
        if(tmp!=tmp2) {
          Log.Warning("Test #{0} FAIL, cur={1}, exp={2}", _testNr, tmp, tmp2);
        } else {
          Log.Info("Test #{0} pass", _testNr);
        }
        break;
         */
      case OP.RET:
        if(_sfp<0) {
          return false;
        } else {
          _sp=_sfp;
          _sfp=(int)PopS().As<long>();
          _pc=(int)PopS().As<long>();
        }
        break;

      default:
        Log.Error("unknown OP [{0:X4}]:{1}", _pc-1, cmd);
        return false;
      }
      return true;
    }
    public void PushS(IPiValue val) {
      _sp++;
      if(_sp>=_s.Count) {
        _s.Add(val);
      } else {
        _s[_sp]=val;
      }
    }
    public IPiValue PopS() {
      return _s[_sp--];
    }
    private IPiValue LoadProgramMemory(ref int ptr, VarType t) {
      PiConst rez=new PiConst();
      switch(t) {
      case VarType.S1:
        rez.Set<long>((sbyte)_prg[ptr++]);
        break;
      case VarType.U1:
        rez.Set<long>(_prg[ptr++]);
        break;
      case VarType.S2:
        rez.Set<long>((short)((_prg[ptr++]<<8) | _prg[ptr++]));
        break;
      case VarType.U2:
        rez.Set<long>((_prg[ptr++]<<8) | _prg[ptr++]);
        break;
      case VarType.S4:
        rez.Set<long>((_prg[ptr++]<<24) | (_prg[ptr++]<<16) | (_prg[ptr++]<<8) | _prg[ptr++]);
        break;
      default:
        rez.Set<long>(0);
        break;
      }
      return rez;
    }
    private IPiValue LoadData(int addr, VarType varType) {
      return _mem[addr];
    }
    private void StoreData(int addr, IPiValue val) {
      if(_mem[addr]==null) {
        _mem[addr]=val;
      } else {
        _mem[addr].Set(val.As<object>());
      }
    }

    public enum VarType {
      Bool=0,
      S1=1,
      S2=2,
      S4=3,
      U1=4,
      U2=5
    }
    public enum OP : byte {
      NOP         =0x00,

      NOT         =0x10,
      AND,
      OR,
      XOR,
      SHL,
      SHR,
      INC,
      DEC,
      NEG,
      ADD,
      SUB,
      MUL,
      DIV,
      MOD,
      CEQ,
      CNE,
      NOT_L,
      AND_L,
      OR_L,
      XOR_L,
      CGT,
      CGE,
      CLT,
      CLE,

      DUP        =0x30,
      DROP,
      NIP,
      SWAP,

      PUSH_P0    =0x40,
      PUSH_P1,
      PUSH_P2,
      PUSH_P3,
      PUSH_P4,
      PUSH_P5,
      PUSH_P6,
      PUSH_P7,
      PUSH_P8,
      PUSH_P9,
      PUSH_PA,
      PUSH_PB,
      PUSH_PC,
      PUSH_PD,
      PUSH_PE,
      PUSH_PF,

      PUSH_L0    =0x50,
      PUSH_L1,
      PUSH_L2,
      PUSH_L3,
      PUSH_L4,
      PUSH_L5,
      PUSH_L6,
      PUSH_L7,
      PUSH_L8,
      PUSH_L9,
      PUSH_LA,
      PUSH_LB,
      PUSH_LC,
      PUSH_LD,
      PUSH_LE,
      PUSH_LF,

      POP_P0   =0x60,
      POP_P1,
      POP_P2,
      POP_P3,
      POP_P4,
      POP_P5,
      POP_P6,
      POP_P7,
      POP_P8,
      POP_P9,
      POP_PA,
      POP_PB,
      POP_PC,
      POP_PD,
      POP_PE,
      POP_PF,

      POP_L0   =0x70,
      POP_L1,
      POP_L2,
      POP_L3,
      POP_L4,
      POP_L5,
      POP_L6,
      POP_L7,
      POP_L8,
      POP_L9,
      POP_LA,
      POP_LB,
      POP_LC,
      POP_LD,
      POP_LE,
      POP_LF,

      PUSHM_B1_S16  =0x80,
      PUSHM_B1_CS8,
      PUSHM_B1_CS16,
      PUSHM_B1_C16,
      PUSHM_S1_S16,
      PUSHM_S1_CS8,
      PUSHM_S1_CS16,
      PUSHM_S1_C16,
      PUSHM_S2_S16,
      PUSHM_S2_CS8,
      PUSHM_S2_CS16,
      PUSHM_S2_C16,
      PUSHM_S4_S16,
      PUSHM_S4_CS8,
      PUSHM_S4_CS16,
      PUSHM_S4_C16,
      PUSHM_U1_S16,
      PUSHM_U1_CS8,
      PUSHM_U1_CS16,
      PUSHM_U1_C16,
      PUSHM_U2_S16,
      PUSHM_U2_CS8,
      PUSHM_U2_CS16,
      PUSHM_U2_C16,

      PUSH_ZERO  =0x98,
      PUSH_S1    =0x99,
      PUSH_S2    =0x9A,
      PUSH_S4    =0x9B,
      PUSH_TRUE  =0x9C,
      PUSH_U1    =0x9D,
      PUSH_U2    =0x9E,

      POPM_B1_S16  =0xA0,
      POPM_B1_CS8,
      POPM_B1_CS16,
      POPM_B1_C16,
      POPM_S1_S16,
      POPM_S1_CS8,
      POPM_S1_CS16,
      POPM_S1_C16,
      POPM_S2_S16,
      POPM_S2_CS8,
      POPM_S2_CS16,
      POPM_S2_C16,
      POPM_S4_S16,
      POPM_S4_CS8,
      POPM_S4_CS16,
      POPM_S4_C16,
      POPM_U1_S16,
      POPM_U1_CS8,
      POPM_U1_CS16,
      POPM_U1_C16,
      POPM_U2_S16,
      POPM_U2_CS8,
      POPM_U2_CS16,
      POPM_U2_C16,

      JMP         =0xBA,
      JZ          =0xBB,
      CALL        =0xBE,
      JNZ         =0xBF,

      TEST_EQ,
      RET         =0xFF,
    }
  }
}