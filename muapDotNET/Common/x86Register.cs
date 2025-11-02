using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Common
{
    public class x86Register
    {
        //public PW pw = null;

        public byte al;
        public byte ah;
        private ushort eaxh;
        public ushort ax
        {
            get
            {
                return (ushort)(ah * 0x100 + al);
            }
            set
            {
                ah = (byte)(value >> 8);
                al = (byte)value;
            }
        }
        public uint eax
        {
            get
            {
                return (uint)(eaxh * 0x1_0000 + ax);
            }
            set
            {
                eaxh = (ushort)(value >> 16);
                ax = (ushort)value;
            }
        }

        public byte bl;
        public byte bh;
        public ushort bx
        {
            get
            {
                return (ushort)(bh * 0x100 + bl);
            }
            set
            {
                //if (pw != null && pw.checkJumpIndexBX)
                //{
                //    if (value == pw.jumpIndex)
                //        pw.jumpIndex = -1;
                //}

                bh = (byte)(value >> 8);
                bl = (byte)value;
            }
        }

        public byte cl;
        public byte ch;
        public ushort cx
        {
            get
            {
                return (ushort)(ch * 0x100 + cl);
            }
            set
            {
                ch = (byte)(value >> 8);
                cl = (byte)value;
            }
        }

        public byte dl;
        public byte dh;
        private ushort edxh;
        public ushort dx
        {
            get
            {
                return (ushort)(dh * 0x100 + dl);
            }
            set
            {
                dh = (byte)(value >> 8);
                dl = (byte)value;
            }
        }
        public uint edx
        {
            get
            {
                return (uint)(edxh * 0x1_0000 + dx);
            }
            set
            {
                edxh = (ushort)(value >> 16);
                dx = (ushort)value;
            }
        }

        public ushort di { get; set; }
        public ushort cs { get; set; }
        public ushort es { get; set; }
        public ushort ds { get; set; }

        public ushort fs { get; set; }

        private ushort _si;
        public ushort si
        {
            get
            {
                return _si;
            }
            set
            {
                //if (pw != null && pw.checkJumpIndexSI)
                //{
                //    if (value == pw.jumpIndex)
                //        pw.jumpIndex = -1;
                //}
                _si = value;
            }
        }

        public ushort bp { get; set; }

        public ushort sp { get; set; }
        public ushort ss { get; set; }

        public bool carry { get; set; }

        public bool sign { get; set; }

        public bool zero { get; set; }
        public bool overflow { get; set; }

        public Stack<ushort> stack = new Stack<ushort>();

        public object lockobj = new object();

        private int[] bitMask = new int[] { 0x00, 0x01, 0x03, 0x07, 0x0f, 0x1f, 0x3f, 0x7f, 0xff };

        public byte rol(byte r, int n)
        {
            n &= 7;
            byte ans = (byte)((r << n) | ((r >> (8 - n))));// & bitMask[n]));
            carry = ((ans & 0x01) != 0);
            return ans;
        }

        public byte ror(byte r, int n)
        {
            n &= 7;
            byte ans = (byte)((r << (8 - n)) | ((r >> n)));// & bitMask[8 - n]));
            carry = ((ans & 0x80) != 0);
            return ans;
        }

        public byte rcl(byte r, int n)
        {
            n &= 7;
            byte ans = (byte)(
                (r << n)
                | ((carry ? 1 : 0) << n)
                | (n < 2 ? 0 : (r >> (9 - n)))
                );// & bitMask[n]));
            carry = ((r & (0x100 >> n)) != 0);
            return ans;
        }

        public ushort rcl(ushort r, int n)
        {
            n &= 15;
            ushort ans = (ushort)(
                (r << n)
                | ((carry ? 1 : 0) << (n - 1))
                | (n < 2 ? 0 : (r >> (17 - n)))
                );// & bitMask[n]));
            carry = ((r & (0x10000 >> n)) != 0);
            return ans;
        }

        public byte rcr(byte r, int n)
        {
            n &= 7;
            byte ans = (byte)(
                (n < 2 ? 0 : (r << (9 - n)))
                | ((carry ? 0x100 : 0) >> n)
                | (r >> n)
                );// & bitMask[n]));
            carry = ((r & (0x1 >> (n - 1))) != 0);
            return ans;
        }

        public ushort rcr(ushort r, int n)
        {
            n &= 0xf;
            ushort ans = (ushort)(
                (n < 2 ? 0 : (r << (17 - n)))
                | ((carry ? 0x10000 : 0) >> n)
                | (r >> n)
                );// & bitMask[n]));
            carry = ((r & (0x1 << (n - 1))) != 0);
            return ans;
        }

        public void test(byte src,byte bbit)
        {
            zero = ((src & bbit) == 0);
            carry = false;
        }

        public void test(ushort src, ushort ubit)
        {
            zero = ((src & ubit) == 0);
            carry = false;
        }

        public void mul(byte a)
        {
            ushort ans = (ushort)(al * a);
            ax = ans;
        }

        public void mul(ushort a)
        {
            uint ans = (uint)(ax * a);
            dx = (ushort)(ans >> 16);
            ax = (ushort)ans;
        }

        public void div(byte a)
        {
            byte q = (byte)(ax / a);
            byte r = (byte)(ax % a);
            al = q;
            ah = r;
        }

        public void div(ushort a)
        {
            uint b = (uint)((dx << 16) + ax);
            ushort q = (ushort)(b / a);
            ushort r = (ushort)(b % a);
            ax = q;
            dx = r;
        }

        /// <summary>
        /// compare  a-bしたとしたら結果のフラグはどうなるか
        /// ex
        ///  ja     (a>b)  c=0&z=0
        ///  jna    (a<=b) c=1|z=1
        ///  jb/jc  (a<b)  c=1
        ///  jnb/jnc(a>=b) c=0
        ///  je/jz  (a=b)      z=1
        ///  jne/jnz(a!=b)     z=0
        /// </summary>
        public void cmp(byte a,byte b)
        {
            zero = a == b;
            carry = a < b;
        }

        /// <summary>
        /// compare  a-bしたとしたら結果のフラグはどうなるか
        /// ex
        ///  ja     (a>b)  c=0&z=0
        ///  jna    (a<=b) c=1|z=1
        ///  jb/jc  (a<b)  c=1
        ///  jnb/jnc(a>=b) c=0
        ///  je/jz  (a=b)      z=1
        ///  jne/jnz(a!=b)     z=0
        /// </summary>
        public void cmp(ushort a, ushort b)
        {
            zero = a == b;
            carry = a < b;
        }

        private Stack<byte> stackMem = new Stack<byte>();

        public void push(ushort v)
        {
            sp -= 2;
            stackMem.Push((byte)v);
            stackMem.Push((byte)(v >> 8));
        }

        public ushort pop()
        {
            sp += 2;
            return (ushort)((stackMem.Pop() << 8) | stackMem.Pop());
        }

        public void pushA()
        {
            push(ax);
            push(cx);
            push(dx);
            push(bx);
            push(bp);
            push(si);
            push(di);
        }

        public void popA()
        {
            di = pop();
            si = pop();
            bp = pop();
            bx = pop();
            dx = pop();
            cx = pop();
            ax = pop();
        }
    }
}
