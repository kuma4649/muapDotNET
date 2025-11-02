using muapDotNET.Common;
using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Driver
{
    public class Pc98
    {
        private int lastPort = 0;
        private int lastData = 0;

        //private byte IMR = 0;
        private int CS4231dmaInt = 0;
        private int CS4231IdxAdr = 0;
        private int CS4231IdxDat = 0;
        private int CS4231INTRst = 0;
        private byte[] CS4231_Reg = new byte[32];
        private int _86PCM_FIFO = 0;

        private Work work;
        private x86Register reg;
        private Action<ChipDatum> writeOPNAP;
        private Action<ChipDatum> writeOPN2P;
        private Action<ChipDatum> writeCS4231;
        private Func<byte, byte> readCS4231;
        private Func<byte, byte, bool> write8253;
        private int sdm = 0;

        // FM音源接続状態
        // 0 無し
        // 1 YM2203
        // 2 YM3438
        // 3 YM2608+ADPCM
        // 4 YM2608+WSS
        // 5 YM2608+86B
        private int[][] connectFMDevice = new int[][]{
            new int[]{
                0, // 0x088～  無し
                4, // 0x188～  98CanBe(YM2608+WSS) (OPNA系(3,4,5)はかならずここ(0x188)へ定義)
                2  // 0x288～  YM3438
            },
            new int[]{
                0, // 0x088～  無し
                3, // 0x188～  音美ちゃん(YM2608+ADPCM) (OPNA系(3,4,5)はかならずここ(0x188)へ定義)
                2  // 0x288～  YM3438
            }
        };

        private byte[] FM_Adr = new byte[6];
        private byte[][] FM_Reg = new byte[6][];

        public Pc98(Work work, x86Register reg,
            Action<ChipDatum> writeOPNAP,
            Action<ChipDatum> writeOPN2P,
            Action<ChipDatum> writeCS4231,
            Func<byte, byte> readCS4231,
            Func<byte, byte, bool> write8253,
            int soundDeviceMode = 0)
        {
            this.work = work;
            this.reg = reg;
            this.writeOPNAP = writeOPNAP;
            this.writeOPN2P = writeOPN2P;
            this.writeCS4231 = writeCS4231;
            this.readCS4231 = readCS4231;
            this.write8253 = write8253;
            this.sdm = soundDeviceMode;

            for (int i = 0; i < FM_Reg.Length; i++)
            {
                FM_Reg[i] = new byte[256];
            }
        }

        public byte InportB(int dx)
        {

            //割り込みコントローラ
            if (dx == 2)
            {
                return readCS4231(5);// IMR
            }

            //Log.WriteLine(LogLevel.TRACE, "IN-PORT Adr:{0:X04}", dx);
            byte m = (byte)(dx >> 8);
            byte l = (byte)dx;

            if (m == 0)
            {
                if (l == 0x42)
                {
                    //ステータスポート
                    //	bit 5: MOD	システムクロック
                    //1 = 8MHz(タイマクロック2.0MHz)
                    //0 = 5 / 10MHz(タイマクロック2.5MHz)
                    return 0x20;//8MHz系マシン
                }
            }

            if (m == 0xa4)
            {
                //機種判定?
                if (l == 0x60)
                {
                    if (connectFMDevice[sdm][1] == 4) return 0x80;//PC9821Cx
                    if (connectFMDevice[sdm][1] == 5) return 0x40;//86B
                    return 0;
                }
                else if (l == 0x68)
                {
                    //86BPCM FIFO制御
                    return 0;
                }

            }

            //CS4231関連
            if (m == 0x0f)
            {
                if (l == 0x40)
                {
                    return readCS4231(4);// work.cs4231.ReadReg(4);
                }
                if (l == 0x44)
                {
                    //bit7:1=初期化中 0=初期化完了
                    return readCS4231(0);// work.cs4231.ReadReg(0);
                }
                if (l == 0x45)
                {
                    return readCS4231(1);// work.cs4231.ReadReg(1);
                }
                if (l == 0x46)
                {
                    return readCS4231(2);// work.cs4231.ReadReg(2);
                }
            }

            //FM音源のポートを読みたいのかも?
            {
                if (m > connectFMDevice[sdm].Length - 1) return 0;
                if (connectFMDevice[sdm][m] == 0) return 0;
                else if (connectFMDevice[sdm][m] == 1)//YM2203 だぜ
                {
                    if (l == 0x88)
                    {
                        return 0;// (byte)work.timerOPNA1.StatReg;
                    }
                    if (l == 0x8a)
                    {
                        //ssg は読み出せる
                        if (FM_Adr[m] < 0x10) return FM_Reg[m][FM_Adr[m]];

                        return 0;
                    }
                }
                else if (connectFMDevice[sdm][m] == 2)//YM3438 だぜ
                {
                    if (l == 0x88)
                    {
                        return 0xff;
                    }
                    if (l == 0x8a)
                    {
                        //ssg は読み出せない
                        //if (FM_Adr[m] < 0x10) return FM_Reg[m][FM_Adr[m]];

                        return 0;
                    }
                }
                else if (connectFMDevice[sdm][m] == 3//YM2608+ADPCM だぜ
                    || connectFMDevice[sdm][m] == 4//YM2608+WSS だぜ
                    || connectFMDevice[sdm][m] == 5//YM2608+86B だぜ
                    )
                {
                    if (l == 0x88)
                    {
                        return (byte)(work.timerOPNA1.StatReg | 0x80);
                    }
                    else if (l == 0x8a)
                    {

                        //私は2608です
                        if ((byte)lastPort == 0x88 && lastData == 0xff) return 1;

                        //ssg は読み出せる
                        if (FM_Adr[m] < 0x10) return FM_Reg[m][FM_Adr[m]];

                        return 0;
                    }
                    else if (l == 0x8c)
                    {
                        return 0x88;//bit7 busy bit3 pcm
                    }
                    else if (l == 0x8d)
                    {
                        return 0x00;//謎ポート 3438の判定に使用(bit7 が立っているとYM3438ではないと判定される)
                    }
                }
            }

            return 0;
        }

        public void OutportB(int dx, byte al)
        {
            //Log.WriteLine(LogLevel.TRACE, "OUT-PORT Adr:{0:X04} Dat:{1:X02}", dx, al);

            byte m = (byte)(dx >> 8);
            byte l = (byte)dx;

            if (m == 0x00)
            {
                if (dx == 0x00)
                {
                    //割り込みコントローラ
                    if (al == 0x20)
                    {
                        //EOI送出
                    }
                    return;
                }
                else if (dx == 0x02)
                {
                    //割り込みコントローラ
                    ChipDatum cd = new ChipDatum(1, 2, al, 0, work.crntMmlDatum);
                    writeCS4231(cd);// IMR
                    //IMR = al;
                    return;
                }
                else if (l == 0x5)
                {
                    ChipDatum cd = new ChipDatum(1, 5, al, 0, work.crntMmlDatum);
                    writeCS4231(cd);// work.dma.WriteReg(l, al);
                    return;
                }
                else if (l == 0x7)
                {
                    ChipDatum cd = new ChipDatum(1, 7, al, 0, work.crntMmlDatum);
                    writeCS4231(cd);// work.dma.WriteReg(l, al);
                    return;
                }

                if (dx == 0x15)
                {
                    //DMA関連
                    return;
                }
                if (dx == 0x17)
                {
                    //DMA関連
                    return;
                }
                if (dx == 0x19)
                {
                    //DMA関連
                    return;
                }
                if (dx == 0x5f)
                {
                    //たぶんウエイト
                    return;
                }

                if (dx >= 0x71 && dx <= 0x77)
                {
                    write8253((byte)dx, al);
                    return;
                }
            }

            //CS4231関連
            if (m == 0x0f)
            {
                if (l == 0x40)
                {
                    ChipDatum cd = new ChipDatum(0, 4, al, 0, work.crntMmlDatum);
                    writeCS4231(cd);
                    //work.cs4231.WriteReg(4, al);
                    return;
                }
                if (l == 0x44)
                {
                    ChipDatum cd = new ChipDatum(0, 0, al, 0, work.crntMmlDatum);
                    writeCS4231(cd);
                    //work.cs4231.WriteReg(0, al);
                    return;
                }
                if (l == 0x45)
                {
                    ChipDatum cd = new ChipDatum(0, 1, al, 0, work.crntMmlDatum);
                    writeCS4231(cd);
                    //work.cs4231.WriteReg(1, al);
                    return;
                }
                if (l == 0x46)
                {
                    ChipDatum cd = new ChipDatum(0, 2, al, 0, work.crntMmlDatum);
                    writeCS4231(cd);
                    //work.cs4231.WriteReg(2, al);
                    return;
                }
            }

            if (m == 0xa4)
            {
                if (l == 0x60)
                {
                    //bit 1: YM2608(OPNA)マスク設定
                    //       0 = YM2608(OPNA)をマスクしない
                    //       1 = YM2608(OPNA)をマスクする
                    //       * 1を設定するとOPNAが切り放される
                    //bit 0: YM2608(OPNA)拡張部分機能
                    //       0 = YM2203(OPN)相当部分のみ使用する
                    //       1 = YM2608(OPNA)拡張部分も使用する
                    return;
                }
                else if (l == 0x6c)//86PCM FIFO入出力
                {
                    _86PCM_FIFO = al;
                    return;
                }
                else
                {

                }
            }

            lastPort = dx;
            lastData = al;

            if (m > connectFMDevice[sdm].Length - 1) return;

            if (l == 0x88) FM_Adr[m * 2] = al;
            else if (l == 0x8a)
            {
                FM_Reg[m * 2][FM_Adr[m * 2]] = al;
                if (m == 1)
                {
                    ChipDatum dat = new ChipDatum(0, FM_Adr[m * 2], al, 0, work.crntMmlDatum);
                    writeOPNAP(dat);
                }
                else
                {
                    ChipDatum dat = new ChipDatum(0, FM_Adr[m * 2], al, 0, work.crntMmlDatum);
                    writeOPN2P(dat);

                }
            }
            else if (l == 0x8c)
            {
                FM_Adr[m * 2 + 1] = al;
            }
            else if (l == 0x8e)
            {
                FM_Reg[m * 2 + 1][FM_Adr[m * 2 + 1]] = al;
                if (m == 1)
                {
                    ChipDatum dat = new ChipDatum(1, FM_Adr[m * 2 + 1], al, 0, work.crntMmlDatum);
                    writeOPNAP(dat);
                }
                else
                {
                    ChipDatum dat = new ChipDatum(1, FM_Adr[m * 2 + 1], al, 0, work.crntMmlDatum);
                    writeOPN2P(dat);
                }
            }

        }

        public void OutportBDummy(int dx)
        {
            byte m = (byte)(dx >> 8);
            byte l = (byte)dx;
            if (l == 0x88)
            {
                ChipDatum dat = new ChipDatum(-1, -1, -1, 0, work.crntMmlDatum);
                if (m == 1)
                {
                    writeOPNAP(dat);
                }
                else
                {
                    writeOPN2P(dat);
                }
            }

        }

        public void OutportC4231_Adrs(byte channel, int index, ushort val)
        {
            ChipDatum cd;//adrs=0
            cd = new ChipDatum(2, channel * 5 * 2 + 0 * 2 + index, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
            cd = new ChipDatum(2, channel * 5 * 2 + 0 * 2 + index, (byte)(val >> 8), 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public void OutportC4231_Cnt(byte channel, int index, ushort val)
        {
            ChipDatum cd;//cnt=1
            cd = new ChipDatum(2, channel * 5 * 2 + 1 * 2 + index, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
            cd = new ChipDatum(2, channel * 5 * 2 + 1 * 2 + index, (byte)(val >> 8), 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public void OutportC4231_Freq(byte channel, int index, ushort val)
        {
            ChipDatum cd;//freq=2
            cd = new ChipDatum(2, channel * 5 * 2 + 2 * 2 + index, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
            cd = new ChipDatum(2, channel * 5 * 2 + 2 * 2 + index, (byte)(val >> 8), 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public void OutportC4231_Pan(byte channel, int index, ushort val)
        {
            ChipDatum cd;//pan=3
            cd = new ChipDatum(2, channel * 5 * 2 + 3 * 2 + index, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
            cd = new ChipDatum(2, channel * 5 * 2 + 3 * 2 + index, (byte)(val >> 8), 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public void OutportC4231_Volume(byte channel, int index, ushort val)
        {
            ChipDatum cd;//volume=4
            cd = new ChipDatum(2, channel * 5 * 2 + 4 * 2 + index, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
            cd = new ChipDatum(2, channel * 5 * 2 + 4 * 2 + index, (byte)(val >> 8), 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public void OutportC4231_Freq2(ushort val)
        {
            ChipDatum cd;//freq=200
            cd = new ChipDatum(2, 200, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
            cd = new ChipDatum(2, 200, (byte)(val >> 8), 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public void OutportC4231_Jump1(ushort val)
        {
            ChipDatum cd;//jump1=201
            cd = new ChipDatum(2, 201, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
            cd = new ChipDatum(2, 201, (byte)(val >> 8), 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public void OutportC4231_Jump2(byte val)
        {
            ChipDatum cd;//jump2=202
            cd = new ChipDatum(2, 202, (byte)val, 0, work.crntMmlDatum); writeCS4231(cd);
        }

        public byte[] ReadOpnaPCMMemory(int port34, int v1, int v2)
        {
            return new byte[] { (byte)'M', (byte)'P', (byte)'2', (byte)'3' };
        }

        internal void Int21(byte[] cusbuff)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Open File(実際は存在確認のみ)
        /// </summary>
        public void Int21_3d(string path)
        {
            Log.WriteLine(LogLevel.DEBUG, "INT21H AH:0x3d Open File:{0}", path);

            if (File.Exists(path))
            {
                reg.carry = false;//success
                reg.ax = 0;//Filehandle
                return;
            }

            reg.carry = true;
        }
    }
}
