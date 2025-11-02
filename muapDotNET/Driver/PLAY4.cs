using muapDotNET.Common;
using muapDotNET.Driver;
using musicDriverInterface;
using System;

namespace muapDotNET.Driver
{
    public class PLAY4
    {
        public ushort fadesave = 160;

        public NAX nax = null;
        public x86Register r = null;
        public Work work = null;
        public ushort[] labelPtr = null;// new ushort[40 * 17];
        public int[] labelPassCnt = new int[40 * 17];

        public PLAY4(NAX nax, Work work, ushort[] labelPtr)
        {
            this.nax = nax;
            r = nax.reg;
            SetJumptable1();
            SetJumptable2();
            SetJumptable3();
            this.work = work;
            this.labelPtr = labelPtr;
            for(int i = 0; i < labelPassCnt.Length; i++)
            {
                labelPassCnt[i] = -1;
            }
        }

        //自己書き換え
        public byte stiof1 = 0;
        public byte intm1 = 0;
        public byte intm2 = 0;
        public byte freq1 = 0b011;
        public ushort freq2 = 0x987;
        public byte freq3 = 0b0010;
        public byte[] naxad1 = new byte[9];
        public byte[] naxad2 = new byte[9];
        public ushort port1 = 0x88;//OPNA portA 
        public ushort port11 = 0x88;//OPNA portA 
        public ushort port12 = 0x88;//OPNA portA 
        public ushort port18 = 0x88;//OPNA portA 
        public ushort port19 = 0x88;//OPNA portA 
        public ushort port3 = 0x18c;
        public ushort port31 = 0x18c;
        public ushort port32 = 0x18c;
        public ushort port33 = 0x18c;
        public ushort port34 = 0x18c;
        public ushort port35 = 0x18c;
        public ushort port36 = 0x18c;
        public ushort port37 = 0x18c;
        public ushort port5 = 0x788;//OPN2 portA 
        public ushort port7 = 0x78c;//OPN2 portB
        public ushort hadr7 = 0;
        public ushort hadr8 = 0;
        public ushort hadr9 = 0;
        public ushort hadr10 = 0;
        public ushort hadr11 = 0;
        public ushort sign1 = 0;
        public ushort sign1_2 = 0;
        public byte sign1_4 = 0;
        public ushort sign2 = 0;
        public ushort sign3_1 = 0;
        public ushort sign4_1 = 0;
        public byte outdata1_ = 0;
        public byte outdata2_ = 0;
        public byte outdata3_ = 0;
        public byte outdata4_ = 0;
        public ushort jump1_ = 0;
        public ushort jump2_ = 0;
        public ushort dsp_exit_ = 0;
        //public ushort test_lop1_ = 0;
        //public ushort test_lop2_ = 0;
        //public ushort test_entry3_ = 0;
        public uint farjmp1_ = 0;
        public uint farjmp2_ = 0;
        public ushort panl1_ = 0xc008;//or al,al
        public ushort panl2_ = 0xc008;//or al,al
        private ushort level1_ = 0x007f;
        private byte level2_ = 0x7f;
        private byte level3_ = 0x7f;

        //スタックによる戻り先変更対策フラグ
        private bool rechannelFlg = false;
        private bool recovFlg = false;
        private bool recovwFlg = false;
        private bool initia0Flg = false;
        private bool another1Flg = false;
        private bool another_ssg1Flg = false;

        public void check_busy(int dx)
        {
            //何もやらない
        }



        //;
        //;	The Packen Software YM2608/3438 music player module V9.32
        //; copyright(c) 1987,1989-1995 by Packen Software[mar.24.1996]
        //;			and Special thanks to S.Tabata(PCM Table)
        //;

        //	include MUAP.INC
        private const byte OBJTOP = 0x2a;

        private const byte TRD = 0x20;
        private const byte SAMPLE_BIT = 0b0010;
        private const ushort O4CDATA = 0x987;
        //SAMPLE_DATA = 011b
        //;  44.10  33.08  22.05  16.54  11.03  8.27  5.52  4.13
        //;   000b   001b   010b   011b   100b  101b  110b  111b
        //;                        987h   65ah  4c3h

        //;	演奏用ワークエリア

        private byte[] chtbl2 = new byte[]{ 0, 1, 2, 3, 4, 5, 6, 7	// @chコマンドによって変更するチャネル(1～17)
                                 ,8, 9,10,11,12,13,14,15,16 };
        private ushort fadedata = 320;// フェードアウト加算値
        private ushort spsave1 = 0;
        private ushort sssave1 = 0;
        //;	VisualPlay用ワーク3
        private ushort fade_count = 0;// フェードアウトレベルカウンタ

        /// <summary>
        /// b0 = EMSマップ, b1 = 演奏中, b2 = @dataDisp
        /// b3 = TracePlay中, b4 = FMintIn, b5 =PCMintIn
        /// b7 = タイマ最入
        /// </summary>
        private byte mapflag = 0;
        private byte[] maxlen = new byte[] { 0, 0, 0, 0 };// 最大音長
        private byte[] con_data = new byte[] { 8, 8, 8, 8, // FMのコネクションによる出力オペのビット値
                                   10,14,14,15 };
        private byte[] out_data = new byte[] { 0, 8, 4, 12 };// FMのアドレスとオペの順番
        private byte realch = 0;// 実際のチャネル番号
        private byte fifo_exec = 0;
        //	   even
        public byte[] pcmtable = new byte[(NAX.MAXPCM + 1) * 2];// PCM音色管理テーブル(@0～89)//熊：多分89ではなく99のtypo
        public byte[] ssgtable = new byte[21 * 2];// SSGPCMの音色管理テーブル(@0～19)

        //fadesave dw	160		; フェードアウトカウンタ保存用
        //skipbyte   db	0,0,0,0,0,8,0,3	; FF-F8 の制御コードのバイト数-1
        //       db	3,1,2,2,0,3,2,1	; F7-F0
        //       db	1,2,1,1,1,2,2,0	; EF-E8
        //       db	2,26,0,4,4,1,0,0; E7-E0
        //       db	0,1,1,3,3,3,6,1	; DF-D8
        //       db   1,1,2,4,4,0,0,1	; D7-D0
        //       db	1,0		; CF-CE
        public byte init_cnt = 0;// ループ回数

        //;	DMA,FIFO用バッファ

        public byte dma_chan = 3;
        public ushort dma_adr = 0;
        public ushort dma_bank = 0;
        public ushort dma_count = 0;
        public ushort dma_data = 0;
        public ushort fifoptr1 = 0;
        public ushort fifoend1 = (ushort)(NAX.FIFO_SIZE * 2);
        public ushort fifoptr2 = (ushort)(NAX.FIFO_SIZE * 2);
        public ushort fifoend2 = (ushort)(NAX.FIFO_SIZE * 4);
        public ushort fifofin = (ushort)(NAX.FIFO_SIZE * 2 * NAX.MAXBUF);

        //;	演奏中のデータバッファ
        //;	VisualPlay用ワーク1

        //       even
        private byte[] data_buff = new byte[]// ワークエリアの番地
        {
            48,0,48,0,48,0,48,0,
            48,0,48,0,48,0,48,0,
            48,0,48,0,48,0,48,0,
            48,0,48,0,48,0,48,0,
            48,0, // 音長カウンタ,割合データ

            110,48,110,48,110,48,110,48,
            110,48,110,48,110,48,110,48,
            110,48,110,48,110,48,110,48,
            110,48,110,48,110,48,110,48,
            110,48, // 音量データ,音長データ

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, // 現在の演奏番地 熊:本来はdup(?)

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊: TS , DY , TI

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, // 出力周波数データ 熊:FD

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:RA LK

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:SC DC PC SD

            0xc0,0,0xc0,0,0xc0,0,
            0,0,0,0,0,0,
            0xc0,0,0xc0,0,0xc0,0,0xc0,0,
            0xc0,0,0xc0,0,0xc0,0,0xc0,0,
            0xc0,0,0xc0,0,0xc0,0, //熊:PN DV LV PT

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:SY

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:LF LL

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:FC

            0,0x80,0,0x80,0,0x80,0,0x80,
            0,0x80,0,0x80,0,0x80,0,0x80,
            0,0x80,0,0x80,0,0x80,0,0x80,
            0,0x80,0,0x80,0,0x80,0,0x80,
            0,0x80,//熊:LD LI

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:SV AR AC FB

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:SE LB

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:LR RC

            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,
            0,0, //熊:LA PM

        };

        private const int LC = 0;
        private const int RS = 1;
        private const int VS = 34;
        private const int LS = 35;
        private const int AD = 68;
        private const int TS = 102;// 音色番号(CH1-3,7-17)
        private const int DY = 102;// ディケイデータ(CH4-6)
        // b0=スラー,b1=タイ,b2=ノコギリLFO,b3=oneshot
        // b4=加算/減算中,b5=Amd/Pmd,b6=LFO停止/動作
        // b7=SYNCoff/on
        private const int TI = 103;
        private const int FD = 136;

        private const int RA = 170;// スタックオフセット
        private const int LK = 171;// LFOインクリメントの最終値

        private const int SC = 204 - 6;// スタートディケイカウンタ(CH4-6)
        private const int DC = 204;// ディケイカウンタ(CH4-6)
        private const int PC = 204 + 6;// エンベロープモード[0 / 10h] (CH4-6)
        private const int SD = 205 + 6;// スタートディケイデータ(CH4-6)

        private const int PN = 238;// PAN & Hard LFO(CH1-3,7-9,11,12-17)
        private const int DV = 239 - 6;// ディケイレベル(CH4-6)
        private const int LV = 238;// 最終音量(CH4-6)
        private const int PT = 239;// エンベロープ形状(CH4-6)

        private const int SY = 272;// デバッグ情報

        private const int LF = 306;// LFOスピード(-128～127)
        private const int LL = 307;// LFOレベル(0～127)

        private const int FC = 340;// LFOカウンタ

        private const int LD = 374;// LFOディレイ(0～255)
        // b0～b3 = LFOインクリメント
        // b4～b6 = LFOレベルベース
        // b7 = 休符ビット
        private const int LI = 375;

        private const int SV = 408 - 6;// スタート音量(CH4-6)
        private const int AR = 408;// アタックレイト(CH4-6)
        private const int AC = 408 + 6;// アタックカウンタ(CH4-6)
        private const int FB = 409;// 周波数データ小数桁

        private const int SE = 442;// システムディチューン
        private const int LB = 443;// LFOスピードベース(b0,1),鍵盤色(b5～7)

        private const int LR = 476;// LFOくり返し回数
        private const int RC = 477;// LFOくり返しカウンタ

        private const int LA = 510;// LFOディレイ継続値
        // b0～4 : PCMモードで使用
        // b7 : キーオンしたフラグ
        private const int PM = 511;

        //; VisualPlay用ワーク2
        //bit_status label byte
        private byte timercnt = 0;// #0  定期周回カウンタ
        private byte ssgpcm = 0;// #1  SSG/PCMモード
        private byte mixsave = 0xb8;// #2  SSG MIXER DATA
        private byte noisef = 0;// #3  ノイズ周波数
        private byte[] tempo = new byte[] { 0x00, 0x02, 0x00, 0x02 };//#4  テンポタイマー値
        //	   dw	200h		;     ダミー

        private byte[] rhytbl = new byte[] { 0, 0, 0, 0, 0, 0 };// #8  リズムテーブル
        private ushort[] playlen = new ushort[] { 0, 0 };// #14 演奏された音長
        private byte playcont = 0;// #18 全体ループ回数
        private ushort skip_data1 = 0;// #19 チャネル停止フラグ(b0 to b16)
        private byte skip_data2 = 0;
        //init_skip1 dw	0		; #22 skip_data比較用
        //init_skip2 db	0
        private ushort ch_mask1 = 0;// #25 チャネル発音停止用
        private byte ch_mask2 = 0;
        private ushort wait_flg1 = 0;// #28 揃え用(F3コマンド)
        private byte wait_flg2 = 0;
        private ushort keymask1 = 0;// #31 鍵盤表示しないよフラグ
        private byte keymask2 = 0;
        private ushort song_flg1 = 0;// #34 AH=8 使用フラグ(for NA)
        private byte song_flg2 = 0;
        private ushort shflag1 = 0;// #37 効果音使用中フラグ
        private ushort shflag2 = 0;
        private byte[] comdataBuf = new byte[73];// #40 歌詞データ
        private string comdata = "";
        private byte comlength = 0x0ff;// #113 色変わり歌詞の桁数
        private byte dsp_mode = 0;// #114 DSPモード
        private byte dsp_level = 0;// #115 DSPレベル
        private byte init_flg = 0;// b1 = @fo
        private byte end_flug = 0;// 再演奏フラグ
        private byte codem1 = 0;// コード演奏モード指定(ch3)
        private byte codem2 = 0;//	〃	      (ch14)
        private byte exit_flg = 0;
        private byte tacount = 0;// Timer-A 分周カウンタ
        // b0 = 9821PCM録音モード, b1 = 録音バッファ超過
        // b2 = DacSample発音, b3 = 再生終了
        // b4 = 再生割り込み入る, b5 = 真の番地格納
        private byte pcmrecmode = 0;

        //PADR	= 0
        //PCNT	= 4
        //PFREQ	= 8
        //PPAN	= 12
        //PVOL	= 16
        private const int PWORKE = 1;//18;

        //	align	4
        private class Pcm0work
        {
            public ushort[] pcm0adrs = new ushort[] { 0, 0 };// 拡張PCM発音開始番地・EMSページ
            public ushort[] pcm0cnt = new ushort[] { 0, 0 };// 発音用減算カウンタ*4
            public ushort[] pcm0freq = new ushort[] { 0, 0 };// 周波数
            public ushort[] pcm0pan = new ushort[] { 0, 0 };// right+leftのパンandデータ(0/FFFF)
            public ushort[] pcm0vol = new ushort[] { 0, 0 };// 音量
        }
        private Pcm0work[] pcm0work =new Pcm0work[]{
            new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
            new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
            new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
            new Pcm0work(),new Pcm0work(),new Pcm0work(),new Pcm0work(),
            new Pcm0work()
        };
        // PCM#2
        // PCM#3
        // PCM#4
        // PCM#5
        // PCM#6
        // PCM#7
        // PCM#8
        // PCM#9
        // PCM#10
        // PCM#11
        // PCM#12
        // PCM#13
        // PCM#14
        // PCM#15
        // PCM#16

        //pcmrseg    dw	0		; PCM録音データ格納セグメント
        private ushort[] pcmNadrs = new ushort[]{
            0, 0, 0, //ch1
            0, 0, 0, //ch2
            0, 0, 0  //ch3
        };// SSGPCM発音開始番地・終了番地・周波数
        public byte[] loopcnt = new byte[17 * 16 + 0xff];// X value*10nest + 17ch 6nest.

        //;============================
        //;	演奏開始ルーチン
        //;============================

        public void music_start()
        {
            music_stop();

            ushort axbk = r.ax;
            ushort bxbk = r.bx;
            ushort cxbk = r.cx;
            ushort dxbk = r.dx;

            ushort esbk = r.es;

            r.ax = 0xbf10;// リズム音源全ダンプ
            outdata1();
            r.al = 0;
            mode_change();// DSP off
            r.al = 8;
            set_buffer_num();// DSP時定数設定
            init_work();
            init_cnt = 0;

            r.es = esbk;

            r.dx = dxbk;
            r.cx = cxbk;
            r.bx = bxbk;
            r.ax = axbk;

            music_again();
        }

        //==========================
        //	再演奏ルーチン
        //==========================
        public void music_again(object o=null)
        {
            ushort axbk = r.ax;
            ushort bxbk = r.bx;
            ushort cxbk = r.cx;
            ushort dxbk = r.dx;
            ushort sibk = r.si;
            ushort dibk = r.di;

            r.ax = 0;
            fade_count = 0;

            r.ch = 0;
            r.si = 0;//ofs:data_buff
            //magain1:
            do
            {
                r.al = data_buff[r.si + TS];
                tone2608(data_buff);// 音色の再設定(ch1～3)

                r.si += 2;
                r.ch++;
            } while (r.ch < 3);

            r.ch = 6;
            r.si = 6 * 2;//ofs:data_buff+6*2
            //magain2:
            do
            {
                r.al = data_buff[r.si + TS];
                tone2608(data_buff);// 音色の再設定(ch6～9)
                r.si += 2;
                r.ch++;
            } while (r.ch < 9);

            r.ch = 11;
            r.si = 11 * 2;//ofs:data_buff+11*2
            //magain3:
            do
            {
                // 拡張PCMチェック
                if (!check_86pcm())
                {
                    //magain4:
                    r.al = data_buff[r.si + TS];
                    tone2608(data_buff);// 音色の再設定(ch12～17)
                }
                else
                {
                    r.al = data_buff[r.si + VS];
                    volchgr(data_buff);// 音量の再設定
                }
                //magain5:
                r.si += 2;
                r.ch++;
            } while (r.ch < 17);

            r.ch = 9;
            r.si = 9 * 2;//ofs:data_buff+9*2
            r.al = data_buff[r.si + VS];
            volchgr(data_buff);// リズムの音量復活

            r.si += 2;
            r.ch++;
            r.al = data_buff[r.si + VS];
            volchgr(data_buff);// PCM音量復活

            ushort esbk = r.es;
            r.es = 0;//ofs:object
            r.ax = (byte)nax.objBuf[nax._object[0]][r.es].dat;
            r.es = esbk;

            // V6以前の演奏ファイル
            // 演奏データのヘッダが正常か?
            if (r.ax == 0x26 || r.ax == OBJTOP)
            {
                //objdata_ready:
                if (check_86pcm())// 拡張86PCM?
                {
                    fifo_start();
                }
                //pt1:
                r.al = nax.pc98.InportB(0xa);// read IMR
                //setand:
                r.al &= 0xef;// clear IR12 mask
                //pt2:
                nax.pc98.OutportB(0xa, r.al);// write IMR
                set_intimer();// 内蔵タイマの初期化
                mapflag |= 2;// 演奏中フラグ
                set_timer();
            }

            //objdata_err:
            r.ax = axbk;
            r.bx = bxbk;
            r.cx = cxbk;
            r.dx = dxbk;
            r.si = sibk;
            r.di = dibk;

            // 割り込みマスククリア
        }

        //･･････････････････････････････
        //	86B-PCMの演奏開始
        //･･････････････････････････････

        private void fifo_start()
        {
            ushort axbk = r.ax;
            ushort bxbk = r.bx;
            ushort cxbk = r.cx;
            ushort dxbk = r.dx;
            ushort dibk = r.di;
            ushort sibk = r.si;

            ushort esbk = r.es;

            r.es = nax.fifoseg;
            r.di = 0;

            //sign4:
            r.ax = 0x8080;
            r.cx = (ushort)(NAX.FIFO_SIZE * NAX.MAXBUF);

            do
            {
                work.fifoBuf[r.di + 0] = r.al;
                work.fifoBuf[r.di + 1] = r.ah;
                r.cx--;
            } while (r.cx > 0);// DMAバッファの初期化

            pcm_stop();// PCMを停止
            fifo_int_off();// FIFO割り込み禁止
            if (check_wsspcm())//trueがwsspcmであることを示す
            {
                fifo_start_wss();
            }
            else
            {
                throw new NotImplementedException();
                //init_fifo();// FIFOデータの初期化
                //reset_fifo();// FIFOリセット
                put_fifo_data();
                change_buffer();
                r.ah = (byte)(NAX.FIFO_SIZE / 64 - 1);
                //set_fifo_size();// FIFO割り込みタイミング設定
                clear_fint();
                fifo_int_on();
                set_volume();
            }

            //exit_start:
            r.es = esbk;

            r.ax = axbk;
            r.bx = bxbk;
            r.cx = cxbk;
            r.dx = dxbk;
            r.di = dibk;
            r.si = sibk;
        }

        private void fifo_start_wss()
        {
            setup_wss();// WSS初期化
            r.bx = (ushort)(NAX.FIFO_SIZE - 1);
            set_dmabase();
            put_fifo_data();
            change_buffer();
            put_fifo_data();
            change_buffer();
            clear_fint();
            fifo_int_on();
            set_volume();

            dma_data = (ushort)((NAX.FIFO_SIZE + 16) * 2);
            program_dma();
            dma_data = (ushort)(NAX.FIFO_SIZE * 2);
            change_buffer();
            r.al = nax.pc98.InportB(2);
            r.al &= 0xf7;// INT0B割り込み許可
            nax.pc98.OutportB(2, r.al);// write IMR
            pcm_start();
        }

        // WSSの初期化

        private void setup_wss()
        {
            nax.reg.ax = 0x0cc0;
            put_wss();//MODE2フラグをセット
            nax.reg.ax = 0x1080;
            put_wss();// DAC有効,Timer無効,0dbモード
            nax.reg.ax = 0x1100;
            put_wss();// HPF無効

            //nax.reg.ax = 0x1acf;
            //put_wss();// MONO入出力mute
            //nax.reg.ax = 0x1200;
            //put_wss();// LEFT LINE入力mute off
            //nax.reg.ax = 0x1300;
            //put_wss();// RIGHT LINE入力mute off

            nax.reg.ax = 0x0406;
            put_wss();// YMF288 left出力
            nax.reg.ax = 0x0506;
            put_wss();// YMF288 right出力
            nax.reg.ax = 0x0dfd;
            put_wss();// ループバック無効

            nax.reg.ax = 0x4810;// stereo
            //freq3:
            nax.reg.al |= (byte)freq3;// SAMPLE_BIT;

            put_wss();// Fs and Playback Data Format
            wait_wss();// レート変更時の同期をまつ
            put_wss();// Fs and Playback Data Format
            wait_wss();// レート変更時の同期をまつ
        }

        //CS4231のINITフラグが下りるのをまつ
        private void wait_wss()
        {
            ushort cx = 0;
            ushort dx = 0x0f44;

            //read_stat_lop:
            do
            {
                byte al = nax.pc98.InportB(dx);
                if ((al & 0x80) == 0) break;
                cx--;
            } while (cx > 0);

            //wait_wss_end:
        }

        //	割り込み発生単位の設定

        private void set_dmabase()
        {
            r.ah = 0x0e;
            r.al = r.bh;
            put_wss();// DMAベースレジスタ上位8bit
            r.ah = 0x0f;
            r.al = r.bl;
            put_wss();// DMAベースレジスタ下位8bit
        }

        //------------------------------------
        //	内蔵タイマの周波数設定
        //------------------------------------

        private void set_intimer()
        {
            //intm1:
            byte dl = 0x33;//dx = 0x0133;
            byte dh = 0x01;
            if (intm1 != 0) 
            {
                dl=0x9a;
            }
            byte al = nax.pc98.InportB(0x42);
            if ((al & 0x20) != 0)// システムクロックは10MHz?
            {
                //intm2:
                dl = 0xf9;//dx = 0x00f9;
                dh = 0x00;
                if (intm2 != 0)
                {
                    dl=0x7d;
                }
            }
            //clock10m:
            nax.pc98.OutportB(0x77, 0x36);
            //  jmp	$+2
            nax.pc98.OutportB(0x71, dl);// 8253#0カウンタを8KHzに設定
            //	jmp	$+2
            nax.pc98.OutportB(0x71, dh);
        }

