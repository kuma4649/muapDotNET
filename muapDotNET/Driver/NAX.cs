using muapDotNET.Common;
using musicDriverInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace muapDotNET.Driver
{

    public class NAX
    {
        public class NAXException : Exception
        {
            public NAXException(string message) : base(message)
            {
            }
        }

        private Work work;
        private IDictionary envVars;
        public Pc98 pc98 = null;
        public EMS ems = null;
        public x86Register reg = null;
        private string arg = "";
        public PLAY4 play4 = null;
        public MyEncoding myEnc = null;
        private byte[] filebuf;

        //自己書き換え判定向け
        private int port11 = 0x188;
        private int port31 = 0x18c;//OPNA portB
        private int port32 = 0x18c;//OPNA portB
        private int port34 = 0x18c;//OPNA portB
        private ushort cyon = 0xffb1;//自己書き換え	mov	wpr cyon,0ffb1h	; inc cl → mov cl,0ffh
        private int pt1 = 0x0a;//PLAY4 IMR操作
        private int pt2 = 0x0a;//PLAY4 IMR操作
        public int jmp1 = 0x0;//PLAY4 タイマ処理(スレーブにも送るかどうか)
        private int setand = 0xef;//PLAY4 IMR操作
        private int intm3 = 0x0;//16kHz切り替え
        private ushort intm4 = 0x0;//16kHz切り替え (有効な場合、NOPx2に書き換え)
        private ushort segad2 = 0;

        //MUAP.INCより
        public static int MAXPCM = 100;
        public static int MAXBUF = 18;
        public static int FIFO_SIZE = 128;

        //アドレス参照
        private ushort setnew_extpcm2 = 0;
        private byte[] toneBuffFromOutside = null;
        private ushort[] pcmtbl = new ushort[]{
            0x0100,0x1010,0x8010,0x6000,
            0xc001,0x0002,0x0003,0xff04,0xff05,
            0xff0c,0xff0d,0xffff
        };
        public List<Tuple<byte, object>> functionList = new List<Tuple<byte, object>>();
        private Action<object>[] jmptbl;


        public byte comlength;
        public string lyric = "";


        public NAX(Work work, x86Register regs,IDictionary envVars, Pc98 pc98,EMS ems,string arg, byte[] toneBuffFromOutside, ushort[] labelPtr)
        {
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            myEnc = new MyEncoding();

            this.work = work;
            this.envVars = envVars;
            this.pc98 = pc98;
            this.ems = ems;
            this.arg = arg;
            this.reg = regs;
            this.play4 = new PLAY4(this, work,labelPtr);
            this.toneBuffFromOutside = toneBuffFromOutside;
            work.fifoBuf= new byte[FIFO_SIZE * MAXBUF * 2];
            for (int i = 0; i < FIFO_SIZE * MAXBUF * 2; i++) work.fifoBuf[i] = 0x80;
            work.int0bEnt = Int0bEntry;


            InitParaJmp();
            InitJmpTbl();

            Init_call_menu();
            Inst();
        }

        public void Int08Entry()
        {
            play4.int08ent();
        }

        public void TimerEntry()
        {
            play4.timer_entry();
        }

        public void Int0bEntry()
        {
            play4.int0bent();
        }

        //
        //	YM2608+3438 Music Integrated Driver NAX3  version 6.32
        //	  copyright(C)1987,1989-1995 by Packen Software[jan.5.1996]
        //

        private ushort funcoff = 0x180;// $108 ファンクションベクタ番号
        private ushort funcseg = 0x182;
        private ushort tboff = 0x50;// $114 save interrupt vector(timer A)
        private ushort tbseg = 0x52;// $116
        public byte[] m_mode = new byte[]{
            0x00		// $118 b1 = SSGPCM
            ,0x00		//      b2 = -V
            ,0x00       //    	b7 = PCM 1MB
            ,0x20       //      b0 = WSS, b1 = 代替YM2608, b2 = ADPCM
                        //      b3 = EMS使用, b4 = 86PCM, b7 = ﾒｲﾝﾒﾓﾘPCM
        };

        public ushort[] _object = new ushort[3] { 0, 0, 0 };// $11c object buffer segment
        public MmlDatum[][] objBuf = new MmlDatum[3][] {
            null,//熊:ここに演奏データが入る
            null,
            null };
        public ushort tone = 0;// $122 tone data buffer segment
        public byte[] toneBuff = null;//熊:Tone保存用バッファ
        private ushort extseg = 0;// $124 86/wss pcm segment
        private ushort extlen = 0x1000;// $126 86/wss pcm length(*16)
        private ushort[] bufleno = new ushort[3] { 0, 0, 0 };// $128 object buffer length
        private ushort obj_len = 0;// $12e object data length
        //point1 dw  ofs:data_buff	; $132 演奏ワークのオフセット
        //point2  dw ofs:comdata	; $134 歌詞データの番地
        public ushort pcmseg = 0;// $136 PCM保存バッファのセグメント
        private ushort pcmlen = 0;		// $138
        public byte[] pcmBuff = null;//熊:PCM保存用バッファ
        private byte ym2203 = 0;// $13a YM2203認識数
        private byte ym2608 = 0;// $13b YM2608認識数
        private byte ym3438 = 0;// $13c YM3438認識数
        public ushort fifoseg = 0;// $13e FIFOセグメント
        //public byte[] fifosegBuf = new byte[FIFO_SIZE * MAXBUF*2];
        private ushort usrbyte = 0;// $140 USRPCM使用バイト数
        private ushort pcmbyte = 0;// $142 SSGPCM使用バイト数
        private ushort pcmfile = 0;// $144 ユーザPCMファイル名セグメント
        private byte[] pcmfileBuf = new byte[(20 + MAXPCM - 50) * 13 + 1];
        //    even
        //int08 vct<>      ; $108 内蔵タイマint
        //int0b   vct<>		; WSS-PCM割り込みint
        public ushort phandle = 0xffff;// PCM用EMSハンドル
        private byte[] pemsbuf = new byte[32];// EMSマップ情報保存用
        //stayseg dw	0		; 常駐セグメント値
        //callseg dw?
        //stay    ends

        //wrks    segment word public 'sr_data'
        private int path1=0;// ファイル名の先頭(\含む) //熊: ファイル名の位置
        private int path2 = 0;// 親ディレクトリの先頭(\含む) //熊: 親ディレクトリの位置
        private int path3 = 0;// 拡張子の先頭(.含む)//熊:拡張子の位置
        private int flength = 0;// ファイル名の長さ
        private byte usrpcm = 0;
        private byte usrpcmn = 0;// ユーザPCM登録中番号
        private ushort voldata = 256;
        private ushort deltax = 127;
        /// <summary>
        /// 注意(byte型になってます)
        /// </summary>
        private byte[] xdata = new byte[4];
        private byte[] bufbuf = new byte[128];
        private string pcm_path = "*.*" + new string((char)0, 61);//usrpcm検索
        private string pcm_path1 = "*.*" + new string((char)0, 61);//sub-usrpcm検索
        private byte[] cusbuff;// label   byte		; PCM.TBL , TONE.DTA 用リードバッファ
        private static int MAXCUS = 512;// 読み込みバイト数
        //wrkbuff ends

        //;===========================================
        //;	インストール時のみ使用ルーチン
        //;===========================================

        private string mess_2 = @"
-Ax   : PCMバッファにEMSを使用しない。xは容量(1～8)*32KB。
-Bxx  : SSGPCMバッファ容量を指定する。
-Fx   : フェードアウト速度設定。
-I    : 演奏ルーチン中で外部からの割り込みを禁止。
-Lxx  : ファンクションベクタ番号を指定。
-Mx   : DMAチャネルを指定する。
-Oxx  : 演奏バッファとしてメモリを確保する。
-P    : 常駐時にPCM.DTAも読み込む(-A指定時・EMS無では不可)。
-Q    : 常駐時にSSGPCM.DTAも読み込む。
-T    : ファイルがない場合のエラー表示をしない。
-Vxx  : タイマA割り込みベクタ番号を指定(0B,10～17)。
-Yx,y : YM2608,YM3438の先頭番地の設定。
-2    : YM3438の代わりにYM2203を1つ使用する。
-3    : 86B/WSS-PCMの使用を禁止する。
-6    : SSGPCMを16KHzで使用する。
-8    : 86B/WSS-PCMバッファを1MB確保する。
-(x   : 86B/WSS-PCMの周波数を指定。
-?    : パラメータヘルプを表示する。

★この音楽ドライバは無申請でご自由に営利目的販売ソフトウェアに組み込んで使用す
　ることができます。
※開発環境はソフトベンダータケルで「みゅあっぷ98/iv」をご購入下さい（3000円）。";

        //mess_2a db	13,10,27,"[m"

        //    db	"-P",2,"PCM.DTA,TBLを読み込む",1
        //	db	"-R",2,"NAX3を常駐から切り離す",1
        //	db	"-X",2,"SSGPCM.DTA/TBLに指定ファイルを読み込む",1
        //	db	"-?",2,"パラメータヘルプを表示する",1,0

        //mess_2x db	"。"
        //mess_2y db	13,10,27,"[m$"
        //mess_2z db	9,": $"

        private string mess_3 = @"
 ＮＡＸⅢ ＤＳＰ  Ｖｅｒｓｉｏｎ ６.３４
 copyright (c)1990-96 by Packen Software.
 for YM2608/3438/2203, YMF288, PC-9801-86PCM, Windows Sound System PCM
 Public copy & use free.
";
        //mess_5 db	"NAX3が既に常駐されています",0
        //mess_5a db	"みゅあっぷ98/ivが常駐されています",0
        private string mess_5b = "無効なオプション設定のため常駐を中止しました";
        //mess_d0 db	"の割り込みベクタが書き換えられている",0
        //mess_d1 db	"タイマA$"
        //mess_d2 db	"INT 60h$"
        //mess_d3 db	"内蔵タイマ$"
        //mess_d4 db	"INT0$"
        //mes_e1 db	"NAX3を切り離した",0
        //mes_e2 db	"強制的に切り離すならリターンキーを押して下さい",0
        private string mes_e3 = "SSGPCM.DTAがPCMバッファをオーバーしました";//,0
        //mes_e4 db	"NAX3の切り離しに失敗した",0
        //mes_e5 db	"MCBの切り離しに失敗した",0
        private string mes_e10 = "WSS割り込みがINT0に";//,24,12,0
        private string mes_e11 = "DMAが";//,24,12,0
        private string mess_f1 = "サウンドボードが指定ポートに接続されていないのでBGMの使用を停止します";//,0
        //mess_g1 db	27,"[31m$"
        private string mess_g2 = " が見つかりません";
        private string mess_p1 = "ADPCM data transfering";
        private string mess_p2 = "タイムアウトのため中止しました";
        private string mess_p3 = " User Aborted.";//,1
        private string mess_p4 = "PCMバッファ容量が不足しています";//,0
        private string mes_s0 = " Using device(s) = YM";
        private string mes_s4 = " + 2203";
        private string mes_s2 = "2203";
        private string mes_s9 = " + 2608";
        private string mes_s3 = "2608";
        private string mes_s5 = " + 3438";
        private string mes_s6 = " + 86B<17ch>DSP-";
        private string mes_s12 = " + WSS<17ch>DSP-";
        private string mes_s7 = " + ADPCM";
        private string mes_s13 = "PCM";
        private string mes_s8 = "";//	13,10,"$"

        private byte sflag = 0x00;
        private byte sys_flg = 0x00;

        private ushort extpcmadr = 0;// 拡張PCM番地
        private byte oldmapadr = 0;// EMSページ番号
        private byte extmapflg = 0;// b0 = EMSマップフラグ, b1 = usrpcm登録
        private byte pcmcnt1 = 0;
        private ushort pcmcnt2 = 0;

        private string[] dspdtop = new string[]
            {
            "o",//27,"[1D$"
            "+",//27,"[1D$"
            "*",//27,"[1D$"
            "･"//$"
            };
        //dspdend label byte
        private int dspcnt1 = 0;//ofs:dspdtop
        private byte dspcnt2 = 0;
        private byte pcm_flg = 0;// -P の指定フラグ
        //emsname db	"EMMXXXX0",0
        private string emsname2 = "MUAP_PCM";
        private byte[] sbufbuf = new byte[128];
        private string tone_path = "TONES.DTA";// + new string((char)0, 55);
        private string pcmt_path = "PCM.TBL";// + new string((char)0, 57);
        private string pcmd_path = "PCM.DTA";// + new string((char)0, 57);
        private string ssg1_path = "SSGPCM.DTA";// + new string((char)0, 54);
        private string ssg2_path = "SSGPCM.TBL";// + new string((char)0, 54);
        public byte[] dma_ch_data = new byte[]{
            0x01,0x01,0x03,0x27,
            0x02,0x05,0x07,0x21,
            0x07,0x00,0x00,0x00,
            0x03,0x0d,0x0f,0x25
        };

        //curbuff   db	64 dup(?); カレントディレクトリ保存
        //wrkinst ends

        public void Init_call_menu()
        {
            //---------------------------------------
            //	環境変数によるパスの設定
            //---------------------------------------
            if (envVars != null)
            {
                if (envVars.Contains("DTA")) 
                    tone_path = Path.Combine(envVars["DTA"].ToString(), tone_path);
                if (envVars.Contains("PCM"))
                {
                    pcmd_path = Path.Combine(envVars["PCM"].ToString(), pcmd_path);
                    pcmt_path = Path.Combine(envVars["PCM"].ToString(), pcmt_path);
                    ssg1_path = Path.Combine(envVars["PCM"].ToString(), ssg1_path);
                    ssg2_path = Path.Combine(envVars["PCM"].ToString(), ssg2_path);
                }
                if (envVars.Contains("UDP"))
                    pcm_path = Path.Combine(envVars["UDP"].ToString(),pcm_path);
                if (envVars.Contains("SUD"))
                    pcm_path1 = Path.Combine(envVars["SUD"].ToString(), pcm_path1);
            }
        }


        //;--------------------------------------------------
        //;	カレントドライブ、ディレクトリの設定
        //;	entry BX = pass name offset
        //;--------------------------------------------------

        //
        // 略
        //

        //;------------------------------------------------
        //;	各パス名にカレントディレクトリを指定
        //;------------------------------------------------

        //
        // 略
        //

        //;-------------------------
        //;	常駐チェック
        //;-------------------------

        //
        // 略
        //

        //;---------------------------------------
        //;	みゅあっぷセグメントのサーチ
        //;	entry SI = vector address
        //;	exit AX = segment data
        //;		NZ = not muap
        //;---------------------------------------

        //
        // 略
        //

        //;======================================
        //;	非常駐処理(常駐されている)
        //;======================================
        //
        // 略
        //
        private void dsp_end(LogLevel llv, string msg)
        {
            Log.writeLine(llv, msg);
            throw new NAXException("常駐処理時失敗");
        }

        //;--------------------------------
        //;	PCMデータの再ロード
        //;--------------------------------
        //
        // 略
        //

        //;-------------------------------
        //;	SSGPCMデータの変更
        //;-------------------------------
        //
        // 略
        //

        //;---------------------------------------------
        //;	ヘルプ、その他のメッセージの表示
        //;---------------------------------------------

        private void err_param()
        {
            string dx = mess_5b;// 無効なパラメータ表示
            dsp_end(LogLevel.ERROR, dx);
        }

        //dsp_help2:
        //
        // 略
        //

        private void dsp_help(string arg)
        {
            //パラメータヘルプの表示
            Log.writeLine(LogLevel.INFO, mess_2);
            throw new NAXException("常駐せずに終了");
        }

        //	特別文字列表示ルーチン
        //	entry DX = 文字列番地
        // [DX] = 0 : "。",13,10,"$"
        //		[DX] = 1 : 13,10,"$"
        private void putasciz(string mes, int sw)
        {
            if (sw == 0)
                Log.writeLine(LogLevel.INFO, mes + "。");
            else if (sw == 1)
                Log.writeLine(LogLevel.INFO, mes);
        }

        //;-------------------------------------
        //;	NAX切り放しサブルーチン
        //;-------------------------------------
        //
        // 略
        //

        //;--------------------------
        //;	MCBの切り離し
        //;--------------------------
        //
        // 略
        //

        //;===================================================
        //;	パラメータのチェック(BX, DX : 破壊禁止)
        //; 以下常駐のためのプログラム
        //;===================================================

        private void Inst()
        {
            Log.writeLine(LogLevel.INFO, mess_3);// 初期メッセージ

            check_port();// YM2608ポートの識別
            check_extend();// YM3438

            //熊 ここからコマンドラインチェック?

            para3();
        }

        private void para3()
        {
            reg.bx = 0;
            while (true)
            {
                if (skip_bl(arg)) break;// separator skip

                char al = arg.Length>reg.bx ? arg[reg.bx] : (char)0;
                char ah = arg.Length > reg.bx + 1 ? arg[reg.bx + 1] : (char)0;
                reg.bx += 2;
                if (al != '-' && al != '/')// セパレータのチェック
                {
                    err_param();
                    return;
                }
                //paras:
                ah = xsmall(ah);
                // mov di, ofs:paradta
                int cx = paradta.IndexOf(ah);// check data number
                if (cx < 0)
                {
                    err_param();// 無効のパラメータしか無かった
                    return;
                }
                parajmp[cx](arg);
            }

            para0();// parameter end
        }

        private string paradta = "FLV?YIOPTMB6Q23(A8";
        private Action<string>[] parajmp;
        private void InitParaJmp()
        {
            parajmp = new Action<string>[]{
                fade_time, funvct,
                vector, dsp_help,
                outport, disint,
                memget, pcm_load,
                undisp, set_dmach,
                ssgpcm_buff, ssgpcm16,
                read_ssgpcm,ext_2203,
                dis9821pcm,pcmfreq,
                disems,pcm1mb
            };
        }

        //･･･････････････････････････････････
        //	エラーメッセージの抑止
        //･･･････････････････････････････････
        private void undisp(string _)
        {
            sflag |= 1;
        }

        //;-----------------------------------
        //;	問答無用ウェイトの設定
        //;-----------------------------------
        //
        // 略
        //

        //-------------------------------------------------
        //	ファイルオープンエラー時のパス名表示
        //-------------------------------------------------
        private void patherr(string path)
        {
            if ((sflag & 1) != 0) return;

            Log.writeLine(LogLevel.ERROR, string.Format("{0}{1}", path, mess_g2));
        }

        //	小文字から大文字変換
        private char xsmall(char al)
        {
            if ((byte)al >= 0x61 && (byte)al <= 0x7a)
                return (char)((byte)al & 0xdf);
            return al;
        }

        //--------------------------------
        //	演奏バッファの確保
        //--------------------------------
        private void memget(string arg)
        {
            Set_buff(arg);// AL = 演奏バッファ確保容量/100h
            reg.ah = reg.al;
            reg.al = 0;
            bufleno[0] = reg.ax;
        }

        private void ssgpcm_buff(string arg)
        {
            Set_buff(arg);
            reg.ah = reg.al;
            reg.al = 0;
            pcmlen = reg.ax;
        }

        //-----------------------------
        //	SSGPCMの16KHz指定
        //-----------------------------
        private void ssgpcm16(string _)
        {
            m_mode[0] |= 0x80;// フラグを立てる
            play4.intm1 = 0x9a; // タイマの周波数変更
            play4.intm2 = 0x7d; //
            reg.ax = 0x9090;
            intm3 = reg.al;
            intm4 = reg.ax;
        }

        //--------------------------------
        //	SSGPCM.TBLの読み込み
        //--------------------------------
        private void read_ssgpcm(string arg)
        {
            ushort bxbk = reg.bx;
            reg.ax = pcmlen;
            if (reg.ax == 0) pcmlen = 0x8000;
            m_mode[0] |= 2;

            try
            {
                play4.ssgtable = File.ReadAllBytes(ssg2_path);
                Log.WriteLine(LogLevel.INFO, "[{0}] File found.", ssg2_path);
            }
            catch
            {
                Log.WriteLine(LogLevel.ERROR, "File not found.{0}",ssg2_path);
                //patherr(ssg2_path);
                //throw;
            }
            finally
            {
                reg.bx = bxbk;
            }          
        }

        //--------------------------------------
        //	タイマAベクタのユーザ設定
        //--------------------------------------
        private void vector(string arg)
        {
            m_mode[1] |= 4;// -Vオプションの指定をした
            Set_buff(arg);// AL = ベクタ番号
            if (reg.al != 0xb //拡張バスINT0の指定か
                || (reg.al < 0x10 && reg.al > 0x17) // 10-17まで
                )
                err_param();
            int00();
        }
        private void int00()
        { 
            reg.ah = 0;
            ushort ax_bk = reg.ax;
            reg.ax <<= 2;// AX = ベクタ番地
            tboff = reg.ax;
            reg.ax += 2;
            tbseg = reg.ax;
            reg.ax = ax_bk;

            if (reg.al == 0x0b)
            {
                pt1 = 2;// IMR操作ポートの変更(-V0B用)
                pt2 = 2;// 
                jmp1 = 0x0ceb;// jmp short +12h とする(スレーブ処理の無視)
                reg.al = 0x13;
            }

            reg.al -= 0x10;// AL = 0-7 (10-17に対応)
            reg.cl = reg.al;
            reg.ax = 0xfe01;// AH = AND data, AL = OR data
            reg.al = reg.rol(reg.al, reg.cl);
            reg.ah = reg.rol(reg.ah, reg.cl);
            setand = reg.ah;// PLAY2のコードに直接書き込む
        }

        private void funvct(string arg)
        {
            Set_buff(arg);// AL = ベクタ番号
            reg.ah = 0;
            reg.ax <<= 2;
            funcoff = reg.ax;
            reg.ax += 2;
            funcseg = reg.ax;
        }

        //;------------------------------------------
        //;	タイマA割り込み番号の自動指定
        //;------------------------------------------

        private void autovct()
        {
            ushort dxbk = reg.dx;
            ushort cxbk = reg.cx;
            ushort axbk = reg.ax;

            if ((m_mode[1] & 4) != 0)
                return;

            reg.cx = 0x40;
            //getv1:
            reg.cx = 0;//loop getv1 //熊 おい

            reg.dx = (ushort)port11;
            reg.al = 0xe;
            pc98.OutportB(reg.dx, reg.al);
            pc98.OutportB(0x5f, reg.al); // for H98

            reg.cx = 0x40;
            //getv2:
            reg.cx = 0;//loop getv2 //熊 おい2

            reg.dx += 2;
            reg.al = pc98.InportB(reg.dx);// B7 = IRST0 , B6 = IRST1
            reg.al &= 0xc0;
            if (reg.al == 0) reg.al = 0xb;// INT0
            else if (reg.al == 0x80) reg.al = 0x13;// INT4
            else if (reg.al == 0xc0) reg.al = 0x14;// INT5
            else if (reg.al == 0x40) reg.al = 0x15;// INT6
            int00(); // 割り込みベクタの設定
            //skip_autov:

            reg.ax = axbk;
            reg.cx = cxbk;
            reg.dx = dxbk;
        }

        //----------------------------------
        //	出力ポート番号の指定
        //----------------------------------
        private void outport(string arg)
        {
            sys_flg |= 4;// -Y指定
            reg.al = (byte)xsmall(arg[reg.bx]);

            getax(arg);
            setport1(reg.ax);// ポートアドレスを直接書き込む(YM2608)
            reg.ax += 4;
            setport3(reg.ax);

            if (arg[reg.bx] != ',') return;

            reg.bx++;
            getax(arg);
            play4.port5 = reg.ax;// ポートアドレスを直接書き込む(YM3438)
            reg.ax += 4;
            play4.port7 = reg.ax;
        }

        //------------------------------------------
        //	ポートのアドレスを格納する
        //	entry AX = 各ポートアドレス
        //------------------------------------------

        private void setport1(int ax)
        {
            //熊　注意：オリジナルは自己書き換えのためここでは変数として持たせるのみとする
            play4.port1 = (ushort)ax;// ポート番号格納番地
            play4.port11 = (ushort)ax;// ポート番号格納番地
            play4.port12 = (ushort)ax;// ポート番号格納番地
            play4.port18 = (ushort)ax;// ポート番号格納番地
            play4.port19 = (ushort)ax;// ポート番号格納番地
        }

        private void setport3(int ax)
        {
            //熊　注意：オリジナルは自己書き換えのためここでは変数として持たせるのみとする
            play4.port3 = (ushort)ax;
            play4.port31 = (ushort)ax;
            play4.port32 = (ushort)ax;
            play4.port33 = (ushort)ax;
            play4.port34 = (ushort)ax;
            play4.port35 = (ushort)ax;
            play4.port36 = (ushort)ax;
            play4.port37 = (ushort)ax;
        }

        //;	16進数4桁文字の獲得
        private void getax(string arg)
        {
            // AX = 獲得した16進数4桁
            Set_buff(arg);
            byte dl = reg.al;
            Set_buff(arg);
            reg.ah = dl;
        }

        //; カンマチェック
        //checkcm:
        //
        // 略
        //

        //;-----------------------------------
        //;	16進2桁パラメータの獲得
        //;	AL = hex data, AH, CL破壊
        //;-----------------------------------
        private void Set_buff(string arg)
        {
            reg.al = arg.Length > reg.bx ? (byte)arg[reg.bx] : (byte)0;
            reg.ah = arg.Length > reg.bx+1 ? (byte)arg[reg.bx+1] : (byte)0;
            reg.bx += 2;

            if(!Chkparam()) err_param();
            byte a = reg.al;
            reg.al = reg.ah;
            reg.ah = a;
            if (!Chkparam()) err_param();
            reg.ah <<= 4;

            reg.al = (byte)((reg.al & 0xf) | (reg.ah & 0xf0));
        }

        private bool Chkparam()
        {
            if (reg.al >= (byte)'0' && reg.al <= (byte)'9')
            {
                reg.al -= (byte)'0';
                return true;
            }

            reg.al &= 0xdf;

            if (reg.al >= (byte)'A' && reg.al <= (byte)'F')
            {
                reg.al -= (byte)'A';
                reg.al += 10;
                return true;
            }

            return false;
        }

        //------------------------------------
        //	フェードアウト時間変更
        //------------------------------------

        private void fade_time(string arg)
        {
            reg.al = arg.Length > reg.bx ? (byte)arg[reg.bx++] : (byte)0;
            reg.al -= (byte)'0';
            if ((reg.al < 0 || reg.al > 9) && (reg.al < 0x11 || reg.al > 0x2a))
                err_param();
            if (reg.al >= 0x11 && reg.al <= 0x2a)
                reg.al -= 7;

            reg.al++;
            reg.ah = 0;
            reg.ax <<= 3;
            play4.fadesave = reg.ax;
        }

        //;----------------------------
        //;	各種モード設定
        //;----------------------------

        private void dis9821pcm(string _)
        {
            sys_flg |= 2;
        }

        private void pcm1mb(string _)
        {
            m_mode[2] |= 0x80;
        }

        private void disems(string arg)
        {
            m_mode[3] |= 0x80;
            setup_mainpcm();
            reg.al = arg.Length > reg.bx ? (byte)arg[reg.bx++] : (byte)0;

            if (reg.al < (byte)'1' || reg.al > (byte)'8') err_param();
            reg.al -= (byte)'1';
            reg.ah = 0;
            reg.al++;
            reg.ax <<= 11;

            extlen = reg.ax;// メインメモリPCMの長さを指定
            return;
        }

        //;･････････････････････････････
        //;	86B-PCM周波数設定
        //;･････････････････････････････

        //ext11khz:
        //
        // 略
        //

        //ext8khz:
        //
        // 略
        //

        //-----------------------------------
        //	PCMデータの読み込み指定
        //-----------------------------------
        private void pcm_load(string _)
        {
            pcm_flg = 1;
        }

        //--------------------------------
        //	演奏中の割り込み禁止
        //--------------------------------
        private void disint(string _)
        {
            reg.al = 0x90;
            play4.stiof1 = reg.al;// PLAY2のSTIにNOPを格納
        }

        //･････････････････････････････
        //	EMSの存在チェック
        //	exit	CY = なし 熊:false = なし
        //･････････････････････････････
        private bool check_ems() 
        {
            //emsがあることにする
            return true;
        }

        //-------------------------------
        //	PC-9821対応の処理
        //-------------------------------

        private void ena_9821()
        {
            ushort dxbk = reg.dx;
            ushort bxbk = reg.bx;
            ushort esbk = reg.es;

            if (!chk_sound())// 拡張音源のチェック
            {
                m_mode[3] &= 0xee;// 86B/WSS-PCMを無効とする
                reg.es = esbk;
                reg.bx = bxbk;
                reg.dx = dxbk;
                return;
            }

            check_extend();// YM3438側を再度行なう
            if ((m_mode[3] & 0x80) != 0)
            {
                reg.es = esbk;
                reg.bx = bxbk;
                reg.dx = dxbk;
                return;
            }

            if (!check_ems())// EMSの存在チェック
            {
                m_mode[3] |= 0x80;// メインメモリPCM
                setup_mainpcm();
                reg.es = esbk;
                reg.bx = bxbk;
                reg.dx = dxbk;
                return;
            }

            if (check_phandle())// MUAP_PCMが存在するかチェック
            {
                m_mode[3] |= 0x08;// EMS使用
                reg.es = esbk;
                reg.bx = bxbk;
                reg.dx = dxbk;
                return;
            }

            reg.bx = 32;//熊:32 * 16Kbyte/page = 512Kbyte
            if ((m_mode[2] & 0x80) != 0) reg.bx = 64;// 1MB確保?
            reg.ah = 0x43;
            ushort dx = reg.dx;
            ems.cS4231EMS_AllocMemory(ref reg.ah, ref dx, reg.bx);// 512KBのEMS割り当て
            reg.dx = dx;

            if (reg.ah != 0)// 不足していた場合はPCM対応しない
            {
                m_mode[3] |= 0x80;// メインメモリPCM
                setup_mainpcm();
                reg.es = esbk;
                reg.bx = bxbk;
                reg.dx = dxbk;
                return;
            }

            phandle = reg.dx;// ハンドルの保存
            reg.ax = 0x5301;
            reg.si = 0;// emsname2;
            ems.cS4231EMS_SetHandleName(ref reg.ah, reg.dx, emsname2);// ハンドル名の設定

            m_mode[3] |= 0x08;// EMS使用
            reg.es = esbk;
            reg.bx = bxbk;
            reg.dx = dxbk;
        }

        private void setup_mainpcm()
        {
            ushort dib = reg.di;
            ushort axb = reg.ax;

            reg.di = 0;
            set_naxadrs(play4.naxad1);
            reg.di = 0;
            set_naxadrs(play4.naxad2);
            reg.di = dib;
            reg.ax = axb;
        }

        private void set_naxadrs(byte[] buf)
        {
            buf[reg.di] = 0xe8;
            reg.ax = setnew_extpcm2;
            reg.ax -= reg.di;
            reg.ax -= 3;

            buf[reg.di + 1] = (byte)reg.ax;
            buf[reg.di + 2] = (byte)(reg.ax>>8);
            reg.ax = 0x3e3e;
            buf[reg.di + 3] = (byte)reg.ax;
            buf[reg.di + 4] = (byte)(reg.ax >> 8);
            buf[reg.di + 5] = (byte)reg.ax;
            buf[reg.di + 6] = (byte)(reg.ax >> 8);
            buf[reg.di + 7] = (byte)reg.ax;
            buf[reg.di + 8] = (byte)(reg.ax >> 8);
        }

        //･････････････････････････････････
        //	EMSハンドルの検索
        //	exit	CY = なかった  熊:false = なし
        //･････････････････････････････････

        private bool check_phandle()
        {
            reg.dx = 0;
            reg.es = reg.cs;

            //load3:
            string sbuf = "";
            do
            {
                reg.di = 0;//ofs:sbufbuf
                reg.ax = 0x5300;
                ems.cS4231EMS_GetHandleName(ref reg.ah, reg.dx, ref sbuf);// ハンドル名の読み取り
                if (reg.ah != 0) return false;

                reg.dx++;// 次のハンドルへ
                if (reg.dx == 0) return false;

                //reg.si = 0;//ofs:emsname2
                //reg.cx = 4;
                //load4:
            } while (sbuf != emsname2);
            reg.dx--;
            phandle = reg.dx;
            m_mode[2] &= 0x7f;
            if (reg.bx == 64)// PCM 1MB?
            {
                m_mode[2] |= 0x80;
            }
            sys_flg |= 0x10;// 登録済み
            return true;
        }

        //･････････････････････････････
        //	86B-PCM周波数設定
        //･････････････････････････････

        private void pcmfreq(string arg)
        {
            reg.al = arg.Length > reg.bx ? (byte)arg[reg.bx++] : (byte)0;
            reg.al = (byte)xsmall((char)reg.al);

            if (reg.al < (byte)'A' || reg.al > (byte)'P') err_param();

            reg.ax = (ushort)(reg.al - 'A');

            play4.freq1 = freq86b[reg.ax];
            play4.freq3 = freqwss[reg.ax];
            play4.freq2 = freqtbl[reg.ax];
            pc98.OutportC4231_Freq2(play4.freq2);
        }

        //;	PCM周波数テーブル
        //;	A:48.00K, B:44.10K, C:37.80K, D:33.08K,
        //;	E:32.00K, F:27.42K, G:22.05K, H:18.90K,
        //;	I:16.54K, J:16.00K, K:11.03K, L: 9.60K,
        //;	M: 8.27K, N: 8.00K, O: 6.62K, P: 5.51K

        private byte[] freq86b = new byte[]{
        0b000,0b000,0b001,0b001,
        0b001,0b010,0b010,0b010,
        0b011,0b011,0b100,0b100,
        0b101,0b101,0b110,0b110
        };
        private byte[] freqwss = new byte[]{
        0b1100,0b1011,0b1001,0b1101,
        0b0110,0b0100,0b0111,0b0101,
        0b0010,0b0010,0b0011,0b1110,
        0b0010,0b0000,0b1111,0b0001
        };
        private ushort[] freqtbl = new ushort[]{
        7078,6503,5574,4877,
        4719,4043,3251,2787,
        2439,2359,1626,1416,
        1220,1180,976,813
        };

        //･･････････････････････････････････････
        //	拡張サウンド機能のチェック
        //	exit	CY = PCMが使えない
        //･･････････････････････････････････････

        private bool chk_sound()
        {
            reg.dx = 0xa460;
            reg.al = pc98.InportB(reg.dx);
            reg.al &= 0b1111_1101;
            reg.al |= 1;
            pc98.OutportB(reg.dx, reg.al);
            reg.al &= 0b1111_0000;
            if (reg.al == 0x10 // PC-98GS
                || reg.al == 0x20 // PC-9801-73(0188)
                || reg.al == 0x40)// PC-9801-86(0188)
            {
                reg.ax = 0x188;
                ex_sound_set();
            }
            else if (reg.al == 0x30//PC-9801-73(0288)
                || reg.al == 0x50)//PC-9801-86(0288)
            {
                reg.ax = 0x288;
                ex_sound_set();
            }
            else if (reg.al == 0x60//PC-9821Np
                || reg.al == 0x70//PC-9821X*
                || reg.al == 0x80)//PC-9821C*
            {
                ex_wss_exist();
            }
            else
            {
                if (!check_srn()) return false;
                ex_wss_exist();
            }
            return true;
        }

        private void ex_sound_set()
        {
            m_mode[3] |= 0x10;//86B-PCM仮有効
            if ((sys_flg & 4) != 0)
                return;
            setport1(reg.ax);
            reg.ax += 4;
            setport3(reg.ax);
        }

        private void ex_wss_exist()
        {
            m_mode[3] |= 0x01;//WSS-PCM仮有効
            if (play4.freq2 != 2439)
            {
                reg.ax = 0x188;
                ex_sound_set();
                return;
            }

            play4.freq2 = 2360;//周波数を設定していない場合だけ
            reg.ax = 0x188;
            ex_sound_set();
            return;
        }

        //･･････････････････････････････････
        //	SRN-F PCM音源許可
        //	exit	CY = 音源なし (熊:false:音源無し)
        //･･････････････････････････････････

        private bool check_srn()
        {
            reg.dx = 0x51e1;
            reg.cx = 8;

            bool fnd = false;
            for (; reg.cx > 0; reg.cx--)
            {
                reg.al = pc98.InportB(reg.dx);
                if (reg.al == 0xc2)
                {
                    fnd = true;
                    break;
                }
                reg.dx += 2;// ポート番地をサーチ(51E1,51E3,･･,51EF)
            }
            if(!fnd) return false;// キーワードC2がなかった

            //portchk1:
            reg.dx &= 0xf;
            reg.di = reg.dx;// di = C2のあった番地の下位4bit奇数
            reg.dx--;
            reg.si = reg.dx;// si = C2のあった番地の下位4bit偶数

            reg.dx = 0x57e0;
            reg.dx += reg.di;
            reg.al = pc98.InportB(reg.dx);// DX = 57e1～57ef(odd)
            reg.al &= 0xbf;// SRN-F 初期化
            pc98.OutportB(reg.dx, reg.al);

            reg.dx = 0x56e0;
            reg.dx += reg.di;
            reg.al = pc98.InportB(reg.dx);// DX = 56e1～56ef(odd)
            reg.al |= 0x51;
            pc98.OutportB(reg.dx, reg.al);

            reg.dx = 0x57e0;
            reg.dx += reg.di;
            reg.al = pc98.InportB(reg.dx);// DX = 57e1～57ef(odd)
            reg.al |= 0x40;// a460 ID = 71h
            pc98.OutportB(reg.dx, reg.al);

            reg.dx = 0x5be0;
            reg.dx += reg.di;
            reg.al = pc98.InportB(reg.dx);// DX = 5be1～5bef(odd)
            reg.al |= 0x06;
            pc98.OutportB(reg.dx, reg.al);

            reg.dx = 0x51e0;
            reg.dx += reg.si;
            reg.al = pc98.InportB(reg.dx);// DX = 51e0～51ee(even)
            reg.al &= 0xfc;// b0 = PCM許可?, b1 = FM音源許可
            pc98.OutportB(reg.dx, reg.al);

            return true;
        }

        //	DMAチャネルの指定
        private void set_dmach(string arg)
        {
            reg.al = arg.Length > reg.bx ? (byte)arg[reg.bx++] : (byte)0;

            if (reg.al < (byte)'0') err_param();
            reg.al -= (byte)'0';
            if (reg.al < 2 || reg.al > 3) err_param();
            reg.ah = reg.al;
            reg.dx = 0xf40;
            reg.al = pc98.InportB(reg.dx);
            reg.al &= 0xf8;// CS4231のDMAを設定
            reg.al |= reg.ah;
            pc98.OutportB(reg.dx, reg.al);
        }

        //	DMA割り込みの初期設定

        private void setup_int_dma()
        {
            reg.dx = 0x0f40;
            reg.al = pc98.InportB(reg.dx);
            reg.al &= 0b1100_0111;
            reg.al |= 0b0000_1000;// CS4231割り込みはINT0にする
            pc98.OutportB(reg.dx, reg.al);
            reg.al = pc98.InportB(reg.dx);
            reg.al &= 0b0011_1000;
            if ((reg.al - 0b0000_1000) != 0)
            {
                // INT0に設定された?
                cs4231_error();
                return;
            }

            reg.dx = 0x0f40;
            reg.al = pc98.InportB(reg.dx);
            reg.al &= 0b0000_0111;
            reg.dl = 0;
            if ((reg.al - 1) == 0)
            {
                dmachset1();
                return;
            }

            reg.dl = 1;
            if ((reg.al - 2) == 0)
            {
                dmachset1();
                return;
            }
            reg.dl = 3;
            if ((reg.al - 3) == 0)
            {
                dmachset1();
                return;
            }

            reg.dx = 0x0f40;// DMAのチャネルが未定義
            reg.al = pc98.InportB(reg.dx);
            reg.al &= 0b1111_1000;// 強制的にDMA#3を指定
            reg.al |= 0b0000_0011;
            pc98.OutportB(reg.dx, reg.al);
            reg.al = pc98.InportB(reg.dx);
            reg.al &= 0b0000_0111;
            if ((reg.al - 0b0000_0011) != 0)
            {
                dma_error();
                return;
            }
            reg.dl = 3;
            dmachset1();
            return;
        }

        private void dmachset1()
        {
            play4.dma_chan = reg.dl;
            reg.bh = 0;
            reg.bl = reg.dl;
            reg.bx <<= 2;
            reg.bx += 0;//ofs:dma_ch_data
            reg.ah = 0;
            reg.al = dma_ch_data[reg.bx + 1];
            play4.dma_adr = reg.ax;
            reg.al = dma_ch_data[reg.bx + 2];
            play4.dma_count = reg.ax;
            reg.al = dma_ch_data[reg.bx + 3];
            play4.dma_bank = reg.ax;
        }

        private void cs4231_error()
        {
            reg.dx = 0;//ofs:mes_e10
            //dmaexit:
            putasciz(mes_e10,0);
            m_mode[3] &= 0xee;// WSS-PCM禁止
        }

        private void dma_error()
        {
            reg.dx = 0;//ofs:mes_e11
            //dmaexit:
            putasciz(mes_e11, 0);
            m_mode[3] &= 0xee;// WSS-PCM禁止
        }

        //;･････････････････････････････
        //;	DSP部分の初期化
        //;･････････････････････････････

        private void init_86pcm()
        {
            reg.ax = 0x3e3e;
            play4.sign1 = reg.ax;// sub al,80h → ds: ds:
            play4.sign1_2 = reg.ax;// sub ah,80h → ds: ds: ds:
            play4.sign1_4 = reg.al;
            play4.sign2 = reg.ax;// sub al,80h → ds: ds:
            reg.ax = 0;
            play4.sign3_1 = reg.ax;// mov ax,8080h → mov ax,0
            play4.sign4_1 = reg.ax;// mov ax,8080h → mov ax,0
        }

        //;-----------------------------------------------------------
        //;	各パラメータによる常駐バッファメモリ容量の計算
        //;-----------------------------------------------------------

        private void para0()
        {
            //常駐範囲計算のための処理を省略
            //(dxに入る)

            //-------------------------------------
            //	バッファ番地指定後の処理
            //-------------------------------------

            ushort dxbk = reg.dx;

            if ((sys_flg & 2) == 0) ena_9821();// PC9821PCMのチェック
            // YM2608が存在するか?
            if ((sys_flg & 0x20) != 0) check_adpcm();// ADPCM存在チェック
            if (play4.check_86pcm())
            {
                if ((m_mode[3] & 0x80) != 0)
                {
                    //メインメモリPCM?
                    reg.dx = dxbk;
                    extseg = reg.dx;
                    reg.dx += extlen;// セグメント長を格納
                    dxbk = reg.dx;
                }
                else
                {
                    //ems_pcm1:
                    if (!play4.check_wsspcm())
                        init_86pcm();// 86-PCM用にPCMルーチンを変更
                    else
                        setup_int_dma();// DMAの初期化
                }
            }

            //skip_wss:
            chkfm();// FM音源ボードのチェック
            wait_port();// 無条件にウェイトを入れる
            autovct();// タイマA割り込みベクタの設定
            set_pcmtable();// PCM管理テーブルの格納
            reg.dx = dxbk;// DX,CX = 最終セグメント値
            if (!play4.check_86pcm())
            {
                fifoseg = reg.dx;// FIFOセグメント
                //reg.dx += FIFO_SIZE * 2 * MAXBUF / 16;//熊：不要と思われる
            }

            //bufset_end:

            check_fopen();
            load_pcm();// PCMデータのロード
            set_vct();// 割り込みベクタの設定
            xtone();// TONES.DTAを転送して常駐で終了
        }

        private void wait_port()
        {
            reg.ax = 0;
            play4.hadr11 = reg.al;
            reg.al = 0x90;
            play4.hadr7 = reg.al;
            play4.hadr8 = reg.al;
            play4.hadr9 = reg.al;
            play4.hadr10 = reg.al;
        }


        //････････････････････････････････
        //	ファイル存在チェック
        //････････････････････････････････

        private void check_fopen()
        {
            reg.dx = 0;//ofs:tone_path
            chksns(tone_path);
            if (reg.carry) patherr(tone_path);
            //else
            //{
            //chkf1:
            //熊:INT21h ah=3eh (close)
            //}


            // 熊:ssgPCM使う場合はサイズチェックする
            //chkf2:
            if ((m_mode[0] & 2) == 0) return;

            reg.dx = 0;//ofs:ssg1_path
            chksns(ssg1_path);
            if (reg.carry) patherr(ssg1_path);
            //else
            //{
            //chkf3:
            //熊:INT21h ah=3eh (close)
            //}

            //chkf4:
            reg.bx = reg.ax;//bx:Set fileHandle
            reg.cx = 0;//上位バイト
            reg.dx = 0;//下位バイト
            byte[] buf = new byte[1];
            if (File.Exists(ssg1_path)) buf = File.ReadAllBytes(ssg1_path);
            else Log.WriteLine(LogLevel.ERROR, "File not found.{0}", ssg1_path);

            reg.dx = (ushort)(buf.Length >> 16);
            if (reg.dx != 0 || buf.Length >= pcmlen)
            {
                //pbuffover:
                reg.dx = 0;//ofs:mes_e3	; PCMバッファがあふれた
                putasciz(mes_e3, 0);
            }

            //foskips2:
            //foskips1:
        }

        //;･･････････････････････････
        //;	拡張2203の指定
        //;･･････････････････････････
        private void ext_2203(string _)
        {
            play4.outdata4_=0xc3;
            cyon = 0xffb1;// inc cl → mov cl,0ffh
        }

        //;---------------------------------
        //;	YM2608自動識別の実行
        //;---------------------------------
        private void check_port()
        {
            int dx = 0x88;
            bool fnd = false;
            for (int cx = 4; cx >= 0; cx--)
            {
                if (fnd = get_port(dx)) break;// 接続されていた
                dx += 0x100;
            }
            if (!fnd) return;// ない場合は無視して終了

            int ax = dx;
            setport1(ax);
            ax += 4;
            setport3(ax);
            sys_flg |= 0x20;// YM2608が存在する!
        }

        private bool get_port(int dx)
        {
            byte al = 0xff;
            pc98.OutportB(dx, al);//熊 0x_88
            play4.check_busy(dx);
            dx += 2;
            al = pc98.InportB(dx);//熊 0x_8a
            if (al == 1) return true;// YM2608の識別子(01)か
            return false;
        }

        //----------------------------------
        //	YM3438自動識別の実行
        //----------------------------------
        private void check_extend()
        {
            int dx = 0x788;
            for (int cx = 8; cx >= 0; cx--)
            {
                //eport5:
                int f = get_3438(dx);
                if (f == 1)// 3438が接続されていた
                {
                    play4.port5 = (ushort)dx;
                    play4.port7 = (ushort)(dx + 4);
                    return;
                }

                if (f == 0)
                {
                    dx -= 0x100;
                    continue;
                }

                if (play4.port1 != dx) //	YM2608のポートか
                {
                    if (get_port(dx)) // YM2608が接続されていた
                    {
                        m_mode[3] |= 2;
                        play4.port5= (ushort)dx;
                        play4.port7 = (ushort)(dx + 4);
                        return;
                    }

                    ext_2203("");// 拡張2203使用
                    play4.port5 = (ushort)dx;
                    play4.port7 = (ushort)(dx + 4);
                    return;
                }

                //eport2:
                dx -= 0x100;
            }
            return;// ない場合は無視して終了
        }

        private int get_3438(int dx)
        {
            byte al = pc98.InportB(dx); // まずは接続されているか
            if (al == 0x00) return 0;

            al = 6;
            pc98.OutportB(dx, al);// SSGがあるかどうかチェックする
            wait_fm();
            dx += 2;
            al = 0x15;
            pc98.OutportB(dx, al);
            wait_fm();
            al = pc98.InportB(dx);
            if (al == 0x15) return 2; // 同じデータが読めるとYM2203
            return 1;//0:not 1:3438 2:2203/2608
        }

        private void wait_fm()
        {
            //なにもしない
        }

        //--------------------------------------
        //	YM2608/3438の状態チェック
        //--------------------------------------

        private void chkfm()
        {
            ushort axbk = reg.ax;
            ushort dxbk = reg.dx;

            reg.dx = (ushort)play4.port1;// YM2203入出力番地
            reg.al = pc98.InportB(reg.dx);
            bool cf = (reg.al & 0x80) != 0;
            reg.al <<= 1;
            if (!cf)
            {
                if ((sflag & 1) != 0)
                    putasciz(mess_f1, 0); // YM2203がBUSYのままだ.
                //dispu2:
                reg.al = 0xc3;
                play4.outdata1_ = reg.al;
                play4.outdata2_ = reg.al;
                play4.outdata3_ = reg.al;
                play4.outdata4_ = reg.al;
                reg.dx = dxbk;
                reg.ax = axbk;
                return;
            }

            //chk_sound1:
            string msg;
            msg = mes_s0;

            if (!get_port(reg.dx))// YM2608が接続されているか
            {
                // YM2203のみ
                msg += mes_s2;
                ym2203++;

                reg.al = 0xc3;
                play4.outdata2_ = reg.al;
                play4.outdata3_ = reg.al;
                play4.outdata4_ = reg.al;
                reg.dx = dxbk;
                reg.ax = axbk;

                Log.writeLine(LogLevel.INFO, msg);
                return;
            }

            //chk_sound2:
            // YM2608使用
            msg += mes_s3;
            ym2608++;

            reg.ax = 0x002d;
            play4.outdata1();// OPnプリスケーラの初期化
            reg.ax = 0x8129;// OPNAモードにする
            play4.outdata1();

            if ((m_mode[3] & 0x02) != 0)//代替YM2608か
            {
                reg.ax = 0x002d;
                play4.outdata3();// OPnプリスケーラの初期化
                reg.ax = 0x8129;// OPNAモードにする
                play4.outdata3();

                msg += mes_s9;
                ym2608++;
            }
            else
            {
                //chk_sound7:
                reg.dx = play4.port5;
                reg.al = pc98.InportB(reg.dx);
                cf = (reg.al & 0x80) != 0;
                reg.al <<= 1;
                if (cf)
                {
                    //chk_sound3:
                    reg.al = play4.outdata4_;
                    if (reg.al == 0xc3)
                    {
                        //chk_sound13:
                        msg += mes_s4; // 拡張YM2203を使用する

                        ext_2203("");
                        ym2203++;
                    }
                    else
                    {
                        reg.dx = (ushort)(play4.port7+1);
                        reg.al = pc98.InportB(reg.dx);
                        cf = (reg.al & 0x80) != 0;
                        reg.al <<= 1;
                        if (!cf)
                        {
                            //chk_sound4:
                            msg += mes_s5; // 拡張YM3438を使用する
                            ym3438++;
                            //chk_sound8:
                        }
                        else
                        {
                            //chk_sound13:
                            msg += mes_s4; // 拡張YM2203を使用する

                            ext_2203("");
                            ym2203++;
                        }
                    }
                }
                else
                {
                    reg.al = 0xc3;
                    play4.outdata3_ = reg.al;
                    play4.outdata4_ = reg.al;
                }
            }

            //chk_sound5:
            msg += mes_s8;
            if (play4.outdata2_ != 0xc3)
            {
                reg.al = m_mode[3];
                if ((reg.al & 0x10) == 0)
                {
                    //chk_sound9:
                    msg += ((reg.al & 4) == 0) ? mes_s8 : mes_s7;//mes_s7 : ADPCM
                }
                else
                {
                    msg += ((reg.al & 0x1) == 0) ? mes_s6 : mes_s12;//s6: 86B-PCM s12: WSS-PCM
                    msg += mes_s13;
                }
            }

            Log.writeLine(LogLevel.INFO, msg);
            //chk_sound14:
            reg.ax = axbk;
            reg.dx = dxbk;
        }

        //;-------------------------------
        //;	PCMデータの読み込み
        //;-------------------------------

        private void load_pcm()
        {
            //	pusha
            ushort axbk = reg.ax;
            ushort bxbk = reg.bx;
            ushort cxbk = reg.cx;
            ushort dxbk = reg.dx;
            ushort sibk = reg.si;

            if (pcm_flg == 0) return;
            if ((m_mode[3] & 0x80) != 0) return; // メインメモリPCM
            if ((m_mode[3] & 0x10) == 0)
                if ((m_mode[3] & 0x04) == 0) return;// ADPCMは有効か?
            //pcm1:
            if ((sys_flg & 0x10) != 0) return;// EMS残留?
            reg.al = m_mode[3];
            reg.dx = 0;//ofs:pcmd_path
            chksns(pcmd_path);// ファイルオープン
            if (reg.carry)// CY = ノットレディ
            {
                patherr(pcmd_path); // エラーメッセージの表示
                reg.ax = axbk;
                reg.dx = dxbk;
                return;
            }

            //set_pcmd1:
            reg.bx = reg.ax;
            string msg = mess_p1;
            reg.dx = 0;//ofs:mess_p1	; "ADPCM ..."
            bool ret = play4.check_86pcm();// 拡張86PCM?
            if (ret) msg = msg.Substring(2);// "PCM ..."
            //Log.writeLine(LogLevel.INFO, msg);

            pcm_init();// 書き込み準備

            byte[] buf = null;
            try
            {
                buf = File.ReadAllBytes(pcmd_path);
            }
            catch
            {
                Log.WriteLine(LogLevel.ERROR, "File not found.{0}", pcmd_path);
            }
            int bufPtr = 0;

            //pcm_main_loop:
            while (true)
            {
                dspcnt2++;
                dspcnt2 &= 3;
                reg.al = dspcnt2;
                if (dspcnt2 == 0)
                {
                    reg.dx = (ushort)dspcnt1;
                    //Log.writeLine(LogLevel.INFO, msg + dspdtop[reg.dx]);
                    reg.dx++;
                    if (reg.dx == dspdtop.Length)
                    {
                        msg += dspdtop[reg.dx - 1];
                        reg.dx = 0;
                    }
                    dspcnt1 = reg.dx;
                }
                //dspdskip1:
                reg.dx = 0;//ofs:cusbuff
                reg.si = reg.dx;// DS:SI = PCMバッファ
                reg.cx = (ushort)MAXCUS;

                int n = (buf.Length - bufPtr);
                n = n > reg.cx ? reg.cx : n;
                if (n == 0) break;
                Array.Copy(buf, bufPtr, cusbuff, 0, n);
                bufPtr += n;

                reg.cx = (ushort)n;// CX = 書き込むバイト数
                if (play4.check_86pcm())
                {
                    // 拡張86PCM?
                    bool cf = extpcm_write(cusbuff);
                    if (cf)
                    {
                        pcm_finish();
                        putasciz(mess_p4, 0);
                        goto pcm0;
                    }
                }
                else
                {
                    //pcm_write_loop:
                    do
                    {
                        reg.ax = 0x1810;
                        outdata2pcm();
                        reg.ax = 0x1010;
                        outdata2pcm();
                        reg.ah = cusbuff[reg.si];// AH = データ
                        reg.si++;
                        reg.al = 8;
                        outdata2pcm();

                        if (pcm_flg != 2)// 演奏中にPCMを書き込むモードか
                        {
                            //normal_write:
                            ushort cxbk2 = reg.cx;
                            reg.cx = 0x200;
                            //pcm_wait_loop:
                            do
                            {
                                reg.dx = (ushort)port31;
                                reg.al = pc98.InportB(reg.dx);// AL = ステータス
                                reg.cx--;
                                if (reg.cx == 0)
                                {
                                    // タイムアウト
                                    //pcm_timeout:
                                    reg.cx = cxbk2;
                                    reg.dx = 0;//ofs:mess_p2
                                    putasciz(mess_p2, 0);
                                    goto pcm0;
                                }
                            } while ((reg.al & 8) == 0);// BRDY フラグのチェック
                            reg.cx = cxbk2;
                            reg.cx--;
                            if (reg.cx > 0) continue;

                            //熊:ESC チェックしない
                            break;
                        }

                        ushort cxbk3 = reg.cx;
                        reg.dx = (ushort)port31; //wpr port31+1
                        play4.check_busy(reg.dx);
                        reg.cx = cxbk3;
                        reg.cx--;
                    } while (reg.cx > 0);
                }
                //spb_pcm_main:
                //熊:ESC チェックしない
            }

            //debug
            //File.WriteAllBytes("pcm", ems.GetEmsArray(0, 0x17));


            //pcm_write_end:
            Log.writeLine(LogLevel.INFO, msg + dspdtop[dspdtop.Length - 1]);
            pcm_finish();

            if (play4.check_86pcm())// 拡張86PCM ?
                goto pcm0;
            if (check_mp23())// ADPCM登録済みかチェック
                goto pcm0;

            reg.si = 0;//ofs:cusbuff	; SI = 識別子読みだしバッファ
            reg.ax = (ushort)(cusbuff[reg.si] + cusbuff[reg.si + 1] * 0x100);
            if ((byte)cusbuff[reg.si] == (byte)'M'
                && (byte)cusbuff[reg.si + 1] == (byte)'P'
                && (byte)cusbuff[reg.si + 2] == (byte)'2'
                && (byte)cusbuff[reg.si + 3] == (byte)'3'
                )
                m_mode[3] |= 4;// ADPCM有効

            pcm0:;
            reg.ax = axbk;
            reg.bx = bxbk;
            reg.cx = cxbk;
            reg.dx = dxbk;
            reg.si = sibk;
            return;
        }

        private void pcm_finish()
        {
            if (play4.check_86pcm())// 拡張86PCM?
            {
                remove_extpcm();// 拡張PCMのEMS切り離し
                return;
            }

            reg.ax = 0;
            play4.outdata2();// 終了シーケンス
            reg.ax = 0x8010;
            play4.outdata2();
            reg.ah = 0x1c;// Timer-A,B Enableにする
            play4.outdata2();
            reg.ah = 0x80;// PCM Flag Reset
            play4.outdata2();
        }

        private bool extpcm_write(byte[] siBuf)
        {
            ushort esbk = reg.es;
            ushort csbk = reg.cs;
            reg.es = csbk;//熊：入れ替え
            extpcm_write_main(siBuf);
            reg.es = esbk;//熊：本来
            reg.dx = 0;//ofs:mess_p4
            return reg.carry;//pcm_abort ; バッファ不足
        }

        //;･････････････････････････････････････
        //;	[ESC] 押下チェック
        //;	exit	CY = 0 : pressed
        //;･････････････････････････････････････

        //esccheck:
        //
        // 略
        //

        //･････････････････････････････････････････････
        //	PCM識別子"MP23"のチェック
        //	exit	[cusbuff] = 読みだし文字列
        //		CY = タイムアウト
        //･････････････････････････････････････････････

        private bool check_mp23()
        {
            cusbuff = pc98.ReadOpnaPCMMemory(port34, 0, 4);
            return true;//true:読み出せた
        }

        //;･･･････････････････････････････
        //;	PCM読みだし用初期化
        //;･･･････････････････････････････

        //pcm_readinit:
        //	mov	si,ofs:prinit
        //_pread2:
        //	lodsw
        //	or	ax,ax
        //	je	_pread1
        //	call	outdata2
        //	jmp	_pread2
        //_pread1:
        //	ret

        //prinit	dw	0100h,1010h,8010h,2000h,0001h,0002h,0003h,0204h,0005h
        //	dw	0ff0ch,0ffcdh,0

        //;-------------------------------------
        //;	YM2608読みだしルーチン
        //;	entry	AL = adrs
        //;	exit	AL = ADPCMデータ
        //;-------------------------------------

        //indata2:
        //	push	dx
        //	push	cx
        //port33:	mov	dx,18ch
        //	push	ax
        //	mov	cx,200h
        //wait4:
        //	in	al,dx
        //	dec	cx
        //	je	abort1		; タイムアウトチェック
        //	shl	al,1
        //	jb	wait4
        //abort1:
        //	pop	ax
        //	out	dx,al
        //	call	check_busy
        //	add	dx,2
        //	in	al,dx
        //	pop	cx
        //	pop	dx
        //	ret

        //････････････････････････････････
        //	ADPCMの有効チェック
        //････････････････････････････････

        private void check_adpcm()
        {
            ushort sibk = reg.si;
            ushort dxbk = reg.dx;
            ushort cxbk = reg.cx;
            ushort axbk = reg.ax;

            if (play4.check_86pcm()) goto adpcm_exit;

            if (!check_mp23())// ADPCM登録済みかチェック
                goto adpcm_exit;

            if (cusbuff[0] == 'M' && cusbuff[1] == 'P' && cusbuff[2] == '2' && cusbuff[3] == '3')
            {
                m_mode[3] |= 4;
                goto adpcm_exit;
            }

            //adpcm1:
            pcm_init();// 書き込み準備
            reg.cx = 4;
            //adpcm_write_lop:
            while (reg.cx > 0)
            {
                reg.ax = 0x1810;
                outdata2pcm();
                reg.ax = 0x1010;
                outdata2pcm();
                reg.ax = 0x5a08;// 5ahを書き込む
                outdata2pcm();
                adpcm_wait();
                reg.cx--;
            }

            pcm_finish();
            if (!check_mp23())// ADPCM登録済みかチェック
                goto adpcm_exit;
            if (cusbuff[0] != 0x5a) goto adpcm_exit;

            //adpcm_ready:
            m_mode[3] |= 4;// ADPCM有効

            adpcm_exit:
            reg.dx = (ushort)port31;
            play4.check_busy(reg.dx);

            reg.ax = axbk;
            reg.cx = cxbk;
            reg.dx = dxbk;
            reg.si = sibk;
        }

        private void adpcm_wait()
        {
            ushort cxbk = reg.cx;
            reg.cx = 0x200;
            //adpcm_wait_loop:
            while (true)
            {
                reg.dx = (ushort)port32;//自己書き換え　初期値:18ch
                reg.al = pc98.InportB(reg.dx); // AL = ステータス
                reg.cx--;
                if (reg.cx >= 0) break;// タイムアウト
                if ((reg.al & 0x8) == 0) break;// BRDY フラグのチェック
            }
            //adpcm_wait0:
            reg.cx = cxbk;
        }

        //---------------------------------
        //	PCM管理テーブルの格納
        //---------------------------------

        private void set_pcmtable()
        {
            ushort dxbk = reg.dx;
            ushort bxbk = reg.bx;

            if (pcm_flg == 0) return;//	je	set_pcm0

            if ((m_mode[3] & 0x80) != 0)
            {
                // メインメモリPCM
                return;//jne	set_pcm0
            }

            reg.dx = 0;//ofs:pcmt_path

            //ファイルオープン
            if (!File.Exists(pcmt_path))
            {
                reg.dx = 0;//ofs:pcmt_path
                patherr(pcmt_path);// エラーメッセージの表示
                reg.dx = dxbk;
                reg.bx = bxbk;
                return;
            }

            //set_pcm1:
            byte[] bin = File.ReadAllBytes(pcmt_path);
            cusbuff = new byte[4000];
            Array.Copy(bin, cusbuff, bin.Length < 4000 ? bin.Length : 4000);

            reg.si = 0;//ofs:cusbuff
            reg.di = 0;//ofs:pcmtable
            reg.cx = (ushort)MAXPCM;

            //set_pcm2:
            do
            {
                reg.carry = gettxt(cusbuff);
                if (reg.carry) break;
                gethex();// AL = 上位バイト
                ushort adr = (ushort)(reg.al << 8);

                reg.carry = gettxt(cusbuff);
                if (reg.carry) break;
                gethex();// AL = 下位バイト
                adr |= reg.al;

                // アドレスを1/2の値にする
                adr >>= 1;
                play4.pcmtable[reg.di] = (byte)adr;
                play4.pcmtable[reg.di + 1] = (byte)(adr >> 8);
                reg.di += 2;
                reg.cx--;
            } while (reg.cx > 0);

            ////DEBUG コード
            //ushort p = 8;
            //while (reg.cx > 0)
            //{
            //    for (int i = 0; i < 2; i++)
            //    {
            //        play4.pcmtable[reg.di] = play4.pcmtable[p];
            //        reg.di++;
            //        p++;
            //    }
            //    reg.cx--;
            //}

            //set_pcm3:
            while (reg.cx > 0)
            {
                // EOF以降の処理
                for (int i = 0; i < 2; i++)
                {
                    play4.pcmtable[reg.di] = play4.pcmtable[reg.di - 2];
                    reg.di++;
                }
                reg.cx--;
            }
            //set_pcm4:

            //set_pcm0:
            reg.bx = bxbk;
            reg.dx = dxbk;
        }

        private bool gettxt(byte[] siBuf)
        {
            while (true)
            {
                reg.ax = (ushort)(siBuf[reg.si] + siBuf[reg.si + 1] * 0x100);
                reg.si++;
                if (reg.al == 0x1a) return true;//熊:cf=1
                if (reg.ah == 0x1a) return true;//熊:cf=1

                // SPC,TAB,CR,LFはスキップ
                if (reg.al == (byte)' '
                    || reg.al == 9
                    || reg.al == 13
                    || reg.al == 10)
                    continue;

                if (reg.al != (byte)';')
                {
                    reg.si++;
                    return false;//熊:cf=0
                }

                //gettxt1:
                do
                {
                    reg.al = siBuf[reg.si++];
                    if (reg.al == 0x1a) return true;//熊:cf=1
                } while (reg.al != 13);// CR,LFまで無視する
            }
            //gettxt2:
            //return true;//熊:cf=1
        }

        private void gethex()
        {
            reg.al -= (byte)'0';
            if (reg.al - 10 > 0) reg.al -= 7;
            if (reg.al - 16 > 0) reg.al -= (byte)' ';
            byte temp = reg.al;
            reg.al = reg.ah;
            reg.ah = temp;
            reg.al -= (byte)'0';
            if (reg.al - 10 > 0) reg.al -= 7;
            if (reg.al - 16 > 0) reg.al -= (byte)' ';

            reg.ah <<= 4;
            reg.al |= reg.ah;
            return;
        }

        //--------------------------------------------------
        //	パラメータファイル用ドライブセンス & OPEN
        //	entry	DS:DX = パス名のoffset
        //	exit	CY = ノットレディ
        //		AX = オープンしたファイルハンドル
        //--------------------------------------------------

        private bool chksns(string path)
        {
            ushort dxbk = reg.dx;
            ushort bxbk = reg.bx;
            reg.bx = reg.dx;
            reg.dl = (byte)path[reg.bx];
            reg.carry = (reg.dl < (byte)'a');
            reg.dl -= (byte)'a';
            if (!reg.carry) reg.dl += (byte)' ';
            drv_sense();
            reg.bx = bxbk;
            reg.dx = dxbk;
            reg.ax = 0x3d00;
            if (!reg.carry) pc98.Int21_3d(path);

            return !reg.carry;
        }

        //---------------------------------------
        //	セパレータのスキップ (DS:BX)
        //---------------------------------------
        private bool skip_bl(string str)
        {
            if (reg.bx >= str.Length) return true;

            while (true)
            {
                if (reg.bx >= str.Length) return true;
                char al = str[reg.bx++];
                if (al == 0x09) continue;
                if (al == ' ') continue;
                if (al == 13) continue;
                reg.bx--;
                return false;
            }
        }

        //;===========================================
        //;	各割り込みベクタの設定ルーチン
        //;===========================================

        private void set_vct()
        {
            //熊:今のところベクタの設定は特に必要ないと思われる

            ushort esbk = reg.es;
            reg.ax = 0;
            reg.es = reg.ax;
            reg.ax = 0;//ofs:timer_entry
            reg.bx = tboff;// タイマAベクタのoffset
            // ベクタの内容をtimer_entryに書き換えつつ元々のアドレスをゲット
            //int14_off = reg.ax;
            reg.ax = reg.cs;
            reg.bx = tbseg;// タイマAベクタのsegment
            // ベクタの内容をtimer_entryのセグメント(cs)に書き換えつつ元々のセグメントをゲット
            //int14_seg = reg.ax;

            reg.ax = 0;//ofs:func_ent
            reg.bx = funcoff;// タイマAベクタのoffset
            //	xchg	ax,es:[bx]
            //int60_off = reg.ax;
            reg.ax = reg.cs;
            reg.bx = funcseg;// タイマAベクタのsegment
            //	xchg	ax,es:[bx]
            //int60_seg = reg.ax;

            if ((m_mode[0] & 2) != 0)
            {
                reg.ax = 0;//ofs:int08ent
                //	xchg	ax,es:int08_vct._off
                //int08_off = reg.ax;
                reg.ax = reg.cs;
                //	xchg	ax,es:int08_vct._seg
                //int08_seg = reg.ax;
            }
            //skip_set08:
            if ((m_mode[3] & 1) != 0)
            {
                reg.ax = 0;//ofs:int0bent
                //	xchg	ax,es:int0b_vct._off
                //int0b_off = reg.ax;
                reg.ax = reg.cs;
                //	xchg	ax,es:int0b_vct._seg
                //int0b_seg = reg.ax;
            }

            //skip_set0b:
            reg.es = esbk;
        }

        //;============================================
        //;	全割り込みベクタの書換えチェック
        //;	entry	DS = 常駐セグメント
        //;============================================

        // 略

        //;===================================
        //;	各割り込みベクタの解除
        //;===================================

        // 略

        private void xtone()
        {
            ushort dxbk = reg.dx;
            if ((m_mode[0] & 2) != 0)
            {
                // SSGPCM.DTAを転送
                reg.dx = 0;//ofs:ssg1_path
                chksns(ssg1_path);// ファイルオープン
                if (!reg.carry)
                {
                    reg.bx = reg.ax;// BX = file handle
                    reg.cx = pcmlen;// CX = read bytes
                    reg.ah = 0x3f;
                    reg.dx = 0;// 読みだしバッファ
                    ushort dsbk = reg.ds;
                    reg.ds = pcmseg;
                    byte[] buf=null;
                    try
                    {
                        buf = File.ReadAllBytes(ssg1_path);
                    }
                    catch
                    {
                        Log.WriteLine(LogLevel.ERROR, "File not found.{0}", ssg1_path);
                    }
                    pcmBuff = new byte[pcmlen];
                    Array.Copy(buf, 0, pcmBuff, 0, buf.Length > pcmlen ? pcmlen : buf.Length);
                    reg.ds = dsbk;
                    // file close
                }
            }

            //xnssg:
            reg.dx = 0;//ofs:tone_path
            chksns(tone_path);// ファイルオープン (CY=ノットレディ)
            if (!reg.carry)
            {
                reg.bx = reg.ax;// BX = file handle
                reg.cx = 6400;// CX = read bytes
                reg.ah = 0x3f;
                reg.dx = 0;// 読みだしバッファ
                ushort dsbk = reg.ds;
                reg.ds = tone;
                byte[] buf = null;
                if (toneBuffFromOutside != null)
                {
                    buf = toneBuffFromOutside;
                }
                else
                {
                    try
                    {
                        buf = File.ReadAllBytes(tone_path);
                    }
                    catch
                    {
                        Log.WriteLine(LogLevel.ERROR, "File not found.{0}", tone_path);
                    }
                }
                toneBuff = new byte[6400];
                Array.Copy(buf, 0, toneBuff, 0, buf.Length > 6400 ? 6400 : buf.Length);
                reg.ds = dsbk;
                // file close
            }

            //xntone:
            reg.dx = dxbk;

            // 常駐終了
        }

        //	public	pushems,popems,save_extpcm,setnew_extpcm,remove_extpcm
        //	public	add_playseg,program_dma_rec,add_recseg
        //	public	farjmp1,farjmp2
        public void pushems() { }
        public void popems() { }
        //add_playseg:
        //add_recseg:
        //program_dma_rec:
        //	ret
        //farjmp1	vct	<ofs:?,?>
        //farjmp2	vct	<ofs:?,?>

        //･････････････････････････････････････････
        //	拡張PCMのEMSマッピングと復活
        //	entry	BX = ページ番号(0～31)
        //･････････････････････････････････････････
        public void save_extpcm(ref ushort bx)
        {
            if ((m_mode[3] & 0x80) != 0)//メインメモリPCM?
                return;
            //ushort esbk = reg.es;
            //ushort dibk = reg.di;
            //ushort axbk = reg.ax;

            //reg.di = 0;//pemsbuf
            //reg.es = reg.cs;
            //reg.ax = 0x4e00;
            ////ems.GetPageMap(reg, pemsbuf); // EMSマップ保存
            //reg.bx = ems.GetPageMap(); // EMSマップ保存

            //reg.ax = axbk;
            //reg.di = dibk;
            //reg.es = esbk;

            bx= ems.cS4231EMS_GetPageMap(); // EMSマップ保存
        }

        //････････････････････････････････････････
        //	EMSのマッピング
        //	entry	BX = 論理ページ番号
        //････････････････････････････････････････

        private void setnew_extpcm()
        {
            reg.carry = false;
            if ((m_mode[3] & 0x80) != 0)//メインメモリPCM?
            {
                emsmain1();
                return;
            }
            ushort sibk = reg.si;
            ushort dxbk = reg.dx;
            ushort bxbk = reg.bx;
            ushort axbk = reg.ax;
            reg.dx = phandle;
            reg.ax = 0x4400;// EMSマッピング
            ems.cS4231EMS_Map(reg.al, ref reg.ah, reg.bx, reg.dx);
            reg.ax = axbk;
            reg.bx = bxbk;
            reg.dx = dxbk;
            reg.si = sibk;
        }

        private bool emsmain1()
        {
            ushort bxbk = reg.bx;
            reg.bx <<= 10;// 16KB/16
            if (reg.bx > extlen)// 範囲チェック
            {
                reg.carry = true;
                reg.bx = bxbk;
                return true;
            }
            reg.bx += extseg;
            segad2 = reg.bx;
            reg.carry = false;
            reg.bx = bxbk;
            return false;
        }

        //setnew_extpcm2:
        //	push	bx
        //	shl	bx,10		; 16KB/16
        //	cmp	bx,extlen	; 範囲チェック
        //	jnb	emsmain3
        //	add	bx,extseg
        //	mov	es,bx
        //	clc
        //	pop	bx
        //	ret
        //emsmain3:
        //	mov	es,extseg
        //emsmain4:
        //	stc
        //	pop	bx
        //	ret

        public void remove_extpcm()
        {
            if ((m_mode[3] & 0x80) != 0) return; // メインメモリPCM?
            //ushort dsbk = reg.ds;
            //ushort sibk = reg.si;
            //ushort axbk = reg.ax;

            //reg.ds = reg.cs;
            //reg.si = 0; //pemsbuf
            //reg.ax = 0x4e01;// EMSマップ復活

            //ems.SetPageMap(0, pemsbuf);

            //reg.ax = axbk;
            //reg.si = sibk;
            //reg.ds = dsbk;
        }

        //;---------------------------------------------------------------
        //;	ファンクション割り込み制御
        //;	entry	AH = ファンクション番号
        //;
        //;	AH = 0 : ステータスの獲得
        //;		exit	AL =  0 : サウンドボードがない
        //;			AL =  1 : 標準サウンドボード
        //;			AL =  2 : YM2608のみ
        //;			AL =  3 : YM2608+2203
        //;			AL =  4 : YM2608+2608/3438
        //;			AL =  7 : YM2608+ADPCM
        //;			AL =  8 : YM2608+ADPCM+2203
        //;			AL =  9 : YM2608+ADPCM+2608/3438
        //;			AL = 12 : YM2608+86BPCM
        //;			AL = 13 : YM2608+86BPCM+2203
        //;			AL = 14 : YM2608+86BPCM+2608/3438
        //;	AH = 1 : 演奏開始
        //;		entry	DX = 演奏バッファセグメント
        //;	AH = 2 : 演奏停止
        //;	AH = 3 : フェードアウト
        //;	AH = 4 : 演奏再開
        //;	AH = 6 : YM-2203レジスタ直接出力
        //;		entry	DL = アドレス
        //;			DH = データ
        //;	AH = 7 : YM-2608拡張部レジスタ直接出力
        //;		entry	DL = アドレス
        //;			DH = データ
        //;	AH = 8 : 位置指定演奏再開(すでにBGMが流れている状態)
        //;		entry	DL = 先頭からの@stopコマンドの個数
        //;			DH = チャネル番号(1-17)
        //;	AH = 9 : フェードアウトの監視
        //;		exit	AL = 0 : フェードアウト終了かAH=3未指定
        //;			AL = 1 : フェードアウト中
        //;	AH = 10 : 演奏ファイルの読み込み&演奏開始(-Oxxの指定要)
        //;		entry	DS:DX = パス名のアドレス
        //;		exit	AL = 0 : 読み込み成功
        //;			AL = 1 : 演奏バッファが指定されていない
        //;			AL = 2 : 読み込み失敗
        //;			AL = 3 : 演奏バッファ容量不足
        //;	AH = 11 : 演奏中のオフセット値獲得
        //;		entry	AL = チャネル番号(1-15)
        //;		exit	DX = 現在演奏中の演奏バッファの番地
        //;			AX = そのチャネルの演奏バッファの先頭
        //;	AH = 12 : ループ終了チェック
        //;		exit	AL = 0 : 1ループ終了していない
        //;			AL = 1 : 1ループ終了(@init,*,**実行)
        //;	AH = 13 : 歌詞情報の獲得
        //;		exit	ES:DX = 歌詞データ格納番地(NUL=終了)
        //;			AL = 色変わり桁位置(0～)
        //;---------------------------------------------------------------

        //LC	=	0		; 演奏バッファの音長カウンタ
        //AD	=	68		; 演奏データ番地
        //RA	=	170		; スタックオフセット

        public void function(byte ah, object obj)
        {
            //if (work.Status == 0)
            //{
            //    lock (work.SystemInterrupt)
            //    {
            //        ushort bx = (ushort)(ah * 2);
            //        //ofs:jmptbl ; 各ファンクションへの分岐
            //        jmptbl[bx / 2](obj);
            //    }
            //}
            //else
            {
                functionList.Add(new Tuple<byte,object>(ah,obj));
            }
        }

        public void functionF(Tuple<byte, object> tp)
        {
            //ofs:jmptbl ; 各ファンクションへの分岐
            jmptbl[tp.Item1](tp.Item2);
        }


        private void InitJmpTbl()
        {
            jmptbl = new Action<object>[]
            {
                play0,play1,
                play4.music_stop,null, //,ofs:fade_out
                play4.music_again,null,null,null,//,play5,play6,play7
                null,null,play10//,play11,play12
                //	dw	play13
            };
        }

        //	ステータスのチェック
        private void play0(object _)
        {
            byte al = 0;

            if (play4.outdata1_ == 0xc3) al = 0;// YM2203?
            else if (play4.outdata2_ == 0xc3) al = 1;// YM2608?
            else if (play4.outdata3_ == 0xc3) al = 2;// YM2608+YM2203?
            else if (play4.outdata4_ == 0xc3) al = 3;// YM2608+YM3438/2608?
            else al = 4;

            // 拡張PCMが有効か
            if (play4.check_86pcm()) al += 10;
            else if ((m_mode[3] & 4) != 0) al += 5;

            reg.ax = al;
        }

        //	演奏開始

        private void play1(object obj)
        {
            objBuf[0] = (MmlDatum[])obj;
            if (bufleno[0] == 0)
            {
                _object[0] = reg.dx;
            }
            play4.music_start();// 演奏の開始
            work.Status = 1;
        }

        //;	レジスタ直接出力

        //play6:
        //	push	ax
        //	mov	ax,dx
        //	call	outdata1	; レジスタ直接出力(YM2203)
        //	pop	ax
        //play5:
        //	ret

        //play7:
        //	push	ax
        //	mov	ax,dx
        //	call	outdata2	; レジスタ直接出力(YM2608拡張部)
        //	pop	ax
        //	ret

        //;	効果音の発生

        //play8:
        //	pusha
        //	push	es
        //	pushf
        //	cli
        //	mov	es,object	; ES = 演奏バッファのセグメント
        //	xor	bx,bx
        //	mov	bl,dh
        //	dec	bx
        //	push	bx
        //	add	bx,bx		; BX = 0,2,4,･･･,1ch (ヘッダの番地)
        //	push	bx
        //	mov	bx,es:[bx]	; BX = DHのチャネルの演奏データの先頭
        //finds2:
        //	or	dl,dl		; @stopコマンドの個数チェック
        //	je	startp1
        //finds1:
        //	inc	bx
        //	mov	al,es:[bx-1]
        //	cmp	al,0fch		; @stopコマンドを見つけたか
        //	je	finds3
        //	cmp	al,3fh		; 0-3Fhは音符(2bytes)
        //	jbe	finds4
        //	cmp	al,0dbh		; 歌詞コマンド
        //	jne	finds5
        //	mov	al,es:[bx+2]	; AL = 文字列長
        //	add	al,3
        //	jmp	finds6
        //finds5:
        //	mov	si,ofs:skipbyte-1
        //	cbw
        //	sub	si,ax		; 各制御コードに関するスキップ数を獲得
        //	lodsb
        //finds6:
        //	cbw
        //	add	bx,ax
        //	jmp	finds1
        //finds4:
        //	inc	bx
        //	jmp	finds1
        //finds3:
        //	dec	dl
        //	jmp	finds2
        //startp1:
        //	mov	dx,bx		; DX = 次に演奏開始する番地
        //	pop	bx
        //	add	bx,ofs:data_buff
        //	mov	bpr [bx+LC],1	; 音長カウンタを初期化
        //	mov	[bx+AD],dx	; 演奏番地を格納
        //	mov	bpr [bx+RA],0
        //	pop	cx		; CX = チャネル番号(0-16)
        //	mov	ch,cl
        //	call	calcbit
        //	or	song_flg1,ax	; AH = 8 フラグを立てる
        //	or	song_flg2,dl
        //	not	ax
        //	not	dl		; DLAX = ANDするチャネルスキップデータ
        //	and	skip_data1,ax
        //	and	skip_data2,dl
        //	popf
        //	pop	es
        //	popa
        //	ret

        //;	フェードアウトの監視

        //play9:
        //	sti
        //	xor	ax,ax
        //	test	mapflag,2	; 演奏中?
        //	je	noplay9
        //	mov	ax,fade_count
        //	or	ax,ax
        //	exen	ne,< mov al,1 >
        //noplay9:
        //	ret

        //;	演奏ファイルの読みこみと演奏開始

        private void play10(object o = null)
        {
            ushort bxbk = reg.bx;
            ushort cxbk = reg.cx;
            ushort dxbk = reg.dx;
            MmlDatum[] oo = null;
            if (o != null && o is MmlDatum[])
            {
                oo = (MmlDatum[])o;
            }
            else
            {
                reg.al = 2;// ファイル読み込み失敗
                return;
            }
            if (bufleno[0] == 0)
            {
                reg.al = 1;// 演奏バッファ未定義
                return;
            }
            play4.music_stop(); // 演奏停止

            //(略)

            objBuf[0] = oo;
            obj_len = (ushort)oo.Length;//	mov	obj_len,ax

            pcm_check();// ユーザPCMチェック
            if (reg.carry)
            {
                reg.al = 4;// ユーザPCMなし
                reg.dx = dxbk;
                reg.cx = cxbk;
                reg.bx = bxbk;
                return;
            }

            play4.music_start();// 演奏開始

            reg.al = 0;
            //	jmp	playok

            //labort3:
            //	mov	al,3		; 演奏バッファ不足
            //	jmp	playok

            //playok:
            reg.dx = dxbk;
            reg.cx = cxbk;
            reg.bx = bxbk;
            return;
        }

        //;	演奏データオフセットの獲得

        //play11:
        //	push	bx
        //	push	ds
        //	mov	bx,ofs:data_buff+AD
        //	xor	ah,ah
        //	dec	ax
        //	add	ax,ax
        //	push	ax
        //	add	bx,ax		; BX = 指定チャネルの演奏番地バッファ
        //	mov	dx,[bx]		; DX = 演奏中のオフセット値
        //	pop	bx
        //	mov	ds,object
        //	mov	ax,[bx]		; AX = 指定チャネルの先頭演奏番地
        //	pop	ds
        //	pop	bx
        //	ret

        //;	ループ状態の読み取り

        //play12:
        //	mov	al,init_cnt
        //	ret

        //;	歌詞関係の情報獲得

        //play13:
        //	mov	dx,ofs:comdata
        //	mov	al,comlength
        //	push	cs
        //	pop	es
        //	ret

        //-----------------------------------
        //	PCMファイルの読みだし
        //	exit	CY = エラー
        //		DL = エラーコード
        //-----------------------------------

        private void pcm_check()
        {
            reg.push(reg.ax);
            reg.push(reg.bx);
            reg.push(reg.cx);
            reg.push(reg.si);
            reg.push(reg.di);
            reg.push(reg.ds);
            reg.push(reg.es);

            try
            {
                reg.ds = reg.cs;

                usrpcm = 0;           // ユーザPCM有無フラグをクリア
                reg.es = 0;//object
                reg.di = 0x24;// ES:DI = UsrPCMのヘッダ番地
                reg.di = (ushort)(objBuf[0][reg.di].dat + objBuf[0][reg.di + 1].dat * 0x100);// ES:DI = 演奏データ番地
                reg.al = (byte)objBuf[0][reg.di].dat;// AL = チェックコード(88h)
                if (reg.al != 0x88)// @plコマンドが存在するか
                {
                    reg.ax = 0;
                    pcmbyte = reg.ax;// 使用しない場合はSSGPCMバイト数をクリア
                    reg.ax = (ushort)(play4.pcmtable[50 * 2] + play4.pcmtable[50 * 2 + 1] * 0x100);
                    usrbyte = reg.ax;// UserPCMバイト数
                                     //pcmload1:
                    reg.carry = false;
                    pcmload15();
                    return;
                }

                //pcmload22:
                usrpcm |= 1;// ユーザPCM(ADPCM)あり
                reg.di++;
                reg.bx = 0;//ofs:ssgtable
                byte[] bxbuf = play4.ssgtable;
                reg.si = 0;// pcmfile:SI = PCMファイル名保存バッファ
                reg.cx = (ushort)(20 + MAXPCM - 50);// ユーザPCMは20+40音色
                reg.ah = 0;// AH = 今回探している音色番号

                //pcmload5:
                do
                {
                    usrpcmn = reg.ah;// 音色番号を保存

                    reg.push(reg.ax);
                    reg.push(reg.cx);
                    reg.push(reg.si);
                    reg.push(reg.di);

                    bool skipPcmload2 = false;
                    int continuePcmload5 = 0;
                    //pcmload4:
                    while (true)
                    {
                        reg.al = (byte)objBuf[0][reg.di].dat;// AL = ユーザ指定音色番号

                        //pcmload18:
                        if ((reg.al >= 20 && reg.al < 50) || reg.al > MAXPCM - 1) break;// 規定外のコードか

                        //pcmload17:
                        if (reg.al == reg.ah)// 該当音色番号のデータか
                        {
                            if (!pcmload3(ref bxbuf))
                            {
                                skipPcmload2 = true;
                                break;
                            }

                            continuePcmload5 = pcmload6(ref bxbuf);
                            if (continuePcmload5!=0) break;
                            reg.carry = false;
                            return;
                        }
                        reg.di += 14;// 次のブロックをチェックする
                    }

                    if (continuePcmload5 != 2)
                    {
                        if (continuePcmload5 != 0) continue;

                        if (!skipPcmload2)
                        {
                            //pcmload2:
                            reg.cx = 13;
                            //pcmload21:
                            do
                            {
                                pcmfileBuf[reg.si] = 0;// 該当番号無指定の場合はファイル名もクリアする
                                reg.si++;
                                reg.cx--;
                            } while (reg.cx > 0);

                            reg.ax = (ushort)(bxbuf[reg.bx] + bxbuf[reg.bx + 1] * 0x100);// AX = PCM音色データの開始番地
                            bxbuf[reg.bx + 2] = reg.al;// データが存在しないので終了番地を書き込む
                            bxbuf[reg.bx + 3] = reg.ah;
                        }
                    }
                    //pcmload7:
                    reg.bx += 2;// 次のPCM管理テーブルへ

                    reg.di = reg.pop();
                    reg.si = reg.pop();
                    reg.cx = reg.pop();
                    reg.ax = reg.pop();

                    reg.si += 13;// 次のPCM保存バッファへ
                    reg.ah++;// @0～19,50～99まで検索
                    if (reg.ah == 20)
                    {
                        reg.ah = 50;// SSGPCMの分が終了したのでADPCMへ移動する
                        reg.bx = 50 * 2;//ofs:pcmtable+50*2
                        bxbuf = play4.pcmtable;
                    }

                    //nextnum:
                    reg.cx--;
                } while (reg.cx > 0);

                reg.carry = false;
            }
            catch
            {

            }

            pcmload15();
            return;
        }

        private void pcmload15()
        {
            reg.es = reg.pop();
            reg.ds = reg.pop();
            reg.di = reg.pop();
            reg.si = reg.pop();
            reg.cx = reg.pop();
            reg.bx = reg.pop();
            reg.ax = reg.pop();
        }

        private bool pcmload3(ref byte[] bxbuf)
        {
            if (reg.al < 20) usrpcm |= 2;// ユーザPCM(SSGPCM)あり

            reg.di++;// ES:DI = 読みだしファイル名格納番地

            //pcmfile ; DS = PCMファイル名セグメント
            reg.cx = 11;
            // 保存バッファのファイル名と比較
            reg.zero = true;
            for (int i = 0; i < 11; i++)
            {
                if (objBuf[0][reg.di + i].dat == pcmfileBuf[reg.si + i]) continue;
                reg.zero = false;
                break;
            }
            if (!reg.zero) return true;

            //pcmfile
            reg.ax = (ushort)(pcmfileBuf[reg.si + 11] + pcmfileBuf[reg.si + 12] * 0x100);// AX = PCM格納開始番地
            reg.ax ^= (ushort)(objBuf[0][reg.di + 11].dat + objBuf[0][reg.di + 12].dat * 0x100);// 音量値とXORして戻す

            if (reg.ax != (ushort)(bxbuf[reg.bx] + bxbuf[reg.bx + 1] * 0x100))// 格納番地も一致しているか
            {
                // すでにPCMが読み込まれている場合は次へ進む
                return true;
            }
            reg.di = (ushort)(bxbuf[reg.bx + 2] + bxbuf[reg.bx + 3] * 0x100);// DI = 格納終了番地

            //pcmload23:
            if (bxbuf != play4.ssgtable)
            {
                pcmbyte = reg.di;
            }

            return false;
        }

        private int pcmload6(ref byte[] bxbuf)
        {
            ushort sibk = reg.si;
            ushort dsbk = reg.ds;
            reg.cx = 11;
            reg.ds = pcmfile;
            //pcmload8:
            do
            {
                reg.al = (byte)objBuf[0][reg.di].dat;// ファイル名を保存バッファに転送する
                pcmfileBuf[reg.si] = reg.al;
                reg.si++;
                reg.di++;
                reg.cx--;
            } while (reg.cx > 0);
            reg.ds = dsbk;
            reg.si = sibk;
            reg.ax = (ushort)(objBuf[0][reg.di].dat + objBuf[0][reg.di + 1].dat * 0x100);// AX = 音量データ
            voldata = reg.ax;
            reg.ax = (ushort)(bxbuf[reg.bx]+ bxbuf[reg.bx+1]*0x100);// AX = PCM格納番地
            //･･･････････････････････････････････････････
            //	PCMデータの転送
            //	entry	AX = 開始番地
            //		SI = ファイル名格納番地
            //･･･････････････････････････････････････････

            reg.pushA();

            reg.push(reg.es);
            pcmtbl[11 / 2] = (ushort)((pcmtbl[11 / 2] & 0x00ff) + reg.al * 0x200);// 書き込み先頭番地を格納//熊:2倍値にする(恐らくNAXのバグ?)
            pcmtbl[13 / 2] = (ushort)((pcmtbl[13 / 2] & 0x00ff) + reg.ah * 0x200);
            reg.push(reg.ax);

            reg.es = reg.cs;
            reg.bx = 0;//ofs:pcm_path1 ; BX = ユーザPCM読みだし用パス(FDD)
            pcm_path1 = "*.*" + new string((char)0, 61);
            set_usrpcmfile(ref pcm_path1);

            //熊:ドライブの準備チェックはskip

            // ファイルオープン
            reg.dx = reg.bx;
            reg.carry = false;
            if (!File.Exists(pcm_path1))
            {
                Log.WriteLine(LogLevel.ERROR, "File not found.{0}", pcm_path1);
                reg.carry = true;
            }
            else
            {
                try
                {
                    filebuf = File.ReadAllBytes(pcm_path1);
                    Log.WriteLine(LogLevel.INFO, "[{0}] File found.", pcm_path1);
                }
                catch
                {
                    Log.WriteLine(LogLevel.ERROR, "File not found.{0}", pcm_path1);
                    reg.carry = true;
                }
            }
            if (reg.carry)
            {
                if (!fdd_notready())
                {
                    pcmload9s();
                    return 0;
                }
            }
            else
            {
                reg.di = reg.pop();
                reg.es = reg.pop();
            }

            return subdir1(ref bxbuf);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true:要subdir1呼び出し false:要pcmload9s呼び出し</returns>
        private bool fdd_notready()
        {
            reg.bx = 0;//ofs:pcm_path ; BX = ユーザPCM読みだし用パス
            pcm_path = "*.*" + new string((char)0, 61);
            set_usrpcmfile(ref pcm_path);
            reg.di = reg.pop();// DI = PCM書き込み先頭番地
            reg.es = reg.pop();

            // ファイルオープン
            reg.carry = false;
            if (!File.Exists(pcm_path)) reg.carry = true;
            else
            {
                try
                {
                    filebuf = File.ReadAllBytes(pcm_path);
                    Log.WriteLine(LogLevel.INFO, "[{0}] File found.", pcm_path);
                }
                catch
                {
                    Log.WriteLine(LogLevel.ERROR, "File not found.{0}", pcm_path);
                    reg.carry = true;
                }
            }
            if (!reg.carry)
            {
                return true;
            }

            //	サブディレクトリの検索
            reg.push(reg.es);
            reg.push(reg.di);
            reg.push(reg.si);
            reg.push(reg.bx);
            reg.es = reg.cs;

            string fn = Path.GetFileName(pcm_path);
            pcm_path = Path.Combine(Path.GetDirectoryName(pcm_path), ".");

            string testPath = searchFile(pcm_path, fn);
            // ファイルオープン
            reg.carry = false;
            if (testPath == null) reg.carry = true;
            else
            {
                try
                {
                    filebuf = File.ReadAllBytes(testPath);
                    Log.WriteLine(LogLevel.INFO, "[{0}] File found.", testPath);
                }
                catch
                {
                    Log.WriteLine(LogLevel.ERROR, "File not found.{0}", testPath);
                    reg.carry = true;
                }
            }
            if (reg.carry)
            {
                reg.bx = reg.pop();
                reg.si = reg.pop();
                reg.di = reg.pop();
                reg.es = reg.pop();
                //pcmload9n:
                reg.dl = 25;
                // 見つからなかった
                return false;
            }

            //subdir4:
            //subdir3:
            //subdir6:
            //subdir5:
            //subdir2:
            //subdir7:
            reg.bx = reg.pop();
            reg.si = reg.pop();
            reg.di = reg.pop();
            reg.es = reg.pop();

            //mov bx, path1
            //mov wpr[bx + 1],"*"; UsrPCMパスをもとのディレクトリに戻す

            return true;
        }

        private string searchFile(string path,string fn)
        {
            string testPath = Path.Combine(path, fn);
            if (File.Exists(testPath)) return testPath;

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                string ans = searchFile(dir, fn);
                if (ans != null) return ans;
            }

            return null;
        }

        //;	ファイルの演奏バッファへの読みだし

        // 1:成功(読みこみ続行) 0:失敗(読み込み中断) 2:pcmload7へ
        private int subdir1(ref byte[] bxbuf)
        {
            reg.bx = reg.ax;
            if (usrpcmn > 20)
            {
                pcm_init();// PCM書き込み準備
            }

            deltax = 127;// ワークの初期化
            reg.ax = 0;
            xdata[0] = reg.al;
            xdata[1] = reg.ah;
            xdata[2] = reg.al;
            xdata[3] = reg.ah;

            //pcmload14:
            int fileptr = 0;
            do
            {
                reg.cx = bufleno[0];//	mov	cx,bufleno	; CX = 演奏バッファ容量
                reg.dx = 0;// obj_len;// DX = 余っている演奏バッファの先頭 //熊:常にバッファの先頭は0
                if ((reg.cx < 0x8000) || (reg.dx >= 0x6000))
                {
                    //adrslim1:
                    reg.si = reg.dx;// SI,DX = 余っているバッファの先頭
                    reg.zero = (reg.cx == reg.dx);
                    reg.cx -= reg.dx;// CX = 余っているバッファ容量
                    if (reg.zero)
                    {
                        pcmload16();
                        return 0;
                    }
                }
                else
                {
                    reg.dx = 0x0;// 0x6000; //熊:常にバッファの先頭は0
                    reg.si = reg.dx;
                    reg.cx = 0x1000;
                }

                //adrslim2:
                ushort dsbk = reg.ds;
                // DS = 演奏バッファセグメント
                reg.ds = reg.es;

                // ファイル読みだし(INT21H ah=3f)
                cusbuff = new byte[reg.cx];
                reg.ax = (ushort)Math.Min((int)reg.cx, (int)(filebuf.Length - fileptr));
                Array.Copy(filebuf, fileptr, cusbuff, 0, reg.ax);
                fileptr += reg.ax;

                reg.ds = dsbk;
                if (reg.ax == 0)
                {
                    // 読みだしデータがない場合は終了
                    pcmload13(ref bxbuf);
                    return 2;
                }
                reg.cx = (ushort)reg.ax;// CX = 書き込むバイト数
                if (usrpcmn > 20)
                {
                    int n = load_userpcm();//ADPCM/86PCMへの転送へ
                    if (n == 0) return 0;
                    else if (n == 14) continue;
                    break;
                }
                reg.dl = 32;
                if (pcmBuff == null) pcmBuff = new byte[pcmlen];
                xferpcm(cusbuff, pcmBuff);// PCMへの変換(for SSGPCM)
                if (reg.carry)
                {
                    pcmload9();// エラー
                    return 0;
                }
            } while (!reg.carry);

            return 0;
        }

        //･･････････････････････････････････････････････
        //	環境変数のパス名にファイル名を追加
        //	entry	BX = パス名の番地
        //		pcmfile:SI = ファイル名の番地
        //･･････････････････････････････････････････････

        private void set_usrpcmfile(ref string bxpath)
        {
            ushort bxbk = reg.bx;
            ushort sibk = reg.si;
            reg.dx = reg.bx;
            getpath_main(bxpath);// パス名の解析
            reg.di = 0;// path1 DI = ファイル名部分の先頭番地
            //reg.di++;
            reg.cx = 8;
            ushort dsbk = reg.ds;
            reg.ds = pcmfile;
            // ファイル名を転送
            byte[] buf = new byte[255];
            do
            {
                buf[reg.di] = pcmfileBuf[reg.si];
                reg.di++;
                reg.si++;
                reg.cx--;
            } while (reg.cx > 0);

            reg.al = (byte)'.';
            buf[reg.di] = reg.al;
            reg.di++;

            reg.cx = 3;
            // 拡張子を転送
            do
            {
                buf[reg.di] = pcmfileBuf[reg.si];
                reg.di++;
                reg.si++;
                reg.cx--;
            } while (reg.cx > 0);

            // NULを追加
            reg.al = 0;
            buf[reg.di] = reg.al;
            reg.di++;

            string text = myEnc.GetStringFromSjisArray(buf);
            bxpath = (bxpath.Substring(0, path1+(path1==0?0:1)) + text).Substring(0, reg.di + path1 - (path1 == 0 ? 1 : 0));
            bxpath = bxpath.Replace(" ", "");
            bxpath = bxpath.Replace("\0", "");
            reg.ds = dsbk;
            reg.si = sibk;
            reg.bx = bxbk;
            return;
        }

        //;-----------------------------------------------
        //;	ADPCM･16KHz→PCM8･8KHzの変換
        //;	entry	ES:SI = ADPCMデータの番地
        //;		pcmseg:DI = PCMデータ格納番地
        //;		CX = ADPCMバイト数
        //;	exit	CY = エラー
        //;-----------------------------------------------

        private void xferpcm(byte[] siBuf, byte[] diBuf)
        {
            reg.ax = 0;
            if (reg.ax == pcmlen)
            {
                // SSGPCMは使用できるか
                reg.carry = true;
                return;
            }
            reg.ax = reg.di;
            uint ans = (uint)(reg.ax + reg.cx);
            reg.ax = (ushort)ans;
            if (ans > 0xffff)
            {
                reg.carry = true;
                return;
            }
            //intm4:
            if (intm4 != 0)
            {
                // 16KHzの場合はデータが倍
                ans = (uint)(ans + reg.cx);
                reg.ax = (ushort)ans;
                if (ans > 0xffff)
                {
                    reg.carry = true;
                    return;
                }
            }
            //skip8khz:
            if (reg.ax >= pcmlen)
            {
                // バッファ容量もチェック
                reg.carry = true;
                return;
            }

            reg.push(reg.dx);
            reg.push(reg.cx);
            reg.push(reg.bx);
            reg.push(reg.ax);
            //_calc3:
            do
            {
                reg.push(reg.cx);
                //Log.writeLine(LogLevel.ERROR, string.Format("si:{0:x08} di:{1:x08}", reg.si, reg.di));
                siBuf[reg.si] = reg.ror(siBuf[reg.si], 4);
                calc_nextxy(siBuf, diBuf);// 16ビットPCMに変換して格納
                                          //intm3
                if (intm3 == 0)
                {
                    reg.di--;// 8KHz分周するのでカット
                }
                siBuf[reg.si] = reg.ror(siBuf[reg.si], 4);
                calc_nextxy(siBuf, diBuf);
                reg.si++;
                reg.cx = reg.pop();
                reg.cx--;
            } while (reg.cx > 0);

            reg.ax = reg.pop();
            reg.bx = reg.pop();
            reg.cx = reg.pop();
            reg.dx = reg.pop();

            reg.carry = false;
            return;
            //errret:
            //	stc
            //	ret
        }

        //････････････････････････････････････････
        //	ADPCMメモリへのデータ転送
        //	entry	ES:SI = データ格納番地
        //		CX,AX = データバイト数
        //		DI = PCM書き込み番地
        //････････････････････････････････････････
        // 0:失敗
        //14:読み込み続行させる(subdir1が)
        private int load_userpcm()
        {
            reg.ax >>= 3;// ADPCM番地→管理番地に変換(512KB→64KB)
            reg.carry = (reg.di + reg.ax) > 0xffff;
            reg.di += reg.ax;// DI = 書き込み終わった時のPCM番地
            usrbyte = reg.di;// 使用バイト数
            reg.dl = 27;

            if (reg.carry // 範囲チェック
                || (
                    ((m_mode[2] & 0x80) == 0) // PCM 1MB?
                    && (reg.di > 0x8000 ) // 512KB用範囲チェック
                )
            )
            {
                pcmload9();
                return 0;
            }

            //pcm_1mb:
            if (play4.check_86pcm())// 拡張86PCM?
            {
                return extpcm_write1();
            }

            return pcmload10(); 
        }

        private int pcmload10()
        {
            do
            {
                //	cli
                reg.ax = 0x1810;
                outdata2pcm();
                reg.ax = 0x1010;
                outdata2pcm();
                reg.ah = cusbuff[reg.si];// AH = データ
                reg.si++;
                reg.al = 8;
                outdata2pcm();
                //	sti
                reg.push(reg.cx);
                reg.cx = 0x200;
                pcmload11:
                reg.dx = play4.port37;
                reg.al = pc98.InportB(reg.dx);// AL = ステータス
                reg.cx--;
                if (reg.cx != 0)
                {
                    reg.test(reg.al, 8);// BRDY フラグのチェック
                    if (reg.zero)
                    {
                        goto pcmload11;
                    }
                    // タイムアウト
                }
                //pcmload12:
                reg.cx = reg.pop();
                reg.cx--;
            } while (reg.cx != 0);

            return 14;//	jmp	pcmload14
        }
        private void pcmload13(ref byte[] bxbuf)
        {
            reg.ah = 0x3e;
            // ファイルクローズは不要
            reg.ax = 0;
            play4.outdata2();// 終了シーケンス
            reg.ax = 0x8010;
            play4.outdata2();
            if (play4.check_86pcm())// 拡張86PCM?
            {
                remove_extpcm();// 拡張PCMのEMS切り離し
            }
            extmapflg &= 0xfe;// マップを戻した
            reg.ax = reg.pop();
            reg.push(reg.di);
            reg.popA();// DI = PCM書き込み終了番地
            //reg.pushA();//熊:独自コード!!
            reg.ax = (ushort)(bxbuf[reg.bx] + bxbuf[reg.bx + 1] * 0x100);// AX = 開始番地を保存
            bxbuf[reg.bx + 2] = (byte)reg.di;
            bxbuf[reg.bx + 3] = (byte)(reg.di >> 8);// 終了番地を格納
            reg.ax ^= voldata;
            reg.push(reg.ds);
            reg.ds = pcmfile;
            pcmfileBuf[reg.si + 11] = reg.al;
            pcmfileBuf[reg.si + 12] = reg.ah;// 音量値とXORしてから保存する
            reg.ds = reg.pop();
            //pcmload23
            if (bxbuf == play4.ssgtable)
            {
                pcmbyte = reg.di;
            }

            //jmp pcmload7();
        }

        private void pcmload16()
        {
            reg.dl = 26;
            pcmload9();
        }

        private void pcmload9()
        {
            //一括で読みこんでしまう為ファイルクローズは不要
            pcmload9s();
        }

        private void pcmload9s()
        {
            if ((extmapflg & 1) != 0)
            {
                remove_extpcm();// 拡張PCMのEMS切り離し
            }
            extmapflg &= 0xfe;// マップを戻した

            //PCM定義でエラー(popa,pop di,si,cx,ax)
            //popA分
            reg.pop();
            reg.pop();
            reg.pop();
            reg.pop();
            reg.pop();
            reg.pop();
            reg.pop();
            reg.pop();//di
            reg.pop();//si
            reg.pop();//cx
            reg.pop();//ax

            reg.carry = true;
            pcmload15();
        }

        private int extpcm_write1()
        {
            reg.push(reg.di);
            reg.push(reg.cx);
            reg.push(reg.bx);
            reg.push(reg.ax);

            reg.di -= reg.ax;// DI = 書き込み開始ADPCM番地
            reg.ax = reg.di;
            pcmcnt2 = reg.ax;
            pcmcnt1 = 0;

            play4.calc_extpcmadr();

            extpcmadr = reg.di;// PCM番地(0～3FFFh)
            oldmapadr = reg.cl;// EMSページ(0～31)

            reg.bx = reg.cx;
            setnew_extpcm();// EMSのマッピング

            reg.ax = reg.pop();
            reg.bx = reg.pop();
            reg.cx = reg.pop();
            reg.di = reg.pop();

            if (reg.carry)
            {
                pcmload16();
                return 0;// バッファ容量エラー
            }
            extmapflg |= 2;
            extpcm_write_main(cusbuff);
            //	pushf
            extmapflg &= 0xfd; // usrpcmモード
            //	popf
            if (reg.carry)
            {
                pcmload16();
                return 0;
            }

            return 14;//pcmload14
        }

        //;-------------------------------------------------
        //;	パス名の解析
        //;	exit	path1 = ファイル名の先頭番地
        //;		path2 = 親ディレクトリの先頭番地
        //;		path3 = 拡張子の先頭番地
        //;		flength = ファイル名の長さ+1
        //;-------------------------------------------------

        //getpath:
        //	push	bx
        //	mov	bx,ofs:bufbuf
        //	call	getpath_main
        //	pop	bx
        //	ret

        private void getpath_main(string bxpath)
        {
            char dx;
            do
            {
                dx = bxpath[reg.bx];
                if (dx == '\\')
                {
                    path2 = path1;// 前回の\番地を保存
                    path1 = reg.bx;
                }
                if (dx == '.')
                {
                    //	cmp	dh,"."		; 拡張子の先頭番地
                    path3 = reg.bx;
                }
                reg.bx++;
            } while (dx != '\0');// パス名の最後か
            flength = reg.bx - path1;// ファイル名の長さ
            return;
        }

        //;==========================================================
        //;	漢字＆半角漢字チェック
        //;
        //;	entry	DX    = shift JIS code
        //;	exit	NC,NZ = ANK data	* jnb  でANK
        //;		CY,Z  = 全角 KANJI	* jnbe でANK,半角
        //;		CY,NZ = 半角 KANJI
        //;==========================================================

        // 略

        //････････････････････････････････････････
        //	拡張PCMのEMS書き込み
        //	entry	CX = 書き込みバイト数
        //		SI = ADPCMデータ番地
        //	exit	CY = バッファ不足
        //････････････････････････････････････････

        private void extpcm_write_main(byte[] siBuf)
        {
            ushort dibk = reg.di;
            ushort dxbk = reg.dx;
            ushort bxbk = reg.bx;
            reg.ax = pcmseg;// SSGPCMセグメント値を保存
            ushort axbk = reg.ax;
            //segad2:
            reg.ax = 0xc000;
            pcmseg = reg.ax;
            reg.di = extpcmadr;// DI = PCM書き込み番地
            //extpcm_write_lop:
            do
            {
                ushort cxbk = reg.cx;
                if (reg.di >= 16384)//16384:0x4000:emsの1pageのサイズ
                {
                    reg.di = 0;
                    reg.ax = 0;
                    reg.al = oldmapadr;
                    reg.al++;
                    oldmapadr = reg.al;
                    reg.bx = reg.ax;
                    setnew_extpcm();// 次のページをマップする
                    if (reg.carry)
                    {
                        //writeover1:
                        reg.cx = cxbk;
                        //writeover2:
                        reg.ax = axbk;
                        pcmseg = reg.ax;
                        reg.bx = bxbk;
                        reg.dx = dxbk;
                        reg.di = dibk;
                        return;
                    }
                }
                //writeadr1:
                byte[] diBuf = ems.cS4231EMS_GetCrntMapBuf();
                byte a = siBuf[reg.si];
                siBuf[reg.si] = (byte)(((a & 0xf0) >> 4) | ((a & 0x0f) << 4));
                calc_nextxy(siBuf, diBuf);// 16ビットPCMに変換して格納
                a = siBuf[reg.si];
                siBuf[reg.si] = (byte)(((a & 0xf0) >> 4) | ((a & 0x0f) << 4));
                calc_nextxy(siBuf,diBuf);
                reg.al = pcmcnt1;
                //debug  ↓を有効にするのが本来ではあるが、ADPCMの展開が上手くいかなくなる。原因不明
                //reg.al++;
                if (reg.al >= 4)
                {
                    reg.al = 0;
                    ushort axbk2 = reg.ax;
                    ushort dxbk2 = reg.dx;
                    ushort sibk2 = reg.si;
                    reg.ax = pcmcnt2;
                    reg.al++;
                    pcmcnt2 = reg.ax;
                    if ((extmapflg & 2) == 0)// usrpcm登録の場合は無視
                    {
                        reg.si = 0;//ofs:pcmtable

                        //writeadr5:
                        do
                        {
                            reg.dx = (ushort)(play4.pcmtable[reg.si]+ play4.pcmtable[reg.si+1]*0x100);
                            reg.dx <<= 1;
                            if (reg.ax == reg.dx)
                            {
                                //writeadr3:
                                deltax = 127;// ワークの初期化
                                reg.ax = 0;
                                xdata[0] = reg.al;
                                xdata[1] = reg.ah;
                                xdata[2] = reg.al;
                                xdata[3] = reg.ah;
                                break;
                            }
                            else if (reg.ax - reg.dx < 0) break;
                            reg.si += 2;
                        } while (reg.si - (MAXPCM * 2) <= 0);
                    }
                    //writeadr4:
                    reg.si = sibk2;
                    reg.dx = dxbk2;
                    reg.ax = axbk2;
                }
                //writeadr2:
                pcmcnt1 = reg.al;
                reg.si++;
                reg.cx = cxbk;
                reg.cx--;
            } while (reg.cx > 0);

            extpcmadr = reg.di;
            reg.carry = false;

            reg.ax = axbk;
            pcmseg = reg.ax;
            reg.bx = bxbk;
            reg.dx = dxbk;
            reg.di = dibk;
            return;
        }

        //;････････････････････････････････
        //;	Ｘn+1を計算・格納
        //;････････････････････････････････

        private void calc_nextxy(byte[] siBuf, byte[] diBuf)
        {
            reg.al = siBuf[reg.si];// AL = L4～L1データ
            reg.dx = deltax;// DX = Δn

            calcgx();

            // ΔＸn+1を計算
            int xd = (int)((uint)xdata[0]
                + (uint)(xdata[1] << 8)
                + (uint)(xdata[2] << 16)
                + (uint)(xdata[3] << 24));
            xd += (int)((uint)reg.ax + (uint)(reg.dx << 16));
            xd = xd > 32767 ? 32767 : (xd < -32768 ? -32768 : xd);//32767:0x7fff  -32768:0x8000
            xdata[0] = (byte)xd;
            xdata[1] = (byte)(xd >> 8);
            xdata[2] = (byte)(xd >> 16);
            xdata[3] = (byte)(xd >> 24);

            if ((xdata[3] & 0x80) != 0)
            {
                //nextx2:
                if (((ushort)(xdata[2] + xdata[3] * 0x100) != 0xffff) ||
                    ((ushort)(xdata[0] + xdata[1] * 0x100) < 0x8000))
                {
                    //nextx4:
                    xdata[0] = 0x00;
                    xdata[1] = 0x80;
                    xdata[2] = 0xff;
                    xdata[3] = 0xff;
                }
            }
            else if (((ushort)(xdata[2] + xdata[3] * 0x100) != 0) || 
                     ((ushort)(xdata[0] + xdata[1] * 0x100) > 0x7fff))
            {
                //nextx3:
                xdata[0] = 0xff;
                xdata[1] = 0x7f;
                xdata[2] = 0x00;
                xdata[3] = 0x00;
            }

            //nextx1:
            reg.ax = (ushort)(xdata[0] + (uint)(xdata[1] << 8));
            reg.dx = voldata;// DX = 音量データ
            int ans = (short)reg.ax * (short)reg.dx;//imul dx -> ax * dx = dx:ax
            reg.ax = reg.dx = (ushort)(ans >> 16);
            reg.ax = (ushort)((short)reg.ax >> 2);
            if ((reg.ah & 0x80) != 0)
            {
                //negdata1:
                if (reg.ax < 0xff80)
                    reg.al = 0x80;
            }
            else if (reg.ax > 127)
            {
                reg.al = 127;
            }

            //negdata2:
            diBuf[reg.di++] = reg.al;
            //	mov	es,pcmseg

            reg.al = siBuf[reg.si];// AL = L4～L1データ
            reg.dx = deltax;// DX = Δn

            calcdn();

            deltax = reg.ax;// Δn+1を格納
        }

        //;---------------------------------------
        //;	Δn+1 の計算
        //;	entry	AL = L3～L1 データ
        //;		DX = Δn
        //;	exit	AX = Δn+1
        //;---------------------------------------

        private void calcdn()
        {
            reg.al &= 7;
            reg.cl = reg.al;

            if (reg.cl < 4) reg.ax = 57;// 57/64
            else if (reg.cl == 4) reg.ax = 77;// 77/64
            else if (reg.cl < 6) reg.ax = 102;// 102/64
            else if (reg.cl == 6) reg.ax = 128;// 128/64
            else reg.ax = 153;// 153/64

            //_calcdn1:
            uint ans = (uint)(reg.ax * reg.dx);
            reg.ax = (ushort)ans;
            reg.dx = (ushort)(ans >> 16);

            reg.cx = 64;
            uint dans = ans / reg.cx;
            reg.ax = (ushort)dans;
            reg.dx = (ushort)(ans % reg.cx);
            if (reg.ax < 127) reg.ax = 127;
            else if (reg.ax >= 24576) reg.ax = 24576;
        }

        //;-----------------------------------------
        //;	L4～L1 データから差分値を計算
        //;	entry	AL = L4～L1 データ
        //;		DX = Δn
        //;	exit	DXAX = 今回の差分値
        //;-----------------------------------------

        private void calcgx()
        {
            if ((reg.al & 8) == 0)
            {
                calcgx_main();
                return;
            }
            calcgx_main();// 負のデータ
            reg.cx = 0;
            reg.bx = 0;
            reg.carry = reg.bx < reg.ax;
            reg.bx -= reg.ax;
            reg.cx -= (ushort)(reg.dx + (reg.carry ? 1 : 0));
            reg.ax = reg.bx;
            reg.dx = reg.cx;
        }

        private void calcgx_main()
        {
            reg.ax &= 7;
            reg.ax <<= 1;
            reg.ax++;
            uint ans = (uint)(reg.ax * reg.dx);
            reg.ax = (ushort)ans;
            reg.dx = (ushort)(ans >> 16);

            ushort n = (ushort)((reg.dx & 7) << 13);
            reg.dx >>= 3;
            reg.ax = (ushort)((reg.ax >> 3) | n);
        }

        //;------------------------
        //;	PCMの初期化
        //;------------------------

        private bool pcm_init()
        {
            if (play4.check_86pcm())
            {
                extpcm_init();
                return false;
            }

            int si = 0;
            ushort axbk = reg.ax;
            while (true)
            {
                reg.ax = pcmtbl[si];
                si++;
                if (reg.ax == 0xffff) break;
                play4.outdata2();
            }
            reg.ax = axbk;

            return true;
        }

        private void extpcm_init()
        {
            //	push	bx
            ushort bxbk = reg.bx;
            voldata = 256;
            reg.bx = 0;

            ushort bx = reg.bx;
            save_extpcm(ref bx);// 最初のページのマッピング
            reg.bx = bx;
            setnew_extpcm();
            extmapflg |= 1;// マップした
            reg.ax = 0;
            extpcmadr = reg.ax;// PCM番地チェック用
            oldmapadr = reg.al;// マップ中のページ番号
            pcmcnt1 = reg.al;
            pcmcnt2 = reg.ax;
            reg.bx = bxbk;
        }

        //===================================================
        //	ドライブセンス V1.4 (DL = drive number)
        //===================================================
        private void drv_sense()
        {
            //熊　恐らくフロッピーなどが入っていることを確認している
            //常に成功
            reg.carry = false;
        }

        //------------------------------
        //	PCM専用YM2608出力
        //	entry	AL = adrs
        //		AH = data
        //------------------------------

        private void outdata2pcm()
        {
            ushort dxbk = reg.dx;
            ushort axbk = reg.ax;

            reg.dx = 0x18c;// port36;
            pc98.OutportB(reg.dx, reg.al);
            //	jmp	$+2
            reg.dx += 2;
            reg.al = reg.ah;
            pc98.OutportB(reg.dx, reg.al);

            reg.ax = axbk;
            reg.dx = dxbk;
        }

        //stay	ends
        //	end	start

    }
}