        //;--------------------------------------
        //;	内蔵タイマ割り込みエントリ
        //;--------------------------------------

        public void int08ent()
        {
            ushort axbk = r.ax;
            ushort dxbk = r.dx;
            ushort sibk = r.si;
            ushort dsbk = r.ds;
            ushort esbk = r.es;

            r.dx = 0;
            r.ax = r.cs;
            r.ds = r.ax;
            r.es = nax.pcmseg;
            r.ax = 0x8000;// AH = 各チャネル非発音フラグ

            r.si = pcmNadrs[0];// SI = PCM発音ポインタ
            if (r.si < pcmNadrs[1])
            {
                r.ax = pcmNadrs[2];// AX = PCMカウンタ
                r.ax += 0x1dd;// SSGのO4Cの値を加算
                if ((short)r.ax >= 0)
                {
                    r.dx = (ushort)(data_buff[FD + 6] + data_buff[FD + 7] * 0x100);
                    if (r.dx != 0)
                    {
                        //pcm1inc:
                        int ans = (short)r.ax;
                        do
                        {
                            r.si++;
                            ans -= (ushort)r.dx;
                        } while (ans >= 0);
                        pcmNadrs[0] = r.si;
                        r.ax = (ushort)ans;
                    }
                }
                //pcm1skip:
                pcmNadrs[2] = r.ax;// PCMカウンタを保存
                r.al = nax.pcmBuff[r.si];// AL = PCMデータ
                //buf.Add(r.al);
                //Log.writeLine(LogLevel.ERROR, string.Format("{0:x04}",r.al));
                r.ax = (ushort)(sbyte)r.al;

                r.dx = r.ax;// DX = PCMデータ
            }
            //else
            //{
            //    ;
            //    byte[] bbuf = new byte[0x5978 - 0x1a24];
            //    Array.Copy(nax.pcmBuff, 0x1a24, bbuf, 0, 0x5978 - 0x1a24);
            //    File.WriteAllBytes(".\\pcmbuff", bbuf.ToArray());
            //    File.WriteAllBytes(".\\pcmbufffull", nax.pcmBuff);
            //}

            //pcm1end:
            //File.WriteAllBytes(".\\test", nax.pcmBuff);
            r.si = pcmNadrs[3 + 0];
            if (r.si < pcmNadrs[3+1])
            {
                //    seges
                r.al = nax.pcmBuff[r.si];
                r.si++;
                pcmNadrs[3+0] = r.si;
                r.ax = (ushort)(sbyte)r.al;
                r.dx += r.ax;
            }
            //pcm2end:

            r.si = pcmNadrs[6+0];
            if (r.si < pcmNadrs[6+1])
            {
                //    seges
                r.al = nax.pcmBuff[r.si];
                r.si++;
                pcmNadrs[6+0] = r.si;
                r.ax = (ushort)(sbyte)r.al;
                r.dx += r.ax;
            }
            else
            {
                //pcm3end:
                if (r.ah == 0x80)
                {
                    // 発音していない
                    //pcm_nouse:
                    stop_intimer();// PCMを使用していないので割り込み停止
                    goto pcm_exit;
                }
            }

            //pcm_using:
            r.ax = r.dx;
            if ((r.ax & 0x8000) != 0)
            {
                //negpcm1:
                if (r.ax < 0xff80) 
                    r.ax = 0xff80;
            }
            else if (r.ax > 0x7f) 
                r.ax = 0x7f;

            //negpcm2:
            r.ax += 0x80;
            r.ax += r.ax;
            r.si = r.ax;
            r.dx = port12;

            //ch1 音量レジスタへセット
            r.al = 8;
            nax.pc98.OutportB(r.dx, r.al);
            r.ax = pcmtbl[r.si/2];
            r.dx += 2;
            nax.pc98.OutportB(r.dx, r.al);

            //ch2 音量レジスタへセット
            r.dx -= 2;
            r.al = 9;
            nax.pc98.OutportB(r.dx, r.al);
            r.dx += 2;
            r.al = r.ah;
            r.al &= 15;
            nax.pc98.OutportB(r.dx, r.al);

            //ch3 音量レジスタへセット
            r.dx -= 2;
            r.al = 10;
            nax.pc98.OutportB(r.dx, r.al);
            r.dx += 2;
            r.al = r.ah;
            r.al >>= 4;
            nax.pc98.OutportB(r.dx, r.al);

        pcm_exit:
            r.al = 0x20;
            nax.pc98.OutportB(0, r.al);

            r.es = esbk;
            r.ds = dsbk;
            r.si = sibk;
            r.dx = dxbk;
            r.ax = axbk;
        }

        //;-------------------------------------------------------------
        //;	8bitリニアPCM → 4bit非直線pcm×3への変換テーブル
        //;-------------------------------------------------------------

        private readonly ushort[] pcmtbl = new ushort[]{
             1 ,    2 ,    3 ,    4 ,    5 ,    6 ,    7 ,  263
        ,  775 ,    8 ,  264 ,  776 ,    9 ,  265 ,  777 , 1033
        , 1289 ,   10 ,  266 ,  778 , 1034 , 1290 , 1546 ,   11
        ,  267 ,  523 ,  779 , 1035 , 1291 , 1547 , 1803 , 1803
        , 1803 ,   12 ,  268 ,  524 ,  780 , 1036 , 1292 , 1548
        , 1804 , 1804 , 2060 , 2060 ,   13 ,  269 ,  525 ,  781
        , 1037 , 1293 , 1549 , 1805 , 5901 ,14093 , 2061 , 6157
        ,14349 , 2317 , 6413 ,10509 ,14605 ,18701 ,22797 ,   14
        ,  270 ,  526 ,  782 , 1038 , 1294 , 1550 , 1806 , 5902
        ,14094 , 2062 , 6158 ,14350 , 2318 , 6414 ,10510 ,14606
        ,18702 ,22798 ,26894 ,30990 ,   15 ,  271 ,  527 ,  783
        , 1039 , 1295 , 1551 , 1807 , 5903 ,14095 , 2063 , 6159
        ,14351 , 2319 , 6415 ,14607 ,18703 ,22799 , 2575 , 6671
        ,14863 ,18959 ,23055 ,27151 , 2831 , 6927 ,11023 ,15119
        ,19215 ,23311 ,27407 ,31503 ,31503 , 3087 , 3087 , 7183
        ,11279 ,15375 ,19471 ,23567 ,27663 ,31759 ,31759 ,35855
        ,35855 ,35855 , 3343 , 7439 ,11535 ,15631 ,19727 ,23823
        ,27919 ,32015 ,32015 ,32015 ,36111 ,36111 ,36111 ,40207
        ,40207 ,40207 , 3599 , 3599 , 3599 , 3599 , 7695 ,11791
        ,15887 ,19983 ,24079 ,28175 ,32271 ,32271 ,32271 ,36367
        ,36367 ,36367 ,40463 ,40463 ,40463 ,40463 ,44559 ,44559
        ,44559 ,44559 , 3855 , 7951 ,12047 ,16143 ,20239 ,24335
        ,28431 ,32527 ,32527 ,32527 ,36623 ,36623 ,36623 ,40719
        ,40719 ,40719 ,40719 ,40719 ,44815 ,44815 ,44815 ,44815
        ,44815 ,44815 ,48911 ,48911 ,48911 ,48911 ,48911 ,48911
        ,48911 ,48911 ,48911 ,48911 ,53007 ,53007 ,53007 ,53007
        ,53007 ,53007 ,53007 ,53007 ,53007 ,53007 ,53007 ,57103
        ,57103 ,57103 ,57103 ,57103 ,57103 ,57103 ,57103 ,57103
        ,57103 ,57103 ,57103 ,57103 ,57103 ,57103 ,57103 ,57103
        ,57103 ,57103 ,61199 ,61199 ,61199 ,61199 ,61199 ,61199
        ,61199 ,61199 ,61199 ,61199 ,61199 ,61199 ,61199 ,61199
        ,61199 ,61199 ,61199 ,61199 ,61199 ,61199 ,61199 ,65295
        };

        //;----------------------------
        //;	内蔵タイマの設定
        //;----------------------------

        private void reset_intimer()
        {
            ushort axbk = r.ax;
            ushort dxbk = r.dx;

            stop_intimer();// 内蔵タイマの割り込み禁止
            r.dx = 0x6000;// 10ms単位に戻す
            r.al = nax.pc98.InportB(0x42);
            if ((r.al & 0x20) != 0)// システムクロックは10MHz?
            {
                r.dx = 0x4e00;
            }

            //熊:0x77  8253(タイマコントローラ) モード設定
            //熊:0x36  #0カウンタ(インターバルタイマ) LSB,MSBのR/W 方形波ジェネレータ 
            r.al = 0x36;
            nax.pc98.OutportB(0x77, r.al);

            r.al = r.dl;
            nax.pc98.OutportB(0x71, r.al); // 8253#0カウンタを100Hzに設定

            r.al = r.dh;
            nax.pc98.OutportB(0x71, r.al); // 8253#0カウンタを100Hzに設定

            r.dx = dxbk;
            r.ax = axbk;
        }

        private void start_intimer()
        {
            r.al = nax.pc98.InportB(2);
            r.al &= 0xfe;// 内蔵タイマの割り込み許可
            nax.pc98.OutportB(2, r.al);
        }

        private void stop_intimer()
        {
            r.al = nax.pc98.InportB(2);
            r.al |= 1; // 内蔵タイマの割り込み禁止
            nax.pc98.OutportB(2, r.al);
        }

        //;===================================
        //;	フェードアウト開始指示
        //;===================================

        private void fade_out()
        {
            init_flg |= 2;// メニューからフェードアウトした
            fade_outm();
        }

        private void fade_outs()
        {
            if ((init_flg & 2) != 0)
            {
                // 既にフェードアウトされているか
                return;
            }
            fade_outm();
        }

        private void fade_outm()
        {
            r.push(r.cx);
            r.ax = (ushort)(tempo[0] + tempo[1] * 0x100);// 現在のテンポ獲得
            uint mans = (uint)(fadesave * r.ax);// FD = (X+1)*8 * 80/(3907*16/(1024-tempo))
            r.dx = (ushort)(mans >> 16);
            r.ax = (ushort)mans;

            r.cx = 49 * 16;

            uint ans = (uint)(((r.dx << 16) + r.ax) / r.cx);
            uint mod = (uint)(((r.dx << 16) + r.ax) % r.cx);
            r.ax = (ushort)ans;
            r.dx = (ushort)mod;

            fadedata = r.ax;// テンポに合った値を格納
            fade_count = 0x100;// フェードアウトカウンタの開始
            r.cx = r.pop();

            //fade_oute:
            return;
        }

        //==================================================
        //	演奏データ初期化
        //	playoffの値に従って次の曲の番地をセット
        //==================================================

        private void init_work()
        {
            ushort esbk = r.es;
            r.ax = 0x1010;// Timer-A,B Enableにする
            outdata2();

            r.ah = 0x80;// PCM Flag Reset
            outdata2();
            r.al = 0;
            tonepcmm();// PCMを@0に設定

            r.cx = 3;
            r.ax = 0xc0b4;// PANを中央にする(OPNA, OPN2C)
            //init_pan1:
            do
            {
                outdata1();// for YM2608
                outdata2();
                outdata3();// for YM3438
                outdata4();
                r.al++;
                r.cx--;
            } while (r.cx > 0);

            r.ax = 0x27;
            outdata3();// YM3438を通常モードにする
            r.ax = 0x0df18;
            r.cx = 6;
            //init_rhythm:
            do
            {
                outdata1();// リズムのパンと音量を設定
                r.al++;
                r.cx--;
            } while (r.cx > 0);

            r.ax = 0x0c001;
            outdata2();// PCM パンの初期化

            r.ax = 0x22;
            outdata1();// ハードLFOの初期化(2608)

            outdata3();// for YM3438
            vol_off();// 全音量カット

            r.es = r.cs;
            r.di = 0;//ofs:data_buff
            r.ax = 0;
            r.cx = (ushort)data_buff.Length;//(topmem - data_buff) / 2;
            do
            {
                data_buff[r.di++] = (byte)r.ax;
                r.cx--;
            } while (r.cx > 0);// バッファをまず全部初期化
            timercnt = 0;
            ssgpcm = 0;
            //mixsave = 0;
            noisef = 0;
            playlen[0] = 0;
            playlen[1] = 0;
            playcont = 0;
            skip_data1 = 0;
            skip_data2 = 0;
            ch_mask1 = 0;
            ch_mask2 = 0;
            wait_flg1 = 0;
            wait_flg2 = 0;
            song_flg1 = 0;
            song_flg2 = 0;
            shflag1 = 0;
            shflag2 = 0;
            comdataBuf = new byte[73];
            comdata = "";
            dsp_mode = 0;
            dsp_level = 0;
            init_flg = 0;
            end_flug = 0;
            codem1 = 0;
            codem2 = 0;
            exit_flg = 0;
            tacount = 0;
            pcmrecmode = 0;

            r.di = PN;//ofs:data_buff+PN
            r.ax = 0xc0;// パン中央
            r.cx = 3;
            do
            {
                data_buff[r.di++] = (byte)r.ax;
                data_buff[r.di++] = (byte)(r.ax >> 8);
                r.cx--;
            } while (r.cx > 0);

            r.di += 6;// SSG部分の無視
            r.cx = 5 + 6;
            do
            {
                data_buff[r.di++] = (byte)r.ax;
                data_buff[r.di++] = (byte)(r.ax >> 8);
                r.cx--;
            } while (r.cx > 0);

            r.di = SV + 6;//ofs:data_buff+SV+6
            r.ax = 0xff;
            r.cx = 3;
            do
            {
                data_buff[r.di++] = (byte)r.ax;
                data_buff[r.di++] = (byte)(r.ax >> 8);
                r.cx--;
            } while (r.cx > 0);// 開始音量の設定

            comlength = r.al;
            nax.comlength = r.al;
            r.si = 0;// SI = 演奏データのオフセット
            r.di = 0;//ofs:data_buff
            r.cx = 17;
            r.es = 0;//nax._object;
                     //    cld //熊 DirectionFlag=0

            //setwork:
            do
            {
                //setw1:
                data_buff[r.di + LC] = 46;// 音長カウンタ(48)
                data_buff[r.di + LC + 1] = 0;// (ushort)
                if (check_86pcm())// 拡張86PCM?
                    if (r.cl != 7)// ch11?
                        data_buff[r.di + LC] = 47;// V5.11

                //setw2:
                data_buff[r.di + VS + 0] = 110;// 音量・音長(110 + 48 * 256)
                data_buff[r.di + VS + 1] = 48;// (ushort))
                data_buff[r.di + LI] = 0xb0;// 休符ビットon + LFO level base(byte)
                data_buff[r.di + LB] = 1;// LFO speed base

                r.al = (byte)nax.objBuf[r.es][r.si + 0].dat;
                r.ah = (byte)nax.objBuf[r.es][r.si + 1].dat;
                data_buff[r.di + AD + 0] = r.al;// 演奏番地バッファに転送
                data_buff[r.di + AD + 1] = r.ah;// (ushort)
                r.si += 2;
                r.di += 2;
                r.cx--;
            } while (r.cx > 0);//loop    setwork

            r.di = 0x26;// ES:DI = 最大音長格納番地
            r.al = (byte)nax.objBuf[r.es][r.di + 0].dat;
            r.ah = (byte)nax.objBuf[r.es][r.di + 1].dat;
            maxlen[0] = r.al;
            maxlen[1] = r.ah;
            r.al = (byte)nax.objBuf[r.es][r.di + 2].dat;
            r.ah = (byte)nax.objBuf[r.es][r.di + 3].dat;
            maxlen[2] = r.al;
            maxlen[3] = r.ah;

            r.ax = 0xb807;
            mixsave = r.ah;// SSG PM2にする
            outdata1();

            r.ax = 0x200;
            tempo[0] = r.al;// テンポ120
            tempo[1] = r.ah;
            tempo[2] = r.al;
            tempo[3] = r.ah;


            r.di = 0;//ofs:chtbl2
            r.al = 0;
            //chset1:
            do
            {
                chtbl2[r.di] = r.al;
                r.di++;
                r.al++;
            } while (r.al < 17);

            r.cx = 17;
            r.di = 0;//ofs:pcm0work
            //pcminit1:
            do
            {
                nax.pc98.OutportC4231_Volume((byte)r.di, 0, 110);// PCMの音量
                r.ax = 0xc008;// or al, al
                nax.pc98.OutportC4231_Pan((byte)r.di, 0, r.ax);// PCMのパン命令
                nax.pc98.OutportC4231_Pan((byte)r.di, 1, r.ax);
                r.di++;
                r.cx--;
            } while (r.cx > 0);

            r.di = 0;//ofs:rhytbl	; リズムテーブルの初期化
            r.al = 0xc0 + 0x1f;
            r.cx = 6;
            do
            {
                rhytbl[r.di++] = r.al;
                r.cx--;
            } while (r.cx > 0);

            r.ax = 0xc3;

            // AH=0 : 音源なし
            //    1 : ch1～6
            //    2 : ch1～10(11)
            //    3 : ch1～13
            //    4 : ch1～17
            if (outdata1_ == r.al) r.ah = 0;
            else if (outdata2_ == r.al) r.ah = 1;
            else if (outdata3_ == r.al) r.ah = 2;
            else if (outdata4_ == r.al) r.ah = 3;
            else r.ah = 4;

            //syscon1:
            r.al = nax.m_mode[3];
            r.al &= 0b10100;// AL=0 : PCMなし, AL=1 : ADPCM
            r.al >>= 2;// AL=4 : 86/WSS-PCM
            r.cx = 17;
            r.di = 0;//ofs:loopcnt+8

            //syscon2:
            do
            {
                loopcnt[r.di + 8 + 0] = r.al;
                loopcnt[r.di + 8 + 1] = r.ah;
                r.di += 2;// 変数X8,X9に格納
                r.di += 16 - 2;
                r.cx--;
            } while (r.cx > 0);

            r.es = esbk;

        }

        //============================
        //	演奏停止ルーチン
        //============================
        public void music_stop(object obj=null)
        {
            ushort axbk = r.ax;
            ushort dxbk = r.dx;// 割り込みマスク

            //r.ax = 0x3027;// タイマAを停止
            r.ax = 0x3827;// タイマAを停止(v6.25)
            outdata1();

            vol_off();

            reset_intimer();// 内蔵タイマの停止
            r.ax = 8;
            outdata1();// SSGPCMでの音量を下げる
            r.ax++;
            outdata1();
            r.ax++;
            outdata1();

            mapflag &= 0xfd;// 演奏中フラグ
            if (check_86pcm())// 拡張86PCM?
            {
                fifo_int_off();// FIFOの停止
                pcm_stop();
            }
            //noextpcm2:

            r.dx = dxbk;
            r.ax = axbk;

        }

        //===================================
        //	FM/SSG音カットルーチン
        //===================================
        private void vol_off()
        {
            ushort sibk = r.si;
            ushort dxbk = r.dx;
            ushort cxbk = r.cx;

            r.cx = 12;

            //off1:
            do
            {
                key_off();// FMをキーオフ
                r.ch++;
                if (r.ch == 3)
                {
                    r.ch = 6;
                }
                if (r.ch == 9)
                {
                    r.ch = 11;
                }
                r.cl--;
            } while (r.cl > 0);

            r.si = 0;//ofs:data_buff+6
            r.cx = 0x303;

            //off2:
            do
            {
                volssg0();// SSG の音量を0に

                r.ch++;
                r.cl--;
            } while (r.cl > 0);

            r.ax = 0xb;
            outdata2();// PCMの音量を0にする
            r.ax = 0x100;
            outdata2a();// PCM Play Reset

            r.cx = cxbk;
            r.dx = dxbk;
            r.si = sibk;
        }

        //;=========================================================
        //;	YM2203/2608/3438 データ、アドレス出力ルーチン
        //;	entry AL = adrs
        //; AH = data
        //;		CH = channel No. (0～16)
        //;
        //;	outdata ch0～5は0188へ
        //;			ch6～10はAL-6して018Cへ
        //;			ch11～13はAL-11して0488へ
        //;			ch14～16はAL-14しては048Cへ
        //;	outdata0 ch0～5は0188へ
        //;			ch6～10は018Cへ
        //;			ch11～13は0488へ
        //;			ch14～16は048Cへ
        //;	outdata1 全て0188へ
        //; outdata1s 全て0188へ・SSGPCMモード時は無視
        //;	outdata2 全て018Cへ
        //; outdata3 全て0488へ
        //; outdata4 全て048Cへ
        //; outdata6 ch0～11は0188へ
        //;			ch11～16は0488へ
        //;=========================================================

        private void outdataa()
        {
            if (!checksh()) return;

            //outdata:
            if (r.ch < 6)
            {
                outdata1();
                return;
            }

            if (r.ch >= 11)
            {
                outdata5();
                return;
            }

            r.al -= 6;// アドレスを減算してから出力
            outdata2();
        }

        public void outdata2()
        {
            if (outdata2_ == 0xc3) return;

            ushort dxbk = r.dx;
            ushort axbk = r.ax;

            r.dx = port3;//熊　自己書き換え 初期値:18ch
            bwait();// BUSYウェイト処理
            r.dx += 2;
            nax.pc98.OutportB(r.dx, r.al);
            check_busy();
            r.ax = axbk;
            r.dx = dxbk;
            if (hadr7 == 0) return;
            nax.pc98.OutportB(0x5f, r.al);
            //skip_outdata:
            return;
        }

        public void outdata0a()
        {
            if (!checksh()) return;

            //outdata0:
            if (r.ch >= 14)
            {
                outdata4();
                return;
            }

            if (r.ch >= 11)
            {
                outdata3();// for YM3438
                return;
            }

            if (r.ch >= 6)
            {
                outdata2();
                return;
            }

            outdata1();
            return;
        }

        public void outdata1()
        {
            if (outdata1_ == 0xc3) return;
            ushort dxbk = r.dx;
            ushort axbk = r.ax;

            r.dx = port1;
            bwait();// BUSYウェイト処理
            r.dx += 2;
            nax.pc98.OutportB(r.dx, r.al);
            check_busy();
            r.ax = axbk;
            r.dx = dxbk;

            //hadr8:	ret
        }

        private void outdata6a()
        {
            if (!checksh()) return;
            outdata6();
        }

        private void outdata6()
        {
            if (r.ch < 11)
            {
                outdata1();
                return;
            }
            outdata3();
        }

        public void outdata3()
        {
            if (outdata3_ == 0xc3) return;
            ushort dxbk = r.dx;
            ushort axbk = r.ax;

            r.dx = port5;
            bwait();// BUSYウェイト処理
            r.dx += 2;
            nax.pc98.OutportB(r.dx, r.al);
            check_busy();
            r.ax = axbk;
            r.dx = dxbk;

            //hadr9:	ret
        }

        private void outdata5()
        {
            r.al -= 11;
            if (r.ch < 14)
            {
                outdata3();
                return;
            }
            r.al -= 3;
            outdata4();
        }

        public void outdata4()
        {
            if (outdata4_ == 0xc3) return;
            ushort dxbk = r.dx;
            ushort axbk = r.ax;

            //port7:
            r.dx = port7;// 0x58c;
            bwait();// BUSYウェイト処理
            r.dx += 2;
            nax.pc98.OutportB(r.dx, r.al);
            check_busy();
            r.ax = axbk;
            r.dx = dxbk;
            //hadr10:	ret
        }

        private void outdata1a()
        {
            if (checksh()) outdata1();
        }

        private void outdata2a()
        {
            if (checksh()) outdata2();
        }

        private void outdata1sa()
        {
            if (!checksh()) return;
            if (ssgpcm == 0) outdata1();
            //outret:
        }

        /// <summary>
        /// ･････････････････････････････････････
        ///	効果音使用中チェック
        ///	exit NZ = 使用中
        /// ･････････････････････････････････････
        /// </summary>
        /// <returns> 熊：true:未使用 false:使用中</returns>
        private bool checksh()
        {
            ushort dxbk = r.dx;
            ushort cxbk = r.cx;
            ushort axbk = r.ax;

            bool ret;
            r.ch = realch;// CH = 本来のチャネル番号
            calcbit();
            if (ret = ((r.ax & shflag1) == 0))
                ret = ((r.dl & shflag2) == 0);

            //chksh1:
            r.ax = axbk;
            r.cx = cxbk;
            r.dx = dxbk;

            return ret;
        }

        //---------------------------------------
        //	BUSY待ち処理メインルーチン
        //---------------------------------------

        private void bwait()
        {
            ushort axbk = r.ax;
            check_busy();
            r.ax = axbk;
            nax.pc98.OutportB(r.dx, r.al);
            if (hadr11 != 0)
                nax.pc98.OutportB(0x5f, r.al);
            check_busy();
            r.al = r.ah;
        }

        //･･･････････････････････････････････････
        //	BUSY待ちサブルーチン
        //	entry	DX = システムポート
        //	break	CX,AL
        //･･･････････････････････････････････････

        private void check_busy()
        {
            ushort cnt = 100;// タイムオーバー制限ループ
            //wait1:
            do
            {
                cnt--;
                if (cnt == 0) break;
                r.al = nax.pc98.InportB(r.dx);// BUSYの確認
            } while ((r.al & 0x80) == 0);
        }

        //--------------------------
        //	タイマの操作
        //--------------------------

        private void set_timer()
        {
            r.ds = r.cs;
            r.ax = (ushort)(tempo[0] + tempo[1] * 0x100);// AX = tempo data (1024-tempo)*.02mS
            r.ax >>= 2;
            r.dl = tacount;

            if (r.dl == 0xff)
            {
                //set_tac1:
                tacount = r.ah;// T61以下は分周を行なう
                // 1.28ms以下は設定困難のため補正する
                if (r.ah != 0 && r.al < 0x40) r.al += 0x40;
                //set_tac5:
                r.al = (byte)~r.al;
                r.ah = r.al;
                r.al = 0x24;
                outdata1();
                r.ah = tempo[0];
                r.ah = (byte)~r.ah;
                r.ah &= 3;
            }
            else
            {
                r.ah = 0;
                //set_tac3:
                // 最初に設定した値を補正した場合は今回修正する
                if (r.dl == 0 && r.al < 0x40) r.ah = 0x40;

                //set_tac4:
                r.al = 0x24;
                outdata1();// 分周中は最も遅い周期で設定
                r.ax = 0;
            }

            //set_tac2:
            r.al = 0x25;
            outdata1();// テンポの設定
            r.ax = 0x3d27;// タイマAを使用
            r.ah |= codem1;
            if ((mapflag & 2) == 0)// 演奏中のみ開始する
            {
                r.ah = 0x30;
            }
            outdata1();
        }

        //;---------------------------------------
        //;	EMSマップ・アンマップの操作
        //;	entry	AH = m_mode+3 の内容
        //;---------------------------------------

        private void mapplay_ems()
        {
            ushort axbk = r.ax;
            if ((mapflag & 1) != 0)
            {
                if (check_emsuse())
                {
                    // EMSを使用してるか
                    mapflag |= 0x01;
                    nax.pushems();
                }
            }
            //notmap:
            r.ax = axbk;
        }

        private void unmapplay_ems()
        {
            ushort axbk = r.ax;
            if ((mapflag & 1) != 0)
            {
                if (check_emsuse())
                {
                    // EMSを使用してるか
                    mapflag &= 0xfe;
                    nax.popems();
                }
            }
            //notunmap:
            r.ax = axbk;
        }

        //============================================
        //	WSS-PCM DMA割り込み処理ルーチン
        //============================================

        //	.386
        public void int0bent()
        {
            lock (work.SystemInterrupt)
            {

                byte al = pcmrecmode;
                if ((al & 1) != 0)// 録音モード?
                {
                    record3();
                }
                else
                {
                    program_dma();
                    clear_fint();
                    change_buffer();
                    put_fifo_data();
                }

                nax.pc98.OutportB(0, 0x20);
            }
        }

        private void record3()
        {
            //if ((r.al & 4) == 0)// DacSample発音モード?
            //{
            //    //record5:
            //    program_dma_rec();// 最初のセグメントは無視する
            //    add_recseg();// 次のセグメントへ
            //}
            //else
            //{
            //    program_dma();// メインメモリ上の転送
            //    add_playseg();// 次のセグメントへ
            //    pcmrecmode |= 0x10;// 1回割り込みが入ったフラグ
            //}
            ////record6:
            //clear_fint();
        }

        //;=======================================
        //;	タイマA割り込み処理ルーチン
        //;=======================================

        public void timer_entry()
        {
            //	cld
            ushort axbk = r.ax;
            ushort dxbk = r.dx;
            ushort dsbk = r.ds;
            ushort esbk;

            r.ax = r.cs;
            r.ds = r.ax;
            r.ah = 0;

            //int_lop:
            bool skipEOI = false;
            while (true)
            {
                bool skipfifoint = timer_entry_CheckSkipFIFO();

                //skip_fifo_int:
                r.dx = port19;
                if (!skipfifoint)
                {
                    timer_entry_FIFO();
                    r.dx = port18;// FM割り込みもあるか?
                }

                r.al = nax.pc98.InportB(r.dx);
                if ((r.al & 0b0000_0001) == 0)
                {
                    skipEOI = false;
                    break;
                }

                //fmint_exec:
                if ((mapflag & 0x10) != 0)
                {
                    skipEOI = true;
                    break;
                }
                mapflag |= 0x10;

                spsave1 = r.sp;
                sssave1 = r.ss;
                r.ss = r.cs;// 専用スタックの設定(PSPを流用)
                r.sp = 0;// start - 0x20;

                esbk = r.es;
                put_eoi();
                r.sign = false;
                if (tacount == 0) r.sign = true;
                tacount--;// Timer-A分周カウンタ
                          //	pushf
                set_timer();
                //stiof1:	sti
                if (r.sign)
                {
                    play_exec();
                }
                r.es = esbk;
                r.ah = 1;
                r.ss = sssave1;
                r.sp = spsave1;
                mapflag &= 0xef;
            }

            //exit_int1:
            if(!skipEOI) if (r.ah == 0) put_eoi();

            //exit_int:
            r.ds = dsbk;
            r.dx = dxbk;
            r.ax = axbk;
        }

        private bool timer_entry_CheckSkipFIFO()
        {
            if (check_wsspcm()) return true;// WSS-PCM?

            r.dx = 0xa468;
            r.al = nax.pc98.InportB(r.dx);
            if ((r.al & 0b0001_0000) == 0) return true;
            else if (!check_86pcm()) return true;// 拡張86PCM?
            return false;
        }

        private void timer_entry_FIFO()
        {
            //	pusha
            ushort esbk = r.es;
            if ((pcmrecmode & 1) != 0)// 録音モード?
            {
                //record1:
                get_fifo_data();// FIFOからデータをもらう（録音）
            }
            else
            {
                put_fifo_data();// FIFOにデータを送る
                change_buffer();
            }
            //record2:
            clear_fint();
            r.es = esbk;
        }

        private void put_eoi()
        {
            //jmp1:
            if (nax.jmp1 == 0)
            {
                r.al = 0x20;// スレーブにEOIを送る↓ここのコード数変更時は
                nax.pc98.OutportB(8, r.al);// muap98.asm のjmp1部分も変更のこと
                r.al = 0x0b;// スレーブのISRを読む
                nax.pc98.OutportB(8, r.al);
                r.al = nax.pc98.InportB(8);
                if (r.al == 0)// まだ処理があるか？
                {
                    r.al = 0x20;// マスタにもEOIを送る
                    nax.pc98.OutportB(0, r.al);// 全処理終了
                }
            }
            else
            {
                r.al = 0x20;// マスタにもEOIを送る
                nax.pc98.OutportB(0, r.al);// 全処理終了
            }
            //exit2:
        }

        //-----------------------------------
        //	FIFOへのデータ書き込み
        //-----------------------------------

        private void put_fifo_data()
        {

            ushort bx = 0;
            nax.save_extpcm(ref bx);// EMSのマップ保存
            ushort ax = nax.fifoseg;// ES,FS = FIFOセグメント
            ushort es = ax;
            ushort fs = ax;
            //sign3:
            ax = 0x8080;
            ushort cx = (ushort)NAX.FIFO_SIZE;
            ushort di = fifoptr1;

            // FIFO転送前バッファの初期化
            do
            {
                work.fifoBuf[di++] = (byte)ax;
                work.fifoBuf[di++] = (byte)(ax >> 8);
                cx--;
            } while (cx > 0);

            //segad3:
            ax = 0xc000;
            es = ax;

            //EMSのマッピング

            cx = 17;
            ushort si = 0;//ofs:pcm0work
            ushort dx,bp;
            //fifo_map1:
            do
            {
                if (pcm0work[si].pcm0cnt[0] == 0 && pcm0work[si].pcm0cnt[1] == 0)
                {
                    si += PWORKE;// 次のチャネルへ
                    cx--;
                    continue;
                }

                ushort cxbk = cx;
                bx = pcm0work[si].pcm0adrs[1];// BX = EMS論理ページ
                                                              //Log.writeLine(LogLevel.INFO, string.Format("{0:X}", r.bx*0x4000+pcm0work[nax.reg.si].pcm0adrs[0]));
                                                              //naxad1:
                dx = nax.phandle;
                ax = 0x4400;// EMSマッピング
                byte ah = 0x44;
                nax.ems.cS4231EMS_Map(00, ref ah, bx, dx);
                byte[] emsMem = nax.ems.cS4231EMS_GetCrntMapBuf();

                //FIFO転送前バッファへの書き込み
                ax = pcm0work[si].pcm0pan[0];// パンの命令(L)
                panl1_ = ax;
                ax = pcm0work[si].pcm0pan[1];//	パンの命令(R)
                panl2_ = ax;
                //	jmp	$+2
                bp = pcm0work[si].pcm0adrs[0];// BP = PCMデータの番地
                cx = pcm0work[si].pcm0freq[0];// CX = PCM周波数カウンタ
                bx = pcm0work[si].pcm0freq[1];// BX = 周波数データを加算
                di = fifoptr1;// FS:DI = FIFO転送前バッファ
                uint edx = pcm0work[si].pcm0cnt[0]
                    + (uint)pcm0work[si].pcm0cnt[1] * 0x1_0000;
                //fifo_lop1:
                do
                {
                    byte al = emsMem[bp];
                    ax = (ushort)((sbyte)al * (sbyte)pcm0work[si].pcm0vol[0]);
                    ax <<= 2;
                    al = (byte)(ax >> 8);
                    ah = (byte)(ax >> 8);
                    //debug
                    //nax.reg.ah = nax.reg.al = emsMem[nax.reg.bp];

                    //panl1:
                    //熊:自己書き換えで切り替えています
                    switch (panl1_)
                    {
                        case 0xc008://or al,al
                            al |= al;
                            break;
                        case 0xc030://xor al,al
                            al ^= al;
                            break;
                    }
                    //panl2: パンのマスクを実行
                    switch (panl2_)
                    {
                        case 0xc008://or al,al
                            al |= al;
                            break;
                        case 0xe430://xor ah,ah
                            ah ^= ah;
                            break;
                    }

                fifo_lop2:
                    work.fifoBuf[di++] += al;// L,Rの値を加算して格納
                    work.fifoBuf[di++] += ah;
                    cx += bx;

                    if ((cx & 0x8000) != 0)
                    {
                        //fifo_freq1:
                        if (di >= fifoend1)
                        {
                            goto fifo_end1;// 低周波数で同じ値を出力する場合の処理
                        }
                        goto fifo_lop2;
                    }
                    //fifo_freq2:
                    do
                    {
                        bp++;
                        if (bp >= 16384)// EMSの次のページに切り換わるか
                        {
                            //fifo_freq3:
                            bp = 0;
                            uint edxbk1 = edx;
                            ushort bxbk1 = bx;
                            bx = pcm0work[si].pcm0adrs[1];// BX = EMS論理ページ
                            bx++;
                            pcm0work[si].pcm0adrs[1] = bx;
                            //naxad2:
                            dx = nax.phandle;
                            ax = 0x4400;// EMSマッピング
                            ah = 0x44;
                            nax.ems.cS4231EMS_Map(00, ref ah, bx, dx);
                            emsMem = nax.ems.cS4231EMS_GetCrntMapBuf();
                            bx = bxbk1;
                            edx = edxbk1;
                        }
                        //fifo_freq5:
                        edx--;
                        if (edx == 0)
                        {
                            pcm0work[si].pcm0cnt[0] = 0;
                            pcm0work[si].pcm0cnt[1] = 0;
                            goto fifo_skip1_;
                        }
                        //freq2:
                        cx -= (ushort)freq2;// (O4CDATA*1.5);// freq2;// O4CDATA;
                    } while ((cx & 0x8000) == 0);
                } while (di < fifoend1); // FIFOバイト数ループする

            fifo_end1:
                pcm0work[si].pcm0cnt[0] = (ushort)edx;
                pcm0work[si].pcm0cnt[1] = (ushort)(edx >> 16);
                pcm0work[si].pcm0freq[0] = cx;
                pcm0work[si].pcm0adrs[0] = bp;

            fifo_skip1_:
                si += PWORKE;// 次のチャネルへ
                cx = cxbk;
                cx--;

            } while (cx > 0);

            nax.remove_extpcm();

            //DSP処理

            //jump1:
            if (jump1_ == 0x3e3e)
            {
                si = fifoptr2;
                di = fifoptr1;
                cx = (ushort)NAX.FIFO_SIZE;
                //jump2:
                switch (jump2_)
                {
                    case 0:
                        //test_lop1:
                        do
                        {
                            //	segfs
                            ax = (ushort)(work.fifoBuf[si] + work.fifoBuf[si + 1] * 0x100);
                            si += 2;
                            //sign1:
                            byte al= (byte)ax;
                            byte ah= (byte)(ax >> 8);
                            al -= 0x80;// none
                            ah -= 0x80;
                            dx = 0;
                            dx = ah;
                            ax = (ushort)(sbyte)al;
                            ushort tmp = ax;
                            ax = dx;
                            dx = tmp;
                            ax = (ushort)(sbyte)(byte)ax;
                            ax += dx;
                            //level1:
                            dx = level1_;// 0x007f;
                            int ans = (short)ax * (short)dx;
                            dx = (ushort)(ans >> 16);
                            ax = (ushort)ans;
                            work.fifoBuf[di] += (byte)(ax >> 8);
                            work.fifoBuf[di + 1] -= (byte)(ax >> 8);
                            di += 2;
                            cx--;
                        } while (cx > 0);
                        break;
                    case 1:
                        //test_lop2:
                        do
                        {
                            //	segfs
                            ax = (ushort)(work.fifoBuf[si] + work.fifoBuf[si + 1] * 0x100);
                            si += 2;
                            byte al = (byte)((byte)ax - (byte)(ax >> 8));
                            //level2:
                            byte ah = level2_;//0x7f;
                            ax = (ushort)((sbyte)al * (sbyte)ah);
                            work.fifoBuf[di + 1] += (byte)(ax >> 8);
                            work.fifoBuf[di] -= (byte)(ax >> 8);
                            di += 2;
                            cx--;
                        } while (cx > 0);
                        break;
                    case 2:
                        //test_entry3:
                        cx <<= 1;
                        //test_lop3:
                        do
                        {
                            //	segfs
                            byte al = work.fifoBuf[si++];
                            //sign2:
                            al -= 0x80;// none
                                               //level3:
                            byte ah = level3_;//0x7f
                            ax = (ushort)((sbyte)al * (sbyte)ah);
                            ax <<= 1;
                            work.fifoBuf[di] -= (byte)(ax >> 8);
                            di++;
                            cx--;
                        } while (nax.reg.cx > 0);
                        break;
                }
            }

            //dsp_exit:
            if (!check_wsspcm())
            {
                //ushort dsbk = ds;
                si = fifoptr1;
                cx = (ushort)(NAX.FIFO_SIZE * 2);
                dx = 0xa46c;
                ax = fs;
                //ds = ax;
                // DS:SIから転送
                do
                {
                    nax.pc98.OutportB(dx, work.fifoBuf[si++]);
                    cx--;
                } while (cx > 0);
                //ds = dsbk;
            }
            //fifo_send1:
            //fs = fsbk;
            //edx = edxbk;
        }


        //-----------------------------
        //	DMAのプログラム
        //-----------------------------
        private void program_dma()
        {
            byte al = 0b0000_0100;// DMAマスクビットをセット
            al |= dma_chan;
            nax.pc98.OutportB(0x15, al);// SingleMaskSet
            al = 0b0100_1000;// DMAモードを設定
            al |= dma_chan;
            nax.pc98.OutportB(0x17, al);// ModeReg.

            ushort ax = nax.fifoseg;// DMA専用セグメントを使用
            uint eax = (uint)((ax << 4) + fifoptr1);

            //progdma_sub:
            nax.pc98.OutportB(0x19, (byte)eax);// ClearByteF/F
            // DMAアドレスの設定
            nax.pc98.OutportB(dma_adr, (byte)eax);
            nax.pc98.OutportB(dma_adr, (byte)(eax >> 8));// DMA adr.

            // DMAバンクレジスタの設定
            eax >>= 16;
            nax.pc98.OutportB(dma_bank, (byte)eax);// DMA bank adr.

            // DMAカウンタの設定
            nax.pc98.OutportB(dma_count, (byte)dma_data);
            nax.pc98.OutportB(dma_count, (byte)(dma_data >> 8));// DMA count

            al = 0;// DMAマスクビットをクリア
            al |= dma_chan;
            nax.pc98.OutportB(0x15, al);// SingleMaskClear
            nax.pc98.OutportB(0x5f, al);
        }

        //･･･････････････････････････････････････
        //	ダブルバッファの切り換え
        //･･･････････････････････････････････････

        private void change_buffer()
        {
            ushort ax = fifoptr1;
            change_ptr(ref ax);
            fifoptr1 = ax;
            ax +=(ushort)(NAX.FIFO_SIZE * 2);
            fifoend1 = ax;

            ax = fifoptr2;
            change_ptr(ref ax);
            fifoptr2 = ax;
            ax += (ushort)(NAX.FIFO_SIZE * 2);
            fifoend2 = ax;

        }

        private void change_ptr(ref ushort ax)
        {
            ax += (ushort)(NAX.FIFO_SIZE * 2);
            if (ax >= fifofin)
            {
                ax = 0;
            }
            //change_ptr1:
        }

        //	.186

        //-----------------------------------
        //	FIFOからデータ取り出し
        //-----------------------------------

        private void get_fifo_data()
        {
            //(略)
        }

        //;	FIFO割り込みのクリア

        private void clear_fint()
        {
            if (check_wsspcm())
            {
                //clear_fintwss:
                nax.pc98.OutportB(0x0f46, 0xfe);// R2に書き込み
                return;
            }
            byte al = nax.pc98.InportB(0xa468);
            al &= 0b1110_1111;
            nax.pc98.OutportB(0xa468, al);
            al = nax.pc98.InportB(0xa468);
            al |= 0b0001_0000;
            nax.pc98.OutportB(0xa468, al);
        }

        //;	FIFO割り込みの禁止

        private void fifo_int_off()
        {
            if (!check_wsspcm())
            {
                r.dx = 0xa468;
                r.al = nax.pc98.InportB(r.dx);
                r.al &= 0b1101_1111;
                nax.pc98.OutportB(r.dx, r.al);
                return;
            }
            //fifo_int_offwss:
            r.ax = 0x0a00;
            put_wss();
        }

        //;	FIFO割り込みの許可

        private void fifo_int_on()
        {
            if (!check_wsspcm())
            {
                r.dx = 0xa468;
                r.al = nax.pc98.InportB(r.dx);
                r.al |= 0b0010_0000;
                nax.pc98.OutportB(r.dx, r.al);
                return;
            }
            //fifo_int_onwss:
            r.ax = 0x0a02;
            put_wss();
        }

        //	PCM再生開始

        private void pcm_start()
        {
            fifo_exec |= 1;
            if (check_wsspcm())
            {
                //pcm_startwss:
                r.ax = 0x4902;
                put_wss();// PIO
                          //r.ax = 0x4981;
                r.ax = 0x4905;// DMA
                put_wss();
                r.al = TRD;
                r.dx = 0xf44;
                nax.pc98.OutportB(r.dx, r.al);// MCE off
                return;
            }
            r.dx = 0xa468;
            r.al = nax.pc98.InportB(r.dx);
            r.al |= 0b1000_0000;
            nax.pc98.OutportB(r.dx, r.al);
        }

        //;	PCM再生の停止

        private void pcm_stop()
        {
            if (!check_wsspcm())
            {
                r.dx = 0xa468;
                r.al = nax.pc98.InportB(r.dx);
                r.al &= 0b0111_1111;
                nax.pc98.OutportB(r.dx, r.al);
                fifo_exec &= 0xfe;
                return;
            }

            //pcm_stopwss:
            //	cli
            r.al = 0b0000_0100;
            r.al |= dma_chan;
            nax.pc98.OutportB(0x15, r.al);// SetSingleMask
            r.ax = 0x4903;// PCM再生を停止
            put_wss();
            r.al = TRD;
            r.dx = 0xf44;
            nax.pc98.OutportB(r.dx, r.al);// MCE off
            r.dx = 0xf46;
            nax.pc98.OutportB(r.dx, r.al);// 割り込みフラグをクリア
            //	sti
        }

        //;	音量の設定

        private void set_volume()
        {
            if (check_wsspcm())
            {
                //set_volumewss:
                r.ax = 0x0600;
                put_wss();
                r.ax = 0x0700;
                put_wss();
                return;
            }

            r.dx = 0xa466;
            r.al = 0b1010_0000;
            nax.pc98.OutportB(r.dx, r.al);
        }

        //････････････････････････････････････
        //	CS4231のレジスタを設定
        //	entry	AH = レジスタ番号
        //		AL = データ
        //････････････････････････････････････
        private void put_wss()
        {
            byte al = r.al;
            byte ah = r.ah;
            ushort dx = 0xf44;

            ah |= TRD;
            nax.pc98.OutportB(dx, ah);
            nax.pc98.OutportB(0x5f, ah);

            dx++;
            nax.pc98.OutportB(dx, al);
            nax.pc98.OutportB(0x5f, al);
        }

        //--------------------------------
        //	演奏メインルーチン
        //--------------------------------

        private void play_exec()
        {
            work.crntMmlDatum = null;
            r.ds = r.cs;
            nax.m_mode[1] |= 8;// 演奏中フラグ
            if ((skip_data1 != 0xffff) || (skip_data2 != 1))// 全チャネル停止中か
            {
                //lenchk2:
                timercnt++;
                r.carry = (playlen[0] == 0xffff);
                playlen[0] += 1;// 演奏音長をカウント
                playlen[1] += (ushort)(0 + (r.carry ? 1 : 0));
                r.ax = playlen[0];
                r.dx = (ushort)(maxlen[0] + maxlen[1] * 0x100);
                r.cx = (ushort)(maxlen[2] + maxlen[3] * 0x100);
                r.carry = (r.dx + 48 > 0xffff);
                r.dx += 48;// L4追加
                r.cx += (ushort)(0 + (r.carry ? 1 : 0));
                if (r.ax == r.dx)
                {
                    r.ax = playlen[1];
                    if (r.ax == r.cx)
                    {
                        r.ax = 0;
                        playlen[0] = r.ax;
                        playlen[1] = r.ax;
                        playcont++;
                    }
                }
            }
            else
            {
                work.Status = 0;
            }

            //lenchk1:
            r.es = nax._object[0];
            replay();
        }

        //------------------------------------
        //	チャネル1-17演奏前後処理
        //------------------------------------

        private void replay()
        {
            r.si = 0;//ofs:data_buff
            r.cx = 17;// CH-No. & loop time
            r.ax = skip_data1;// channel skip data (17bit)
            r.dl = skip_data2;
            r.ax |= wait_flg1;// 全データが揃うまで待ってもらいます
            r.dl |= wait_flg2;

            //debug
            //r.cx = 11;
            //r.ax = 0xfdfe;
            //r.dl = 0x01;

            //play_main:
            do
            {
                work.crntMmlDatum = null;
                r.carry = (r.dl & 1) != 0;
                r.dl >>= 1;
                r.ax = r.rcr(r.ax, 1);// チャネルが停止されているか
                ushort sibk = r.si;
                ushort dxbk = r.dx;
                ushort cxbk = r.cx;
                ushort axbk = r.ax;// ***このスタック数を変える場合はwait_rに注意
                if (!r.carry)
                {
                    r.al = r.ch;
                    realch = r.ch;
                    r.bx = 0;//ofs:chtbl2
                    r.al = chtbl2[r.bx + r.al]; //	xlat
                    r.ch = r.al;
                // 実際のチャネル番号に変換
                Rechannel:
                    rechannelFlg = false;
                    r.al = data_buff[r.si + LC];// 音長カウンタを読み取る
                    if (r.ch == 10)
                    {
                        // ADPCM
                        play_pcm();
                        if (initia0Flg) return;
                    }
                    else if (check_extpcm(data_buff))
                    {
                        // 拡張PCMチェック
                        play_pcm();
                        if (initia0Flg) return;
                    }
                    else if (r.ch == 9)
                    {
                        // RHYTHM
                        play_rhythm();
                    }
                    else if (r.ch >= 6)
                    {
                        // FM
                        play_fm();
                        if (rechannelFlg)
                            goto Rechannel;
                    }
                    else if (r.ch >= 3)
                    {
                        // SSG
                        play_ssg();
                    }
                    else
                    {
                        play_fm();
                        if (rechannelFlg)
                            goto Rechannel;
                    }
                }
                //ch_skip:
                r.ax = axbk;
                r.cx = cxbk;
                r.dx = dxbk;
                r.si = sibk;
                r.ch++;// 次のチャネルへ
                r.si += 2;// 次のデータバッファへ
                r.cl--;
            } while (r.cl > 0);

            //	フェードアウト処理
            r.dx = fade_count;// フェードアウトチェック
            if (r.dh != 0)
            {
                r.overflow = (r.dx + fadedata) > 0xffff;
                r.dx += fadedata;// フェードアウト量を加算
                if (r.overflow)// フェードアウト終了音量値か
                {
                    music_stop();// sti なし
                    r.dx = 0;
                }
                //skip_op3:
                fade_count = r.dx;
            }
            //fade_skip:

            if (end_flug != 0)// *,**コマンドが実行されたか
            {
                if ((init_flg & 2) == 0)// @foでフェードアウトしたか
                {
                    r.ax = 0;
                    fade_count = r.ax;
                }
                //ext_fade:
                mapplay_ems();
                init_work();// ax,cx,dx,si,es を破壊
            }

            //---------------------------------
            //	演奏ルーチン終了処理
            //---------------------------------

            //ret00:
            unmapplay_ems();// 演奏バッファを元に戻す
            nax.m_mode[1] &= 0xf7;// 演奏中フラグ解除
            //	ret
        }

        //-------------------------------
        //	FM1の音長を処理
        //-------------------------------

        private void play_fm()
        {
            r.al--;
            if (r.al == 0)
            {
                mapplay_ems();// 演奏バッファのマップ
                main_fm();
                if (rechannelFlg) return;
            }
            //nextstep1:
            data_buff[r.si + LC] = r.al;// 次のカウント値をセット

            //	FM1のLFO処理
            ushort axbk = r.ax;
            calc_lfo();// LFOカウンタの計算
            //Log.writeLine(LogLevel.ERROR, string.Format("変異 carry:{1} AX:{0:x04}", r.ax,r.carry));
            if (!r.carry)
            {
                if (r.zero)
                {
                    mapplay_ems();// 音量LFOの時は演奏バッファをマップ
                    vol_lfo();
                    ushort sivsbk = (ushort)(
                        data_buff[r.si + VS]
                        + data_buff[r.si + VS + 1] * 0x100//熊:多分不要
                        );
                    data_buff[r.si + VS] = r.al;
                    volchgm(data_buff);// 音量の変更
                    data_buff[r.si + VS] = (byte)sivsbk;
                    data_buff[r.si + VS + 1] = (byte)(sivsbk >> 8);//熊:多分不要
                }
                else
                {
                    //pmd_fm1:
                    r.dx = (ushort)(
                        data_buff[r.si + FD]
                        + data_buff[r.si + FD + 1] * 0x100
                        );
                    ushort dxbk = r.dx;
                    r.dh &= 7;// DX = 26ah～48fh
                    //Log.writeLine(LogLevel.ERROR, string.Format("before AX:{0:x04}　DX:{0:x04}", r.ax, r.dx));
                    freq_master_lfo();
                    //Log.writeLine(LogLevel.ERROR, string.Format("after AX:{0:x04}　DX:{0:x04}", r.ax,r.dx));
                    r.ax += r.dx;
                    r.dx = dxbk;
                    r.dx &= 0xf800;
                    r.ax += r.dx;
                    //Log.writeLine(LogLevel.ERROR, string.Format("AX:{0:x04}", r.ax));
                    setfreq1();// YM2608の周波数設定
                }
            }

            //exit_fm1:
            r.ax = axbk;

            //	FM1の音長割合処理

            if ((data_buff[r.si + TI] & 1) == 0)// スラー処理なら無視
            {
                if (r.al == data_buff[r.si + RS])
                {
                    key_off();// 音長割合時間がくればキーオフ
                }
            }

            //ch_skip1:            
        }

        //----------------------------------------
        //	SSGの音長、ディケイ、持続処理
        //----------------------------------------

        private void play_ssg()
        {
            r.al--;
            if (r.al == 0)
            {
                mapplay_ems();// 演奏バッファのマッピング
                main_ssg();
            }

            //nextstep2:
            data_buff[r.si + LC] = r.al;// 音長カウンタを-1
            if (data_buff[r.si + PC] != 0)// エンベロープモード時のチェック
            {
                lfo_ssg();
                return;
            }
            if (data_buff[r.si + LV] == 0)// 現在音が出ているか
            {
                return;// 0 なら無音状態
            }

            //	SSGの音長割合処理

            if ((data_buff[r.si + TI] & 1) != 0)// スラー処理なら無視
            {
                skip_ratio();
                return;
            }

            if (r.al != data_buff[r.si + RS])
            {
                skip_ratio();
                return;
            }
            r.al = 0;
            volssg();// 割合時間が来たら音量0

            //ch_skip1
        }

        //;	アタック・ディケイレベルの処理

        private void skip_ratio()
        {
            r.bl = data_buff[r.si + VS];// BL = 音量データ
            if (data_buff[r.si + SV] == 255)
            {
                decay_ssg();// アタックなし
                return;
            }
            r.al = data_buff[r.si + AC];
            if (r.al == 255)
            {
                decay_ssg();// アタック終了後はディケイ処理へ
                return;
            }
            r.al--;
            data_buff[r.si + AC] = r.al;// アタックカウンタを-1
            if (r.al != 0)
            {
                lfo_ssg();
                return;
            }
            set_attackrate();// アタックカウンタの値転送
            r.al = 31;
            bool beflg = (r.al - data_buff[r.si + AR]) <= 0;
            r.al -= data_buff[r.si + AR];
            if (beflg)
            {
                r.al = 1;// AL = 音量加算値
            }
            r.al += data_buff[r.si + LV];
            if (r.al > r.bl)
            {
                r.al = r.bl;// [si+VS]の範囲に収める
                data_buff[r.si + AC] = 255;// 終了フラグを書き込む
            }
            decays1();
        }

        private void decay_ssg()
        {
            r.al = data_buff[r.si + SD];
            r.al |= data_buff[r.si + DY];// ディケイ値のチェック
            if (r.al == 0)
            {
                lfo_ssg();
                return;
            }

            r.cl = 4;
            r.al = data_buff[r.si + SC];
            r.ah = data_buff[r.si + SC + 1];// スタートディケイカウンタ
            if (r.ah < data_buff[r.si + DV])// ディケイレベルに達しているか
            {
                r.dl = data_buff[r.si + SD];// 第一ディケイカウンタ
                calc_decay();
                data_buff[r.si + SC] = r.al;
                data_buff[r.si + SC + 1] = r.ah;
            }

            //decay2:
            r.al = data_buff[r.si + DC];
            r.ah = data_buff[r.si + DC + 1];// 第二ディケイカウンタ
            r.dl = data_buff[r.si + DY];
            calc_decay();
            data_buff[r.si + DC] = r.al;
            data_buff[r.si + DC + 1] = r.ah;
            r.ax += (ushort)(data_buff[r.si + SC] + data_buff[r.si + SC + 1] * 0x100);// AH = 下げる音量
            r.al = r.bl;
            r.carry = r.al < r.ah;
            r.al -= r.ah;// use b15-b8
            if (r.carry)
            {
                r.al = 0;
            }
            decays1();
        }

        private void decays1()
        {
            r.bl = r.al;// BL = ディケイ後の音量データ
            volssg_set();
            lfo_ssg();
        }

        //;	SSGのLFO処理

        private void lfo_ssg()
        {
            calc_lfo();// LFOカウンタの計算
            if (r.carry)
            {
                return;//	jb	ch_skip
            }
            if (!r.zero)
            {
                pmd_ssg();
                return;//ch_skipへ
            }

            r.al = r.bl;
            vol_lfo_ssg();
            volssg1();// [si+LV]を変更せずに音量変更
            return; //	jmp	ch_skip
        }

        private void pmd_ssg()
        {
            r.dl = data_buff[r.si + FD];// DX = ee8h～1fh
            r.dh = data_buff[r.si + FD+1];
            freq_master_lfo();
            r.dx -= r.ax;
            r.ax = r.dx;
            setfreqs();// SSGの周波数設定
            return;//	jmp	ch_skip
        }

        private void calc_decay()
        {
            r.dh = 0;
            r.dx <<= r.cl;// 1回に加算するディケイレベル
            r.overflow = (ushort)(r.ax + r.dx) > 0x7fff;//熊：注意
            r.ax += r.dx;
            if (r.overflow) r.ax = 0x7fff;
            //decay1:
        }

        private void set_attackrate()
        {
            ushort axbk = r.ax;
            r.al = data_buff[r.si + AR];// アタックカウンタの値転送
            bool beflg = r.al - 30 <= 0;
            r.al -= 30;
            if (beflg)
            {
                r.al = 1;
            }
            data_buff[r.si + AC] = r.al;
            r.ax = axbk;
        }

        //;--------------------------------------
        //;	RHYTHMの音長カウンタ処理
        //;--------------------------------------

        private void play_rhythm()
        {
            r.al--;
            if (r.al == 0)
            {
                mapplay_ems();// 演奏バッファのマッピング
                main_rhythm();
            }

            //nextstep3:
            data_buff[r.si + LC] = r.al;// 次の音長データのセット
            return;//ch_skip
        }

        //------------------------------------
        //	PCMの音長カウンタ処理
        //------------------------------------

        private void play_pcm()
        {
            r.al--;
            if (r.al == 0)
            {
                mapplay_ems();// 演奏バッファのマッピング
                main_pcm();
                if (initia0Flg) return;
            }
            //nextstep4:
            data_buff[r.si + LC] = r.al;// 次の音長データのセット

            //	PCMのLFO処理
            ushort axbk = r.ax;
            calc_lfo();// LFOカウンタの計算
            if (!r.carry)
            {
                if (r.zero)
                {
                    vol_lfo();
                    ushort sivsbk = (ushort)(
                        data_buff[r.si + VS]
                        + data_buff[r.si + VS + 1] * 0x100//熊:多分不要
                        );
                    data_buff[r.si + VS] = r.al;
                    volchgr(data_buff);// 音量の変更
                    data_buff[r.si + VS] = (byte)sivsbk;
                    data_buff[r.si + VS + 1] = (byte)(sivsbk >> 8);//熊:多分不要
                }
                //pmd_pcm:
                r.dx = (ushort)(
                        data_buff[r.si + FD]
                        + data_buff[r.si + FD + 1] * 0x100
                        ); // DX = DELTA-N
                freq_master_lfo();
                r.dx += r.ax;
                if (!check_86pcm()) // 拡張86PCM ?
                {
                    r.dx <<= 3;
                    r.ah = r.dl;
                    r.al = 9;
                    outdata2a();// DELTA-Nの設定
                    r.ah = r.dh;
                    r.al = 0xa;
                    outdata2a();
                }
                else
                {
                    //pmd_pcm86:
                    calc_pcmwork(data_buff);
                    //pcm0work[r.di].pcm0freq[1] = r.dx;// 拡張PCM用周波数データ
                    nax.pc98.OutportC4231_Freq((byte)r.di,1, r.dx);// 拡張PCM用周波数データ
                }
            }
            //exit_pcm:
            r.ax = axbk;

            //	PCMの音長割合処理

            if ((data_buff[r.si + TI] & 2) != 0)// タイ処理なら無視
            {
                return;
            }
            if (r.al != data_buff[r.si + RS])
            {
                return;
            }
            rest2_main();// 音長割合時間がくればカット
        }

        /// <summary>
        ///------------------------------------
        ///	発音マスクチェック
        ///	exit	NZ = マスク中
        ///------------------------------------
        /// </summary>
        private void check_mask()
        {
            ushort dxbk = r.dx;
            ushort cxbk = r.cx;
            ushort axbk = r.ax;

            r.ch = realch;
            calcbit();
            r.zero = (ch_mask1 & r.ax) == 0;
            if (r.zero)
            {
                r.zero = (ch_mask2 & r.dl) == 0;
            }

            //_mask1:
            r.ax = axbk;
            r.cx = cxbk;
            r.dx = dxbk;
        }

        /// <summary>
        ///	LFOカウンタの計算
        ///	exit	CY = LFOなし
        ///		NZ = PMD
        ///		AX = 変移データ
        /// </summary>
        private void calc_lfo()
        {
            r.carry = false;
            r.ah = data_buff[r.si + TI];// LFOありか
            if ((r.ah & 0x40) == 0)
            {
                r.carry = true;
                return;
            }
            r.al = data_buff[r.si + LS];
            r.al -= data_buff[r.si + LC];// AL = キーオンからのカウント値
            int flg = 0;
            if ((r.ah & 2) != 0)// タイ指定中か
            {
                //clfo7:
                r.al = data_buff[r.si + LA];
                r.al++;
                if (r.al == 0)
                {
                    // 前回までの音長を加算
                    //clfo11:
                    r.al = data_buff[r.si + LK];
                    flg = 1;//jmp clfo10
                }
                else
                {
                    data_buff[r.si + LA] = r.al;
                    flg = 2;//jmp clfo12
                }
            }
            if (flg == 0)
            {
                data_buff[r.si + LA] = r.al;// カウント値を保存
            }
            if (flg == 0 || flg == 2)
            {
                //clfo12:
                r.carry = r.al < data_buff[r.si + LD];
                if (r.carry)// LFOディレイ
                {
                    return;
                }
                r.al -= data_buff[r.si + LD];
                data_buff[r.si + LK] = r.al;// LFOインクリメント値の保存
            }

            //clfo10:
            ushort dxbk = r.dx;
            ushort cxbk = r.cx;
            r.ch = data_buff[r.si + LI];
            r.ch &= 0xf;// 休符ビットなどをクリア
            r.ax = (ushort)(r.al * r.ch);// インクリメント値
            r.ax >>= 5;// ***V2.12(4)
            r.ch = 0;
            r.cl = data_buff[r.si + LL];
            r.ax += r.cx;
            if (r.ax > 127)
            {
                r.al = 127;
            }
            r.cl = r.al;// AL,CL = レベル値
            r.ax = (ushort)(short)((sbyte)r.al * (sbyte)data_buff[r.si + LF]);
            ushort cxbk2 = r.cx;
            r.cl = data_buff[r.si + LB];// CL = スピードベース
            r.cl &= 3;
            r.sign = ((sbyte)r.cl - 1) < 0;
            r.cl--;
            if (!r.sign)
            {
                //speedb1:
                r.ax = (ushort)((ushort)r.ax << r.cl);
            }
            else
                r.ax = (ushort)((short)r.ax >> 1);
            //speedb2:
            if ((data_buff[r.si + TI] & 4) != 0)// 鋸LFOでは速度係数を1/4にする
            {
                r.ax = (ushort)(((short)r.ax) >> 2);
            }
            r.cx = cxbk2;
            r.dx = (ushort)(data_buff[r.si + FC] + data_buff[r.si + FC + 1] * 0x100);
            if ((data_buff[r.si + TI] & 0x10) == 0)// 加算中/減算中か
            {
                //clfo_add:
                int ans = (short)r.ax + (short)r.dx;
                r.ax = (ushort)ans;// (ushort)(ans > 0x7fff ? 0x7fff : ans);
                //r.overflow = (r.ax + r.dx > 0x7fff);
                //r.ax += r.dx;
            }
            else
            {
                int ans = (short)r.dx - (short)r.ax;
                r.ax = (ushort)ans;// (ushort)(ans < -32768 ? -32768 : ans);
                //r.overflow = ((short)r.ax > (short)r.dx);
                //r.dx -= r.ax;
                //r.ax = r.dx;
            }
            //if (r.overflow)
            //{
            //    //clfo6:
            //    if ((r.ah & 0x80) != 0)
            //    {
            //        //clfo5:
            //        r.ax = 0x7fff;// オーバフローした時の値
            //    }
            //    else
            //    {
            //        r.ax = 0x8000;// オーバフローした時の値
            //    }
            //}

            //clfo1:
            r.dl = r.ah;
            if ((r.dl & 0x80) != 0)
            {
                r.dl = (byte)-r.dl;//	exen	ne,< neg dl >
            }

            while (true)
            {
                r.carry = (r.dl < r.cl);
                if (!r.carry)// 変移レベルを超えたか
                {
                    r.dx = (ushort)(data_buff[r.si + LR] + data_buff[r.si + LR + 1] * 0x100);// DL = LFOくり返し回数 **[si+RC]**
                    if (r.dl != 0)// 0の場合は連続
                    {
                        r.dh++;// DH = LFOくり返しカウンタ
                        if (r.dh >= r.dl) break;// 指定回数以上の場合は終了
                        data_buff[r.si + RC] = r.dh;// くり返しカウンタを次へ
                    }
                    //clfo9:
                    r.dl = data_buff[r.si + TI];
                    if ((r.dl & 8) != 0) break;// ワンショットLFOか
                    if ((r.dl & 4) == 0)// ノコギリLFOか
                    {
                        //clfo8:
                        data_buff[r.si + TI] ^= 0x10;
                    }
                    else
                        r.ax = 0;// 初期値に戻す
                }
                //clfo2:
                data_buff[r.si + FC] = r.al;
                data_buff[r.si + FC + 1] = r.ah;
                break;
            }

            //clfo4:
            r.cx = cxbk;
            r.dx = dxbk;
            r.carry = false;
            r.zero = ((data_buff[r.si + TI] & 0x20) == 0);// AMD/PMD のチェック

            //clfo3:
            //Log.writeLine(LogLevel.ERROR, string.Format("AX:{0:x04}", r.ax));
        }

        //;-----------------------------------
        //;	LFO周波数の補正
        //;	entry	DX = 周波数成分
        //;		AH = 変移レベル
        //;	exit	AX = 加算値
        //;-----------------------------------

        private void freq_master_lfo()
        {
            r.push(r.cx);
            r.cx = 0;
            r.cl = data_buff[r.si + LI];
            r.cx >>= 4;// CL = 0～7,8～F
            r.cl &= 7;
            r.cx++;// CX = LFOベース値(1～8)
            freq_lfo2();
        }

        private void freq_lfo()
        {
            r.push(r.cx);
            r.cx = 3;// ***V2.12(4) 内部演算用
            freq_lfo2();
        }

        private void freq_lfo2()
        {
            r.push(r.dx);
            r.dx >>= 4;
            r.al = r.ah;
            r.ax = (ushort)(short)(sbyte)r.al;
            int ans = (short)r.ax * (short)r.dx;
            r.dx = (ushort)(ans >> 16);
            r.ax = (ushort)ans;
            //freq_lfo1:
            do
            {
                r.carry = (r.dx & 1) != 0;
                r.dx = (ushort)((short)r.dx >> 1);
                r.ax = r.rcr(r.ax, 1);
                r.cx--;
            } while (r.cx > 0);

            r.dx = r.pop();
            r.cx = r.pop();
        }

        //･･････････････････････････････････････
        //	システムディチューンの設定
        //	entry	AX = 周波数成分
        //	exit	AX = 変移値
        //･･････････････････････････････････････

        private void freq_detune_lfo()
        {
            r.ah = data_buff[r.si + SE];
            r.push(r.cx);
            r.cx = 7;// for システムディチューン(@ds5)
            freq_lfo2();
        }

        //--------------------------
        //	LFO音量の補正
        //--------------------------
        private void vol_lfo()
        {
            r.al = data_buff[r.si + VS];
            vol_lfo_ssg();
        }

        private void vol_lfo_ssg()
        {
            r.al += r.ah;
            if (r.al > 127)// オーバフローしたか
            {
                if ((r.ah & 0x80) != 0)// 加算なら@V127、減算なら@V0
                {
                    //vlfo2:
                    r.al = 0;
                }
                else r.al = 127;
            }
            //vlfo1:
            return;
        }

        //;===============================================================
        //;	FM-OPN 演奏制御処理
        //;		CH : channel number (0,1,2,6,7,8,11～16)
        //;		SI : data buffer address
        //;		ES : object data segment
        //;
        //;	command		FF : Rest music
        //;			FE : End (loop)
        //;			FD : End (break)
        //;			FC : This channel skip_play
        //;			FB : wait on '
        //;			FA : 3ch 4harm play (9bytes)
        //;			F9 : Same frequency play
        //;			F8 : Add frequency (4bytes)
        //;			F7 : N times loop (4bytes)
        //;			F6 : Pan set (2bytes)
        //;			F5 : Timer-A tempo (3bytes)
        //;			F4 : Length/Ratio change (3bytes)
        //;			F3 : Wait all channel
        //;			F2 : Nop (4bytes)
        //;			F1 : Register data set (3bytes)
        //;			F0 : System detune set (2bytes)
        //;			EF : Hard LFO speed (2bytes)
        //;			EE : Hard LFO AMD,PMD,AMon (3bytes)
        //;			ED : 3ch 4harm mode (2bytes)
        //;			EC : Key display mask on/off & color (2bytes)
        //;			EB : Tone number change (2bytes)
        //;			EA : @Jump (3bytes)
        //;			E9 : @Call (3bytes)
        //;			E8 : @Ret
        //;			E7 : Source Line symbolic information (3bytes)
        //;			E6 : Set USR Tone parameter (27bytes)
        //;			E5 : Play Stack Initialize
        //;			E4 : @If Jump (5bytes)
        //;			E3 : @If Call (5bytes)
        //;			E2 : Volume data change (2bytes)
        //;			E1 : Tie
        //;			E0 : Loop counter clear
        //;			DF : Slur
        //;			DE : Ratio only change (2bytes)
        //;			DD : Set comment length (2bytes)
        //;			DC : Init Skip_data (4bytes)
        //;			DB : Comment set (4-46bytes)
        //;			DA : X Value set (4bytes)
        //;			D9 : LFO parameter set (7bytes)
        //;			D8 : LFO start(pmd,amd)/stop (2bytes)
        //;			D7 : Volume add (2bytes)
        //;			D6 : Volume sub (2bytes)
        //;			D5 : Nop (3bytes)
        //;			D4 : Nop (5bytes)
        //;			D3 : @If Exit (5bytes)
        //;			D2 : Play Stack +1
        //;			D1 : Fade out
        //;			D0 : SSG/PCM mode(2bytes)
        //;			CF : Send channel change(2bytes)
        //;			CE : Set last tone,volume
        //;		     00-3F : Music code (2bytes) b7,b6=0
        //;	各バイト数を変更する時はskip_byteも変更すべし.
        //;===============================================================

        private void main_fm()
        {
            getentry();// BX = 演奏データの番地
            if (!r.zero)
            {
                r.al = data_buff[r.si + VS];
                volchgm(data_buff);// フェードアウトに従って音量変更
            }

            //back_fm:
            ushort dxbk;
            do
            {
                //dxbk=0:dummy
                //dxbk=1:recov
                //dxbk=2:back_fm
                //dxbk=3:recovw
                recovFlg = false;
                recovwFlg = false;
                another1Flg = false;
                dxbk = 0;

                work.crntMmlDatum = nax.objBuf[0][r.bx];
                r.ax = (ushort)((byte)nax.objBuf[0][r.bx].dat + ((r.bx + 1) >= nax.objBuf[0].Length ? 0 : ((byte)nax.objBuf[0][r.bx + 1].dat * 0x100)) );
                if (r.al < 0xce)
                {
                    another();
                    if (recovFlg)
                    {
                        dxbk = 1;
                        break;
                    }
                }

                r.di = 0;//ofs:jump_table1
                getadrs();// コマンド分岐処理
                check_ret();// リターンアドレスを分ける
                if (r.carry)
                {
                    r.dx = 2;//ofs:back_fm
                }
                dxbk = r.dx;//熊：戻り先をスタックに積んでいる
                r.bx++;
                r.al = r.ah;
                //if(r.si==12) Log.writeLine(LogLevel.ERROR, string.Format("{0}",r.di/2));
                jump_table1[r.di / 2]();
                if (another1Flg)
                {
                    another1();
                    if (recovFlg)
                    {
                        dxbk = 1;
                        break;
                    }
                }
                if (rechannelFlg) return;
            } while (dxbk == 2);

            if (dxbk == 1) recov();

        }

        private Action[] jump_table1;
        private void SetJumptable1()
        {
            jump_table1 = new Action[] {
                rest,       quit,//                   FF(0)
                quit2,      stopm,//                  FD(2)
                kwait,      code_play,//              FB(4)
                same_play,  addfreq,//                F9(6)
                n_loop,     pan,//                    F7(8)
                tempoa,     mlength,//	              F5(10)
                wait_r,     continue_,//              F3(12)
                set_reg,    sysdetune,//              F1(14)
                hlfo_speed, hlfo_data,//              EF(16)
                codeset,    key_mask,//               ED(18)
                tone2608,   jump_to,//	              EB(20)
                call_to,    ret_to,//                 E9(22)
                symbol,     usr_tone,//               E7(24)
                sinit,      if_jump,//                E5(26)
                if_call,    vol_change,//             E3(28)
                tie2,       c_loop,//                 E1(30)
                tie_tone,   ratio_change,//           DF(32)
                setcomlen,  initia,//                 DD(34)
                mcomment,   value,//                  DB(36)
                lfopara,    lfoset,//                 D9(38)
                voladd1,    volsub1,//                D7(40)
                continue_,  continue_,//              D5(42)
                if_exit,    pops,//                   D3(44)
                fade_outs,  pcm_change,//             D1(46)
                channel,    last_set//                CF(48)
            };
        }

        private void getadrs()
        {
            ushort axbk = r.ax;
            r.al = (byte)~r.al;
            r.al *= 2;
            r.ax = (ushort)(sbyte)r.al;
            r.di += r.ax;
            r.ax = axbk;
        }

        private void check_ret()
        {
            r.dx = 1;//ofs:recov ; デフォルトのリターン番地
            if (r.al >= 0xf8)// リターンアドレスを分ける
            {
                //skipret:
                r.carry = false;
                return;
            }
            exit_flg++; // ウォッチドッグタイマもどき
            if (exit_flg == 0)
            {
                //skipret:
                r.carry = false;
                return;
            }
            r.carry = true;// CY = 再びループする Ret Adrs
            return;
        }

        private void getentry()
        {
            r.bx = (ushort)(data_buff[r.si + AD] + data_buff[r.si + AD + 1] * 0x100);
            data_buff[r.si + TI] &= 0xfc;
            r.zero = (fade_count == 0);// check fade_out
        }

        //-------------------------------
        //	音符制御コード処理
        //-------------------------------

        private void another()
        {
            check_tie();
            (r.ah, r.al) = (r.al, r.ah);
            data_buff[r.si + FB] = 0;// 小数桁はクリア
            r.dx = r.ax;
            ushort dxbk = r.dx;
            r.dh &= 7;// DX = 26ah～48fh
            freq_detune_lfo();// システムディチューンの設定
            r.ax += r.dx;
            r.dx = dxbk;
            r.dx &= 0xf800;
            r.ax += r.dx;
            setkeyon();// 休符ビットをクリア,キーオンフラグ
            another1();
        }

        private void another1()
        {
            data_buff[r.si + FD] = r.al;
            data_buff[r.si + FD + 1] = r.ah;
            setfreq1();// YM2608の周波数設定
            r.bx += 2;
            sync();// LFO SYNC チェック
            keyon();
        }

        private void keyon()
        {
            check_mask();// チャネルマスクのチェック
            if (!r.zero)
            {
                recovFlg = true;
                return;
            }
            if ((data_buff[r.si + TI] & 3) == 0)// タイフラグチェック
            {
                r.al = data_buff[r.si + LS];
                if (r.al - data_buff[r.si + RS] <= 0)
                {
                    recovFlg = true;
                    return;
                }
            }
            //keyon2:
            r.ax = 0xf028;
            calc_keych();
            if (r.carry)
            {
                recovFlg = true;
                return;
            }
            outdata6a();// キーオン
            recovFlg = true;
        }

        private void recov()
        {
            r.al = data_buff[r.si + LS];// 音長カウント値を獲得
            recovw();
        }

        private void recovw()
        {
            data_buff[r.si + AD] = r.bl;// 演奏番地を保存
            data_buff[r.si + AD + 1] = r.bh;
            exit_flg = 0;// ウォッチドッグタイマリセット
        }

        //;････････････････････････････････････････････････････････
        //;	キーオン・オフチャネルの計算
        //;	entry	CH = チャネル番号(0～3,6～9,11～16)
        //;		AH = スロットデータ(b7～b4)
        //;	exit	CL : CH=0～3,11～13 → 0～3
        //;		     CH=6～9,14～16 → 4～6
        //;		CY = 出力禁止(for Extended 2203)
        //;････････････････････････････････････････････････････････
        private void calc_keych()
        {
            byte cl;
            byte ch = r.ch;
            cl = ch;

            if (ch < 6)
            {
                //keyon1:
                r.carry = (r.ah + cl > 255);
                r.ah += cl;
                return;
            }

            cl -= 2;

            if (ch < 11)
            {
                //keyon1:
                r.carry = (r.ah + cl > 255);
                r.ah += cl;
                return;
            }

            cl -= 9;

            if (ch >= 14)
                //cyon:
                cl++;

            //keyon1:
            r.carry = (r.ah + cl > 255);
            r.ah += cl;
        }

        //------------------------------------------
        //	YM2608に周波数を設定
        //	entry	AH = f-number2 + block
        //		AL = f-number
        //		CH = channel number
        //------------------------------------------

        private void setfreq1()
        {
            ushort axbk = r.ax;
            r.al = 0xa4;// f-number 2 + block
            r.al += r.ch;
            outdataa();
            r.ax = axbk;
            r.ah = r.al;
            r.al = 0xa0;// f-number 1
            r.al += r.ch;
            outdataa();// 周波数を設定
        }

        private void same_play()
        {
            key_off();// 同じ周波数で発音(TIEは無効)
            keyon();
        }

        //;---------------------------------
        //;	周波数を加算して発音
        //;---------------------------------

        private void addfreq()
        {
            r.push(r.cx);
            r.ax = (ushort)(data_buff[r.si + FD] + data_buff[r.si + FD + 1] * 0x100);
            r.cx = r.ax;// AX = オクターブ以下の周波数成分
            r.ax &= 0x7ff;
            r.cx = (ushort)(((short)r.cx) >> 11);// CX = オクターブ(0-7)
            r.dx = 0;
            r.dl = r.ah;
            r.ah = r.al;
            r.al = data_buff[r.si + FB];// 256倍する
            if (r.cx != 0)
            {
                //afreq1:
                do
                {
                    r.carry = ((r.ax & 0x8000) != 0);
                    r.ax = (ushort)(((short)r.ax) << 1);
                    r.dx = r.rcl(r.dx, 1);
                    r.cx--;
                } while (r.cx != 0);// DXAX = 展開した周波数データ
            }

            //afreq5:
            r.cx = 0;
            r.cl = (byte)nax.objBuf[0][r.bx + 2].dat;
            if (r.cl > 0x7f)
            {
                r.ch--;// 符号拡張
            }
            uint ans = (uint)(r.ax + (byte)nax.objBuf[0][r.bx].dat + (byte)nax.objBuf[0][r.bx + 1].dat * 0x100);
            r.carry = (ans > 0xffff);
            r.ax = (ushort)ans;
            r.dx += (ushort)(r.cx + (r.carry ? 1 : 0));
            r.cx = 0;// DXAX = 復元用データ

            //afreq3:
            while (r.dh != 0)
            {
                r.carry = ((r.dx & 0x0001) != 0);
                r.dx = (ushort)(r.dx >> 1);
                r.ax = r.rcr(r.ax, 1);
                r.cx++;// オクターブを求める
            }

            //afreq2:
            while (r.dl >= 4)
            {
                if (r.dl == 0)
                {
                    if (r.ax < 0xd400)
                        break;
                }
                //afreq6:
                r.carry = ((r.dl & 0x01) != 0);
                r.dl = (byte)(r.dl >> 1);
                r.ax = r.rcr(r.ax, 1);
                r.cx++;
            }

            //afreq4:
            data_buff[r.si + FB] = r.al;
            r.al = r.ah;
            r.ah = r.dl;
            r.cx = (ushort)(((ushort)r.cx) << 11);// CX = オクターブ
            r.ax |= r.cx;// AX = 周波数データ
            r.cx = r.pop();
            r.bx++;
            data_buff[r.si + TI] |= 1;// Q8にする
            clearrest();// 休符ビットクリア
            another1Flg = true;// 周波数設定へ
        }

        //;-----------------------------
        //;	休符コマンド処理
        //;-----------------------------

        private void rest()
        {
            key_off();
            rest0();
        }

        private void rest0()
        {
            data_buff[r.si + LI] |= 0x80;// 休符フラグを立てる
        }

        private void continue_()
        {
            //熊:何もやらない
        }

        //;---------------------------
        //;	タイのチェック
        //;---------------------------

        private void check_tie()
        {
            if ((data_buff[r.si + TI] & 2) == 0)// タイフラグチェック
            {
                key_off();// まずキーオフ
            }
        }

        //;-----------------------------------------
        //;	コードネームによる3CH和音演奏
        //;-----------------------------------------

        private void code_play()
        {
            setkeyon();// 休符ビットをクリア,キーオンフラグ
            check_tie();// タイのチェック
            sync();
            r.ax = (ushort)((byte)nax.objBuf[0][r.bx + 6].dat+ (byte)nax.objBuf[0][r.bx + 7].dat*0x100);
            (r.ah, r.al) = (r.al, r.ah);
            data_buff[r.si + FD] = r.al;// 根音の保存
            data_buff[r.si + FD+1] = r.ah;
            data_buff[r.si + FB] = 0;// 小数桁はクリア

            r.dx = 0xa2a6;// op4
            set_freq();
            r.dx = 0xa8ac;// op3
            set_freq();
            r.dx = 0xaaae;// op2
            set_freq();
            r.dx = 0xa9ad;// op1
            set_freq();
            keyon();
        }

        //;-----------------------------------
        //;	3CH和音演奏モードの指定
        //;-----------------------------------

        private void codeset()
        {
            r.bx++;
            if (r.ch == 2)
            {
                codem1 = r.al;
                return;
            }
            //codeset14:
            codem2 = r.al;
            r.al = 0x27;
            r.ah |= 0xc;// enableにする
            outdata0a();// for YM3438,2203
        }

        //;-------------------------------------------
        //;	3CH周波数の設定
        //;	entry	DL = f2+block のアドレス
        //;		DH = f1
        //;		ES:BX = 演奏データ番地
        //;-------------------------------------------

        private void set_freq()
        {
            r.ah = (byte)nax.objBuf[0][r.bx].dat;
            r.al = r.dl;
            outdata0a();
            r.bx++;
            r.ah = (byte)nax.objBuf[0][r.bx].dat;
            r.al = r.dh;
            outdata0a();
            r.bx++;
            return;
        }

        //--------------------------------
        //	FM キーオフルーチン
        //--------------------------------
        private void key_off()
        {
            ushort axbk = r.ax;
            r.ax = 0x28;
            calc_keych();
            if (!r.carry) outdata6a();
            r.ax = axbk;
        }

        //;-----------------------------
        //;	タイコマンド処理
        //;-----------------------------

        private void tie2()
        {
            data_buff[r.si + TI] |= 2;// ~タイフラグon
            return;
        }

        private void tie_tone()
        {
            data_buff[r.si + TI] |= 1;// &タイフラグon
        }

        private void kwait()
        { 
            return;
        }

        //;-------------------------------------------------------
        //;	演奏停止、再開、スタック初期化コマンド処理
        //;-------------------------------------------------------

        private void quit2()
        {
            music_stop();
            quit();
        }

        private void quit()
        {
            end_flug = 1;
            sinit();
        }

        private void sinit()
        {
            data_buff[r.si + RA] = 0;// ループスタックを初期化する
            init_cnt++;// 1ループ終了フラグを立てる
        }

        //==================================
        //	音長変更コマンド処理
        //==================================

        private void mlength()
        {
            //Lコマンド
            data_buff[r.si + LS] = r.al;
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;// 音長割合データ
            ratio_change();
        }

        private void ratio_change()
        {
            //Qコマンド
            nax.pc98.OutportBDummy(port1);
            data_buff[r.si + RS] = r.al;
            r.bx++;
        }

        //;===================================
        //;	テンポ変更コマンド処理
        //;===================================

        private void tempoa()
        {
            r.al = (byte)nax.objBuf[0][r.bx].dat;
            r.ah = (byte)nax.objBuf[0][r.bx + 1].dat;
            r.bx++;
            tempo[0] = r.al;
            tempo[1] = r.ah;
            tempo[2] = r.al;
            tempo[3] = r.ah;
            r.bx++;
            return;
        }

        //;//////////////////////////////////
        //;	（）繰り返し命令処理
        //;//////////////////////////////////

        private void n_loop()
        {
            r.dl = (byte)nax.objBuf[0][r.bx].dat;
            r.dh = (byte)nax.objBuf[0][r.bx + 1].dat;// オフセット値
            r.bx += 2;
            pops();// カウント値をpop
            if (r.al == (byte)nax.objBuf[0][r.bx].dat)// AL = カウンタのデータ
            {
                //loop_end:
                r.bx++;
                return;
            }
            r.al++;
            pushs();// +1したカウント値を保存
            r.bx -= 3;
            r.bx -= r.dx;// 演奏番地にオフセット値を減算
            return;
        }

        private void c_loop()
        {
            r.al = 1;// カウンタ開始値をpush↓(pushs)
            pushs();
        }
        //;////////////////////////////////////////
        //;	ループスタックに対するpush
        //;	entry	AL = write data
        //;		CH = チャネル番号
        //;		SI = ワーク番地
        //;////////////////////////////////////////

        private void pushs()
        {
            r.push(r.di);
            pushs_main();//di loopcnt
            loopcnt[r.di] = r.al;
            r.di = r.pop();
            r.al = data_buff[r.si + RA];
            if (r.al < 15)
            {
                r.al++;
            }
            data_buff[r.si + RA] = r.al;
            return;
        }

        //;////////////////////////////////////////
        //;	ループスタックに対するpop
        //;	entry	CH = チャネル番号
        //;		SI = ワーク番地
        //;	exit	AL = read data
        //;////////////////////////////////////////

        private void pops()
        {
            r.al = data_buff[r.si + RA];
            if (r.al != 0)
            {
                r.al--;
            }
            data_buff[r.si + RA] = r.al;

            r.push(r.di);
            pushs_main();
            r.al = loopcnt[r.di];
            r.di = r.pop();
            return;
        }

        //;////////////////////////////////////////
        //;	ループスタックに対する読みだし
        //;	entry	CH = チャネル番号
        //;		SI = ワーク番地
        //;		AL = 6-F : X0-X9 の変数
        //;	exit	AL = read data
        //;////////////////////////////////////////

        private void gets()
        {
            r.al &= 0xf;
            if (r.al >= 6)
            {
                getx();
                return;
            }
            ushort sibk = (ushort)(data_buff[r.si + RA]+data_buff[r.si+RA+1]*0x100);
            pops();
            data_buff[r.si + RA] = (byte)sibk;
            data_buff[r.si + RA + 1] = (byte)(sibk >> 8);
        }

        private void getx()
        {
            ushort dibk = r.di;
            getx_adrs();
            r.al = loopcnt[r.di]; // AL = ループカウンタ値
            r.di = dibk;
            //	ret
        }

        private void getx_adrs()
        {
            r.ah = 0;
            r.al &= 0xf;
            r.al -= 6;//AL = 変数番号(0-9)
            r.di = 0;//ofs:loopcnt ; カウンタバッファ
            r.di += r.ax;
            r.al = realch;// チャネル番号(0-16)
            r.ax <<= 4;
            r.di += r.ax;// 16倍した値を加算
            //	ret
        }

        private void pushs_main()
        {
            r.push(r.ax);
            r.ax = 0;
            r.al = realch;
            r.ax <<= 4;// AX = 各チャネルに応じたオフセット
            r.ax += 15;//ofs:loopcnt+15
            r.di = r.ax;
            r.ax = 0;
            r.al = data_buff[r.si + RA];
            r.di -= r.ax;// DI = 現在のスタックポインタの位置
            r.ax = r.pop();
            return;
        }

        //;//////////////////////////////
        //;	変数設定命令処理
        //;//////////////////////////////

        private void value()
        {
            r.push(r.di);
            r.push(r.ax);
            getx_adrs();// DS:[DI] = 格納する番地
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;
            //r.al += 6;//熊：???????????????????
            getx();
            r.dh = r.al;// DH = 元のデータ
            r.ax=r.pop();
            r.bx++;
            r.dl = (byte)nax.objBuf[0][r.bx].dat;// DL = イミディエイトデータ
            r.bx++;
            r.al >>= 4;
            if (r.al != 0)// イミディエイト設定
            {
                r.al--;
                if (r.al != 0)// 加算
                {
                    r.dl = (byte)-r.dl;
                }
                //value_add:
                r.dh += r.dl;
                r.dl = r.dh;
            }
            //value1:
            loopcnt[r.di] = r.dl;
            r.di = r.pop();
            return;
        }

        //;/////////////////////////////////////////
        //;	オブジェクトジャンプ命令処理
        //;/////////////////////////////////////////

        private void call_to()
        {
            r.ax = r.bx;
            r.ax += 2;// 次のデータ番地
            pushs();
            r.al = r.ah;
            pushs();// リターンアドレスをpush
            jump_to();
        }


        private void jump_to()
        {
            r.dx = (ushort)((byte)nax.objBuf[0][r.bx].dat + (byte)nax.objBuf[0][r.bx + 1].dat * 0x100);
            r.dx--;
            r.bx += r.dx;

            if (labelPtr == null) return;

            int ptr = realch * 40;
            for (int i = 0; i < 40; i++)
            {
                if (r.bx == labelPtr[ptr + i] && labelPassCnt[ptr + i] < 255)
                {
                    labelPassCnt[ptr + i]++;
                    if (labelPassCnt[ptr + i] == 0) labelPassCnt[ptr + i]++;
                }
            }
            return;
        }

        private void ret_to()
        {
            pops();
            r.bh = r.al;
            pops();
            r.bl = r.al;// リターンアドレスをpop
            return;
        }

        //;=====================================
        //;	条件ジャンプ､コール処理
        //;=====================================

        private void if_jump()
        {
            if_main();
            if (!r.carry)
            {
                jump_to();// 一致すればジャンプ
                return;
            }
            not_exit();
        }

        private void if_exit()
        {
            if_main();
            if (r.carry)
            {
                not_exit();// 一致すればジャンプ
                return;
            }
            pops();// スタックを移動してからループ外に抜ける
            jump_to();
        }

        private void if_call()
        {
            if_main();
            if (!r.carry)
            {
                call_to();// 一致すればコール
                return;
            }
            not_exit();
        }

        private void not_exit()
        {
            r.bx++;
            r.bx++;
        }

        private void if_main()
        {
            r.dl = r.al;
            gets();// カウンタ値を読み出す
            r.bx++;
            r.ah = (byte)nax.objBuf[0][r.bx].dat; // 条件値と比較
            r.bx++;
            r.dl &= 0xf0;

            if (r.dl == 0)
            {
                //if_equal:
                r.zero = (r.al - r.ah == 0);
                r.carry = false;
                if (!r.zero) r.carry = true; // @if x=n jump ･･･
                return;
            }
            if (r.dl == 0x10)
            {
                //if_above:
                r.zero = (r.al - r.ah == 0);
                r.carry = (r.al - r.ah < 0);
                if (r.zero) r.carry = true; // @if x>n jump ･･･
                return;
            }
            if (r.dl == 0x20)
            {
                //if_borrow:
                r.zero = (r.al - r.ah == 0);
                r.carry = (r.al - r.ah < 0);
                r.carry = !r.carry;// @if x<n jump ･･･
                return;
            }

            r.zero = (r.al - r.ah == 0);
            r.carry = false;
            if(r.zero)r.carry = true; // @if x!n jump ･･･
            return;
        }

        //;------------------------------
        //;	音量の加算・減算
        //;------------------------------

        private void add_vol()
        {
            r.al += data_buff[r.si + VS];
            if (r.al > 127)
            {
                r.al = 127;
            }
            return;
        }

        private void sub_vol()
        {
            r.al = data_buff[r.si + VS];
            r.carry = r.al < r.ah;
            r.al -= r.ah;
            if (r.carry) r.al = 0;
            return;
        }

        private void voladd1()
        {
            add_vol();
            vol_change();
        }

        private void volsub1()
        {
            sub_vol();
            vol_change();
        }

        //;============================================
        //;	音量値変更サブルーチン (FM OPN)
        //;	entry	CH = channel number
        //;		AL = volume data (0-7Fh)
        //;============================================

        private void vol_change()
        {
            volset();// 音量データの保存
            volchgm(data_buff);
        }

        private void volchgm(byte[] siBuf)
        {
            ushort axbk = r.ax;
            ushort bxbk = r.bx;
            ushort cxbk = r.cx;
            ushort dxbk = r.dx;
            ushort dibk = r.di;
            ushort sibk = r.si;

            ushort dsbk = r.ds;

            r.al = siBuf[r.si + TS];// 音色番号を獲得 (AL)
            byte[] toneBuf = tone_adrs();// DS:BX = その番号のデータ格納番地
            r.al = toneBuf[r.bx + 0x18];// オペ接続形態のデータ
            r.al &= 7;
            //	segcs 熊:たぶん不要
            r.al = con_data[0 + r.al];//xlat;どのオペが出力かビットで表現

            r.bx += 4;// DS:BX = 出力レベル格納番地
            r.di = 0;//ofs:out_data
            r.cl = 4;
            //loop_1:
            do
            {
                r.dl = siBuf[r.si + VS];// DL : 現在の音量値
                r.carry = (r.al & 1) != 0;
                r.al >>= 1;// オペ1-3が出力かどうかチェック
                if (r.carry)
                {
                    operate(toneBuf, out_data);// 出力オペに計算した出力レベルを設定
                }
                else
                {
                    // （オペ4は常に出力）
                    //skip_op9:
                    ushort axbk2 = r.ax;
                    r.al = out_data[r.di];// 出力するアドレス
                    r.al += 0x40;
                    r.al += r.ch;
                    r.ah = toneBuf[r.bx];// 総合出力を設定
                    outdataa();
                    r.ax = axbk2;
                }
                //skip_op11:
                r.di++;// 次のオペレータへ
                r.bx++;
                r.cl--;
                if (work.crntMmlDatum != null)
                {
                    MmlDatum md = new MmlDatum(work.crntMmlDatum.dat);
                    work.crntMmlDatum = md;//1opだけ音量情報を有効にする
                }
            } while (r.cl > 0);

            r.ds = dsbk;

            r.si = sibk;
            r.di = dibk;
            r.dx = dxbk;
            r.cx = cxbk;
            r.bx = bxbk;
            r.ax = axbk;
        }

        //------------------------------------------------------
        //	オペレータの総合出力を計算して設定
        //	entry	DL = volume data (0-7f)
        //		BX = tone OP total data address
        //------------------------------------------------------

        private void operate(byte[] toneBuf, byte[] diBuf)
        {
            ushort dxbk = r.dx;
            ushort axbk = r.ax;

            r.al = toneBuf[r.bx];// AL = オペnの総合出力値
            multi();// DL = 音量
            r.al = r.ah;// 計算結果

            r.dx = fade_count;
            // フェードアウトチェツク
            if (r.dx != 0)
            {
                r.dx >>= 1;// DH = fade vol data (7Fh-0)
                r.dl = r.dh;// 1/2 fade vol
                r.dl = (byte)~r.dl;
                r.dl -= 0x80;
                multi();// フェードアウト時は再計算
            }

            //skip_op10:
            r.al = diBuf[r.di];
            r.al += 0x40;// 出力アドレス
            r.al += r.ch;
            outdataa();

            r.ax = axbk;
            r.dx = dxbk;
        }

        //-------------------------------------------
        //	出力音量の計算ルーチン
        //	entry	AL = total level (7fh-0)
        //		DL = volume data (0-7fh)
        //	exit	AH = total level (7fh-0)
        //-------------------------------------------
        private void multi()
        {
            r.al = (byte)~r.al;
            r.al -= 0x80;
            r.ax = (ushort)(r.al * r.dl);
            r.ax += r.ax;
            r.al = r.ah;
            r.ah = (byte)~r.ah;
            r.ah -= 0x80;
        }

        //;==============================================
        //;	音色番号変更処理
        //;	entry	AL = 音色番号
        //;		SI = data_buff+ch*2
        //;		CH = チャネル番号(0-2,6-8)
        //;==============================================

        private void tone2608()
        {
            tone2608(data_buff);
        }

        private void tone2608(byte[] siBuf)
        {
            //	pusha
            ushort axbk = r.ax;
            ushort bxbk = r.bx;
            ushort cxbk = r.cx;
            ushort dxbk = r.dx;
            ushort sibk = r.si;
            ushort dibk = r.di;

            ushort dsbk = r.ds;

            siBuf[r.si + TS] = r.al;// 音色番号を格納
            byte[] tonebuff = tone_adrs(); // DS:BX = 音色データ格納番地
            r.al = 0x30;
            set_tone(tonebuff);// アドレス$30-に設定
            volchgm(siBuf);// アドレス$40-
            r.bx += 4;// next tone data
            r.al = 0x50;// アドレス$50,60,70,80に設定
            r.cl = 4;
            //loop_4:
            do
            {
                set_tone(tonebuff);
                r.al += 0x10;
                r.cl--;
            } while (r.cl > 0);

            r.al = 0xb0;// フィードバック、コネクションの設定
            r.al += r.ch;
            r.ah = tonebuff[r.bx];
            outdataa();

            r.ds = dsbk;

            r.di = dibk;
            r.si = sibk;
            r.dx = dxbk;
            r.cx = cxbk;
            r.bx = bxbk;
            r.ax = axbk;

            r.bx++;
        }

        private void set_tone(byte[] tonebuff)
        {
            ushort cxbk = r.cx;
            r.di = 0;//ofs:out_data
            byte cl = 4;
            //loop_2:
            do
            {
                ushort axbk = r.ax;
                r.al += out_data[r.di];// AL = 出力アドレス基値
                r.al += r.ch;
                r.ah = tonebuff[r.bx];// 出力データ値
                outdataa();
                r.di++;
                r.bx++;
                r.ax = axbk;
                cl--;
            } while (cl > 0);
            r.cx = cxbk;
        }

        //;--------------------------------------
        //;	音色データ格納番地の獲得
        //;	entry	AL = tone number
        //;	exit	DS:BX = tone data adrs
        //;--------------------------------------

        private byte[] tone_adrs()
        {
            // AL =  音色番号
            r.bx = (ushort)(25 * r.al);
            r.ds = nax.tone;//熊:たぶん不要
            return nax.toneBuff;
        }

        //;------------------------------------------
        //;	効果音発音前の音色・音量に戻す
        //;------------------------------------------

        private void last_set()
        {
            r.push(r.bx);
            r.push(r.si);
            calc_nowwork();
            r.al = data_buff[r.si + TS];
            tone2608();// 音色の設定
            r.al = data_buff[r.si + VS];
            volchgm(data_buff);// 音量の設定
            r.ah = data_buff[r.si + PN];
            pan();// パンの設定
            r.al = data_buff[r.si + FD];
            r.ah = data_buff[r.si + FD + 1];
            setfreq1();// YM2608の周波数設定
            r.si = r.pop();
            r.bx = r.pop();
            return;
        }

        private void calc_nowwork()
        {
            r.si = 0;//ofs:data_buff
            r.ax = 0;
            r.al = r.ch;
            r.ax += r.ax;
            r.si += r.ax;
            return;
        }

        //;---------------------------------
        //;	ハードLFO速度の設定
        //;---------------------------------

        private void hlfo_speed()
        {
            r.al = 0x22;
            r.bx++;
            outdata6a();
        }

        //;----------------------------------
        //;	ハードLFOデータの設定
        //;----------------------------------

        private void hlfo_data()
        {
            r.al = data_buff[r.si + PN];
            r.al &= 0xc0;// パンデータを保存
            r.ah |= r.al;
            data_buff[r.si + PN] = r.ah;// AMD,PMDデータを保存
            r.al = 0xb4;
            r.al += r.ch;// B4,B5,B6 にデータを出力
            r.bx++;
            outdataa();
            r.dl = (byte)nax.objBuf[0][r.bx].dat;// DL = AMon データ(b3-b0)
            r.pushA();
            r.push(r.ds);
            r.al = data_buff[r.si + TS];// AL = 音色番号
            byte[] toneBuff=tone_adrs();// DS:BX = 音色データ格納番地
            r.bx += 12;// next tone data
            r.al = 0x60;// アドレス$60に設定
            r.di = 0;//ofs:out_data
            r.cl = 4;
            //hlfo1:
            do
            {
                r.push(r.ax);
                r.al += out_data[r.di];// AL = 出力アドレス基値
                r.al += r.ch;
                r.ah = toneBuff[r.bx];// 出力データ値
                r.ah <<= 1;
                r.carry = ((r.dl & 1) != 0);
                r.dl >>= 1;
                r.ah = r.rcr(r.ah, 1);// b7にAMonデータを格納
                outdataa();
                r.di++;
                r.bx++;
                r.ax = r.pop();
                r.cl--;
            } while (r.cl != 0);
            r.ds = r.pop();
            r.popA();
            r.bx++;
            return;
        }

        //---------------------------------------------
        //	現チャネルの使用停止コマンド処理
        //---------------------------------------------

        private void stopm()
        {
            r.ch = realch;
            if (r.al != r.ch)
            {
                ushort cxbk = r.cx;
                ushort bxbk = r.bx;

                r.al = r.ch;
                r.bx = 0;//ofs:chtbl2

                r.al = chtbl2[r.bx + r.al];
                r.ch = r.al;
                calcbit();
                r.ax = (ushort)~r.ax;
                r.dl = (byte)~r.dl;
                shflag1 &= r.ax;// 該当チャネルの効果音中フラグを寝かす
                shflag2 &= r.dl;
                r.bx = bxbk;
                r.cx = cxbk;
            }

            //stopm2:
            calcbit();
            skip_data1 |= r.ax;
            skip_data2 |= r.dl;
            if (r.ch == 16)// 最終チャネルか
            {
                check_wait();
                if (r.zero)
                {
                    initia0();// 全チャネル停止ならチャネル1から演奏開始
                    return;
                }
            }

            //stopm1:
            //	pop	ax
            r.al = 1;
            recovwFlg = true;
        }

        //;=======================================
        //;	YM-2608 への直接データ出力
        //;=======================================

        private void set_reg()
        {
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;// AH = address , AL = data
            r.bx++;
            outdata0a();
        }

        //;----------------------------------------
        //;	YM-2608音色データユーザ設定
        //;----------------------------------------

        private void usr_tone()
        {
            r.bx++;// AL = 音色番号
            ushort dsbk = r.ds;
            ushort dibk = r.di;
            ushort cxbk = r.cx;
            ushort bxbk = r.bx;

            byte[] toneBuff= tone_adrs();// DS:DI = 音色データバッファ番地
            r.di = r.bx;
            r.bx = bxbk;// ES:BX = ユーザ音色
            r.cx = 25;// 転送データ数
            movtone();

            r.cx = cxbk;
            r.di = dibk;
            r.ds = dsbk;
        }

        //-------------------------------------------------
        //	ユーザ音色の転送
        //	entry	CX    = 転送バイト数
        //		DS:DI = 音色バッファ番地
        //		ES:BX = 演奏データ番地
        //	exit	ES:BX = 次の演奏データ番地
        //-------------------------------------------------

        private void movtone()
        {
            ushort esbk = r.es;
            ushort sibk = r.si;

            r.si = r.bx;
            r.bx += r.cx;

            ushort tmp = r.ds;
            r.ds = r.es;// DS:SI = 演奏データ番地
            r.es = tmp;// ES:DI = 音色データ番地

            //	cld
            do
            {
                nax.toneBuff[r.di] = (byte)nax.objBuf[0][r.si].dat;
                r.di++;
                r.si++;
                r.cx--;
            } while (r.cx > 0);
            //	rep	movsb

            r.si = sibk;
            r.es = esbk;
        }

        //;===============================================
        //;	Wait(F3) コマンド処理 (OPN,SSG,OPL)
        //;===============================================

        private void wait_r()
        {
            r.ch = realch;
            calcbit();
            wait_flg1 |= r.ax;// 現チャネルに待ちフラグを立てる
            wait_flg2 |= r.dl;
            check_wait();// ZR = 全チャネル停止
            if (r.zero)
            {
                initia0Flg = true;
                return;
            }
            r.ax = r.pop();// Skip Return offset
            r.al = 1;
            recovwFlg = true;// ( Waitの後に休まないことにする )
        }

        private void initia0()
        {
            data_buff[r.si + LC] = 1;// 音長カウンタの初期化
            data_buff[r.si + AD] = r.bl;
            data_buff[r.si + AD+1] = r.bh;// 演奏番地を次へ進める
            r.ax = 0;
            wait_flg1 = r.ax;
            wait_flg2 = r.al;
            initia0Flg = true;//	add	sp,12		; [back_fm],[play_fm],[ax],[cx],[dx],[si]
            replay();
        }

        //;･･････････････････････････････････････････････
        //;	指定チャネルのビット値計算
        //;	entry	CH = チャネル番号(0～16)
        //;	exit	DLAX = 2^CH (マスクフラグ)
        //;･･････････････････････････････････････････････
        private void calcbit()
        {
            ushort cxbk = r.cx;
            r.ax = 0;
            r.cl = r.ch;
            if (r.cl < 16)
            {
                //_calcbit1:
                r.ax++;
                r.ax <<= r.cl;
                r.dl = 0;
            }
            else
            {
                r.dl = 1;
            }
            //_calcbit2:
            r.cx = cxbk;
        }

        //;･･･････････････････････････････････
        //;	全チャネル待機チェック
        //;	exit	Z = 全待機
        //;･･･････････････････････････････････

        private void check_wait()
        {
            r.ax = wait_flg1;
            r.dl = wait_flg2;
            r.ax |= skip_data1;
            r.dl |= skip_data2;
            r.ax |= song_flg1;
            r.dl |= song_flg2;

            r.zero = (r.ax == 0xffff);
            if (r.zero) // 全チャネル停止、待ちなら次へ演奏する
            {
                r.zero = (r.dl == 1);
            }
            //chwait1:
        }

        //;-----------------------------
        //;	SKIP_DATAの初期化
        //;-----------------------------

        private void initia()
        {
            r.al = (byte)nax.objBuf[0][r.bx].dat;
            r.ah = (byte)nax.objBuf[0][r.bx + 1].dat;
            skip_data1 &= r.ax;// 指定チャネルを有効にする
            r.bx += 2;
            r.al = (byte)nax.objBuf[0][r.bx].dat;
            skip_data2 &= r.al;
            r.bx++;
            initia0Flg = true;// チャネル1から演奏し直す
        }

        //;---------------------------------
        //;	LFOパラメータセット
        //;---------------------------------

        private void lfopara()
        {
            data_buff[r.si + TI] &= 0x73;// 該当ビットをまずクリア

            r.al--;// SYNC on(1)
            if (r.al == 0)
            {
                lfop2();
                return;
            }
            
            r.al--;// One Shot(2)
            if (r.al == 0)
            {
                lfop4();
                return;
            }
            
            r.al--;// Delta(3)
            if (r.al != 0)
            {
                lfop3();// 非同期LFO
                return;
            }
            
            data_buff[r.si + TI] |= 4;// ノコギリLFO
            lfop2();
        }

        private void lfop4()
        {
            data_buff[r.si + TI] |= 8;// ワンショットLFO
            lfop2();
        }

        private void lfop2()
        {
            data_buff[r.si + TI] |= 0x80;// 同期LFO
            lfop3();
        }

        private void lfop3()
        {
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;
            r.ah = (byte)nax.objBuf[0][r.bx + 1].dat;
            data_buff[r.si + LL] = r.al;// LFO level
            data_buff[r.si + LF] = r.ah;// LFO speed
            r.bx += 2;
            r.al = (byte)nax.objBuf[0][r.bx].dat;
            r.ah = (byte)nax.objBuf[0][r.bx + 1].dat;
            data_buff[r.si + LD] = r.al;// LFO delay
            data_buff[r.si + LI] = r.ah;// LFO increment/base
            r.bx += 2;
            r.ah = (byte)nax.objBuf[0][r.bx].dat;
            r.ah &= 3;// AH = スピードベース値
            r.al = data_buff[r.si + LB];
            r.al &= 0xfc;
            r.al |= r.ah;
            data_buff[r.si + LB] = r.al;// LFO speed base
            r.al = (byte)nax.objBuf[0][r.bx].dat;// LFO repeat time
            r.al >>= 2;
            data_buff[r.si + LR] = r.al;
            r.bx++;
            return;
        }

        //;------------------------------
        //;	LFO開始／停止制御
        //;------------------------------

        private void lfoset()
        {
            r.ah = data_buff[r.si + TI];
            if (r.al == 0)
            {
                lfostop();
                return;
            }
            r.al--;
            if (r.al == 0)
            {
                lfoamd();
                return;
            }
            r.al--;
            if (r.al == 0)
            {
                lfopmd();
                return;
            }
            sync_reset();// LFOカウンタのリセット
            lfos0();
            return;
        }

        private void lfopmd()
        {
            r.ah |= 0x60;// START / PMD set
            lfos1();
        }

        private void lfoamd()
        {
            r.ah |= 0x40;// START
            r.ah &= 0xdf;// AMD set
            lfos1();
        }

        private void lfostop()
        {
            r.al = data_buff[r.si + VS];
            if (r.ch < 3)
            {
                lstop1();
                return;
            }
            if (r.ch < 6)
            {
                lstops();
                return;
            }
            if (r.ch != 10)
            {
                lstop1();
                return;
            }
            volchgr(data_buff);
            lstop();
            return;
        }

        private void lstop1()
        {
            volchgm(data_buff);
            lstop();
            return;
        }

        private void lstops()
        {
            //;+++	call	volssg_set
            lstop();
        }

        private void lstop()
        {
            r.ah &= 0xbf;// STOP
            lfos1();
        }

        private void lfos1()
        {
            data_buff[r.si + TI] = r.ah;
            lfos0();
        }

        private void lfos0()
        {
            r.bx++;
            return;
        }

        //;-------------------------------
        //;	LFO SYNC チェック
        //;-------------------------------

        private void sync()
        {
            r.push(r.ax);
            r.al = data_buff[r.si + TI];
            if ((r.al & 2) != 0)
            {
                // タイの時は同期しない
                r.ax = r.pop();
                return;
            }
            if ((r.al & 0x80) == 0)
            {
                r.ax = r.pop();
                return;
            }
            sync2();
        }

        private void sync2()
        {
            data_buff[r.si + TI] &= 0xef;// 加算モードへ
            r.ax = 0;
            data_buff[r.si + FC] = r.al;// LFO初期値設定
            data_buff[r.si + FC + 1] = r.ah;
            data_buff[r.si + RC] = r.al;// LFOカウンタクリア

            //sync1:
            r.ax = r.pop();
        }

        private void sync_reset()
        {
            r.push(r.ax);
            sync2();
        }

        //;---------------------------------------
        //;	システムディチューンの設定
        //;---------------------------------------

        private void sysdetune()
        {
            data_buff[r.si + SE] = r.al;
            r.bx++;
            return;
        }

        //;-----------------------
        //;	PAN 設定
        //;-----------------------

        private void pan()
        {
            r.al = data_buff[r.si + PN];
            r.al &= 0x3f;// AMD,PMDデータを保存
            r.ah |= r.al;
            data_buff[r.si + PN] = r.ah;// パンデータを保存
            r.al = 0xb4;
            r.al += r.ch;// B4,B5,B6 にデータを出力
            r.bx++;
            outdataa();
        }

        //;-----------------------------------
        //;	鍵盤表示のマスク・色指定
        //;-----------------------------------

        private void key_mask()
        {
            r.push(r.ax);
            r.al = data_buff[r.si + LB];
            r.al &= 0x1f;
            r.ah &= 0xe0;
            r.al |= r.ah;
            data_buff[r.si + LB] = r.al;// 色を格納
            r.ax = r.pop();
            r.al &= 1;// マスクビットをチェック
            if (r.al != 0)
            {
                calcbit();
                keymask1 |= r.ax;
                keymask2 |= r.dl;
            }
            else
            {
                //mask_reset:
                calcbit();
                r.ax = (ushort)~r.ax;
                r.dl = (byte)~r.dl;
                keymask1 &= r.ax;
                keymask2 &= r.dl;
            }
            //key_mask1:
            r.bx++;
            return;
        }

        //-----------------------------------
        //	PCMモードへの切り換え
        //-----------------------------------

        private void pcm_change()
        {
            r.ah = data_buff[r.si + PM];
            r.ah &= 0x80;
            r.al |= r.ah;
            data_buff[r.si + PM] = r.al;
            ch_change();
        }

        //;	休符ビットのクリアとキーオンフラグセット

        private void setkeyon()
        {
            data_buff[r.si + PM] |= 0x80;// キーオンフラグ
            //clearrest:
            data_buff[r.si + LI] &= 0x7f;// 休符ビットをクリア
        }

        private void clearrest()
        {
            data_buff[r.si + LI] &= 0x7f;// 休符ビットをクリア
        }

        //;==========================================================
        //;	SSG 演奏制御ルーチン
        //;		CH : channel number (3 to 5)
        //;		SI : data buffer address
        //;		ES : object data segment
        //;
        //;	command		FF : Rest music
        //;			FE : End (loop)
        //;			FD : End (break)
        //;			FC : This channel skip_play
        //;			FB : Wait on '
        //;			FA : Nop (9bytes)
        //;			F9 : Nop
        //;			F8 : Add frequency (4bytes)
        //;			F7 : N times loop (4bytes)
        //;			F6 : Noise frequency change (2bytes)
        //;			F5 : Timer-A tempo (3bytes)
        //;			F4 : Length/Ratio change (3bytes)
        //;			F3 : Wait all channel
        //;			F2 : Start decay data set (4bytes)
        //;			F1 : Register data set (3bytes)
        //;			F0 : System detune set (2bytes)
        //;			EF : Envelope mode set,reset (2bytes)
        //;			EE : Envelope speed (3bytes)
        //;			ED : Envelope type (2bytes)
        //;			EC : Key display mask on/off (2bytes)
        //;			EB : Mixer mode change (2bytes)
        //;			EA : @Jump (3bytes)
        //;			E9 : @Call (3bytes)
        //;			E8 : @Ret
        //;			E7 : Source Line symbolic information (3bytes)
        //;			E6 : Nop (27bytes)
        //;			E5 : Play Stack Initialize
        //;			E4 : @If Jump (5bytes)
        //;			E3 : @If Call (5bytes)
        //;			E2 : Volume data change (2bytes)
        //;			E1 : Tie
        //;			E0 : Loop counter clear
        //;			DF : Slur
        //;			DE : Ratio only change (2bytes)
        //;			DD : Set comment length (2bytes)
        //;			DC : Init Skip_data
        //;			DB : Comment set (4-46bytes)
        //;			DA : X Value set (4bytes)
        //;			D9 : LFO parameter set (7bytes)
        //;			D8 : LFO start(pmd,amd)/stop (2bytes)
        //;			D7 : Volume add (2bytes)
        //;			D6 : Volume sub (2bytes)
        //;			D5 : Start vol/Attack rate (3bytes)
        //;			D4 : Nop (5bytes)
        //;			D3 : @If Exit (5bytes)
        //;			D2 : Play Stack +1
        //;			D1 : Fade out
        //;			D0 : SSG/PCM mode(2bytes)
        //;			CF : Send channel change(2bytes)
        //;			CE : Set last mixermode,volume,envelope
        //;		     00-0F : Music code (2bytes)
        //;	各バイト数を変更する時はskip_byteも変更すべし.
        //;==========================================================

        private void main_ssg()
        {
            getentry();
            //backssg:
            ushort dxbk = 0;
            do
            {
                //dxbk=0:dummy
                //dxbk=1:recov
                //dxbk=2:backssg
                //dxbk=3:recovw
                recovFlg = false;
                recovwFlg = false;
                another_ssg1Flg = false;
                dxbk = 0;

                work.crntMmlDatum = nax.objBuf[0][r.bx];
                r.al = (byte)nax.objBuf[0][r.bx].dat;
                r.ah = (byte)nax.objBuf[0][r.bx + 1].dat;
                if (r.al < 0xce)
                {
                    another_ssg();
                    if (recovFlg)
                    {
                        dxbk = 1;
                        break;
                    }
                    return;
                }
                r.di = 0;//ofs:jmp_table2
                getadrs();
                check_ret();// リターンアドレスを分ける
                if (r.carry)
                {
                    r.dx = 2;//ofs:backssg
                }
                dxbk = r.dx;
                r.bx++;
                r.al = r.ah;
                jump_table2[r.di / 2]();
                if (another_ssg1Flg)
                {
                    another_ssg1();
                }
                if (recovFlg)
                {
                    dxbk = 1;
                    break;
                }
                if (recovwFlg)
                {
                    dxbk = 3;
                    break;
                }
            } while (dxbk == 2);

            if (dxbk == 1) recov();
            else if (dxbk == 3) recovw();
        }

        private Action[] jump_table2;
        private void SetJumptable2()
        {
            jump_table2=new Action[]{
                rest_ssg,  quit, //         FF(0)
                quit2,     stopm,//         FD(2)
                kwait,     continue_,//     FB(4)
                continue_, addfreq2,//      F9(6)
                n_loop,    noise, //        F7(8)
                tempoa,    mlength, //      F5(10)
                wait_r,    sdecay,//        F3(12)
                set_reg,   sysdetune,//     F1(14)
                set_env,   env_speed,//		EF(16)
                env_type,  key_mask,//      ED(18)
                mixer,     jump_to,//       EB(20)
                call_to,   ret_to,//        E9(22)
                symbol,    continue_,//     E7(24)
                sinit,     if_jump,//       E5(26)
                if_call,   volset,//        E3(28)
                tie2,      c_loop,//        E1(30)
                tie_tone,  ratio_change,//  DF(32)
                setcomlen, initia,//        DD(34)
                mcomment,  value,//         DB(36)
                lfopara,   lfoset, //       D9(38)
                voladds,   volsubs,//       D7(40)
                attack,    continue_,//     D5(42)
                if_exit,   pops,//          D3(44)
                fade_outs, ssgmode, //      D1(46)
                channel,   last_setssg,//   CF(48)
            };
        }

        //;=========================================
        //;	SSG用音符による周波数設定処理
        //;=========================================

        private void another_ssg()
        {
            (r.ah, r.al) = (r.al, r.ah);
            data_buff[r.si + FB] = 0;// 小数桁はクリア
            r.dx = r.ax;
            freq_detune_lfo();
            r.dx -= r.ax;
            r.ax = r.dx;
            another_ssg1();
        }

        private void another_ssg1()
        { 
            setkeyon();// 休符ビットをクリア,キーオンフラグ
            data_buff[r.si + FD] = r.al;
            data_buff[r.si + FD + 1] = r.ah;
            setfreqs();// SSGの周波数設定
            r.bx += 2;
            sync();// LFO SYNC チェック

            check_mask();// チャネルマスクのチェック
            if (!r.zero)
            {
                //	jne	ssgmask
                volssg0();
                recovFlg = true;
                return;
            }
            if ((data_buff[r.si + TI] & 2) != 0)// タイフラグのチェック
            {
                //	jne	recovs
                recovFlg = true;
                return;
            }
            if (ssgpcm != 0)
            {
                // PCMモードか
                play_ssgpcm();
                return;
            }

            r.ax = 0;
            data_buff[r.si + SC] = r.al;
            data_buff[r.si + SC+1] = r.ah;
            data_buff[r.si + DC] = r.al;
            data_buff[r.si + DC+1] = r.ah;// ディケイカウンタのクリア

            r.al = data_buff[r.si + SV];// 開始音量の指定ありか
            r.al++;
            if (r.al != 0)
            {
                //ssgon1:
                set_attackrate();// アタックカウンタの値転送
            }
            else
            {
                r.al = data_buff[r.si + VS];
            }

            //ssgon2:
            volssg();// set volume data
            if (r.carry)
            {
                // エンベロープ使用時にフェードアウトした
                recovFlg = true;
                return;
            }
            if (data_buff[r.si + PC] == 0)
            {
                // エンベロープモードか
                recovFlg = true;
                return;
            }
            r.ah = data_buff[r.si + PT];
            r.al = 0xd;
            outdata1sa();// エンベロープ形状の設定
            r.ah = 16;
            r.al = r.ch;
            r.al += 5;
            outdata1sa();
            //recovs:
            recovFlg = true;
            return;
            //ssgmask:
            //	call	volssg0
            //	jmp	recovs
        }

        //;･･････････････････････････
        //;	SSGPCMでの発音
        //;･･････････････････････････

        private void play_ssgpcm()
        {
            r.push(r.si);
            r.ax = 0;
            r.al = data_buff[r.si + DY];// AL = PCM音色番号
            //r.al = 2;
            r.ax += r.ax;
            r.di = 0;//ofs:ssgtable
            r.di += r.ax;
            calc_pcm1adrs();
            r.ax = (ushort)(ssgtable[r.di] + ssgtable[r.di + 1] * 0x100);// AX = 現在の音色の開始番地
            pcmNadrs[r.si / 2] = r.ax;
            r.ax = (ushort)(ssgtable[r.di + 2] + ssgtable[r.di + 3] * 0x100);// AX = 現在の音色の終了番地
            pcmNadrs[r.si / 2 + 1] = r.ax;
            r.ax = 0;
            pcmNadrs[r.si / 2 + 2] = r.ax;// PCMカウンタのクリア
            r.dx = fade_count;
            if (r.dh < 30)
            {
                // SSGPCM時のフェードアウト処理
                start_intimer();// 内蔵タイマ起動
            }
            r.si = r.pop();
            recovFlg = true;
            return;
        }

        private void calc_pcm1adrs()
        {
            r.si = 0;//ofs:pcm1adrs
            r.ax = 0;
            r.al = r.ch;
            r.al -= 3;
            r.ax <<= 1;
            r.si += r.ax;
            r.ax <<= 1;
            r.si += r.ax;
            //si=pcm1adrs+ chnum*2+chnum*4;
            return;
        }

        //-------------------------------------------
        //	SSGに周波数を設定
        //	entry	AH = tune2
        //		AL = tune1(fine)
        //		CH = channel number
        //-------------------------------------------

        private void setfreqs()
        {
            ushort dxbk = r.dx;
            r.dl = r.al;
            r.al = r.ch;
            r.al += r.al;
            r.al -= 5;
            outdata1sa();// tune2
            r.al--;
            r.ah = r.dl;
            outdata1sa();// tune1
            r.dx = dxbk;
            return;
        }

        //;---------------------------------
        //;	周波数を加算して発音
        //;---------------------------------

        private void addfreq2()
        {
            addf_main();
            another_ssg1Flg = true;
            //	jmp	another_ssg1	; 周波数設定へ
        }

        private void addf_main()
        {
            r.ax = (ushort)(data_buff[r.si + FD] + data_buff[r.si + FD + 1] * 0x100);
            r.dl = r.ah;
            r.ah = r.al;
            r.al = data_buff[r.si + FB];// 256倍する
            int ans = r.ax + (byte)nax.objBuf[0][r.bx].dat + (byte)nax.objBuf[0][r.bx + 1].dat * 0x100;//	add	ax,es:[bx]
            r.ax = (ushort)ans;
            r.carry = ans > 0xffff;
            r.dl += (byte)((byte)nax.objBuf[0][r.bx + 2].dat + (r.carry ? 1 : 0));
            data_buff[r.si + FB] = r.al;// 小数部を格納
            r.al = r.ah;
            r.ah = r.dl;
            r.bx++;
            data_buff[r.si + TI] |= 1;// Q8にする
            return;
        }

        //;============================
        //;	休符処理 (SSG)
        //;============================

        private void rest_ssg()
        {
            if (ssgpcm == 0)
            {
                volssg0();// 強制的に0
                rest0();// 休符フラグを立てる
                return;
            }

            //cancel_spcm:
            ushort sibk = r.si;
            calc_pcm1adrs();
            r.ax = 0;
            pcmNadrs[r.si / 2] = r.ax;
            pcmNadrs[r.si / 2 + 1] = r.ax;
            r.si = sibk;
            rest0();// 休符フラグを立てる
            return;
        }

        //;------------------------------
        //;	音量の加算・減算
        //;------------------------------

        private void voladds()
        {
            add_vol();
            volset();
        }
        private void volsubs()
        {
            sub_vol();
            volset();
        }

        //;=============================
        //;	音量設定処理 (SSG)
        //;=============================

        private void volset()
        {
            data_buff[r.si + VS] = r.al;
            nax.pc98.OutportBDummy(port1);
            r.bx++;
        }

        private void volssg()
        {
            if (data_buff[r.si + PC] != 0)// エンベロープか
            {
                volenv();
                return;
            }
            volssg_set();
        }

        private void volssg_set()
        {
            data_buff[r.si + LV] = r.al; // 最終出力音量を保存
            volssg1();
        }

        private void volssg1()
        { 
            r.dx = fade_count;
            r.ah = r.al;

            r.dl = r.al;// DL : vol data (0-7fh)
            r.al = r.dh;// AL : fade level (7fh-0)
            if (r.al != 0)
            {
                multi();// フェードアウトによる音量計算
                r.ah = (byte)~r.ah;
                r.ah -= 0x80;
            }

            //skip_op33:
            r.ah >>= 3;// AH :vol data (0-0Fh)
            r.al = r.ch;
            r.al += 5;
            outdata1sa();
            r.carry = false;
        }

        private void volssg0()
        {
            r.al = 0;
            volssg_set();
        }

        private void volenv()
        {
            r.dx = fade_count;
            r.carry = (r.dh < 30);// エンベロープモード時のフェードアウト処理
            r.carry = !r.carry;
            if (!r.carry)
            {
                return;
            }
            r.ax = 0;
            r.al = r.ch;
            r.al += 5;
            outdata1sa();
            r.carry = true;// CY=1 then Fade-out Env Cut.
            //ret01:
            return;
        }

        //;====================================
        //;	エンベロープモードの設定
        //;====================================

        private void set_env()
        {
            data_buff[r.si + PC] = r.al;// Envelope mode on/off (b4)
            r.al = r.ch;
            r.al += 5;
            r.bx++;
            outdata1sa();
        }

        //;===================================
        //;	エンベロープ速度の変更
        //;===================================

        private void env_speed()
        {
            r.al = 0x0b;// AH = 下位8bit
            outdata1sa();
            r.ax = (byte)nax.objBuf[0][r.bx].dat;// AH = 上位8bit
            r.bx++;
            r.bx++;
            r.al = 0x0c;
            outdata1sa();
        }

        //;===================================
        //;	エンベロープ形状の設定
        //;===================================

        private void env_type()
        {
            data_buff[r.si + PT] = r.al;// save ENV type
            r.bx++;
            return;
        }

        //;================================
        //;	ノイズ周波数のセット
        //;================================

        private void noise()
        {
            if (ssgpcm != 0)
            {
                set_tonenum();
                return;
            }
            noisef = r.ah;
            r.al = 6;
            r.bx++;
            outdata1sa();
            return;
        }

        private void set_tonenum()
        {
            data_buff[r.si + DY] = r.al;// SSGPCMモードでは音色番号とする
            r.bx++;
            return;
        }

        //;================================
        //;	ミクサーモードの設定
        //;================================

        private void mixer()
        {
            r.ah = mixsave;// ミクサー保存データ
            r.cl = r.ch;
            r.cl -= 3;// SSG 0,1,2
            r.al = 0b1001;
            r.al <<= r.cl;
            r.ah |= r.al;// 現チャネルのデータをマスク
            r.ah &= (byte)nax.objBuf[0][r.bx].dat;
            r.bx++;
            r.al = 7;
            outdata1sa();
            r.push(r.ax);
            calcbit();
            ushort ans = (ushort)(r.ax & shflag1);// 効果音中フラグ
            r.ax = r.pop();
            if (ans != 0)
            {
                return;
            }
            byte ansb = (byte)(r.dl & shflag2);
            if (ansb != 0)
            {
                return;
            }
            mixsave = r.ah;
            //mix1:
            return;
        }

        //;===========================================
        //;	スタートディケイデータの設定
        //;===========================================

        private void sdecay()
        {
            data_buff[r.si + SD] = r.al;
            r.bx++;
            r.ax = (ushort)((byte)nax.objBuf[0][r.bx].dat + (byte)nax.objBuf[0][r.bx + 1].dat * 0x100);
            data_buff[r.si + DV] = r.al;
            data_buff[r.si + DY] = r.ah;
            r.bx += 2;
            return;
        }

        //;-------------------------------------------
        //;	開始音量・アタックレイトの設定
        //;-------------------------------------------

        private void attack()
        {
            data_buff[r.si + SV] = r.al;
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;
            data_buff[r.si + AR] = r.al;
            r.bx++;
            return;
        }

        //;----------------------------------
        //;	ソース行の行番号情報
        //;----------------------------------

        private void symbol()
        {
            r.ax = (ushort)((byte)nax.objBuf[0][r.bx].dat + (byte)nax.objBuf[0][r.bx + 1].dat * 0x100);
            data_buff[r.si + SY] = r.al;
            data_buff[r.si + SY + 1] = r.ah;// ソース行を格納
            r.bx += 2;// 行番号情報は飛ばす
            return;
        }

        //;//////////////////////////
        //;	歌詞転送機能
        //;//////////////////////////

        private void mcomment()
        {
            r.bx++;
            if (r.al == 0xff)// 無条件か
            {
                comment1();
                return;
            }
            gets();
            if (r.al == (byte)nax.objBuf[0][r.bx].dat)// 条件データと一致するか
            {
                comment1();
                return;
            }
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;// 文字列長
            r.ah = 0;
            r.bx += r.ax;// 無視する
            r.bx++;
            return;
        }

        private void comment1()
        {
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;// AL = 文字列長
            r.bx++;
            r.push(r.di);
            r.di = 0;//ofs:comdata ; DI = 保存番地
            //comment3:
            for (int i = 0; i < comdataBuf.Length; i++) comdataBuf[i] = 0;
            while (r.al>0)
            {
                r.ah = (byte)nax.objBuf[0][r.bx].dat;
                comdataBuf[r.di] = r.ah;
                r.bx++;
                r.di++;
                r.al--;
            }
            //comment2:
            comdataBuf[r.di] = 0;// 最後に0を格納
            comdata = nax.myEnc.GetStringFromSjisArray(comdataBuf);
            Log.writeLine(LogLevel.INFO, comdata);
            nax.lyric = comdata;
            r.di = r.pop();
            return;
        }

        //;----------------------------------
        //;	色変わり歌詞の桁数指定
        //;----------------------------------

        private void setcomlen()
        {
            comlength = r.al;
            nax.comlength=comlength;
            r.bx++;
            return;
        }

        //;------------------------------------
        //;	SSG/PCMモードの切り換え
        //;------------------------------------

        private void ssgmode()
        {
            if ((r.al & 0x80) == 0)
            {
                pcm_change();// 拡張PCM
                return;
            }
            r.al &= 0x7f;
            r.bx++;
            if (r.al == 0)
            {
                ssgpcm = r.al;
                return;
            }

            //set_pcm:
            r.push(r.cx);
            r.ch = 3;
            //ssgfreqx:
            do
            {
                r.ax = 3;
                setfreqs();// 最高周波数に設定する
                r.ch++;
            } while (r.ch != 6);
            r.cx = r.pop();

            r.ax = 0xb807;
            outdata1a();// Mixerモードの設定
            r.al = 1;
            if ((nax.m_mode[0] & 0x80) != 0)
            {   // 16KHz:2
                r.al++;
            }

            //set_ssg:
            ssgpcm = r.al;
            return;
        }

        //;------------------------------------------
        //;	効果音発音前の音色・音量に戻す
        //;------------------------------------------

        private void last_setssg()
        {
            r.push(r.bx);
            r.push(r.si);
            calc_nowwork();
            r.al = data_buff[r.si + VS];
            volssg();// 音量
            r.al = data_buff[r.si + PC];
            set_env();// エンベロープモード
            r.al = data_buff[r.si + FD];
            r.ah = data_buff[r.si + FD + 1];
            setfreqs();// SSGの周波数設定
            r.si = r.pop();
            r.bx = r.pop();
            r.ah = mixsave;
            r.al = 7;
            outdata1sa();// ノイズ
        }

        //;===============================================================
        //;	RHYTHM,PCM 演奏制御処理
        //;		CH : channel number (9,10)
        //;		SI : data buffer address
        //;		ES : object data segment
        //;
        //;	command		FF : Rest
        //;			FE : End (loop)
        //;			FD : End (break)
        //;			FC : This channel skip_play
        //;			FB : Wait on '
        //;			FA : Nop (9bytes)
        //;			F9 : Rhythm command end
        //;			F8 : Add frequency (4bytes)
        //;			F7 : N times loop (4bytes)
        //;			F6 : PCM Pan set (2bytes)
        //;			F5 : Timer-A tempo (3bytes)
        //;			F4 : Length/Ratio change (3bytes)
        //;			F3 : Wait all channel
        //;			F2 : DSP mode,level,delay (4bytes)
        //;			F1 : Register data set (3bytes)
        //;			F0 : Rhythm Key on (2bytes)
        //;			EF : Rhythm Dump (2bytes)
        //;			EE : Rhythm Pan/Volume Set (3bytes)
        //;			ED : Nop (2bytes)
        //;			EC : Key display mask on/off (2bytes)
        //;			EB : PCM Tone change (2bytes)
        //;			EA : @Jump (3bytes)
        //;			E9 : @Call (3bytes)
        //;			E8 : @Ret
        //;			E7 : Source Line symbolic information (3bytes)
        //;			E6 : Nop (27bytes)
        //;			E5 : Play Stack Initialize
        //;			E4 : @If Jump (5bytes)
        //;			E3 : @If Call (5bytes)
        //;			E2 : Volume data change (2bytes)
        //;			E1 : Tie
        //;			E0 : Loop counter clear
        //;			DF : PCM Repeat
        //;			DE : Ratio only change (2bytes)
        //;			DD : Set comment length (2bytes)
        //;			DC : Init Skip_data
        //;			DB : Comment set (4-46bytes)
        //;			DA : X Value set (4bytes)
        //;			D9 : LFO parameter set (7bytes)
        //;			D8 : LFO start(pmd,amd)/stop (2bytes)
        //;			D7 : Volume add (2bytes)
        //;			D6 : Volume sub (2bytes)
        //;			D5 : PCM play (3bytes)
        //;			D4 : PCM address set (5bytes)
        //;			D3 : @If Exit (5bytes)
        //;			D2 : Play Stack +1
        //;			D1 : Fade out
        //;			D0 : SSG/PCM mode(2bytes)
        //;			CF : Send channel change(2bytes)
        //;			CE : Set last tone/volume/pan
        //;	各バイト数を変更する時はskip_byteも変更すべし.
        //;===============================================================

        private void main_rhythm()
        {
            main_pcm();
        }

        private void main_pcm()
        {
            getentry();// BX = 演奏データの番地
            if (!r.zero)
            {
                r.al = data_buff[r.si + VS];
                volchgr(data_buff);// フェードアウトに従って音量変更
            }

            //back_pcm:
            ushort dxbk;
            do
            {
                recovFlg = false;
                recovwFlg = false;
                initia0Flg = false;
                dxbk = 0;

                work.crntMmlDatum = nax.objBuf[0][r.bx];
                r.ax = (ushort)((byte)nax.objBuf[0][r.bx].dat + (byte)nax.objBuf[0][r.bx + 1].dat * 0x100);
                if (r.al < 0xce)
                {
                    recov();
                    return;
                }

                r.di = 0;//ofs:jump_table3
                getadrs();// コマンド分岐処理
                check_ret();// リターンアドレスを分ける
                if (r.carry || r.al == 0xf9)
                    r.dx = 2;//ofs:back_pcm

                //checkpcm1:
                dxbk = r.dx;
                r.bx++;
                r.al = r.ah;
                jump_table3[r.di / 2]();
                if (recovFlg)
                {
                    dxbk = 1;
                    break;
                }
                if (recovwFlg)
                {
                    dxbk = 3;
                    break;
                }
                if(initia0Flg)
                {
                    dxbk = 4;
                    break;
                }
            } while (dxbk == 2);

            if (dxbk == 1) recov();
            else if (dxbk == 3) recovw();

        }

        private Action[] jump_table3;
        private void SetJumptable3()
        {
            jump_table3 = new Action[]{
                rest2,       quit,//         FF(0)
                quit2,       stopmp,//       FD(2)
                kwait,       continue_,//    FB(4)
                rhythm_on,   addfreq_pcm,//  F9(6)
                n_loop,      pan2,//         F7(8)
                tempoa,      mlength,//      F5(10)
                wait_r,      dsp_set,//      F3(12)
                set_reg,     rhythm_keyon,// F1(14)
                rhythm_dump, rhythm_pan,//   EF(16)
                continue_,   key_mask,//     ED(18)
                tonepcm,     jump_to,//      EB(20)
                call_to,     ret_to,//       E9(22)
                symbol,      continue_,//    E7(24)
                sinit,       if_jump,//      E5(26)
                if_call,     vol_change2,//  E3(28)
                tie2,        c_loop,//       E1(30)
                repeat,      ratio_change,// DF(32)
                setcomlen,   initia,//       DD(34)
                mcomment,    value,//        DB(36)
                lfopara,     lfoset,//       D9(38)
                voladd2,     volsub2,//      D7(40)
                pcm_keyon,   pcm_adrs,//     D5(42)
                if_exit,     pops,//         D3(44)
                fade_outs,   pcm_change,//   D1(46)
                channel,     last_setpcm//   CF(48)
            };
        }

        //;-----------------------------
        //;	リズムキーオン
        //;-----------------------------

        private void rhythm_keyon()
        {
            data_buff[r.si + TS] = r.al;// TracePlay 用に保存
            setkeyon();// 休符ビットをクリア,キーオンフラグ
            check_mask();
            if (r.zero)
            {
                r.al = 0x10;
                outdata1a();
                r.al = 100;
                rhythm_wait();
            }
            //not_rkeyon:
            r.bx++;
            data_buff[r.si + FD] = 1;
            data_buff[r.si + FD + 1] = 0;
            return;
        }

        //;--------------------------
        //;	リズムダンプ
        //;--------------------------

        private void rhythm_dump()
        {
            r.al = 0x10;
            r.ah |= 0x80;
            r.bx++;
            outdata1a();
            r.al = 100;
            rhythm_wait();
        }

        //;-----------------------------------
        //;	リズムのパン・音量設定
        //;-----------------------------------

        private void rhythm_pan()
        {
            r.bx++;
            r.al = (byte)nax.objBuf[0][r.bx].dat;// AL = address(18h～1dh) , AH = data
            r.bx++;
            r.di = 0;//ofs:rhytbl
            r.dx = 0;
            r.dl = r.al;
            r.dl -= 0x18;
            r.di += r.dx;
            r.carry = (r.di < 6);
            if (r.carry)
            {
                rhytbl[r.di] = r.ah;// テーブルに格納
                outdata1a();
            }
            //rhypan1:
            r.al = 25;
            rhythm_wait();
        }

        //;---------------------------------
        //;	リズム相対音量の可変
        //;---------------------------------

        private void voladd2()
        {
            add_vol();
            vol_change2();
        }

        private void volsub2()
        {
            sub_vol();
            vol_change2();
        }

        //;-------------------------------------
        //;	リズム・PCMの音量を変更
        //;	entry	AL = vol data
        //;-------------------------------------

        private void vol_change2()
        {
            volset();
            volchgr(data_buff);
        }

        private void volchgr(byte[] siBuf)
        {
            r.dx = fade_count;
            if (r.dx != 0)
            {
                // フェードアウトチェツク
                r.dx >>= 1;// DH = fade vol data (0-3Fh)
                r.carry = r.al < r.dh;
                r.al -= r.dh;
                if (r.carry) r.al = 0;
            }

            //skip_volr:
            if (check_extpcm(siBuf))
            {
                volchgp(siBuf);
                return;
            }

            // PCMか
            if (r.ch != 9)
            {
                volchgp(siBuf);
                return;
            }

            r.al >>= 1;// 0-3fh の範囲にする
            r.ah = r.al;
            r.al = 0x11;
            outdata1a();

            r.al = 25;
            rhythm_wait();
        }

        private void rhythm_wait()
        { 
            //pushf

            //cli
            do
            {
                //rhythm_wait1:
                nax.pc98.OutportB(0x5f, r.al);
                r.al--;
            } while (r.al != 0);
            //popf
        }

        private void volchgp(byte[] siBuf)
        {
            Pcm0work[] diBuf = calc_pcmwork(siBuf);// DI = PCM専用ワーク
            r.carry = r.al < r.dh;
            r.al -= r.dh;
            if (r.carry) r.al = 0;

            nax.pc98.OutportC4231_Volume((byte)r.di, 0, r.al);
            //diBuf[r.di].pcm0vol[0] = r.al;
            r.al <<= 1;//0-ffh の範囲にする
            r.al++;
            r.ah = r.al;
            r.al = 0xb;
            outdata2a();
        }

        //;･･････････････････････････････････････
        //;	PCM専用ワークの番地を計算
        //;	exit	DI = ワーク番地
        //;･･････････････････････････････････････

        private Pcm0work[] calc_pcmwork(byte[] siBuf)
        {
            ushort axbk = r.ax;
            r.al = siBuf[r.si + PM];
            r.al &= 0x1f;
            if (r.al > 16) r.al = 16;// ワークエリア保護用

            r.ah = 1;
            r.ax = (ushort)(r.al * r.ah);
            r.di = 0;//ofs:pcm0work
            r.di += r.ax;
            r.ax = axbk;

            return pcm0work;
        }

        //--------------------------
        //	PCM キーオン
        //--------------------------

        private void pcm_keyon()
        {
            data_buff[r.si + FB] = 0;// 小数桁はクリア
            r.ah = (byte)nax.objBuf[0][r.bx + 1].dat;
            another_pcm1();
        }

        private void another_pcm1()
        {
            data_buff[r.si + FD] = r.al;// 周波数データの格納
            data_buff[r.si + FD + 1] = r.ah;
            setkeyon();// 休符ビットをクリア,キーオンフラグ
            sync();// LFO SYNC チェック
            r.bx += 2;
            if (check_86pcm())// 拡張86PCM?
            {
                extpcm_play();
                return;
            }
            if (realch != 10)// ADPCMチャネルか
            {
                recovFlg = true;
                return;
            }
            if ((nax.m_mode[3] & 4) != 0)// ADPCMあり?
            {
                r.ax <<= 3;
                ushort axbk = r.ax;
                r.ah = r.al;
                r.al = 9;
                outdata2a();// DELTA-N (L)
                r.ax = axbk;
                r.al = 0xa;
                outdata2a();// DELTA-N (H)
                if ((data_buff[r.si + TI] & 2) == 0)
                {
                    r.ax = 0x100;
                    outdata2a();// PCM Play Reset
                    check_mask();
                    if (r.zero)
                    {
                        r.al = data_buff[r.si + VS];
                        volchgr(data_buff);// 音量を戻す
                        r.ax = 0xa000;
                        r.ah |= data_buff[r.si + PT];// リピートチェック
                        outdata2a();// PCM Play Start
                    }
                }
            }

            //not_pkeyon:
            data_buff[r.si + PT] = 0;// リピートフラグのクリア
            rhythm_on();
        }

        private void rhythm_on()
        {
            recovFlg = true;
        }

        //	拡張PCM用の処理

        private void extpcm_play()
        {
            calc_pcmwork(data_buff);
            nax.pc98.OutportC4231_Freq((byte)r.di, 1, r.ax);// 拡張PCM用周波数データ
            //pcm0work[r.di].pcm0freq[1] = r.ax;// 拡張PCM用周波数データ
            if ((data_buff[r.si + TI] & 2) == 0)
            {
                check_mask();
                if (r.zero)
                {
                    extpcm_keyon();
                }
            }

            //not_pkeyon:
            data_buff[r.si + PT] = 0;// リピートフラグのクリア
            //rhythm_on:
            recovFlg = true;
        }

        private void extpcm_keyon()
        {
            r.ax = 0;
            r.al = data_buff[r.si + TS];// AL = 音色番号
            //r.al = 38;
            r.ax += r.ax;
            r.ax += 0;//ofs:pcmtable ; SI = PCMテーブル番地
            r.di = r.ax;
            r.ax = (ushort)(pcmtable[r.di] + pcmtable[r.di + 1] * 0x100);// AX = 開始番地
            r.dx = (ushort)(pcmtable[r.di+2] + pcmtable[r.di + 3] * 0x100);// DX = 終了番地
            r.carry = (r.dx - r.ax) < 0;
            r.dx -= r.ax;
            if (r.carry)
            {
                r.dx = 0;
            }
            ushort bxbk = r.bx;
            r.bx = 0;

            r.carry = (r.dx & 0x8000) != 0;
            r.dx <<= 1;
            r.bx = r.rcl(r.bx, 1);

            r.carry = (r.dx & 0x8000) != 0;
            r.dx <<= 1;
            r.bx = r.rcl(r.bx, 1);

            r.carry = (r.dx & 0x8000) != 0;
            r.dx <<= 1;
            r.bx = r.rcl(r.bx, 1);

            r.carry = (r.dx & 0x8000) != 0;
            r.dx <<= 1;
            r.bx = r.rcl(r.bx, 1);// BXDX = PCM発音バイト数(16倍)

            ushort cxbk = r.cx;
            calc_extpcmadr();
            ushort dibk = r.di;
            ushort axbk = r.ax;
            r.ax = r.di;
            calc_pcmwork(data_buff);// DI = PCMワーク
                                    //	pushf
                                    //	cli
            nax.pc98.OutportC4231_Adrs((byte)r.di, 0, r.ax);// PCM番地(0～3FFFh)
            nax.pc98.OutportC4231_Adrs((byte)r.di, 1, r.cx);// EMSページ(0～31)
            nax.pc98.OutportC4231_Cnt((byte)r.di, 0, r.dx);
            nax.pc98.OutportC4231_Cnt((byte)r.di, 1, r.bx);
            //pcm0work[r.di].pcm0adrs[0] = r.ax;// PCM番地(0～3FFFh)
            //pcm0work[r.di].pcm0adrs[1] = r.cx;// EMSページ(0～31)
            //pcm0work[r.di].pcm0cnt[0] = r.dx;
            //pcm0work[r.di].pcm0cnt[1] = r.bx;
            //	popf
            r.ax = axbk;
            r.di = dibk;
            r.cx = cxbk;
            r.bx = bxbk;

            if ((fifo_exec & 1) != 0)// FIFOが動いているか
            {
                return;
            }

            ushort dxbk = r.dx;
            cxbk = r.cx;
            pcm_start();
            r.cx = cxbk;
            r.dx = dxbk;
            //noteon1:
        }

        //	ADPCM管理番地からEMS番地とページの計算
        //	entry	AX = ADPCM番地(0～FFFF)
        //	exit	CX = EMSページ(0～63)
        //		DI = EMS番地(0～3FFF)
        public void calc_extpcmadr()
        {
            r.cx = 0;

            r.carry = (r.ax & 0x8000) != 0;
            r.ax <<= 1;
            r.cx = r.rcl(r.cx, 1);

            r.carry = (r.ax & 0x8000) != 0;
            r.ax <<= 1;
            r.cx = r.rcl(r.cx, 1);

            r.carry = (r.ax & 0x8000) != 0;
            r.ax <<= 1;
            r.cx = r.rcl(r.cx, 1);

            r.carry = (r.ax & 0x8000) != 0;
            r.ax <<= 1;// 管理番地 = 64KB→1MB(16倍)
            r.cx = r.rcl(r.cx, 1);// ADPCM = 256KB

            r.di = r.ax;// PCM = 1MB
            r.di &= 0x3fff;// EMS16KB内のオフセット

            r.carry = (r.ax & 0x8000) != 0;
            r.ax <<= 1;
            r.cx = r.rcl(r.cx, 1);

            r.carry = (r.ax & 0x8000) != 0;
            r.ax <<= 1;
            r.cx = r.rcl(r.cx, 1);
            // ページ番号へ拡張(上位2bitシフト)
        }

        //;---------------------------------
        //;	周波数を加算して発音
        //;---------------------------------

        private void addfreq_pcm()
        {
            addf_main();
            another_pcm1();// 周波数設定へ
        }

        //;------------------------------
        //;	PCMリピートの指定
        //;------------------------------

        private void repeat()
        {
            data_buff[r.si + PT] = 0x10;
            return;
        }

        //;-----------------------------
        //;	休符の場合の処理
        //;-----------------------------

        private void rest2()
        {
            rest2_main();
            rest0();// 休符ビットを立てる
        }

        private void rest2_main()
        {
            if (!check_extpcm(data_buff))// 拡張PCM?
            {
                if (r.ch == 9)
                {
                    nax.pc98.OutportBDummy(port1);
                    return;// リズムの場合なにもしない
                }
                r.ax = 0xb;
                outdata2a();// 音量を0にする
                //	jne	restp1
            }
            //restp1:
            calc_pcmwork(data_buff);
            //	pushf
            //	cli
            r.ax = 0;
            nax.pc98.OutportC4231_Cnt((byte)r.di, 0, r.ax);// +++ 拡張PCM用
            nax.pc98.OutportC4231_Cnt((byte)r.di, 1, r.ax);
            //pcm0work[r.di].pcm0cnt[0] = r.ax;// +++ 拡張PCM用
            //pcm0work[r.di].pcm0cnt[1] = r.ax;
            //	popf
            //restp0:
        }

        //;-----------------------------------
        //;	PCMの場合のチャネル終了
        //;-----------------------------------

        private void stopmp()
        {
            rest2();
            stopm();
        }

        //;-------------------------
        //;	PCM 範囲指定
        //;-------------------------

        private void pcm_adrs()
        {
            r.push(r.cx);
            r.cx = 4;
            r.al = 2;
            //pcm_adrs1:
            do
            {
                r.ah = (byte)nax.objBuf[0][r.bx].dat;
                outdata2a();// Start adrs(L,H) , End adrs(L,H)
                r.bx++;
                r.al++;
                r.cx--;
            } while (r.cx > 0);
            r.cx = r.pop();
            return;
        }

        //--------------------------
        //	PCM PAN 設定
        //--------------------------

        private void pan2()
        {
            data_buff[r.si + PN] = r.ah;
            r.bx++;
            if (!check_86pcm())// 拡張86PCM?
            {
                r.ah &= 0xc0;
                r.al = 1;
                outdata2a();// PCMのパン設定
                return;
            }

            //extpcm_pan2:
            calc_pcmwork(data_buff);

            r.dx = 0xd8f6;// neg al
            if ((r.ah & 4) == 0)
            {
                r.dx = 0x3e3e;// ds: ds:
                if ((r.ah & 0x80) == 0)
                {
                    r.dx = 0xf8d0;// sar al,1
                    if ((r.ah & 1) == 0)
                    {
                        r.dx = 0xc030;// xor al,al
                    }
                }
            }
            //pancode1:
            nax.pc98.OutportC4231_Pan((byte)r.di, 0, r.dx);// L
            //pcm0work[r.di].pcm0pan[0] = r.dx;// L

            r.dx = 0xdcf6;// neg ah
            if ((r.ah & 8) == 0)
            {
                r.dx = 0x3e3e;// ds: ds:
                if ((r.ah & 0x40) == 0)
                {
                    r.dx = 0xfcd0;// sar ah,1
                    if ((r.ah & 2) == 0)
                    {
                        r.dx = 0xe430;// xor ah,ah
                    }
                }
            }
            //pancode2:
            nax.pc98.OutportC4231_Pan((byte)r.di, 1, r.dx);// R
            //pcm0work[r.di].pcm0pan[1] = r.dx;// R
        }

        //;---------------------------------
        //;	PCM音色の指定(ADPCM用)
        //;---------------------------------

        private void tonepcm()
        {
            data_buff[r.si + TS] = r.al;// for TracePlay
            tonepcmm();
        }

        private void tonepcmm()
        {
            ushort di = (ushort)(r.al * 2); //ofs:pcmtable // DI = PCM管理テーブルの番地
            r.dx = (ushort)(pcmtable[di + 0] + pcmtable[di + 1] * 0x100);
            r.dx <<= 1;
            //;++	inc	dx		; DX = PCM開始番地

            r.al = 2;
            r.ah = r.dl;
            outdata2a();// Start adrs(L)
            r.al++;
            r.ah = r.dh;
            outdata2a();// Start adrs(H)

            r.al++;
            r.dx = (ushort)(pcmtable[di + 2] + pcmtable[di + 3] * 0x100);
            r.dx <<= 1;// DX = PCM終了番地
            r.dx--;

            r.ah = r.dl;
            outdata2a();// End adrs(L)
            r.al++;
            r.ah = r.dh;
            outdata2a();// End adrs(H)

            r.bx++;
        }

        //･･････････････････････････････････
        //	DSP設定コマンドの処理
        //･･････････････････････････････････

        private void dsp_set()
        {
            mode_change();
            r.al = (byte)nax.objBuf[0][r.bx + 1].dat;
            set_mul_level();
            r.al = (byte)nax.objBuf[0][r.bx + 2].dat;
            r.bx += 3;
            set_buffer_num();
        }

        //････････････････････････････････････
        //	DSPモードの切り換え
        //	entry	AL = モード
        //		0 : なし
        //		1 : 疑似ステレオ
        //		2 : サラウンド
        //		3 : エコー
        //････････････････････････････････････
        private void mode_change()
        {
            ushort dsbk = r.ds;
            r.ds = (ushort)(farjmp1_ >> 16);// スワップセグメント
            mode_change_sub();
            r.ds = (ushort)(farjmp2_ >> 16);// メインセグメント
            mode_change_sub();
            r.ds = dsbk;
        }

        private void mode_change_sub()
        {
            //	pushf
            ushort axbk = r.ax;

            dsp_mode = r.al;// モードを保存
            //	cli
            jump1_ = 0x3e3e;
            nax.pc98.OutportC4231_Jump1(jump1_);
            r.al--;
            if (r.al == 0)
            {
                //mode1:
                jump2_ = 0;
                nax.pc98.OutportC4231_Jump2((byte)jump2_);
                goto mode_chg_exit;
            }
            r.al--;
            if (r.al == 0)
            {
                //mode2:
                jump2_ = 1;// (ushort)(test_lop2_ - test_lop1_);
                nax.pc98.OutportC4231_Jump2((byte)jump2_);
                goto mode_chg_exit;
            }
            r.al--;
            if (r.al == 0)
            {
                //mode3:
                jump2_ = 2;// (ushort)(test_entry3_ - test_lop1_);
                nax.pc98.OutportC4231_Jump2((byte)jump2_);
                goto mode_chg_exit;
            }

            jump1_ = (ushort)(0xeb + (dsp_exit_ - jump1_ - 2) * 256);
            nax.pc98.OutportC4231_Jump1(jump1_);

        mode_chg_exit:
            r.ax = axbk;
            //	popf
        }

        //････････････････････････････････････････
        //	DSPレベルの設定
        //	entry	AL = レベル(0～127)
        //････････････････････････････････････････

        private void set_mul_level()
        {
            dsp_level = r.al;
            if (r.al > 127)
            {
                r.al = 127;
            }
            ushort dsbk = r.ds;
            r.ds = (ushort)farjmp1_;// スワップセグメント
            set_mul_sub();
            r.ds = (ushort)farjmp2_;// メインセグメント
            set_mul_sub();
            r.ds = dsbk;
        }

        private void set_mul_sub()
        {
            level1_ = r.al;
            level2_ = r.al;
            level3_ = r.al;
        }

        //;･･･････････････････････････････････････
        //;	バッファ数の指定
        //;	entry	AL = 数(0～MAXBUF-2)
        //;･･･････････････････････････････････････

        private void set_buffer_num()
        {
            r.al += 2;
            if (r.al >= NAX.MAXBUF)
            {
                r.al = (byte)NAX.MAXBUF;
            }

            r.ah = 0;
            r.dx = (ushort)(NAX.FIFO_SIZE * 2);
            uint ans = (uint)(r.ax * r.dx);
            r.ax = (ushort)ans;
            r.dx = (ushort)(ans >> 16);

            //	pushf
            //	cli
            fifofin = r.ax;
            fifoptr1 = 0;
            fifoend1 = (ushort)(NAX.FIFO_SIZE * 2);
            fifoptr2 = (ushort)(NAX.FIFO_SIZE * 2);
            fifoend2 = (ushort)(NAX.FIFO_SIZE * 4);
            //	popf
        }

        //;------------------------------------------
        //;	効果音発音前の音色・音量に戻す
        //;------------------------------------------

        private void last_setpcm()
        {
            r.push(r.bx);
            r.push(r.si);
            calc_nowwork();
            r.al = data_buff[r.si + TS];
            tonepcm();
            r.al = data_buff[r.si + VS];
            volchgr(data_buff);
            r.ah = data_buff[r.si + PN];
            pan2();
            r.al = data_buff[r.si + FD];
            r.ah = data_buff[r.si + FD+1];
            r.push(r.ax);
            r.ah = r.al;
            r.al = 9;
            outdata2a();// DELTA-N (L)
            r.ax = r.pop();
            r.al = 0x0a;
            outdata2a();// DELTA-N (H)
            r.si = r.pop();
            r.bx = r.pop();
            return;
        }

        //;-----------------------------------
        //;	送信チャネルの切り換え
        //;-----------------------------------

        private void channel()
        {
            r.push(r.ax);
            r.ax = 0;
            r.al = realch;
            r.di = 0;//ofs:chtbl2
            r.di += r.ax;
            r.ax = r.pop();
            r.al--;
            chtbl2[r.di] = r.al;
            r.ch = r.al;// 新しいチャネル番号とする
            calcbit();
            shflag1 |= r.ax;// 効果音中フラグを立てる
            shflag2 |= r.dl;
            ch_change();
        }

        private void ch_change()
        {
            r.bx++;
            data_buff[r.si + LC] = 1;// 音長カウンタの初期化
            data_buff[r.si + AD] = r.bl;
            data_buff[r.si + AD+1] = r.bh;// 演奏番地を次へ進める
            r.sp += 4;// [back_fm],[play_fm]
            rechannelFlg = true;
        }
        //;	拡張86PCMのチェック

        /// <summary>
        /// 拡張86PCMのチェック
        /// true(ZeroFlg=0):拡張86PCMを使用している false(ZeroFlg=1):していない
        /// </summary>
        /// <returns></returns>
        public bool check_86pcm()
        {
            return (nax.m_mode[3] & 0x10) != 0;
        }

        /// <summary>
        /// WSS-PCMのチェック
        /// </summary>
        /// <returns>true:ZF==0(WSS-PCMである) false:ZF==1</returns>
        public bool check_wsspcm()
        {
            return ((nax.m_mode[3] & 0x1) != 0);
        }


        /// <summary>
        /// EMS使用のチェック
        /// </summary>
        /// <returns>使っている:true:ZF==0  使っていない:false:ZF==1</returns>
        private bool check_emsuse()
        {
            return ((nax.m_mode[3] & 0x8) != 0);
        }
        /// <summary>
        /// 拡張PCMチェック
        /// </summary>
        /// <param name="siBuf"></param>
        /// <returns>true:ZF==0 false:ZF==1</returns>
        private bool check_extpcm(byte[] siBuf)
        {
            return (siBuf[r.si + PM] & 0x1f) != 0;
        }

    }
}
