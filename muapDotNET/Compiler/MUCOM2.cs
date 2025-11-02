//using Compiler;
using muapDotNET.Common;
using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace muapDotNET.Compiler
{
    public class MUCOM2
    {
        public Common.x86Register r = null;
        public MENU menu;
        public muap98 muap98;
        public MUCOMSUB mucomsub;
        public Work work;

        public MUCOM2(x86Register r, MENU menu, muap98 muap98, MUCOMSUB mucomsub,Work work)
        {
            this.r = r;
            this.menu = menu;
            this.muap98 = muap98;
            this.mucomsub = mucomsub;
            this.work = work;
        }

        public void Init()
        { 
            InitJpdata();
            InitCalltbl();
        }

        private ushort GetSourceData(int adr)
        {
            return (ushort)(
                (byte)(adr >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[adr])
                | ((byte)(adr + 1 >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[adr + 1]) << 8)
                );
        }

        private void pc98_Int18()
        {
            if (r.ah == 0) r.bh = 0;
            r.ax = 0;
            return;
        }

        public void stosbObjBufAL2DI(MmlDatum md = null)
        {
            if (md == null)
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            else
            {
                MmlDatum md2 = new MmlDatum(md.type, md.args, md.linePos, r.al);
                muap98.object_Buf[r.di++] = md2;
            }
        }

        public void stoswObjBufAX2DI(MmlDatum md=null)
        {
            if (md == null)
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            else
            {
                MmlDatum md2 = new MmlDatum(md.type, md.args, md.linePos, r.al);
                muap98.object_Buf[r.di++] = md2;
            }
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
        }

        //WINDOW.ASM
        private byte topx = 8;
        private byte topy = 4;
        private byte sizex = 60;
        private byte sizey = 15;
        private byte locatex = 0;
        private byte locatey = 1;
        private byte attr = 0xe1;
        private void save_text() { }
        private void set_text() { }
        private void putword(string msg)
        {
            string m = msg.Substring(0, msg.IndexOf("$"));
            musicDriverInterface.Log.writeLine(LogLevel.INFO, m);
        }
        private void putchr(byte c)
        {
            Log.writeLine(LogLevel.INFO, ((char)c).ToString());
        }
        private void putchrs(byte[] c)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in c)
            {
                sb.Append((char)b);
            }   
            Log.writeLine(LogLevel.INFO, sb.ToString());
        }
        private void putstr(string fmt,params object[] prm)
        {
            Log.writeLine(LogLevel.INFO, string.Format(fmt,prm));
        }
        private void load_text() { }


        //MUTRACE.ASM
        private byte saples = 0;
        private byte zero = 0;
        private byte stamode = 0;
        private void dsp2dec()
        {
            r.push(r.bx);
            r.push(r.cx);
            saples = 0;
            r.cx = 2;
            r.bx = 10;
            div_loop();
            r.cx = r.pop();
            r.bx = r.pop();
            return;
        }

        private void dsp3dec()
        {
            r.push(r.bx);
            r.push(r.cx);
            saples = 0;
            r.cx = 3;
            r.bx = 100;
            div_loop();
            r.cx = r.pop();
            r.bx = r.pop();
            return;
        }

        private void dsp4dec()
        {
            r.push(r.bx);
            r.push(r.cx);
            saples = 0;
            r.cx = 4;
            r.bx = 1000;
            div_loop();
            r.cx = r.pop();
            r.bx = r.pop();
            return;
        }

        private void dsp5dec()
        {
            r.push(r.bx);
            r.push(r.cx);
            saples = 0;
            r.cx = 5;
            r.bx = 10000;
            div_loop();
            r.cx = r.pop();
            r.bx = r.pop();
            return;
        }

        private void dsp5decl()
        {
            r.push(r.bx);
            r.push(r.cx);
            saples = 2;
            r.cx = 5;
            r.bx = 10000;
            div_loop();
            r.cx = r.pop();
            r.bx = r.pop();
            return;
        }

        private void div_loop(byte[] diBuf = null)
        {
            r.push(r.ax);
            r.push(r.dx);
            //div_loop1:
            do
            {
                r.dx = 0;
                int ans = (r.dx * 0x10000 + r.ax) / r.bx;
                int mod = (r.dx * 0x10000 + r.ax) % r.bx;
                r.ax = (ushort)ans;
                r.dx = (ushort)mod;
                r.push(r.dx);// DX = 100,10,1
                int m = 1;
                // "0"か?
                if (r.al == 0 && r.cl != 1)// 1桁目では必ず表示
                {
                    if (saples == 2) m = 0;// 左寄せの時は表示しない
                    else if (saples != 1)
                    {
                        r.dl = (byte)((zero != 0) ? '0' : ' ');//2,3桁目が0の時はスペースを表示
                        m = 2;
                    }
                }

                switch (m)
                {
                    case 0:
                        break;
                    case 1:
                        //skip_zero:
                        saples = 1;
                        r.dl = r.al;
                        r.dl += (byte)'0';
                        //skip_dsp:
                        if (stamode != 1) putchr(r.dl);//mode1:
                        else
                        {
                            r.al = r.dl;// メモリ格納モードか(Zパラ出力用)
                            if (diBuf != null && diBuf.Length > r.di) diBuf[r.di] = r.al;//es:di
                            r.di++;
                        }
                        break;
                    case 2:
                        //skip_dsp:
                        if (stamode != 1) putchr(r.dl);//mode1:
                        else
                        {
                            r.al = r.dl;// メモリ格納モードか(Zパラ出力用)
                            if (diBuf != null && diBuf.Length > r.di) diBuf[r.di] = r.al;//es:di
                            r.di++;
                        }
                        break;
                }

                //skip_spc:
                r.ax = r.bx;
                r.dx = 0;
                r.push(r.di);
                r.di = 10;
                ans = (r.dx * 0x10000 + r.ax) / r.di;
                mod = (r.dx * 0x10000 + r.ax) % r.di;
                r.ax = (ushort)ans;
                r.dx = (ushort)mod;
                r.di = r.pop();
                r.bx = r.ax;
                r.dx = r.pop();
                r.ax = r.dx;
                r.cx--;
            } while (r.cx != 0);

            r.dx = r.pop();
            r.ax = r.pop();
            return;
        }

        //MUAP.INC
        private const int OTAME = 0;// 試用版
        private const int CSEG = 0x8600;// EMSスワップ実行セグメント
        private const int FIFO_SIZE = 128;
        public int MAXBUF = 18;
        private const int OBJTOP = 0x2a;
        private const int MXPOS = 7;
        private const int MYPOS = 12;
        private const int MXSIZE = 57;
        private const int MYSIZE = 7;
        public  const int TONEOFS = (MXSIZE + 4) * (MYSIZE + 2) * 3;// 音色番号置換バッファ番地(256bytes)
        public byte[] TONEOFSbuf = new byte[256];
        public const int IFSTACK = TONEOFS + 256;// if then/exitのスタック(128bytes)
        public byte[] IFSTACKbuf = new byte[128];
        public const int MACACHE = IFSTACK + 128;// 1文字マクロキャッシュ(52bytes)
        public byte[] MACACHEbuf = new byte[52];
        //private const int VOLBASE = 80;// V1の内部音量(+3)
        private const int SYMDTA = 0x22;// object:[22h] シンボリック情報の有無
        public const int MAXPCM = 100;

        //PLAY4.ASM
        private ushort[] maxlen = new ushort[] { 0, 0 };
        private byte[] skipbyte = new byte[]{
             0,0,0,0,0,8,0,3	// FF-F8 の制御コードのバイト数-1
            ,3,1,2,2,0,3,2,1	// F7-F0
            ,1,2,1,1,1,2,2,0	// EF-E8
            ,2,26,0,4,4,1,0,0   // E7-E0
            ,0,1,1,3,3,3,6,1	// DF-D8
            ,1,1,2,4,4,0,0,1	// D7-D0
            ,1,0		// CF-CE
            };

        private void init_work() { }

        public void freq_lfo()
        {
            r.push(r.cx);
            r.cx = 3;// ***V2.12(4) 内部演算用

        //freq_lfo2:
            r.push(r.dx);
            r.dx >>= 4;
            r.al = r.ah;
            r.ax = (ushort)(short)(sbyte)r.al;
            short a = (short)r.ax;
            short b = (short)r.dx;
            int ans = a * b;
            r.dx = (ushort)(ans >> 16);
            r.ax = (ushort)ans;

        //freq_lfo1:
            do
            {
                r.carry = (r.dx & 1) != 0;
                r.dx = (ushort)((short)r.dx >> 1);
                r.ax = r.rcr(r.ax, 1);
                r.cx--;
            } while (r.cx != 0);

            r.dx = r.pop();
            r.cx = r.pop();

            return;
        }

        public byte[] tone_adrs()
        {
            // AL =  音色番号
            r.bx = (ushort)(25 * r.al);
            r.ds = muap98.tone;
            return muap98.toneBuff;
        }

        //;･･････････････････････････････････････････････
        //;	指定チャネルのビット値計算
        //;	entry	CH = チャネル番号(0～16)
        //;	exit	DLAX = 2^CH (マスクフラグ)
        //;･･････････････････････････････････････････････
        public void calcbit()
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
        //VIEWPLAY.ASM
        private void testknj()
        {
            //testknj:
            if (r.dh < 0x81)
            {
                // 英数
                r.carry = false;// NB,NZ
                r.zero = false;
                return;
            }
            if (r.dh <= 0x9f)
            {
                // 漢字
                if (r.dx < 0x8540)
                {
                    // 半角チェック
                    r.zero = true;
                    r.carry = true;// B
                    return;
                }
                if (r.dx <= 0x869e)
                {
                    r.zero = false;
                    r.carry = true;// B
                    return;
                }
                //knjdata:
                r.zero = true;
                r.carry = true;// B
                return;
            }
            if (r.dh < 0xe0)
            {
                // カナ
                r.carry = false;// NB,NZ
                r.zero = false;
                return;
            }
            if (r.dh <= 0xfc)
            {
                r.zero = true;
                r.carry = true;// B
                return;
            }

            //制御コード
            r.carry = false;// NB,NZ
            r.zero = false;
            return;
        }

        //CAL.ASM
        private Action[] calltbl;
        private string[] calerror1;

        private void InitCalltbl()
        {
            calltbl = new Action[]{
                null,cal_mucom2,null,null,// helpcal2, mucom2, menu2, dsperr2; 0～3
                null,null,null,null,//dw tonedsp2, get_bufseg, toneedit_ent, load_usrpcm; 4～7
                null,null,null,null,//dw set_fepgaiji, get_onbu, get_onpu, get_choshi; 8～11
                null,null,null,null,//dw read_nmivram, tonedsp3, save_vram, init_vram; 12～15
                null,null,null,null,//dw load_vram, set_vram, dummy, visualplay_main; 16～19
                null,null,null,null,//dw visualplay_sub, visualplay_ret, info_disp; 20～22
                null,null,null,null,//dw set_kengaiji, ikey_main, calc_keypos; 23～25
            };

            calerror1 =new string[]{
                "()ループ回数$"//; Error Code 1
                ,"演奏バッファ領域不足$"
                ,"()ループネスト(15まで)$"
                ,"連符データ$"
                ,"連符回数$"// Error Code 5
                ,"パラメータ範囲$"
                ,"無効な調音記号がある$"
                ,"リズムパターンの数が多い$"
                ,"コードネームが存在しない$"
                ,"和音番号$"// Error Code 10
                ,"和音が音符でない$"
                ,"@si/@soがネストした$"
                ,"ループの()が不一致$"
                ,"パラメータに数字以外$"
                ,"総合音長が超過した$"// Error Code 15
                ,"3CH以外で@codeinか@dtの複数パラメータ使用$"
                ,"コマンドの後が音符か<>か@+-%でない$"
                ,"ポルタメントの範囲が広すぎる$"
                ,"��置換先が存在しない$"
                ,"��置換のネスト数(10まで)$"// Error Code 20
                ,"��置換に用いた文字列の長さ(32まで)$"
                ,"チャネル10でポルタメント指定$"
                ,"移調データ$"
                ,"移調によるオクターブ値$"
                ,"()外にIF命令がある$"// Error Code 25
                ,"@label開始位置未設定$"
                ,"@jump,@call無限ループ$"
                ,"調音記号の音符データ$"
                ,"Zコマンドパラメータ$"
                ,"V=:の無効記号による$"// Error Code 30
                ,"音量の範囲$"
                ,"文法上$"
                ,"歌詞のデータ$"
                ,"ＭＭＬのバージョンが異なりますが強行します。$"
                ,"@if then/exit のネスティング$"// Error Code 35
                ,"PCMのオクターブ範囲$"
                ,"オートパンパターン指定数$"
                ,"PCM,リズムにシステムディチューン設定$"
                ,"ユーザPCM指定のファイル名異常$"
                ,"マクロ変数パラメータ不足$"// Error Code 40
                ,"拡張PCMの指定はch11以外$"
            };
        }

        private void cal_mucom2()
        {
            //pushall();
            //r.es = r.ds;// ES: DI = bufbufの番地
            //r.ds = r.cs;
            //r.si = 0;//ofs:error1; DS: SI = エラーメッセージ格納番地

            ////search_d:
            //r.cl--;

            ////xfererr:
            //do
            //{
            //    r.al = calerror1[r.cl];
            //    r.si++;
            //    muap98.bufbuf[r.di] = r.al;
            //    r.di++; // エラーメッセージの転送
            //    r.zero = r.al == (byte)'$';
            //} while (!r.zero);
            //popall();
            return;
        }

        private void call_func()
        {
            r.push(r.bx);
            r.push(r.ax);
            r.al = r.ah;// AH = ファンクション番号
            r.ah = 0;
            r.ax += r.ax;

            r.bx = 0;//ofs:calltbl
            r.bx += r.ax;
            r.ax = r.pop();

            calltbl[r.bx / 2]();// ←この後の pop bx ret retf は変更禁止(gaiji)
            r.bx = r.pop();
            return;
        }

        private void clbuff()
        {

        }

        //MENU.ASM
        private void check_calplay()
        {
            r.zero = (muap98.m_mode[0] & 2) == 0;
            return;
        }

        //;
        //;	Music Macro Assembler for MUAP98(with Debug Information) V11.27
        //;		copyright(c) 1987,1989-1995 by Packen Software[feb.18.1996]
        //;

        private const string mess_2 = "  over  $";
        private const string mess_3a = "[ESC]:abort$";
        private const string mess_5 = "エラー    $";
        private const string mess_6 = "<全音符.端数音長>$";
        private const string mess_7 = "演奏データは $";
        private const string mess_8 = " バイトです.$";
        private const string mess_9 = "強制中断しました.$";
        private const string mess_10 = "デバッグ$";
        private const string mess_11 = "情報出力$";


        //    public bxsave,data1,data2,data3,musdata,sbufbuf,symbol2,comcnt
        //    public optimiz,opt_tne,rhydata,mac_mod,dionpu,harmno,octdata,tbufbuf
        //    public mode,nesting,jumpnes,chglen,rhyvol,opt_pan,opt_rhy,pandata
        //    public panadrs,commode,flatdata,flatdata2,creslen,crescnt,dcrelen
        //    public volsave,volstt,cresvol,tmpdata,tmplen,tempos,tmpstt,tmpcnt
        //    public slurmod,porta1,porta2,porcnt,freqsv1,freqsv2,rtm_max,ope_no
        //    public rhyadrs,rhythmdta,lastrp,stttbl,to_no,from_no,ichosav,macroflg
        //    public wordbuf,macrov,cal_num,nest2,linedta,trildef,tridta1,tridta2
        //    public trillen,trionpu,tridta0,totalen,octsave,ratdata,slursav
        //    public ichodta,dtdata,dt2mode,debug,codemod,dtshift,lastfrq,musdata
        //    public slbase,slspeed,mmlver,accbase,dwnbase,por_end,ssgpcmm,volsft
        //    public ratmode,onpucnt,sendch,ifflag,srctop

        public int VOLBASE = 80;// V1の内部音量(+3)
        private ushort srctop = 0;// ソース開始番地
        public byte mmlver = 0x30;// MMLバージョン
        public byte[] mode = new byte[] { 0, 0 };// b0=連符モード b1 = 音階出力したか  b2=和音@+@%@-
        //			; b3=&自動タイ禁止 b4 = 改行調号クリア  b5=Z反転モード
        //			; b6='F3非出力  b7=自動タイ希望
        //	db	0	; b0=[] 変換中 b1 = 1文字マクロ
        private byte onkai = 0;// 音階保存用
        private byte bassdta = 0;// ベース音階用
        private ushort codesav = 0;// コード番号保存用

        private ushort bxsave = 0xffff;// ソース番地
        public ushort linedta = 0;// ソース行番号
        public byte ope_no = 1;// チャネル番号(=CH,1-17)
        public byte sendch = 0;// 実送信チャネル番号(1～17)
        private ushort disave = 0;// 演奏データ番地格納番地
        private ushort disave3 = 0;// 連符の音長設定番地保存用
        private ushort spsave = 0;// スタック保存用
        private ushort bef_len = 0;// 直前のチャネルの終了番地
        public byte cal_num = 0;// $xx$ calling number
        public byte symbol2 = 0;// b0=デバッグ情報出力フラグ, b1=マクロ内部なし
        public byte[] wordbuf = new byte[]{(byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ',
        (byte)' ',(byte)' ',(byte)' ',(byte)' ',(byte)' ',
        (byte)' ',(byte)' ',(byte)' ',(byte)' ',(byte)' ',
        (byte)' ',(byte)' ',(byte)' ',(byte)' ',(byte)' ',
        (byte)' ',(byte)' ',(byte)' ',(byte)' ',(byte)' ',
        (byte)' ',(byte)' ',(byte)' ',(byte)' ',(byte)' ',
        (byte)' ',(byte)' ' };
        public byte[] rhyvol =new byte[] { 31, 31, 31, 31, 31, 31 };
        //waitadd dw	?	; 直前のWAITコマンド番地
        public byte from_no = 0;// 複写元音色番号
        public byte to_no = 0;// 複写先音色番号
        public ushort rhyadrs = 0;// リズムデータテーブル番地
        public byte rtm_max = 0;// リズム残り音長の保存
        public byte volstt = 0;// クレッシェンド開始音量
        public ushort tmpstt = 0;// リタルダンド開始テンポ
        public byte lensave = 0;// 音符解析の音長保存用

        public ushort tempos = 120;// テンポの保存
        public byte volsave =	110	;// 内部音量の保存(@V110)
        private byte lendata =	48	;// デフォルト音長(L4)
        public byte octdata =	3	;// オクターブ(O4)
        public byte octsave =	3	;// トリル用
        public byte ratdata =	1	;// 音長割合(Q7)
        private byte maxrest =	192	;// 圧縮休符の最大値(いわば1小節の長さ)

        public byte tridta0 =	0	;// 基音の変音データ
        public byte tridta1 =	0	;// トリラーの+,-指定
        public byte tridta2 =	0	;// 下側音符
        public byte totalen =	0	;// 全体の音長
        public byte trillen =	0	;// トリラーの長さ
        public byte trionpu =	0	;// 音符コード
        public byte trildef =	6	;// トリル速度
        public byte slursav =	0	;// トリル時Qの値保存
        public byte debug = 0;// 小節終了時に休符を挿入するかどうか

        public ushort panadrs = 0;// 現在のオートパン位置

        public ushort porta1 = 0;// ポルタメント開始周波数
        public ushort porta2 = 0;// 　　〃　　　終了周波数
        public ushort freqsv1 = 0;//
        public ushort freqsv2 = 0;//
        public ushort porcnt = 0;// 分割回数
        public byte slbase = 0xfc;// スライドの変移値
        public byte slspeed =	6	;// スライド速度
        public byte accbase =	4	;// アクセントの加算値
        public byte dwnbase =	6	;// 逆アクセントの加算値
        public ushort por_end = 0;

        //;	以下はチャネル毎に0初期化される

        //tbufbuf label byte
        public ushort dionpu = 0;// 直前の音符のあった番地
        public byte[] dionpuBuf = new byte[256];// 直前の音符のあった番地
        public byte slurmod =0;// スラーモード中のQ保存用(b7= @si)
        public ushort lastfrq = 0;// 直前の音符の周波数データ(&用)
        public byte harmno = 0;// @harm の和音番号
        public byte codemod = 0;//コード変換モード
        public byte dt2mode = 0;// ch3の複数ディチューンモード
        private byte arpmode = 0;////アルペジョモード
        private byte arpharm = 0;// アルペジョの展開する和音数(arpmodeとで1ワード)
        private byte arplen = 0;//
        public byte volsft = 0;// V=:の音量変移
        public byte nesting = 0;// () nest counter
        public byte nest2 = 0;// $xx$ nest
        public byte chglen = 0;// 前回指定された音長
        private byte chgsav = 0;// 休符の直前のchglen保存用
        private byte renplen = 0;// 連符用直前のchglen保存
        private byte restlen = 0;// 連続している休符の合計音長
        private ushort disave1 = 0;// 休符開始の番地
        private ushort disave2 = 0;// 休符終了の番地
        public ushort cresvol = 0;// クレッシェンドの変化量
        public ushort creslen = 0;// クレッシェンドの長さ
        public ushort dcrelen = 0;//
        public ushort crescnt = 0;// クレッシェンド開始からの音長
        public ushort tmpdata = 0;// @ACC,@RITの値(符号付き)
        public ushort tmplen = 0;// その長さ
        public ushort tmpcnt = 0;//
        public byte[] dtdata = new byte[] { 0, 0, 0, 0 };// ディチューン値
        public byte[] dtshift =new byte[] { 1, 1, 1, 1 };// ディチューンシフト値(dtdataとくっつける)
        public byte rhydata = 0;// リズム音色
        public byte jumpnes = 0;// ネスト値
        public byte ichosav = 0;// 移調データ(_C)
        public ushort lastrp = 0;// 直前のリズムキーオン・ダンプデータ
        private byte macrof = 0;// $name[] のチェック
        public byte commode = 0;// b0=歌詞出力の有無, b1=色歌詞モード
        public byte mac_mod = 0;// 小文字／大文字変換モード(b0,1)
        public byte comcnt = 0;// 色変わり歌詞位置桁カウンタ
        public byte optimiz = 0;// 最適化フラグ(b0= 音色, b1= パン, b2= 音量, b3= リズム)
        public byte opt_tne = 0;// 音色番号チェック
        public byte opt_pan = 0;// パンチェック
        private byte opt_vol = 0;// 音量チェック
        public ushort opt_rhy = 0;// リズム用パン・音量チェック
        public byte ssgpcmm = 0;// SSGPCMモード
        public byte ratmode = 0;// Q/@Qモード
        public byte onpucnt = 0;// 連符内音符数のカウンタ
        public byte ifflag = 0;// @if thenなどの実行フラグ
        //sbufbuf label byte		; TracePlayワークエリア333bytes
        private ushort[] alllen = new ushort[]{0, 0, 0, 0,
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
        };// 総合音長 + ループ脱出間の音長
        // ループ内ネスト分の音長
        public ushort[] macrov =new ushort[]{
            0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0
            };// マクロ変数バッファ
        // そのテキスト開始番地
        public ushort macroflg = 0;//変数指定フラグ
        public byte[] pandata = new byte[]{
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0
        };// オートパンデータ
        public byte[] rhythmdta = new byte[]{
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0
        };// リズム演奏パターン
        public byte[] flatdata = new byte[]{
            0,0,0,0,0, 0,0
        };// ABCDEFG +-調号
        public byte[] flatdata2 = new byte[]{
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0
        };// 1小節のみ有効な調号(o1～o8, o9)
        public ushort[] stttbl = new ushort[]{
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0,
            0,0,0,0,0, 0,0,0,0,0
        };// () n コマンドの開始番地,@ifexit番地
        //endadrs   label byte

        public byte[] ichodta = new byte[] { 0xfd, 0xff, 0, 2, 4, 5, 0xfb };
        public byte[] musdata = new byte[] { 9, 11, 0, 2, 4, 5, 7 };

        public byte[] data1 = new byte[]{
             0x6a,0x2,0x8f,0x2,0xb6,0x2,0xdf,0x2 // C C+ D D+  (YM-2203)
        	,0x0b,0x3,0x39,0x3,0x6a,0x3          // E F F+
        	,0x9e,0x3,0xde,0x3,0x10,0x4,0x4e,0x4 // G G+ A A+
            ,0x8f,0x4                            // B
        };

        public byte[] data2 = new byte[]{
         0xe8,0xe,0x12,0xe,0x48,0xd,0x89,0xc	// (SSG)
        ,0xd5,0xb,0x2b,0xb,0x8a,0xa
        ,0xf3,0x9,0x64,0x9,0xdd,0x8,0x5e,0x8
        ,0xe6,0x7
        };

        public byte[] data3 = new byte[]{
            0xbc,0x49,0x1e,0x4e,0xc4,0x52,0xaf,0x57,// (PCM)
            0xe6,0x5c,0x6c,0x62,0x47,0x68,
            0x7a,0x6e,0x0c,0x75,0x02,0x7c,0x61,0x83,
            0x31,0x8b
        };
        //work    ends

        //program segment word public 'code'
        //	.186
        //	assume cs:main,ds:main

        //    include exen.h
        //    public compile
        //    public c90,c91,c94,c95
        //    public theend,codein,code_exe,same_code,error,stopm,rednums,chktxt
        //    public retvol2,chkval,set_symbol2,tnelnmx,tnelnx,setrat,codeout,bass
        //    public set_max,arpeggio,octdown,octup,tnelnm,retvol,rednum,calct
        //    public calc_tempo,gethenon,move_obj,retvol3,com_main
        //    public harm_main,get_harm,kyufu,chgrat
        //    public init_looplen,set_looplen,exit_looplen,add_tlen

        //;==========================================
        //;	ワークエリア初期化
        //;	entry CH = チャネル番号(1 - 17)
        //;==========================================

        private void work_init()
        {
            r.push(r.es);
            r.push(r.ds);
            r.push(r.di);
            r.push(r.cx);
            r.push(r.ax);
            r.ds = r.cs;
            r.es = r.cs;

            //    cld

            mode[0] &= 0b0_0101_1000;//    and wpr mode,001011000b ; モードの初期化
            mode[1] &= 0;
            volsave = 110;// 音量をセット(V10)
            lendata = 48;// デフォルトの音長をセット(L4)
            octdata = 3;// オクターブを初期化(O4)
            slbase = 0xfc;// スライドの初期化 -4
            slspeed = 6;
            accbase = 4;// アクセント値の初期化
            dwnbase = 6;
            r.push(r.si);
            mucomsub.pan_init();// オートパンの初期化
            r.si = r.pop();
            r.ax = 0;
            if (r.ch < 10 || r.ch > 11)
            {
                //winit2:
                r.ax++;
            }
            //winit3:
            ratdata = r.al;// 音長割合(Q7)  リズム・PCMはQ8
            maxrest = 192;// 圧縮休符の最大値(L1)
            trildef = 6;// トリル,アルペジョ速度(L32)
            mucomsub.init_rhythm();// リズムデータテーブル番地の初期化
            r.di = 0;//ofs:rhyvol
            r.cx = 6;
            r.al = 31;
            for (int i = 0; i < rhyvol.Length; i++) // リズム音量バッファを初期化
            {
                rhyvol[i] = r.al;
            }


            r.ax = 0;
            r.di = 0;//ofs:dionpu	; 変数初期化開始番地
            r.cx = 0;//ofs:endadrs-ofs:dionpu ; 最終番地

            dionpu = 0;// 直前の音符のあった番地
            slurmod = 0;// スラーモード中のQ保存用(b7= @si)
            lastfrq = 0;// 直前の音符の周波数データ(&用)
            harmno = 0;// @harm の和音番号
            codemod = 0;//コード変換モード
            dt2mode = 0;// ch3の複数ディチューンモード
            arpmode = 0;////アルペジョモード
            arpharm = 0;// アルペジョの展開する和音数(arpmodeとで1ワード)
            arplen = 0;//
            volsft = 0;// V=:の音量変移
            nesting = 0;// () nest counter
            nest2 = 0;// $xx$ nest
            chglen = 0;// 前回指定された音長
            chgsav = 0;// 休符の直前のchglen保存用
            renplen = 0;// 連符用直前のchglen保存
            restlen = 0;// 連続している休符の合計音長
            disave1 = 0;// 休符開始の番地
            disave2 = 0;// 休符終了の番地
            cresvol = 0;// クレッシェンドの変化量
            creslen = 0;// クレッシェンドの長さ
            dcrelen = 0;//
            crescnt = 0;// クレッシェンド開始からの音長
            tmpdata = 0;// @ACC,@RITの値(符号付き)
            tmplen = 0;// その長さ
            tmpcnt = 0;//
            dtdata = new byte[] { 0, 0, 0, 0 };// ディチューン値
            dtshift = new byte[] { 1, 1, 1, 1 };// ディチューンシフト値(dtdataとくっつける)
            rhydata = 0;// リズム音色
            jumpnes = 0;// ネスト値
            ichosav = 0;// 移調データ(_C)
            lastrp = 0;// 直前のリズムキーオン・ダンプデータ
            macrof = 0;// $name[] のチェック
            commode = 0;// b0=歌詞出力の有無, b1=色歌詞モード
            mac_mod = 0;// 小文字／大文字変換モード(b0,1)
            comcnt = 0;// 色変わり歌詞位置桁カウンタ
            optimiz = 0;// 最適化フラグ(b0= 音色, b1= パン, b2= 音量, b3= リズム)
            opt_tne = 0;// 音色番号チェック
            opt_pan = 0;// パンチェック
            opt_vol = 0;// 音量チェック
            opt_rhy = 0;// リズム用パン・音量チェック
            ssgpcmm = 0;// SSGPCMモード
            ratmode = 0;// Q/@Qモード
            onpucnt = 0;// 連符内音符数のカウンタ
            ifflag = 0;// @if thenなどの実行フラグ
                       //sbufbuf label byte		; TracePlayワークエリア333bytes
            alllen = new ushort[]{
                0, 0, 0, 0,
                0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
                0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
                0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
            };// 総合音長 + ループ脱出間の音長
              // ループ内ネスト分の音長
            macrov =new ushort[]{
                0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0
            };// マクロ変数バッファ
              // そのテキスト開始番地
            macroflg = 0;//変数指定フラグ
            pandata = new byte[]{
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0
            };// オートパンデータ
            rhythmdta = new byte[]{
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0
            };// リズム演奏パターン
            flatdata = new byte[]{
                0,0,0,0,0, 0,0
            };// ABCDEFG +-調号
            flatdata2 = new byte[]{
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0
            };// 1小節のみ有効な調号(o1～o8, o9)
            stttbl = new ushort[]{
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0,
                0,0,0,0,0, 0,0,0,0,0
            };// () n コマンドの開始番地,@ifexit番地

            r.di = 0;//ofs:bufbuf	; @LABEL用番地バッファとして使用
            r.cx = 32;
            do
            {
                muap98.bufbuf[r.di] = r.al;
                muap98.bufbuf[r.di + 1] = r.ah;
                r.di += 2;
                r.cx--;
            } while (r.cx != 0);

            r.di = 0;//ofs:dtshift
            r.cx = 4;
            r.al = 5;
            if (mmlver < 0x30)
            {
                r.al = 1;
            }
            do// ディチューンシフト値(デフォルト5)
            {
                dtshift[r.di] = r.al;
                r.di++;
                r.cx--;
            } while (r.cx != 0);

            r.es = muap98.text;
            r.di = 0;// TONEOFS;
            r.ax = 0;

            //winit1:
            do
            {
                TONEOFSbuf[r.di++] = r.al;// 音色番号置換テーブルの初期化
                r.al++;
            } while (r.al != 0);

            r.ax = r.pop();
            r.cx = r.pop();
            r.di = r.pop();
            r.ds = r.pop();
            r.es = r.pop();
            return;
        }

        //;-----------------------------------
        //;	MMLアセンブラエントリ
        //;	entry CL = デバッグ(0, 1, 5)
        //; exit CL = エラーの有無
        //;-----------------------------------

        public void compile()
        {
            r.push(r.cs);
            r.ds = r.pop();

            symbol2 = r.cl;
            menu.check_calplay();// cal* 呼び出し?
            if (!r.zero)
            {
                //    jne selcal1
                return;
            }

            topx = MXPOS;
            topy = MYPOS;
            sizex = MXSIZE;
            sizey = MYSIZE;

            r.dx = 0;
            save_text();

            //c90:
            attr = 0xa1;
            set_text();

            if (r.cl != 0)
            {
                //c91:
                attr = 0xc1;
                locatex = MXPOS + 2;
                locatey = MYPOS + 2;
                r.dx = 0;//ofs:mess_10
                putword(mess_10);// デバッグ
                locatex = MXPOS + 2;
                locatey = MYPOS + 3;
                r.dx = 0;//ofs:mess_11
                putword(mess_11);// 情報出力
                return;
            }

            //skip_sym3:
            //c95:
            attr = 0xe1;
            locatex = MXPOS + 1;
            locatey = MYPOS + 6;
            r.dx = 0;//ofs:mess_3a
            putword(mess_3a);
            locatex = MXPOS + 1;
            locatey = MYPOS;
            r.dx = 0;//ofs:mess_6
            putword(mess_6);

            r.bl = MXPOS + 21;// 横位置
            r.cx = 5;//ループ回数
            r.al = (byte)'1';// 番号の初期値

            //dspch1:
            do
            {
                locatex = r.bl;// チャネル番号の表示
                //r.dl = (byte)'#';
                //putchr(r.dl);
                r.dl = r.al;
                //putchr(r.dl);
                putchrs(new byte[] { (byte)'#', r.dl });
                r.bl += 8;
                r.al++;
                r.cx--;
            } while (r.cx > 0);

            locatey = MYPOS + 1;
            r.cx = 4;// ループ回数
            r.ax = 0;// 番号の初期値

            //dspch2:
            do
            {
                locatex = MXPOS + 12;// チャネル番号の表示
                //r.dl = (byte)'+';
                //putchr(r.dl);
                //dsp2dec();
                putstr("+{0,2:D}",r.ax);
                r.ax += 5;
                locatey++;
                r.cx--;
            } while (r.cx > 0);

            //;-----------------------------
            //;	全体のワーク初期化
            //;-----------------------------
            // selcal1:
            r.es = muap98.text;
            r.di = MACACHE;
            r.ax = 0;
            r.cx = 26;
            do
            {
                muap98.text_Buf[r.di++] = r.al;
                muap98.text_Buf[r.di++] = r.ah;
                r.cx--;
            } while (r.cx != 0); // キャッシュバッファを初期化

            r.es = r.cs;
            r.di = 64;//ofs:bufbuf+64
            r.ax = 0;
            r.cx = 8;// 共通ラベル領域のクリア
            do
            {
                muap98.bufbuf[r.di++] = r.al;
                muap98.bufbuf[r.di++] = r.ah;
                r.cx--;
            } while (r.cx != 0);

            maxlen[0] = r.ax;// 最大音長
            maxlen[2 / 2] = r.ax;
            tempos = 120;// テンポ値(T120)
            mode[0] = r.al;
            debug = r.al;// デバッグフラグのクリア
            r.es = muap98.object_;
            r.di = OBJTOP;// DI = 演奏データ格納エリア先頭番地
            bef_len = r.di;
            r.bx = 0;// disave = 音楽演奏データポインタ格納開始番地
            disave = r.bx;// set 1st object address
            muap98.object_Buf[r.bx] = new MmlDatum((byte)r.di);// チャネル1のデータ格納開始位置のセット
            muap98.object_Buf[r.bx + 1] = new MmlDatum((byte)(r.di >> 8));
            muap98.object_Buf[0x24] = new MmlDatum(r.bl);// ユーザPCMオフセットの初期化
            muap98.object_Buf[0x25] = new MmlDatum(r.bh);
            r.ch = 1;// CH = channel number(1-17)
            spsave = r.sp;
            r.ds = muap98.source;// DS = ソースSEG, ES = 演奏SEG

            ver_check();

        recov8_:
            work.crntChip = r.ch < 12 ? "YM2608" : "YM3438";
            work.crntChannel = (int)(r.ch - 1);
            work.crntPart = r.ch < 4 ? "FM" : r.ch < 7 ? "SSG" : r.ch == 10 ? "RHYTHM" : r.ch == 11 ? "ADPCM" : "FM";
            try
            {
                recov8();
            }
            catch (MusCompileEndException mcee)
            {
                compile_end();
            }
            catch (MusRecov8Exception mr8e)
            {
                goto recov8_;
            }
        }

        //;----------------------------------
        //;	バージョンのチェック
        //;----------------------------------

        //熊:許可するバージョンは以下の通り
        // V2.2# V2.3# V2.4# V2.5# V2.6#
        // V3.0#
        // V4.0#
        //許可できなかった、或いはバージョン表記が見つからない場合はV3.0#(初期値)

        private void ver_check()
        {
        ver_check:
            do
            {
                r.ax = GetSourceData(r.bx);
                r.bx++;
                if (r.al == 0xff)// EOFか?
                {
                    error15();
                    return;
                }
                if (r.bx == muap98.sor_len)//熊:本来は >
                {
                    error15();
                    return;
                }
                if (r.al == (byte)'_' && r.ah == (byte)'v')
                {
                    break;
                }
            } while (r.al != (byte)'_' || r.ah != (byte)'V');

            //_ver1:
            // V2.x#,V3.x#,V4.x#を許可
            r.ax = GetSourceData(r.bx + 1);
            if (r.ax < (byte)'2' + ((byte)'.' * 0x100)) goto ver_check;
            if (r.ax > (byte)'4' + ((byte)'.' * 0x100)) goto ver_check;
            r.ax = GetSourceData(r.bx + 3);
            if (r.ax < (byte)'0' + ((byte)'#' * 0x100)) goto ver_check;
            if (r.ax > (byte)'9' + ((byte)'#' * 0x100)) goto ver_check;

            r.al = (byte)(muap98.source_Buf[r.bx + 1] - (byte)'0');// AH = 整数桁
            r.al <<= 4;
            r.ah = (byte)(muap98.source_Buf[r.bx + 3] - (byte)'0');// AL = 小数桁
            r.al |= r.ah;

            if (r.al < 0x22) goto ver_check;
            if (r.al != 0x40)
            {
                if (r.al != 0x30)
                {
                    if (r.al > 0x30) goto ver_check;
                    if (r.al > 0x26) goto ver_check;
                }
            }

            //_ver2:
            mmlver = r.al;// バージョン番号を保存
            return;
        }

        private void error15()
        {
            menu.check_calplay();// cal* 呼び出し?
            if (r.zero)
            {
                r.pushA();
                r.push(r.ds);
                r.ds = r.cs;
                locatex = MXPOS + 1;
                locatey = MYPOS + 5;
                r.cl = 34;
                r.di = 0;//ofs:bufbuf	; DS:DI = エラーメッセージ格納番地
                r.ah = 1;
                muap98.call_func();// CALL側でbufbufにエラーメッセージを転送
                r.dx = r.di;
                if (!r.carry)
                {
                    putword("");// エラーメッセージの表示
                }
                r.ds = r.pop();
                r.popA();
            }
            return;
        }


        //;===================================
        //;	チャネル1-17変換ループ
        //;===================================

        //	assume nothing, cs:main	; 変数にCS:をつけるため
        private void recov8()
        {
            do
            {
                menu.check_calplay();// cal* 呼び出し?
                if (r.zero)
                {
                    r.ax = 0x400;
                    pc98_Int18();
                    if ((r.ah & 1) != 0)
                    {
                        // ESC押下で中断
                        locatex = MXPOS + 1;
                        locatey = MYPOS + 6;
                        r.dx = 0;//ofs:mess_9
                        putword(mess_9);
                        //abort2:
                        do
                        {
                            r.ah = 1;
                            pc98_Int18();
                            if (r.bh == 0)
                            {
                                break;
                            }
                            r.ah = 0;
                            pc98_Int18();
                        } while (true);

                        //abort3:
                        r.ax = OBJTOP;// 演奏出来ないようにする
                        r.di = 0;
                        r.cx = 17;
                        do
                        {
                            muap98.object_Buf[r.di] = new MmlDatum(r.al);
                            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
                            r.di += 2;
                            r.cx--;
                        } while (r.cx != 0);

                        r.di = OBJTOP;
                        r.al = 0xfc;
                        muap98.object_Buf[r.di] = new MmlDatum(r.al);
                        r.di++;

                        r.ds = r.cs;
                        r.cl = 1;// エラーにする
                                 //    jmp com_end_abort
                    }
                }

                //abort1:
                ope_no = r.ch;// チャネル番号の保存
                sendch = r.ch;//    mov sendch,ch
                work_init();// ワークエリア初期化
                linedta = 1;// ソースの行数
                work.row = 1;// 行数の初期化
                work.col = 1;// 列数の初期化
                work.oldbx = 0;

                r.bx = srctop;        // BX = ソース開始番地
            } while (recov7());
        }

        //;-------------------------------------------
        //;	チャネルスタートX[] コマンド探索
        //;-------------------------------------------

        private bool recov7()
        {
        recov7:
            do
            {
                mode[1] &= 0xfe;// []変換フラグoff
                r.ch = ope_no;// 探索チャネル番号
                if (chkpart()) return true;// AL にソースの内容1文字獲得
                          //skip_num:
            } while (r.al < (byte)'1' || r.al > (byte)'9');

            bool s = true;
            r.al -= (byte)'0';// AL = 1-9
            if (r.al == 1)// チャネル1か10-17か
            {
                r.dl = r.al;
                if (chkpart()) return true;// 10-17のチェック
                if (r.al < (byte)'0' || r.al > (byte)'7') s = false;
                else r.al -= 38;// AL = 10-17
            }
            if (s)
            {
                //skip_chk1:
                r.dl = r.al;// テキストの数字保存
                if (chkpart()) return true;
            }

        not1015:
            int ret = 0;
            if (r.al == (byte)'[')// チャネルの単数指定
                ret = ch_nomulti();
            else if (r.al == (byte)',')// 複数指定
                ret = ch_multi();
            else if (r.al == (byte)'-')// 連続指定
                ret = cnt_multi();

            switch (ret)
            {
                case 0://goto recov7;
                    break;
                case 1:
                    find4();
                    break;
                case 2:
                    goto not1015;
            }

            goto recov7;
        }

        //単独指定(|有効)の処理

        private int ch_nomulti()
        {
            if (r.dl > r.ch)// 現チャネル番号と比較
            {
                return 0;// より大きければ無視
            }
            if (r.dl == r.ch)
            {
                return 1;// 一致した
            }
            r.dl -= r.ch;
            r.dl = (byte)-r.dl;// '|'スキップ数

            //chkskip:
            do
            {
                do
                {
                    do
                    {
                        chkpart();
                    } while (r.carry);// 漢字2バイト目は無視

                    if (r.al == (byte)']')
                        return 0;// このx[] 中には現オペ番号データはない
                } while (r.al != (byte)'|');

                r.dl--;
            } while (r.dl != 0);

            return 1;
        }

        //複数指定の処理

        private int ch_multi()
        {
            if (r.dl != r.ch)// 現チャネル番号と比較
                return 0;// 複数指定時は'|'は使用できない

            //find5:
            do
            {
                do
                {
                    chkpart();// "x,x[" 複数指定のチェック
                    if (r.al == (byte)'[')
                        return 1;// スタート記号があれば変換へ
                } while (r.al == ',' || r.al == '-');// ? "x,x,･･,x[" の間の有効文字ならOK

                mucomsub.chknum();// 数字チェック
            } while (!r.carry);
            return 0;// チャネル番号に数字以外の文字あり(無効)
        }

        //; 連続指定の処理
        private int cnt_multi()
        {
            if (r.dl > r.ch)// 開始チャネル番号と現チャネル番号との比較
            {
                return 0;
            }
            r.dh = r.dl;//開始チャネル番号
            chkpart();// - の次か数字であるかチェック
            if (r.al < (byte)'1') return 0;
            if (r.al > (byte)'9') return 0;// 数字でない

            r.al -= (byte)'0';// AL = 1-9
            if (r.al == 1)// チャネル1か10-17か
            {
                r.dl = r.al;
                chkpart();
                if (r.al >= (byte)'0' && r.al <= (byte)'7')// チャネル10-17のチェック
                {
                    r.al -= 38;// AL = 10-17
                }
            }

            //skip_chk2:
            r.dl = r.al;// テキストの数字保存
            chkpart();

            //not10152:
            if (r.al != (byte)'[' && r.al != (byte)',')
            {
                if (r.al != (byte)'-')
                {
                    return 0;// 無効データだった
                }
            }

            //cntmul1:
            if (r.dl < r.ch)// 現チャネル番号との比較
            {
                return 2;// DL に前のチャネル番号を入れて再チェック
            }
            // 範囲内にあった

            //tofind4:
            if (r.al == (byte)'[')// 範囲内なら[記号までBXを移動する
            {
                return 1;
            }

            //tofind5:
            do
            {
                do
                {
                    chkpart();//; "x,x[" 複数指定のチェック
                    if (r.al == (byte)'[')//スタート記号があれば変換へ
                    {
                        return 1;
                    }
                } while (r.al == (byte)',' || r.al == (byte)'-');// ? "x-x,･･,x[" の間の有効文字ならOK

                mucomsub.chknum();// 数字チェック
            } while (!r.carry);

            return 0;// チャネル番号に数字以外の文字あり(無効)
        }

        //;===================================
        //;	[ ] 内変換ループ処理
        //;===================================

        private int find4()
        {
            do
            {
                do
                {
                    mode[1] |= 1; // []
                                  //        変換フラグon
                    chktxt();// AL = 音楽制御コード
                } while (r.carry);// 漢字2バイト目は無視

                if (r.al == (byte)']')// 終了コードか
                {
                    return 0;
                }
                if (r.al == (byte)'|')// 次のチャネルへ移るなら終了
                {
                    //skipend:
                    do
                    {
                        chktxt();// "|" 終了なので"]"まで飛ばす
                    } while (r.al != ']');
                    return 0;
                }

                r.ch = sendch;// 出力するチャネル番号
                com_main();// アセンブルメインルーチン
                r.push(r.ax);
                r.ax = muap98.bufleno;// 演奏バッファ容量を超えそうか
                r.ax -= 0x10;
                r.carry = (r.di < r.ax);
                r.cl = 2;// エラーコード
                r.ax = r.pop();
            } while (r.carry);
            return 1;
        }

        //;===========================================
        //;	ソーステキストから1文字獲得
        //;	entry DS:BX = source address
        //; exit AL = text data
        //;		CY = 漢字2バイト目
        //;		他のregは保存
        //;===========================================

        public void chktxt()
        {
        chktxt3_:
            do
            {
                if (r.bx >= muap98.sor_len)
                {
                    // ソーステキストが終了しているか
                    theend();
                }
                work.col = r.bx - work.oldbx + 1;
                r.al = muap98.source_Buf[r.bx];
                r.bx++;
                if (r.al == 0xff)
                {// EOF コードか
                    theend();
                }

                // スペース、タブは飛ばす
                if ((r.al != (byte)' ') && (r.al != (byte)9))
                {
                    if (r.al != 0xfe)// 0x0d || muap98.source_Buf[r.bx]!=0x0a)
                    {
                        // CR,LF コードなら行カウンタを増やす
                        break;
                    }
                    //r.bx++;
                    //chktxt3:
                    mucomsub.check_flatclear();// 改行時の臨時調号クリア
                    linedta++;
                    work.row++;
                    work.oldbx = r.bx;
                }
                //chktxt2:
                macrof = 0;// "$" 指定フラグ解除
            } while (true);

            //getdata:
            if (r.al == (byte)';')
            {
                // コメント文も無視
                skip_rem();
                mucomsub.check_flatclear();// 改行時の臨時調号クリア
                linedta++;
                work.row++;
                work.oldbx = r.bx;
                //chktxt2:
                macrof = 0;// "$" 指定フラグ解除
                goto chktxt3_;
            }

            if (r.al == (byte)'$')
            {
                macrof ^= 1;// "$" 指定フラグをたてる
            }
            r.push(r.dx);
            r.dh = (byte)(r.bx - 2 < 0 ? 0 : muap98.source_Buf[r.bx - 2]);// 前の文字
            r.dl = r.al;
            testknj();// 漢字2バイト目チェック
            r.dx = r.pop();
            bxsave = r.bx;// テキスト番地の保存
            Log.writeLine(LogLevel.TRACE, string.Format("{0}(${1:X02}) {2}行目 di:{3:X04}", (char)r.al, r.al, linedta,r.di));
            return;
        }

        private void skip_rem()
        {
            do
            {
                r.al = muap98.source_Buf[r.bx];
                r.bx++;

                if (r.bx == muap98.source_Buf.Length)
                {
                    // EOFのチェック
                    theend();
                }
            } while (r.al != 0xfe);// 1行終了までスキップ

            //r.bx++;
            if (r.bx == muap98.source_Buf.Length)
            {
                // EOFのチェック
                theend();
            }

            return;
        }

        //;----------------------------------------------
        //;	チャネル番号検索用テキスト読みとり
        //;----------------------------------------------

        private bool chkpart()
        {
            do
            {
                chktxt();
                //    pushf
                if (macrof == 0)
                {
                    break;
                }
                //    popf
            } while (true);
            //chkpart1:
            //    popf
            return false;
        }

        //;-------------------------------------------------------
        //;	バッファ領域の最後、EOFに到達したときの処理
        //;-------------------------------------------------------

        /// <summary>
        /// 戻り先はthrowで対処
        /// </summary>
        /// <returns></returns>
        public void theend()
        {
            r.sp = spsave;
            r.ch = ope_no;// 本来のチャネル番号
            r.zero = (nesting == 0);// バッファ終了またはEOF
            r.cl = 13;// ループネスト異常終了
            if (!r.zero)
            {
                error(); return;
            }
            r.cl = 35;
            if (jumpnes != 0)// @if exit/then のネスト
            {
                //error19:
                error(); return;
            }

            //theend1:
            r.al = 0xfc;// ストップコードを格納しておく
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);

            check_calplay();// cal* 呼び出し?
            if (r.zero)
            {
                // オブジェクト量の表示
                r.push((ushort)(locatex | (locatey << 8)));
                r.ax = 0;
                r.al = r.ch;
                r.al--;
                r.cl = 5;
                r.div(r.cl);// AL = 縦位置, AH = 横位置
                r.al += MYPOS + 1;
                locatey = r.al;// 縦位置の設定
                r.al = r.ah;
                r.al <<= 3;
                r.al += MXPOS + 16;
                locatex = r.al;
                r.ax = r.di;
                r.ax -= bef_len;// 前チャネルとのデータ量の差
                bef_len = r.di;
                attr = 0xe1;
                r.test(ifflag, 1);// @if thenなどを実行した場合
                if (!r.zero)
                {
                    attr = 0x81;
                }
                //exeif1:
                r.dx = alllen[2 / 2];
                r.ax = alllen[0 / 2];
                r.zero = r.dx == maxlen[2 / 2];
                r.carry = r.dx < maxlen[2 / 2];
                if (!r.carry)
                {
                    bool flg1 = false;
                    if (r.zero)
                    {
                        if (r.ax <= maxlen[0 / 2])
                        {
                            flg1 = true;
                        }
                    }
                    if (!flg1)
                    {
                        //maxchk2:
                        maxlen[0 / 2] = r.ax;
                        maxlen[2 / 2] = r.dx;
                    }
                }
                //maxchk1:
                r.zero = r.dx == 2;
                r.carry = r.dx < 2;
                if (!r.carry)
                {
                    if (r.carry || r.zero)
                    {
                        if (r.ax <= 0xed40)// 999*192
                        {
                            goto divsafe1;
                        }
                    }
                    //divsafe3:
                    r.push(r.ds);
                    r.dx = 0;//ofs:mess_2
                    putword(mess_2);// over表示
                    r.ds = r.pop();
                    goto divsafe2;
                }
            divsafe1:
                if (work.compilerInfo.totalCount == null) work.compilerInfo.totalCount = new System.Collections.Generic.List<int>();
                work.compilerInfo.totalCount.Add((r.dx << 16) + r.ax);
                r.bx = 192;
                r.div(r.bx);
                //dsp4dec();// 全音符の数
                int zen = r.ax;
                r.ax = r.dx;
                r.dl = (byte)'.';
                //putchr(r.dl);
                zero = 1;
                //dsp3dec();// 端数
                putstr("#{0,-2} : {1,4:D}.{2:D3}", work.compilerInfo.totalCount.Count, zen, r.ax);
                zero = 0;
            divsafe2:
                ushort ans = r.pop();
                locatex = (byte)ans;
                locatey = (byte)(ans >> 8);
            }

            //selcal4:
            r.push(r.cx);
            r.bx = disave;
            r.bx = (ushort)((byte)muap98.object_Buf[r.bx].dat
                | ((byte)muap98.object_Buf[r.bx + 1].dat << 8));// このチャネルの演奏データの最初の番地
            r.ch = 0;// ローカルラベルの指定
            set_labeladrs();// パス２の実行
            r.cx = r.pop();
            r.ch++;// 次のチャネルへ
            if (r.ch > 17)
            {
                throw new MusCompileEndException();// compile_end; check end(di = next end address)
            }
            r.bx = disave;
            r.bx += 2;
            disave = r.bx;// 次の演奏格納番地を設定
            muap98.object_Buf[r.bx] = new MmlDatum((byte)r.di);
            muap98.object_Buf[r.bx + 1] = new MmlDatum((byte)(r.di >> 8));

            throw new MusRecov8Exception();
        }

        //;-----------------------------------------------
        //;	@LABEL 開始番地のセット(パス２)
        //; entry BX = 検索開始番地
        //; DI = 検索終了番地
        //;		CH = 0 : ローカルラベル(0～31)
        //; CH = 1 : 共通ラベル(32～39)
        //;-----------------------------------------------

        private void set_labeladrs()
        {
            r.pushA();
            r.push(r.es);
            linedta = 0;// 行数は決定できない
            work.row = -1;
            work.col = -1;

            //search1:
            do
            {
                if (r.bx >= r.di)
                {
                    // このチャネルの終了番地まで
                    setad0();
                    return;
                }
                r.ax = (ushort)((byte)muap98.object_Buf[r.bx].dat | ((byte)muap98.object_Buf[r.bx + 1].dat << 8));
                r.bx++;

                if (r.al <= 0x3f)
                {
                    // 00-3F は音符データ
                    //onpu:
                    r.bx++;// 音符コードの時は1byteスキップ
                    continue;
                }

                if (r.al == 0x80)
                {
                    // @if then
                    find_then();
                    continue;
                }

                if (r.al == 0x81)
                {
                    // @if exit
                    find_exit();
                    continue;
                }

                if (r.al == 0x89)
                {
                    // @call32～39
                    find_gljp();
                    continue;
                }

                if (r.al == 0x8a)
                {
                    // @jump32～39
                    find_gljp();
                    continue;
                }

                if (r.al == 0xea)
                {
                    // @jump コマンドのオフセット設定
                    find_jp();
                    continue;
                }

                if (r.al == 0xe9)
                {
                    // @call コマンド
                    find_jp();
                    continue;
                }

                if (r.al == 0xe4)
                {
                    // @if jump
                    find_if();
                    continue;
                }

                if (r.al == 0xe3)
                {
                    // @if call
                    find_if();
                    continue;
                }

                if (r.al == 0xdb)
                {
                    // @com" "
                    find_com();
                    //onpu:
                    r.bx++;// 音符コードの時は1byteスキップ
                    continue;
                }

                r.si = 0xffff;//ofs:skipbyte-1
                r.ax = (ushort)(short)(sbyte)r.al;
                r.si -= r.ax;// 各制御コードに関するスキップ数を獲得
                r.dl = skipbyte[r.si];
                r.dh = 0;

                r.bx += r.dx;// スキップ数を加算
            } while (true);

        }

        //;･････････････････････････････････････
        //;	グローバルジャンプの設定
        //;･････････････････････････････････････

        private void find_gljp()
        {
            muap98.object_Buf[r.bx - 1].dat |= 0x60;// 89→E9,8A→EAに戻す
            r.al = (byte)muap98.object_Buf[r.bx].dat;// 変数番号を獲得

            //local3:
            r.si = 0;//ofs:bufbuf
            r.ah = 0;
            r.ax += r.ax;
            r.si += r.ax;
            r.ax = (ushort)(muap98.bufbuf[r.si] | (muap98.bufbuf[r.si + 1] << 8));// 変数番号の @labelの番地を読み取る
            r.zero = r.ax == 0;
            r.cl = 26;// @label 未設定
            if (r.zero) { error(); return; }
            r.ax -= r.bx;
            r.ax++;// オフセット値
            r.zero = r.ax == 0xfffd;
            r.cl = 27;// 無限ループ
            if (r.zero) { error(); return; }
            muap98.object_Buf[r.bx] = new MmlDatum(r.al);
            muap98.object_Buf[r.bx + 1] = new MmlDatum(r.ah);
            //local2:
            r.bx += 2;
            return;//jmp search1
        }

        //;･････････････････････････････････････
        //;	分岐命令の飛び先番地設定
        //;･････････････････････････････････････

        private void find_then()
        {
            r.al = 0xe4;
            //set_jump:
            muap98.object_Buf[r.bx - 1] = new MmlDatum(r.al);
            r.bx += 4;// @if jump 命令に書き換える
            return;
        }

        private void find_exit()
        {
            r.al = 0xd3;// @if jump 命令に書き換える
            //set_jump:
            muap98.object_Buf[r.bx - 1] = new MmlDatum(r.al);
            r.bx += 4;// @if jump 命令に書き換える
            return;
        }

        private void find_if()
        {
            r.bx += 2;
            find_jp();
        }

        private void find_jp()
        {
            if ((byte)muap98.object_Buf[r.bx + 2].dat != 0x88)// ユーザPCM登録用は無視する
            {
                r.al = (byte)muap98.object_Buf[r.bx].dat;// 変数番号を獲得
                if (r.ch == 0)// 共通の時は無視する
                {
                    if (r.al > 31)
                    {
                        // ローカルの時は32～39のみ別処理
                        muap98.object_Buf[r.bx - 1].dat &= 0x9f;// EA→8A,E9→89に変更する
                    }
                    else
                    {
                        //local3:
                        r.si = 0;//ofs:bufbuf
                        r.ah = 0;
                        r.ax += r.ax;
                        r.si += r.ax;
                        r.ax = (ushort)(muap98.bufbuf[r.si] | (muap98.bufbuf[r.si + 1] << 8));// 変数番号の @labelの番地を読み取る
                        r.zero = r.ax == 0;
                        r.cl = 26;// @label 未設定
                        if (r.zero) { error(); return; }
                        r.ax -= r.bx;
                        r.ax++;// オフセット値
                        r.zero = r.ax == 0xfffd;
                        r.cl = 27;// 無限ループ
                        if (r.zero) { error(); return; }
                        muap98.object_Buf[r.bx] = new MmlDatum(r.al);
                        muap98.object_Buf[r.bx + 1] = new MmlDatum(r.ah);
                    }
                }
                //local2:
                r.bx += 2;
                return;//jmp search1
            }

            //skip_jp:
            r.ax = (ushort)((byte)muap98.object_Buf[r.bx].dat | ((byte)muap98.object_Buf[r.bx + 1].dat << 8));// AX = 無視するバイト数
            r.ax--;
            r.bx += r.ax;// ファイル名など全て無視する
            return;//jmp search1
        }

        private void find_com()
        {
            r.bx += 2;
            r.al = (byte)muap98.object_Buf[r.bx].dat;// 歌詞文字列長
            r.ah = 0;
            r.bx += r.ax;// 文字列の長さだけスキップ
            return;// onpu
        }

        private void setad0()
        {
            r.es = r.pop();
            r.popA();
            return;
        }

        //;----------------------------
        //;	全チャネル終了
        //;----------------------------

        //	assume cs:main,ds:main
        private void compile_end()
        {
            r.bx = OBJTOP;// BX = 演奏データの先頭
            r.ch = 1;// 共通ラベルの指定
            set_labeladrs();// パス３の実行
            r.ds = r.cs;
            muap98.obj_len = r.di;// 演奏データ長を格納
            r.al = symbol2;
            muap98.object_Buf[SYMDTA] = new MmlDatum(r.al);// ES:[22h] にシンボル情報の有無をセット
            r.ax = maxlen[0];// ES:[26h～29h] に最大音長を格納
            muap98.object_Buf[0x26] = new MmlDatum(r.al);
            muap98.object_Buf[0x27] = new MmlDatum(r.ah);
            r.ax = maxlen[2 / 2];
            muap98.object_Buf[0x28] = new MmlDatum(r.al);
            muap98.object_Buf[0x29] = new MmlDatum(r.ah);

            //;--------------------------------
            //;	終了メッセージの表示
            //;--------------------------------
            check_calplay();// cal* 呼び出し?
            if (r.zero)
            {
                locatex = MXPOS + 1;
                locatey = MYPOS + 6;
                r.dx = 0;//ofs:mess_7
                putword(mess_7);
                r.ax = r.di;
                //dsp5decl();// 同じく、表示
                putstr("{0,5}", r.ax);
                r.dx = 0;//ofs:mess_8
                putword(mess_8);

                muap98.object_Buf.RemoveAll(r.ax);
            }
            //com_end3:
            r.cx = 0;// CX = エラーフラグ
            //com_end_abort:
            bxsave = 0xffff;
            com_end2();
        }

        private void com_end2()
        {
            check_calplay();// cal* 呼び出し?
            if (r.zero)
            {
                if ((menu.crflag & 0x40) == 0) // 連続モードでは待たない
                {
                    if ((r.cl != 0)|| ((symbol2 & 4) == 0))
                    {
                        // エラー時は待機
                        //selcal6:
                        menu.check_visualplay();// VisualPlay実行中?
                        if (r.zero)
                        {
                            r.ah = 0;// キー入力待ち
                            pc98_Int18();
                        }
                    }
                }
                //com_skip1:
                r.dx = 0;
                load_text();// 画面復活･プロセス終了
            }

            //selcal5:
            init_work();
            return;
        }

        //;================================
        //;	エラー処理ルーチン
        //;================================

        public void error()
        {
            r.ds = r.cs;
            r.es = r.cs;
            r.sp = spsave;
            clbuff();
            check_calplay();// cal* 呼び出し?

            string errMsg = "";
            if (r.zero)
            {
                //c94:
                attr = 0xe1;
                r.di = 0;//ofs:bufbuf	; DS:DI = エラーメッセージ格納番地
                r.push(r.di);
                stamode = 1;
                r.ah = 0;
                r.al = r.cl;// AX = エラー番号
                dsp2dec();// bufbufにエラー番号を格納
                stamode = 0;
                r.al = (byte)'$';
                muap98.bufbuf[r.di] = r.al;
                r.di++;
                r.di = r.pop();
                r.ah = 1;
                call_func();// CALL側でbufbufにエラーメッセージを転送

                //error_disp1:
                //do
                //{
                locatex = MXPOS + 1;
                locatey = MYPOS + 6;
                r.dx = r.di;
                //putword(calerror1[r.cl - 1]);// エラーメッセージの表示
                errMsg = calerror1[r.cl - 1].Replace("$","");

                r.dx = 0;//ofs:mess_5
                //putword(mess_5);
                errMsg += mess_5.Replace("$", "");

                r.al = attr;
                r.ah = r.al;
                r.al >>= 5;// AL = 色番号(0-7)

                r.al++;// 次の色へ
                r.al <<= 5;
                r.ah &= 0x1f;
                r.al |= r.ah;
                attr = r.al;

                r.ah = 1;
                pc98_Int18();

                //} while (r.bh == 0);
            }

            //selcal3:
            r.cl = 1;// エラーフラグを立てる
            com_end2();

            string msg= string.Format("Error {0} at line {1}", errMsg, linedta);
            if (work.compilerInfo == null) work.compilerInfo = new CompilerInfo();
            work.compilerInfo.errorList.Add(new Tuple<int, int, string>(linedta - 1, -1, msg));
            throw new MusException(string.Format("{0} in {1}", errMsg, linedta));
        }

        //;========================================================
        //;	変換メインルーチン
        //;	ES:DI = object address >> end address
        //; DS:BX = source address >> next source address
        //;	   AL = text data
        //; CH = channel data(1-17)
        //;========================================================

        //	assume nothing, cs:main
        public void com_main()
        {
            work.md = null;
            r.push(r.di);

            r.ah = (byte)(r.bx >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[r.bx]);// 次の文字も獲得
            if (mac_mod != 0)// 小文字／大文字マクロモードか
            {
                if ((mac_mod & 2) == 0)
                {
                    //com4:
                    if ((r.al >= (byte)'a') && (r.al <= (byte)'z'))// 小文字の範囲か
                    {
                        //com5:
                        r.di = r.pop();
                        mucomsub.macro_exec();// １文字マクロモードへ
                        return;
                    }
                }
                else
                {
                    if ((r.al >= (byte)'A') && (r.al <= (byte)'Z'))// 大文字の範囲か
                    {
                        //com5:
                        r.di = r.pop();
                        mucomsub.macro_exec();// １文字マクロモードへ
                        return;
                    }
                }
            }

            //com3:
            r.cl = 32;
            if (r.al <= (byte)' ')// 制御文字はエラー
            {
                error();
                return;
            }

            if (r.al > (byte)'~')
            {
                // GRPH､かなはエラー
                error();
                return;
            }

            if (r.al <= (byte)'z')
            {
                if (r.al >= (byte)'a')
                {
                    r.al -= (byte)'a' - (byte)'A';// 小文字＞大文字変換
                }
            }
            else
            {
                //com1:
                r.al -= (byte)'{' - 0x60;// {|}~ = 60,61,62,63h に変換
            }
            //com2:
            r.push(r.ax);
            r.al -= (byte)'!';// !を0にする
            r.al += r.al;
            r.di = 0;//ofs:jpdata
            r.ah = 0;
            r.di += r.ax;
            r.dx = r.di;
            r.ax = r.pop();
            r.di = r.pop();

            //for (int j = 0; j < 32; j++)
            //{
            //    string hex = "";
            //    for (int i = 0; i < 16; i++)
            //    {
            //        hex += string.Format("{0:X02} ", muap98.object_Buf[i + j * 16]);
            //    }
            //    Log.WriteLine(LogLevel.TRACE, hex);
            //}

            Log.WriteLine(LogLevel.TRACE, "com index {0}",r.dx/2);
            jpdata[r.dx / 2]();// 各処理ルーチンへ分岐
        }

        private Action[] jpdata;
        private void InitJpdata()
        {
            jpdata = new Action[]{
                acc,stak,error,mucomsub.dtcall,                          //0  !"#$
                error,tie,mucomsub.wait_r,mucomsub.nloop1,               //4  %&'(
                mucomsub.nloop2,mloop,error,error,                       //8  )*+,
                error,error,mucomsub.harm_onpu,error,                    //12 -./0
                error,error,error,error,                                 //16 1234
                error,error,error,error,                                 //20 5678
                error,error,error,octdown,                               //24 9:;<
                error,octup,unacc,mucomsub.exp_cmd,                      //28 =>?@
                unexst,unexst,unexst,unexst,                             //32 ABCD
                unexst,unexst,unexst,mucomsub.reghex,                    //36 EFGH
                error,error,mucomsub.rhyexp,mlength,                     //40 IJKL
                mucomsub.env_speed,mucomsub.noise,oct,mucomsub.envelope, //44 MNOP
                ratio,rest,mucomsub.env_type,tempo,                      //48 QRST
                error,volume,mucomsub.porta,value,                       //52 UVWX
                mucomsub.reg,mucomsub.usr_tone,error,error,              //56 YZ[\
                error,error,mucomsub.icho,renpu,                         //60 ]^_{
                error,error,tie2                                         //64 |}~
            };
        }

        //;==============================
        //;	音符コードの処理
        //;==============================

        private void unexst()
        {
            LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
            linePos.chip = work.crntChip;
            linePos.chipNumber = 0;
            linePos.ch = (byte)work.crntChannel;
            linePos.part = work.crntPart;
            int clk = 0;
            List<object> args = new List<object>();
            args.Add(0);
            args.Add(clk);
            MmlDatum md = new MmlDatum(enmMMLType.Note, args, linePos, clk);
            work.md = work.FlashLstMd(md);

            set_symbol2();// ソース番地の格納
            check_comlen();// 色変わり歌詞のチェック
            if (codemod != 0)
            {
                code_change();// コード指定へ
                return;
            }
            if (r.ch == 10)// リズム音源か
            {
                mucomsub.rhyexp();
                return;
            }
            r.push(r.ax);// CDEFGABのみ
            r.al = muap98.source_Buf[r.bx];
            gethenon();// #+-%をチェックしてDLに返す
            tnelnmx();// 音長チェック(AL)
            lensave = r.al;// 音長を保存
            work.md.args[work.mdArgsStep + 1] = work.otoLength;
            r.ax = r.pop();// AL = 中間コード DL = 変音データ

            if (harmno != 0)
            {
                harm_mode();// 和音モードのチェック
                return;
            }
            if (arpharm != 0)
            {
                arp_press0();// 縮小アルペジョのチェック
                return;
            }

            r.push(r.ax);
            r.al = lensave;// 音長を格納する
            tnelnx();
            r.ax = r.pop();
            mucomsub.read();// 音符から実際の設定値を計算
            reset_arp();
        }
        private void reset_arp()
        {
            arpmode = 0;// アルペジョモードのクリア
            return;
        }

        //;-----------------------------
        //;	ソース番地の格納
        //;-----------------------------

        public void set_symbol2()
        {
            r.push(r.ax);
            r.al = symbol2;

            r.test(r.al, 1);
            if (!r.zero)
            {
                r.test(r.al, 2);
                // マクロ内部も無条件出力
                // マクロ中か
                if (r.zero || nest2 == 0)
                {
                    //not_sym2:
                    r.al = 0xe7;// シンボリック情報コード
                    muap98.object_Buf[r.di++] = new MmlDatum(r.al);
                    r.ax = r.bx;
                    r.ax--;
                    muap98.object_Buf[r.di++] = new MmlDatum(r.al);
                    muap98.object_Buf[r.di++] = new MmlDatum(r.ah);// ソースデータの現在値を出力
                }
            }

            //not_sym1:
            r.ax = r.pop();
            return;
        }

        //;---------------------------------
        //;	和音演奏処理
        //;	entry AL = CDEFGAB
        //; DL = 変音データ
        //;---------------------------------

        private void harm_mode()
        {
            harm_main();
            if (!r.carry)
            {
                set_honpu();
                return;
            }
            r.dl = lensave;
            kyufu();// 音長､圧縮式休符(FF)を格納
            reset_arp();// アルペジョモードのクリア
        }

        //;---------------------------
        //;	和音音符の格納
        //;---------------------------

        private void set_honpu()
        {
            if (arpmode != 0)
            {
                // アルペジョモードか
                r.dl = arpharm;// 縮小アルペジョモードか
                if (r.dl != 0)
                {
                    arp_press();
                    return;
                }
                get_arprest();
            }

            //set_hon1:
            r.push(octdata);// オクターブ値は保存しておく
            get_harm();
            r.push(r.ax);
            r.al = muap98.source_Buf[r.bx];
            gethenon();// 調音記号チェック
            r.al = lensave;
            tnelnx();// 音長の格納(最初に指定された物)
            r.ax = r.pop();
            mucomsub.read();// 和音の音符格納
            octdata = (byte)r.pop();
            mucomsub.harm_onpu();// コロンの次へ
            reset_arp();// アルペジョモードのクリア
            return;
        }

        private void harm_rest()
        {
            r.dl = lensave;
            kyufu();// 音長､圧縮式休符(FF)を格納
            mucomsub.harm_onpu();
            reset_arp();// アルペジョモードのクリア
        }

        //;----------------------------------------
        //;	縮小アルペジョモードの解析
        //;	entry AL = 次のテキスト文字
        //; DL = [arpharm]
        //;----------------------------------------

        private void arp_press0()
        {
            r.push(octdata);// オクターブ値は保存しておく
            r.push(r.ax);
            int ret=arpp6();
            if (ret != 0) arpp4();
        }

        private void arp_press()
        {
            if (r.dl <= harmno)
            {
                // 現在の和音モードは範囲外か
                harm_rest();// 全て休符を格納する
                return;
            }
            get_arprest();// 最初の休符を格納
            arpp4();
        }

        private void arpp4()
        {
            int ret = 0;
            do
            {
                r.push(octdata);// オクターブ値は保存しておく
                get_harm();
                r.push(r.ax);// AL = 音符コード
                r.al = muap98.source_Buf[r.bx];
                gethenon();// 調音記号チェック
                ret = arpp6();
            } while (ret != 0);
        }

        private int arpp6()
        {
            r.push(r.dx);
            r.al = arpharm;
            get_defarp();// DL = アルペジョ音長
            r.mul(r.dl);// AL = 発音する音長
            r.dx = r.pop();
            r.carry = lensave < r.al;
            lensave -= r.al;// 残りの音長を計算
            r.cl = 15;
            if (r.carry)
            {
                error();return 0;// 音長が不足
            }
            r.si = r.di;// SI = 直前に音長を格納した番地
            tnelnx();// 音長の格納(最初に指定された物)
            r.ax = r.pop();
            mucomsub.read();// 和音の音符格納
            octdata = (byte)r.pop();
            r.dl = arpharm;// DLの数だけ"/"を飛ばす
            arpp3:
            r.al = muap98.source_Buf[r.bx];// 指定先の音符まで移動
            mucomsub.xsmall();// 大文字変換
            r.bx++;
            r.cl = 32;
            if (r.al >= 0xfe)
            {
                // CR,LF,EOF なら error end
                error();return 0;
            }

            if (r.al != (byte)':')
            {
                if (r.al != (byte)'/')
                {
                    if (r.al == (byte)'@')
                    {
                        r.push(r.dx);
                        r.push(r.si);
                        r.push(octdata);
                        mode[0] |= 4;
                        mucomsub.exp_cmd();// @ 系コマンドの実行(@+,@-,@%)
                        octdata = (byte)r.pop();
                        r.si = r.pop();
                        r.dx = r.pop();
                    }
                    goto arpp3;
                }
                //arpp2:
                r.dl--;// "/"が指定個数あるまで繰り返す
                if (r.dl != 0)
                {
                    goto arpp3;
                }
                return 1;// arpp4へ// 次の音符を格納
            }
            //arpp1:
            r.al = (byte)muap98.object_Buf[r.si].dat;
            r.ah = (byte)muap98.object_Buf[r.si+1].dat;// 音符がもう存在しなかった場合
            if (r.al != 0xf4)
            {
                // 音長コードが格納されているか
                if (r.al == 0xfa)
                {
                    // 複数ディチューンモードか
                    r.push(r.si);
                    r.push(r.cx);
                    r.cx = 9;
                    //arpp8:
                    do
                    {
                        r.al = (byte)muap98.object_Buf[r.si + 8].dat;
                        muap98.object_Buf[r.si + 11] = new MmlDatum(r.al);// 9バイト移動する
                        r.si--;
                        r.cx--;
                    } while (r.cx != 0);
                    r.cx = r.pop();
                    r.si = r.pop();
                }
                else
                {
                    //arpp7:
                    muap98.object_Buf[r.si + 3] = new MmlDatum(r.al);
                    muap98.object_Buf[r.si + 4] = new MmlDatum(r.ah);// 音符コード（2バイト）を移動
                }
                //arpp9:
                muap98.object_Buf[r.si] = new MmlDatum(0xf4);
            }
            //arpp5:
            r.di = r.si;
            get_defarp();
            r.al = r.dl;
            r.mul(arpharm);
            r.al += lensave;// AL = 残りの音長
            setrat();
            r.push(r.ax);
            r.push(r.dx);
            get_defarp();
            r.al -= r.dl;
            r.al -= r.dl;
            add_tlen();
            r.dx = r.pop();
            r.ax = r.pop();
            r.al = (byte)muap98.object_Buf[r.di].dat;
            if (r.al == 0xfa)
            {
                // 複数ディチューン音符データか
                r.di += 7;
            }
            r.di += 2;
            reset_arp();// アルペジョモードのクリア
            return 0;
        }

        //;･･･････････････････････････････････････
        //;	和音の音符獲得(<>,@の実行)
        //;･･･････････････････････････････････････

        public void get_harm()
        {
            mucomsub.skipoct();// >,< コマンドを実行
            if (r.al == (byte)'@')
            {
                mode[0] |= 4;
                mucomsub.exp_cmd();// @ 系コマンドを実行
                chktxt();
                mucomsub.xsmall();// 大文字変換
            }
            //seth1:
            mucomsub.check_onpu();// 音符以外は禁止
        }

        //;･･･････････････････････････････････････
        //;	アルペジョ最初の休符を格納
        //;	exit	DL = 休符の音長
        //;		[lensave] = 残りの音長
        //;･･･････････････････････････････････････

        private void get_arprest()
        {
            r.push(r.ax);
            r.al = lensave;// 音長を獲得
            r.push(r.ax);
            r.al = harmno;// AL = 和音番号(1,2･･･)
            get_defarp();// DL = アルペジョ音長
            r.mul(r.dl);// AL = 休符する長さ
            add_tlen();
            r.dl = r.al;
            kyufu();// 圧縮式休符の格納(DL)
            r.ax = r.pop();// AL = 全体の音長
            r.carry = r.al < r.dl;
            r.al -= r.dl;// AL = 残りの音長
            r.cl = 15;
            if (r.carry)
            {
                // 音長が不足
                error();return;
            }
            lensave = r.al;// 音符の保存音長を格納
            r.ax = r.pop();
            return;
        }

        private void get_defarp()
        {
            r.dl = arplen;// アルペジョする長さ
            if (r.dl == 0)
            {
                r.dl = trildef;// デフォルトはトリル速度を使用する
            }
            return;
        }

        //;-------------------------------------
        //;	和音の存在チェック
        //;	entry	BX = ソーステキスト
        //;	exit	CY = 和音無し
        //;-------------------------------------

        public void harm_main()
        {
            mucomsub.skipoct();// <>コマンドの実行
            r.bx--;
            r.push(r.bx);
            chktxt();// AL = 次のテキスト
            if (r.al != (byte)'/')
            {
                // 和音指定はないので休符をセット
                //make_rest:
                r.bx = r.pop();
                //make_rest1:
                r.carry = true;
                return;
            }
            r.dx = r.pop();
            r.dl = harmno;// "/"のスキップ回数
        harmm2:
            r.dl--;
            if (r.dl == 0)
            {
                //harm_exsist:
                r.carry = false;// 和音は存在した
                return;
            }
            //harmm1:
            do
            {
                chktxt();
                if (r.al == (byte)':')
                {
                    //make_rest1:
                    r.carry = true;
                    return;
                }
                if (r.al != (byte)'@')
                {
                    //harmm3:
                    if (r.al != (byte)'/')
                    {
                        continue;
                    }
                    goto harmm2;
                }
                r.push(r.dx);
                r.push(octdata);
                mode[0] |= 4;// <>の後にBXを移動するフラグ
                mucomsub.exp_cmd();// @+,@-,@% の実行
                octdata = (byte)r.pop();
                r.dx = r.pop();
            } while (true);
        }

        //;----------------------------
        //;	アルペジョ処理
        //;----------------------------

        public void arpeggio()
        {
            arpmode = 1;
            arplen = 0;// デフォルト値を使用
            r.al = muap98.source_Buf[r.bx];
            mucomsub.chknum();// 数字チェック
            if (!r.carry)
            {
                tnelnmx();// 音長データの獲得
                arplen = r.al;
            }
            //arp1:
            r.al = muap98.source_Buf[r.bx];
            r.zero = r.al == (byte)',';// 縮小アルペジョの指定か
            r.al = 0;
            if (!r.zero)
            {
                //arp2:
                arpharm = r.al;
                return;
            }
            r.bx++;
            rednums();// AL = 展開する和音数
            //arp2:
            arpharm = r.al;
            return;
        }

        //;-------------------------------------
        //;	コード指定による変換処理
        //;-------------------------------------

        private void code_change()
        {
            get_keycode();// AL = 音符データ
            bassdta = r.al;
            r.push(r.ax);// AL = 中間コード
            r.push(r.bx);
            r.cl = 9;// エラーコード
            r.si = 0;//ofs:codedta
            r.dx = 0;// コード番号

            //codem2:
            do
            {
                r.al = muap98.source_Buf[r.bx];
                r.ah = muap98.source_Buf[r.bx + 1];
                if (r.al == (byte)'o' && r.al == (byte)'n')
                {
                    // ベース別指定か
                    //codebase:
                    r.bx++;// onX のチェック
                    r.bx++;
                    r.al = muap98.source_Buf[r.bx];
                    mucomsub.xsmall();// ALの大文字変換
                    mucomsub.check_onpu();// Xが音符か
                    r.bx++;// AL = 音符コード(onX)
                    get_keycode();// AL = 中間データ
                    bassdta = r.al;
                    if (muap98.source_Buf[r.bx] != (byte)':')
                    {
                        //to_err:
                        error(); return;
                    }
                    r.bx++;
                    break;
                }
                if (r.al != (byte)codedta[r.si])
                {
                    //codem4:
                    do
                    {
                        if (r.al == 0)
                        {
                            // なかった
                            //to_err:
                            error(); return;
                        }
                        r.al = (byte)codedta[r.si];// 次の検索文字まで移動
                        r.si++;
                    } while (r.al != (byte)':');
                    r.bx = r.pop();// ソース番地を戻す
                    r.push(r.bx);
                    r.dx++;// 次のコード番号
                    continue;
                }
                r.si++;
                r.bx++;
            } while (r.al != (byte)':');// 最後の文字か

            //codem5:
            r.ax = r.pop();// BXを戻さない
            r.ax = r.pop();// AL = 中間コード
            if (r.al < bassdta)
            {
                // ベース音と比較
                r.al += 12;
            }
            onkai = r.al;
            codesav = r.dx;// DX = コード番号(0-31)

            codem0();// 文字列を発見(次の処理へ)
        }

        //;-------------------------------------------
        //;	キーコードの獲得
        //;	entry	AL = CDEFGAB
        //;		DS:[BX] = 調音記号(#,+,-)
        //;	exit	AL = 中間コード(0-11)
        //;-------------------------------------------

        private void get_keycode()
        {
            r.push(r.dx);
            r.push(r.ax);
            r.al = muap98.source_Buf[r.bx];
            gethenon();// 調音記号のチェック(DL)
            r.carry = r.dl < 3;
            r.cl = 7;
            if (!r.carry)
            {
                // %,##,--は指定できない
                //to_err:
                error(); return;
            }
            r.ax = r.pop();

            r.si = 0;//ofs:musdata
            r.ah = 0;
            r.al -= (byte)'A';
            r.si += r.ax;
            r.al = musdata[r.si];// AL = 中間コード(0-11)
            r.dl--;
            if (r.dl == 0)
            {
                r.al++;// 嬰音の指定
            }
            r.dl--;
            if (r.dl == 0)
            {
                r.al--;// 変音の指定
            }
            r.dx = r.pop();
            return;
        }

        private string codedta = ":m:6:m6:"// C,Cm,C6,Cm6
        + "7:m7:M7:mM7:"// C7,Cm7,CM7,CmM7
        + "sus4:7sus4:(+5):(-5):"// Csus4,C7sus4,C(+5),C(-5)
        + "7(+5):7(-5):m7(-5):dim:"// C7(+5),C7(-5),Cm7(-5),Cdim
        + "add9:madd9:69:m69:"// Cadd9,Cmadd9,C69,Cm69
        + "7(+9):7(-9):9:m9:"// C7(+9),C7(-9),C9,Cm9
        + "9(+5):9(-5):M9:mM9:"// C9(+5),C9(-5),CM9,CmM9
        + "11:m11:9(+11):13:";// C11,Cm11,C9(+11),C13

        private byte[] codetne = new byte[]{
            0x00,0x04,0x07,0xff, 0x00,0x03,0x07,0xff, 0x00,0x04,0x07,0x09, 0x00,0x03,0x07,0x09,
            0x00,0x04,0x07,0x0a, 0x00,0x03,0x07,0x0a, 0x00,0x04,0x07,0x0b, 0x00,0x03,0x07,0x0b,
            0x00,0x05,0x07,0xff, 0x00,0x05,0x07,0x0a, 0x00,0x04,0x08,0xff, 0x00,0x04,0x06,0xff,
            0x00,0x04,0x08,0x0a, 0x00,0x04,0x06,0x0a, 0x00,0x03,0x06,0x0a, 0x00,0x03,0x06,0x09,
            0x00,0x04,0x07,0x0e, 0x00,0x03,0x07,0x0e, 0x04,0x09,0x0e,0xff, 0x03,0x09,0x0e,0xff,
            0x04,0x0a,0x0f,0xff, 0x04,0x0a,0x0d,0xff, 0x04,0x0a,0x0e,0xff, 0x03,0x0a,0x0e,0xff,
            0x04,0x08,0x0a,0x0e, 0x04,0x06,0x0a,0x0e, 0x04,0x07,0x0b,0x0e, 0x03,0x07,0x0b,0x0e,
            0x0a,0x10,0x11,0xff, 0x0a,0x0f,0x11,0xff, 0x04,0x06,0x0a,0x0e, 0x04,0x09,0x0a,0x0e
        };

        //;-----------------------------------------------
        //;	コード解析後の処理
        //;	entry	onkai = 根音
        //;		bassdta = ベース音
        //;		codesav = コード番号(0-31)
        //;-----------------------------------------------

        private void codem0()
        {
            tnelnmx();// 音長の解析
            rtm_max = r.al;

            mode[0] &= 0xfd;// @rhythmによる同音階出力フラグのクリア

            //rhy_init1:
            do
            {
                r.dl = rtm_max;
                getrhythm_table();// AL = リズム音長
                if (r.zero)
                {
                    norm_code();// リズムモードでなかった
                    return;
                }
                if (r.dl <= r.al)
                {
                    rend5();// 分割の最後
                    return;
                }
                r.dl -= r.al;
                rtm_max = r.dl;
                rend4();// 音長､E2FFの格納(次のリズムへ)
            } while (true);
        }

        //;	リズムモードの音符,休符格納

        private void rend5()
        {
            r.al = r.dl;// 全体の残りを音長にする
            rend4();
        }

        private void rend4()
        {
            tnelnx();// 音長の格納
            if (r.ah == 0xff)
            {
                // 休符か
                rend6();
                return;
            }
            r.push(r.ax);
            set_codedta();// コードの出力
            r.ax = r.pop();
            r.push(r.ax);
            if (r.ah == 0xfe)
            {
                // アクセントの指定ありか
                acc();// コードの前にVOL加算
            }
            r.ax = r.pop();
            r.push(r.ax);
            if (r.ah == 0xfd)
            {
                // 逆アクセントの指定ありか
                unacc();// コードの前にVOL減算
            }
            r.ax = r.pop();
            if (r.ah == 0xfc)
            {
                // スタカートの指定ありか
                stak();
            }
            return;
        }

        private void rend6()
        {
            mucomsub.cres_check();
            dionpu = 0;
            r.al = 0xff;
            stosbObjBufAL2DI();
            return;// リズムによる分割の終了
        }

        //;-------------------------------------------------------
        //;	リズムテーブルの獲得
        //;	exit	AL = リズム音長
        //;		AH = 0(音符),FF(休符),FE(アクセント),FD
        //;		ZR = リズムモードでない
        //;-------------------------------------------------------

        private void getrhythm_table()
        {
            getr_main();// リズムテーブルの番地を獲得(SI,DL)
            if (r.zero)
            {
                //init_tbl:
                mucomsub.init_rhythm();// ZR = 1 なら非リズムモード
                getr_main();
            }
            return;
        }

        private void getr_main()
        {
            r.ah = 0;// AH=0 : 音符 , FF : 休符 , FE : アクセント
            r.push(r.si);// FD : 逆アクセント , FC : スタカート
            r.si = rhyadrs;// リズムテーブルの番地を獲得(SI,DL)
            //getr2:
            do
            {
                r.al = rhythmdta[r.si];
                if (r.al < 0xfc)
                {
                    // 休符,アクセントの指定か
                    break;
                }
                r.si++;
                rhyadrs = r.si;
                r.ah = r.al;// AH=255 にする
            } while (true);
            //getr1:
            r.si = r.pop();
            rhyadrs++;
            r.zero = r.al == 0;
            return;
        }

        private void norm_code()
        {
            mucomsub.init_rhythm();// リズムテーブルを先頭にする
            r.al = rtm_max;
            tnelnx();// 前回と同じ音長(AL)なら格納しない
            set_codedta();
        }

        //;--------------------------------------------
        //;	コード演奏分岐処理
        //;	entry	codesav = コード番号(0-31)
        //;		onkai = 根音の中間コード
        //;		bassdta = ベースの中間コード
        //;	exit	SI = コードテーブルの先頭
        //;		CL = 根音の中間コード
        //;--------------------------------------------

        private void set_codedta()
        {
            mucomsub.cres_check();// クレッシェンドのチェック
            dionpu = r.di;// 音符の演奏番地を保存
            if (sendch == 10)
            {
                mucomsub.rhyexp_code();// リズム音源用へ
                return;
            }

            mucomsub.pan_check();// オートパンのチェック
            r.dx = codesav;
            r.dx += r.dx;
            r.dx += r.dx;
            r.si = 0;//ofs:codetne	; SI = 和音データテーブル
            r.si += r.dx;
            r.cl = onkai;// CL = 中間コード
            r.al = codemod;// コード演奏モード(1-7)
            r.al--;
            if (r.al == 0)
            {
                set_3ch();
                return;
            }
            r.al--;
            if (r.al == 0)
            {
                set_code0();// 根音を発生
                return;
            }
            r.al--;
            if (r.al == 0)
            {
                set_code1();// 第一音
                return;
            }
            r.al--;
            if (r.al == 0)
            {
                set_code2();// 第二音
                return;
            }
            r.al--;
            if (r.al == 0)
            {
                set_code3();// 第三音
                return;
            }
            r.al--;
            if (r.al == 0)
            {
                set_code4();// 第四音
                return;
            }
            set_bassdta();// ベース音
        }

        //;-------------------------------------
        //;	3ch効果音モードの和音演奏
        //;-------------------------------------

        private void set_3ch()
        {
            r.test(mode[0], 2);// @rhythmによって音階を出力したか
            if (!r.zero)
            {
                r.al = 0xf9;// +++
                stosbObjBufAL2DI();// 再同音階出力指定
                return;
            }
            //set_r3ch:
            r.al = 0xfa;// コード和音演奏の指定 +++
            stosbObjBufAL2DI();
            r.al = codetne[r.si];
            r.ah = codetne[r.si + 1];// AX,DX = 和音データ
            r.dl = codetne[r.si + 2];
            r.dh = codetne[r.si + 3];
            set_codedta();// 中間コードによる音符データ作成
            r.al = r.ah;// (E2 xxxx xxxx xxxx xxxx)
            setcode();
            r.al = r.dl;
            setcode();
            r.al = r.dh;
            setcode();
            mode[0] |=2;// 音階出力した
            return;
        }

        //;----------------------------------
        //;	根音､単独コード音の演奏
        //;----------------------------------

        private void set_code0()
        {
            r.al = 0;
            setcode2();
        }

        private void set_code1()
        {
            r.al = codetne[r.si + 3];
            setcode2();
        }

        private void set_code2()
        {
            r.al = codetne[r.si + 2];
            setcode2();
        }

        private void set_code3()
        {
            r.al = codetne[r.si + 1];
            setcode2();
        }

        private void set_code4()
        {
            r.al = codetne[r.si];
            setcode2();
        }

        private void set_bassdta()
        {
            r.cl = 0;
            r.al = bassdta;
            setcode2();
        }

        //;-----------------------------------------
        //;	演奏データに周波数データを格納
        //;	entry	AL = コードデータ
        //;		CL = 中間コード(根音)
        //;-----------------------------------------

        private void setcode()
        {
            if (r.al != 0xff)
            {
                r.al += r.cl;// 中間コードを加算
                mucomsub.read2();
                return;
            }

            //setnop:
            r.push(r.ax);
            r.ax = 0;
            stoswObjBufAX2DI();
            r.ax = r.pop();
            return;
        }

        private void setcode2()
        {
            if (r.al != 0xff)
            {
                r.al += r.cl;
                mucomsub.read2();
                return;
            }
            //setnop2:
            stosbObjBufAL2DI();// 休符を格納
            return;
        }

        //;-------------------------------------------
        //;	コード指定モードの変更(CH3のみ)
        //;-------------------------------------------

        public void codein()
        {
            r.cl = 16;
            mucomsub.check_314();// チャネル3,14以外は無効
            if (!r.zero)
            {
                //error9:
                error();return;
            }
            dt2mode = 0;// 複数ディチューンをクリア
            codemod = 1;
            r.ax = 0x40ed;// 効果音モードに
            stoswObjBufAX2DI();
            return;
        }

        public void codeout()
        {
            codemod = 0;
            mucomsub.check_314();// CH=3,14か
            if (!r.zero)
            {
                //skipse:
                return;
            }
            r.ax = 0xed;// 効果音モード解除
            stoswObjBufAX2DI();
            //skipse:
            return;
        }

        public void bass()
        {
            r.al = 5;
            bass1();
        }

        public void code_exe()
        {
            rednums();// コード演奏番号
            r.carry= r.al > 4;
            r.cl = 6;
            if (r.carry)
            {
                error();return;
            }
            bass1();
        }

        private void bass1()
        {
            r.al++;
            r.al++;
            codemod = r.al;// 0-4,Bass = 2-7
            return;
        }

        //error9:
        //	jmp	error

        //;------------------------------
        //;	同じ音符の演奏(@sc)
        //;------------------------------

        public void same_code()
        {
            set_symbol2();
            mucomsub.cres_check();
            tnelnmx();// 音長の解析
            r.dl = r.al;// DL = 音長
            r.si = dionpu;
            if (r.si == 0)
            {
                // 前回が音符か
                //set_rest:
                kyufu();// 圧縮休符の格納
                return;
            }
            mucomsub.pan_check();// オートパンのチェック
            tnelnx();// 音長の格納
            r.test(mode[0], 0x80);// タイ指定か
            if (!r.zero)
            {
                tietie();
            }
            dionpu = r.di;// 次のタイ,スタカート処理用
            if (codemod == 1)
            {
                // @codeinモードか
                //scode3:
                r.al = 0xf9;// 3ch @codeinモードの時のみ +++
                stosbObjBufAL2DI();
                return;
            }
            if (sendch == 11)
            {
                // PCM用
                //scode4:
                r.al = 0xd5;
                stosbObjBufAL2DI();
                r.al = (byte)muap98.object_Buf[r.si+1].dat;
                r.ah = (byte)muap98.object_Buf[r.si + 2].dat;// AX = 前回の音符の周波数
                stoswObjBufAX2DI();
                return;
            }
            r.al = (byte)muap98.object_Buf[r.si].dat;
            r.ah = (byte)muap98.object_Buf[r.si+1].dat;// AX = 前回の音符の周波数
            stoswObjBufAX2DI();// 演奏データとして格納する
            return;
        }

        //;-------------------------------------------------------------
        //;	変音記号のチェック
        //;	entry	AL = chr data
        //;		BX = text offset
        //;	exit	AL = next chr data
        //;		DL = 変音データ(0:なし 1:# 2:- 3:% 4:## 5:--)
        //;-------------------------------------------------------------

        public void gethenon()
        {
            r.dl = 1;// 次にシャープコードがあるか (DL=1)
            if (r.al == (byte)'+' || r.al == (byte)'#')
            {
                //hen2:
                r.bx++;
                r.al = muap98.source_Buf[r.bx];// 重嬰音のチェック
                if (r.al != (byte)'#' && r.al != (byte)'+') return;
                //hen1:
                r.dl += 3;
                //hen4:
                r.bx++;
                r.al = muap98.source_Buf[r.bx];
                //hen0:
                return;
            }

            r.dl = 2;// 次にフラットコードがあるか (DL=2)
            if (r.al == (byte)'-')
            {
                //hen3:
                r.bx++;
                r.al = muap98.source_Buf[r.bx];// 重変音のチェック
                if (r.al != (byte)'-') return;
                //hen1:
                r.dl += 3;
                //hen4:
                r.bx++;
                r.al = muap98.source_Buf[r.bx];
                //hen0:
                return;
            }

            r.dl = 3;// 次にナチュラルコードがあるか (DL=3)
            if (r.al == (byte)'%')
            {
                //hen4:
                r.bx++;
                r.al = muap98.source_Buf[r.bx];
                //hen0:
                return;
            }

            r.dl = 0;// 標準 (DL=0)
            //hen0:
            return;
        }

        //;------------------------------------------
        //;	前回の音長と比較格納
        //;	entry	AX = 現在の音長データ
        //;------------------------------------------

        public void tnelnx()
        {
            r.push(r.ax);
            if (chglen != r.al)
            {
                // 前回の音長と比較
                setrat();// 音長、音長割合セットコマンド格納
            }
            if ((mode[0] & 1) == 0)
            {
                // 連符モードでは加算しない
                add_tlen();// 総合音長の加算
            }
            r.ax = r.pop();
            return;
        }

        //;-----------------------------------------
        //;	音長解析サブルーチン
        //;	entry	BX = テキスト番地
        //;	exit	AX = 音長値
        //;-----------------------------------------

        public void tnelnmx()
        {
            tnelnm();
            r.cl = 6;
            if (r.ax > 255)
            {
                // Length範囲チェック
                error();return;
            }
            work.otoLength = r.ax;
            return;
        }

        public void tnelnm()
        {
            check_macrov();// マクロ変数チェック
            if (!r.carry)
            {
                tnelnm1();
                return;
            }
            r.bx++;
            r.push(r.bx);// 次のテキスト番地を保存する
            r.push(r.si);
            get_macroadrs();// SI = マクロ変数格納番地
            r.bx = macrov[(r.si + 18)/2];// AX = 該当マクロ変数テキスト番地
            r.si = r.pop();
            r.al = muap98.source_Buf[r.bx];
            tnelnm1();// 音長解析ルーチンを呼ぶ
            r.bx = r.pop();// 元のテキスト番地に戻す
            return;
        }

        private void tnelnm1()
        {
            r.push(r.dx);
            r.push(r.cx);
            r.dx = 0;// 音長データの初期化
            r.ch = 0;
            if (r.al == (byte)'=')
            {
                // 直接モードか
                r.bx++;
                rednum();
                //tne_end:
                r.cx = r.pop();
                r.dx = r.pop();// 加算終了
                return;
            }

            //tnelnm2:
            do
            {
                gettxtlen();// テキストの音長数字を読み取り変換
                r.cl = 6;// 音長がオーバーフローのエラー番号
                if (r.ch != 0)
                {
                    // 加算か
                    r.carry = r.dx < r.ax;
                    r.dx -= r.ax;
                    if (r.carry || r.zero)
                    {
                        //error10:
                        error();
                        return;
                    }
                    r.ax = r.dx;
                }
                else
                {
                    //tnelnm3:
                    r.carry = r.ax + r.dx > 0xffff;
                    r.ax += r.dx;// （^ コマンド対策で、音長加算）
                    if (r.carry)
                    {
                        //error10:
                        error();
                        return;
                    }
                }

                //tnelnm4:
                if (muap98.source_Buf[r.bx] != (byte)'^') // 音長の加算
                {

                    if (muap98.source_Buf[r.bx] != (byte)'_')
                    {
                        // 音長の減算
                        //tne_end:
                        r.cx = r.pop();
                        r.dx = r.pop();// 加算終了
                        return;
                    }
                    r.ch = muap98.source_Buf[r.bx + 1];
                    if (r.ch >= (byte)'a')
                    {
                        r.ch -= 0x20;
                    }
                    if (r.ch == (byte)'>')
                    {
                        //tne_end:
                        r.cx = r.pop();
                        r.dx = r.pop();// 加算終了
                        return;
                    }
                    if (r.ch == (byte)'<')
                    {
                        //tne_end:
                        r.cx = r.pop();
                        r.dx = r.pop();// 加算終了
                        return;
                    }
                    if (r.ch < (byte)'A')
                    {
                        goto tne_sub1;
                    }
                    if (r.ch <= (byte)'G')
                    {
                        //tne_end:
                        r.cx = r.pop();
                        r.dx = r.pop();// 加算終了
                        return;
                    }

                    tne_sub1:
                    r.ch = 1;// 減算フラグon
                }
                else
                {
                    //tne_add:
                    r.ch = 0;// 加算フラグon
                }

                //tne_sub:
                r.bx++;
                r.dx = r.ax;
                r.al = muap98.source_Buf[r.bx];// 次のテキストを読む
                                               //	jmp	tnelnm2
            } while (true);

            ////tne_end:
            //r.cx = r.pop();
            //r.dx = r.pop();// 加算終了
            //return;
        }

        //;･････････････････････････････････････････････････････････
        //;	テキストの音長数字を読み取り変換サブルーチン
        //;	exit	AX = 音長 (符点チェック)
        //;･････････････････････････････････････････････････････････

        private void gettxtlen()
        {
            r.push(r.dx);
            if (r.al == (byte)'.')
            {
                // 符点チェック
                loop17(true);
                return;
            }
            mucomsub.chknum();// 数字チェック
            if (r.carry)
            {
                loop17(true);
                return;
            }
            rednums();// テキストの数字を読む
            r.zero = (r.al == 0);
            r.cl = 6;
            if (r.zero)
            {
                //error25
                r.cl = 6;
                error();
                return;
            }
            r.dl = r.al;
            r.ax = 192;
            r.div(r.dl);
            r.bx--;
            r.ah = 0;
            r.dx = r.ax;// 今の基本音長を保存
            loop17(false);
        }

        private void loop17(bool nrm)
        {
            do
            {
                if (!nrm)
                {
                    do
                    {
                        r.bx++;
                        if (muap98.source_Buf[r.bx] != (byte)'.')
                        {
                            // また符点か（複数チェック）
                            //len_exit:
                            r.dx = r.pop();
                            work.otoLength = r.ax;
                            return;
                        }
                        r.dx >>= 1;// 1.5倍の計算
                        r.ax += r.dx;
                    } while (true);
                }
                nrm = false;

                //normal:
                r.al = lendata;// デフォルト音長を使用
                r.ah = 0;
                r.bx--;
                r.dx = r.ax;
            } while (true);// 符点チェックへ
        }

        //;=============================
        //;	連符コマンド処理
        //;=============================

        private void renpu()
        {
            r.al = chglen;// 連符前の音長を保存
            renplen = r.al;
            disave3 = r.di;// 音長を格納する番地を保存
            onpucnt = 0;// 連符数カウンタ
            mode[0] |= 1;// 連符モード

            //loop9:
            do
            {
                chktxt();
                r.ah = muap98.source_Buf[r.bx];// @wチェック用
                if (r.al == (byte)'}')
                {
                    // 連符終了か
                    exit1();
                    return;
                }

                //;--------------------------------
                //;	音符､休符､和音を変換
                //;--------------------------------

                r.push(r.dx);
                r.push(r.cx);
                r.push(r.ax);
                com_main();// 音符を変換する
                r.ax = r.pop();
                r.cx = r.pop();
                r.dx = r.pop();
                r.carry = onpucnt <= 192;// 最大音符数チェック
                r.cl = 4;
            } while (r.carry);// 次のコード変換へ

            //error14:
            error(); return;
        }
        //;----------------------------
        //;	連符の音長を計算
        //;----------------------------

        private void exit1()
        {
            mode[0] &= 0xfe;// 連符モードのクリア
            r.dl = onpucnt;
            r.zero = r.dl == 0;// 音符か休符が存在したか
            r.cl = 4;
            if (r.zero)
            {
                //error14:
                error(); return;
            }
            tnelnmx();// 音長を読み取り解析する
            add_tlen();
            r.push(r.ax);
            r.ax = r.di;
            r.ax -= disave3;// AX = 連符音符に要したバイト数
            if (r.al == r.dl)
            {
                // 全て休符(1バイト系)か?(和音の時に有り得る)
                //all_rest:
                r.ax = r.pop();
                r.di = disave3;// 連符の中身が全て休符の場合の処理
                r.dl = r.al;
                kyufu();// 圧縮式休符を指定する
                return;
            }
            r.ax = r.pop();
            r.ah = 0;// AX = 総合音長  DL = 連符数
            r.div(r.dl);// 連符長の計算
            r.zero = r.al == 0;// 分割された音の長さが0ならエラー
            r.cl = 5;
            if (r.zero)
            {
                //error14:
                error(); return;
            }
            chglen = r.al;// 今回の音長を保存
            if (r.al != renplen)
            {
                // 連符の音長と前回の音長を比較

                r.push(r.ax);
                r.push(r.dx);
                r.ax = disave3;
                r.dx = 3;
                move_obj();// 演奏データの移動
                r.push(r.di);
                r.di = r.ax;
                r.al = chglen;
                setrat();// 音長の格納
                r.di = r.pop();
                r.dx = r.pop();
                r.ax = r.pop();
            }
            //noset_renpu:
            if (r.ah == 0)
            {
                // 音長に余りがなかったか
                //skip_rr:
                return;
            }
            r.dl = r.ah;// DL = 余った音長
            kyufu();// 圧縮式休符の格納(DL)
            //skip_rr:
            return;
        }

        //;--------------------------------------
        //;	演奏データの移動
        //;	enotry	AX = 移動開始番地
        //;		DX = 移動バイト数
        //;		DI = 現在の演奏番地
        //;	exit	DI = その後の番地
        //;		dionpu = + DX
        //;		CY = 異常終了(AX=0)
        //;--------------------------------------

        public void move_obj()
        {
            // 音符が既に格納されていたか
            if (r.ax < OBJTOP)
            {
                r.carry = true;
                return;
            }

            //mobj1:
            r.push(r.di);
            r.push(r.si);
            r.push(r.cx);
            r.push(r.ds);
            r.push(r.es);
            r.ds=r.pop();

            r.si = r.di;// SI = 転送元
            r.cx = r.di;
            r.cx -= r.ax;// DI = 連符前の音長指定番地
            r.di += r.dx;
            r.si--;
            r.di--;

            //	std
            // 格納されたデータをDXバイト移動
            do
            {
                muap98.object_Buf[r.di] = muap98.object_Buf[r.si];
                r.di--;//df=1なので
                r.si--;//df=1なので
                r.cx--;
            } while (r.cx != 0);
            //	cld

            dionpu += r.dx;

            r.ds = r.pop();
            r.cx = r.pop();
            r.si = r.pop();
            r.di = r.pop();

            r.di += r.dx;
            r.carry = false;
            return;
        }

        //;==========================================
        //;	自動スラー／タイコマンドの処理
        //;==========================================

        private void tie()
        {
            r.ax = dionpu;
            r.dx = 1;
            move_obj();
            if (r.carry)
            {
                return;
            }
            r.push(r.di);
            r.di = r.ax;
            r.al = 0xdf;
            stosbObjBufAL2DI();// 空けた所にスラーコマンドを入れる
            if (muap98.source_Buf[r.bx] == (byte)'&')
            {
                // 強制スラーか
                r.bx++;
                // タイ指定しない
            }
            else
            {
                //tslur1:
                r.al = (byte)muap98.object_Buf[r.di].dat;
                r.ah = (byte)muap98.object_Buf[r.di + 1].dat;// 直前の音符の周波数を保存
                lastfrq = r.ax;
                mode[0] |= 0x80;// 次の音符にタイ,スラーを決定させる
            }
            //tslur2:
            r.di = r.pop();
            r.al = 0;
            r.al++;// NZ
            r.zero = false;
            //tie0:
            return;
        }

        //;====================================
        //;	単なるタイコマンドの処理
        //;====================================

        private void tie2()
        {
            if (r.ch != 11)
            {
                tie();// 音長割合を最大に
                if (r.zero)
                {
                    return;
                }
                // PCMは単純にF8を出力
            }
            tietie();
        }

        private void tietie()
        {
            mode[0] &= 0x7f;// 自動タイを無効にする
            r.al = 0xe1;// 次はキーオフしない +++
            stosbObjBufAL2DI();
            return;
        }

        //;====================================
        //;	スタカートの処理(Q4)
        //;====================================

        private void stak()
        {
            r.ax = dionpu;
            r.cl = muap98.source_Buf[r.bx];
            if (r.cl != 0x22)
            {
                // スタカティシモ

                //;--------------------------
                //;	スタカート処理
                //;--------------------------

                r.dl = 4;// Q4
                if (r.cl == (byte)'.')
                {
                    // メゾスタカート
                    r.dl = 3;// Q5
                    r.bx++;
                }
                //stak4:
                r.push(r.dx);
                r.dx = 2;
                move_obj();// 2bytes演奏データを空ける
                r.dx = r.pop();
                if (r.carry)
                {
                    //nostak:
                    return;
                }
                r.push(r.di);
                r.di = r.ax;
                r.al = ratdata;// Qの値を保存
                r.push(r.ax);
                r.al = r.dl;
                chgrat();// Q4にする
                r.ax = r.pop();
                r.di = r.pop();
                chgrat();// 音長割合を元に戻す
                         //nostak:
                return;
            }

            //;------------------------------
            //;	スタカティシモ処理
            //;------------------------------

            //stak1:
            r.bx++;
            r.dx = 4;// 音量も上げるため
            move_obj();// 4bytes演奏データを空ける
            if (r.carry)
            {
                //nostak:
                return;
            }
            r.push(r.di);
            r.di = r.ax;
            r.al = ratdata;// AL = Qの値
            r.push(r.ax);
            r.al = 5;// Q3に指定
            chgrat();
            r.ax = 0x4d7;
            stoswObjBufAX2DI();// 音量値を+4する
            r.ax = r.pop();
            r.di = r.pop();
            chgrat();
            r.ax = 0x4d6;// 音量値を-4する
            stoswObjBufAX2DI();
            return;
        }

        //;--------------------------
        //;	アクセント処理
        //;--------------------------

        private void acc()
        {
            r.al = muap98.source_Buf[r.bx];
            mucomsub.chknum();// 数字チェック
            if (r.carry)
            {
                acc_main();
                return;
            }
            rednums();
            r.push(accbase);
            accbase = r.al;
            acc_main();
            accbase = (byte)r.pop();
            return;
        }

        private void acc_main()
        {
            r.ax = dionpu;
            r.dx = 2;
            move_obj();// 2bytes演奏データを空ける
            if (r.carry)
            {
                //noacc:
                return;
            }
            r.push(r.di);
            r.di = r.ax;
            r.ah = accbase;// AH = 加算値
            r.al = 0xd7;// 音量を+4する
            stoswObjBufAX2DI();
            r.di = r.pop();
            r.al = 0xd6;
            stoswObjBufAX2DI();// 音量の復活(-4)
            //noacc:
            return;
        }

        //;----------------------------
        //;	逆アクセント処理
        //;----------------------------

        private void unacc()
        {
            r.al = muap98.source_Buf[r.bx];
            mucomsub.chknum();// 数字チェック
            if (r.carry)
            {
                unacc_main();
                return;
            }
            rednum();
            r.push(dwnbase);
            dwnbase = r.al;
            unacc_main();
            dwnbase = (byte)r.pop();
            return;
        }

        private void unacc_main()
        {
            r.ax = dionpu;
            r.dx = 2;
            move_obj();// 2bytes演奏データを空ける
            if (r.carry)
            {
                //nounacc:
                return;
            }
            r.push(r.di);
            r.di = r.ax;
            r.ah = dwnbase;
            r.al = 0xd6;// 音量を-6する
            stoswObjBufAX2DI();
            r.di = r.pop();
            r.al = 0xd7;
            stoswObjBufAX2DI();// 音量の復活(+6)
            //nounacc:
            return;
        }

        //;==================================
        //;	オクターブコマンド処理
        //;==================================

        private void oct()
        {
            rednums();// ソーステキストの数字を読み取る
            r.zero = r.al == 0;// 範囲チェック
            r.cl = 6;
            if (r.zero)
            {
                //error3:
                error();return;
            }
            if (r.al >= 10)
            {
                //	jnb	error3
            }
            r.al--;// 実際の値に変換(-1)
            octdata = r.al;// オクターブデータバッファに格納
            return;
        }

        //;========================================
        //;	音長割合セットコマンド処理
        //;========================================

        private void ratio()
        {
            mucomsub.MakeDatum(enmMMLType.GatetimeDiv);
            work.md = work.FlashLstMd(work.md);

            rednums();// テキストの数字を読み取る

            work.md.args.Add((int)(sbyte)r.al);//ゲートタイム値(int)

            r.cl = 6;// 範囲チェック
            if (r.al >= 9)
            {
                error();return;
            }
            if (r.al == 0)
            {
                error(); return;
            }
            r.al = (byte)~r.al;// 前計算（反転）をしてからバッファに格納
            r.al -= 0xf7;
            ratmode &= 0xfe;
            chgrat();
        }

        //;================================================================
        //;	音長、及び音長と割合から音出しの時間を計算して格納
        //;		計算式 : time = length*ratio/8 
        //;	entry	AL = chglen(指定音長)
        //;================================================================

        public void setrat()
        {
            if ((mode[0] & 1) != 0)
            {// 連符の際は音長を格納しない(一括格納)
                return;
            }
            r.push(r.ax);
            chglen = r.al;// 念のため保存
            r.ah = 0xf4;
            (r.ah, r.al) = (r.al, r.ah);
            muap98.object_Buf[r.di] = new MmlDatum(r.al);
            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
            r.di += 2;

            r.al = ratdata;// 音長割合データ
            if ((ratmode & 1) == 0)
            {
                // @Qモード?
                r.ax = (ushort)(r.al * r.ah);
                r.ax >>= 3;// 1/8 計算してセット
            }

            //setrat1:
            muap98.object_Buf[r.di] = new MmlDatum(r.al);
            r.di++;
            r.ax = r.pop();
            //skip_setrat:
            return;
        }

        //;-----------------------------
        //;	音長割合のみ変更
        //;	entry	AL = Qの値
        //;-----------------------------

        public void chgrat()
        {
            r.push(r.ax);
            ratdata = r.al;
            r.test(ratmode, 1);
            if (r.zero)
            {
                r.ah = chglen;
                r.mul(r.ah);
                r.ax >>= 3;
            }
            //chgrat1:
            r.ah = r.al;
            r.al = 0xde;
            muap98.object_Buf[r.di] = work.Copy(work.md, r.al);
            work.md = null;
            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
            r.di += 2;
            r.ax = r.pop();
            return;
        }

        //;===================================
        //;	オクターブ上昇コマンド
        //;===================================

        public void octup()
        {
            r.al = octdata;// 現オクターブ値を読み取る
            r.al++;
            r.carry = r.al < 9;
            r.cl = 6;
            if (!r.carry)
            {
                error();return;
            }
            //recov3:
            octdata = r.al;
            return;
        }

        //;===================================
        //;	オクターブ下降コマンド
        //;===================================

        public void octdown()
        {
            r.al = octdata;
            r.carry = r.al < 1;
            r.al--;
            r.cl = 6;
            if (!r.carry)
            {
                //recov3:
                octdata = r.al;
                return;
            }
            //error11:
            error();return;
        }

        //;==========================
        //;	休符コマンド
        //;==========================

        private void rest()
        {
            int row = work.row;
            int col = work.col;
            set_symbol2();// ソース番地の格納
            check_comlen();// 色変わり歌詞のチェック
            tnelnmx();// 音長の解析ルーチン(AL=音長)
            r.test(mode[0], 1);// 連符モードでは加算しない
            if (r.zero)
            {
                add_tlen();
            }
            r.dl = r.al;
            work.row = row;
            work.col = col;
            kyufu();// 圧縮式休符格納
        }

        //;================================
        //;	圧縮休符の最大値指定
        //;================================

        public void set_max()
        {
            tnelnmx();
            maxrest = r.al;
            return;
        }

        //;----------------------------------
        //;	休符の圧縮処理
        //;	entry	DL = 休符の音長
        //;----------------------------------

        public void kyufu()
        {
            mucomsub.cres_check();// クレシェンドチェック
            mode[0] &= 0x7f;// 自動タイの無効
            dionpu = 0;// 音符無しにする
            r.test(mode[0], 1);// 連符モードか
            if (r.zero)
            {
                kyufu1();
                return;
            }
            onpucnt++;// 連符カウンタ
            r.push(r.ax);
            r.al = 0xff;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            r.ax =r.pop();
            return;
        }

        private void kyufu1()
        {
            r.push(r.ax);
            if (r.di != disave2)// 前回が休符であったか
            {
                init_kyufu();
                return;
            }
            r.al = restlen;// 前回までの休符の合計長さ
            r.carry = r.al + r.dl > 255;
            r.al += r.dl;// 今回の休符の音長を加算
            if (r.carry)
            {
                init_kyufu();
                return;
            }
            if (r.al > maxrest)
            {
                init_kyufu();
                return;
            }
            r.di = disave1;// DI = 前回の演奏データ番地
            set_kyufu();
            r.ax = r.pop();
            return;
        }

        private void set_kyufu()
        {
            restlen = r.al;// 休符の合計長さを保存
            chglen = r.al;// 現在の音長
            if (r.al != chgsav)
            {
                // 休符音長とその前の音符の音長が一致するか
                setrat();// 音長格納(F4xxxx)
            }
            r.al = 0xff;
            LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
            linePos.chip = work.crntChip;
            linePos.chipNumber = 0;
            linePos.ch = (byte)work.crntChannel;
            linePos.part = work.crntPart;
            MmlDatum md = new MmlDatum(r.al, enmMMLType.Rest, linePos, new object[] { work.otoLength });
            md = work.FlashLstMd(md);
            muap98.object_Buf[r.di++] = md;// 休符を格納
            disave2 = r.di;// 最後の番地を保存
            return;
        }

        private void init_kyufu()
        {
            disave1 = r.di;// 休符指定開始番地を保存
            r.al = chglen;
            chgsav = r.al;// その前の音符の音長を保存(比較用)
            r.al = r.dl;
            set_kyufu();
            r.ax = r.pop();
            return;
        }

        //;･･･････････････････････････････････
        //;	色変わり歌詞のチェック
        //;･･･････････････････････････････････

        private void check_comlen()
        {
            r.test(commode, 2);// 色変わり歌詞モードか
            if (r.zero)
            {
                //comlen1:
                return;
            }
            if (muap98.source_Buf[r.bx - 2] != (byte)' ')
            {
                // 音符・休符の前がスペースか
                //comlen1:
                return;
            }
            r.push(r.ax);
            r.al = 0xdd;// 桁指定
            r.ah = comcnt;// AH = 桁カウント
            r.push(r.bx);

            //comlen2:
            do
            {
                r.bx--;
                r.ah++;
            } while (muap98.source_Buf[r.bx - 2] == (byte)' ');// スペースが複数あるか

            r.bx = r.pop();
            comcnt = r.ah;// 次の桁へ移動
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            r.ax = r.pop();

            //comlen1:
            return;
        }

        //;===================================================
        //;	ソーステキストの数字を読み取りAXに出力
        //;		(マクロ変数対応版)
        //;===================================================

        public void rednum()
        {
            chktxt();
            r.bx--;// 数字の前のスペースを除去
            r.push(r.dx);
            r.push(r.cx);
            check_macrov();// マクロ変数チェック
            if (r.carry)
            {
                r.push(r.cx);
                r.push(r.ax);
                r.cl = r.al;
                r.ax = 1;
                r.ax <<= r.cl;
                r.cl = 40;
                if ((macroflg & r.ax) == 0)
                {
                    // パラメータが指定されたかをチェック
                    error();
                }
                r.ax = r.pop();
                r.cx = r.pop();
                r.push(r.si);
                get_macroadrs();// SI = マクロ変数格納番地
                r.ax = macrov[r.si/2];// AX = 該当マクロ変数の値
                r.si = r.pop();
                r.bx++;
                //exit3:
                r.cx = r.pop();
                r.dx = r.pop();
                return;
            }

            //rednum1:
            r.cl = 14;
            mucomsub.chknum();// 数字チェック
            if (r.carry)
            {
                error();
            }
            r.ax = 0;// 出力データの初期化
            r.cx = r.ax;
            //loop12:
            do
            {
                r.cl = (byte)(r.bx >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[r.bx]);// 数字チェック
                r.push(r.ax);
                r.al = r.cl;
                mucomsub.chknum();
                r.ax = r.pop();
                if (r.carry)
                {
                    //exit3:
                    r.cx = r.pop();
                    r.dx = r.pop();
                    return;
                }
                r.bx++;
                r.dx = 10;
                uint ans = (uint)r.ax * r.dx;// 前のデータを10倍にして今回の数値を加算
                r.ax = (ushort)ans;
                r.dx = (ushort)(ans >> 16);
                r.cl -= (byte)'0';
                r.ax += r.cx;
            }while (true);
        }

        //;---------------------------------
        //;	0～255 までの数字獲得
        //;---------------------------------

        public void rednums()
        {
            r.push(r.cx);
            rednum();
            r.cl = 14;
            if (r.ah != 0)
            {
                //error2:
                error();
            }
            r.cx = r.pop();
            return;
        }

        //;･･････････････････････････････････････････････････
        //;	マクロ変数の指定チェック
        //;	entry	DS:BX = ソーステキストの番地
        //;	exit	CY = 1 : マクロ変数あり(BX+1)
        //;		    AL = マクロ変数番号(0～8)
        //;		CY = 0 : マクロ変数なし(BXそのまま)
        //;		    AL = テキストの内容
        //;	break	CL
        //;･･････････････････････････････････････････････････

        private void check_macrov()
        {
            r.cl = 40;
            r.al = (byte)(r.bx >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[r.bx]);
            if (r.al != (byte)'\\')
            {// マクロ変数指定か
             //cmacrov1:
                r.carry = false;
                return;
            }
            r.bx++;
            r.al = (byte)(r.bx >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[r.bx]);
            r.carry = r.al < (byte)'1';
            r.al -= (byte)'1';// \1～\9まで指定可
            if (r.carry)
            {
                error();
            }
            if (r.al >= 9)
            {
                error();
            }
            r.carry = true;
            return;
        }

        //;･･････････････････････････････････････
        //;	マクロ変数番地の読みだし
        //;	entry	AL = マクロ変数番号
        //;	exit	SI = マクロ変数番地
        //;･･････････････････････････････････････

        /// <summary>
        /// </summary>
        private void get_macroadrs()
        {
            r.si = 0;//ofs:macrov
            r.ah = 0;
            r.ax += r.ax;
            r.si += r.ax;
            return;
        }

        //;====================================
        //;	テンポ設定コマンド処理
        //;====================================

        private void tempo()
        {
            rednum();// テキストのデータを読み取りAXに出力
            if (mmlver < 0x26)
            {
                r.ax -= 2;// テンポ補正
            }
            r.cl = 6;
            // 範囲チェック
            if (r.ax < 16)
            {
                error(); return;
            }
            if (r.ax > 3907)
            {
                error(); return;
            }
            tmplen = 0;// @ACC,@RITの中止
            calc_tempo();
        }

        public void calc_tempo()
        {
            tempos = r.ax;// 現在のテンポ保存
            calct();// タイマカウント値に変換
            r.push(r.ax);
            r.al = 0xf5;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            r.ax = r.pop();
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            return;
        }

        public void calct()
        {
            r.push(r.cx);// AX = 外部テンポ値
            r.push(r.dx);
            r.cx = r.ax;
            r.ax = 62500;// タイマカウント値に変換
            r.dx = 0;
            r.div(r.cx);// AX = タイマA設定値
            r.dx = r.pop();
            r.cx = r.pop();
            return;
        }

        //;==============================
        //;	繰り返し演奏処理
        //;==============================

        private void mloop()
        {
            r.al = 0xfe;
            r.dl = muap98.source_Buf[r.bx];
            r.dh = muap98.source_Buf[r.bx + 1];
            if (r.dl != (byte)'*')
            {
                // ** コマンドか
                //break:
                stosbObjBufAL2DI();
                r.al = chglen;
                setrat();// 音長の格納
                return;
            }
            r.bx++;
            r.al = 0xfd;
            if (r.dh != (byte)'*')
            {
                // *** コマンドか
                //break:
                stosbObjBufAL2DI();
                r.al = chglen;
                setrat();// 音長の格納
                return;
            }
            r.bx++;
            stopm();
        }

        public void stopm()
        {
            optimiz = 0;// 最適化フラグをクリア
            r.al = 0xfc;// @STOPを実行
            //break:
            stosbObjBufAL2DI();
            r.al = chglen;
            setrat();// 音長の格納
        }

        //;==========================================
        //;	デフォルト音長セット命令処理
        //;==========================================

        private void mlength()
        {
            tnelnmx();// 音長値よりタイマカウント値を計算
            lendata = r.al;// デフォルト音長にセット
            chglen = r.al;
            setrat();// 音長、割合設定
        }

        //;==============================
        //;	音量設定命令処理
        //;==============================

        private void volume()
        {
            r.al = muap98.source_Buf[r.bx];
            r.ah = muap98.source_Buf[r.bx + 1];
            if (r.al == (byte)'=')// mf,f,ff などによる指定
            {
                exp_vol();
                return;
            }
            if (r.al == (byte)'+')
            {
                add_vol();
                return;
            }
            if (r.al == (byte)'-')
            {
                sub_vol();
                return;
            }
            rednums();// テキストの数字を読み取る
            retvol();
        }

        private void retvol()
        {
            if (r.al >= 16)
            {
                // 範囲チェック
                //error4:
                r.cl = 31;
                error();
                return;
            }
            mucomsub.init_cres();// クレッシェンドのクリア
            r.ah = r.al;
            if (r.ch == 11)
            {
                r.ah <<= 3;
            }
            else
            {
                //setvolfm:
                r.ah += r.ah;
                r.ah += r.al;
                if (r.ah != 0)
                {
                    r.ah += (byte)VOLBASE;
                }
            }
            retvol2();
        }

        public void retvol2()
        {
            r.ah += volsft;// 音量値を変移する
            if (r.ah > 127)
            {
                //error4:
                r.cl = 31;
                error();
                return;
            }
            retvol3();
        }

        public void retvol3()
        { 
            volsave = r.ah;// 音量データを保存
            r.test(optimiz, 4);
            if (!r.zero)
            {
                if (r.ah == opt_vol)
                {
                    // 直前に指定した音量と一致か
                    //svol2:
                    return;
                }
                opt_vol = r.ah;// 音量を保存
                optimiz |= 4;
            }
            //svol1:
            r.al = 0xe2;// 音量設定コマンドを格納 +++
            LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
            linePos.chip = work.crntChip;
            linePos.chipNumber = 0;
            linePos.ch = (byte)work.crntChannel;
            linePos.part = work.crntPart;
            MmlDatum md = new MmlDatum(r.al, enmMMLType.Volume, linePos, new object[] { (int)r.ah, (byte)2 });
            md = work.FlashLstMd(md);
            muap98.object_Buf[r.di] = md;
            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
            r.di += 2;
            //svol2:
            return;
        }

        //;-----------------------------
        //;	音量の相対的変化
        //;-----------------------------

        private void add_vol()
        {
            get_nowvol();
            r.ah = r.al;
            r.al = 0xd7;
            LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
            linePos.chip = work.crntChip;
            linePos.chipNumber = 0;
            linePos.ch = (byte)work.crntChannel;
            linePos.part = work.crntPart;
            MmlDatum md = new MmlDatum(r.al, enmMMLType.VolumeUp, linePos, new object[] { (int)r.ah });
            md = work.FlashLstMd(md);
            muap98.object_Buf[r.di] = md;
            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
            r.di += 2;
            return;
        }

        private void sub_vol()
        {
            get_nowvol();
            r.ah = r.al;
            r.al = 0xd6;

            LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
            linePos.chip = work.crntChip;
            linePos.chipNumber = 0;
            linePos.ch = (byte)work.crntChannel;
            linePos.part = work.crntPart;
            MmlDatum md = new MmlDatum(r.al, enmMMLType.VolumeDown, linePos, new object[] { (int)r.ah });
            md = work.FlashLstMd(md);
            muap98.object_Buf[r.di] = md;
            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
            r.di += 2;
            return;
        }

        private void get_nowvol()
        {
            r.bx++;
            rednums();
        }

        //;--------------------------------------------
        //;	音楽記号による音量指定(V=mP;など)
        //;--------------------------------------------

        private void exp_vol()
        {
            if (r.ah == (byte)'+')// V=+1
            {
                volshift1();
                return;
            }
            if (r.ah == (byte)'-')// V=-1
            {
                volshift2();
                return;
            }

            for (int i = 0; i < volcheck.Length; i++)
            {
                r.push(r.bx);
                bool fnd = true;
                for (int j = 0; j < volcheck[i].Item1.Length; j++)
                {
                    byte b = muap98.source_Buf[r.bx++];
                    b -= (byte)' ';
                    if (volcheck[i].Item1[j] != b)
                    {
                        fnd = false;
                        break;
                    }
                }
                if (fnd)
                {
                    r.dx = r.pop();// BX = 次のソース番地
                    r.al = volcheck[i].Item2;// AL = vol data (0-15)
                    retvol();
                    return;
                }
                r.bx = r.pop();
            }

            // なかった
            //volc3:
            r.cl = 30;
            error();
            return;
        }

        private Tuple<string, byte>[] volcheck = new Tuple<string, byte>[]{
            new Tuple<string, byte>("PPP:",7),new Tuple<string, byte>("PP:",8),
            new Tuple < string, byte >("P:", 9),new Tuple < string, byte >("MP:", 10),
            new Tuple < string, byte >(":", 11),new Tuple < string, byte >("MF:", 12),
            new Tuple < string, byte >("F:", 13),new Tuple < string, byte >("FF:", 14),
            new Tuple < string, byte >("FFF:", 15)
        };


        //;--------------------------------------------
        //;	V=+,V=- による記号指定音量シフト
        //;--------------------------------------------

        private void volshift1()
        {
            r.bx += 2;
            rednums();
            volsft = r.al;// 全体の音量に加算する値
            return;
        }

        private void volshift2()
        {
            r.bx += 2;
            rednums();
            r.al = (byte)-r.al;
            volsft = r.al;// 全体の音量に減算する値
            return;
        }

        //;==========================
        //;	変数コマンド
        //;==========================

        private void value()
        {
            chkval_x();// 変数番号のチェック(ソース)
            r.dl = r.ah;
            chktxt();
            r.cl = 32;
            if (r.al != (byte)'=')
            {
                //error1:
                error(); return;
            }
            r.al = 0xda;
            stosbObjBufAL2DI();// コマンド番号
            r.dh = 0;
            chkval();// 変数番号のチェック(ディスティネーション)
            if (r.carry)
            {
                set_value();
                return;
            }
            r.dh = r.ah;
            chktxt();
            r.dl |= 0x10;
            if (r.al == (byte)'+')
            {
                set_value();// 加算指定
                return;
            }
            r.dl |= 0x20;
            if (r.al == (byte)'-')
            {
                set_value();// 減算指定
                return;
            }
            //error1:
            error(); return;
        }
        
        private void set_value()
        {
            rednums();
            r.ah = r.al;
            (r.ah, r.dh) = (r.dh, r.ah);
            r.al = r.dl;
            stoswObjBufAX2DI();
            r.al = r.dh;
            stosbObjBufAL2DI();
            return;
        }

        //;-------------------------------------
        //;	変数指定チェック
        //;	entry	DS:BX = text adrs
        //;	exit	DS:BX = next text
        //;		CY = not X
        //;		AH = X nest (6-F)
        //;	break	CL,AL
        //;-------------------------------------

        public void chkval()
        {
            chktxt();
            mucomsub.xsmall();

            if (r.al != (byte)'X')
            {
                //chkval1:
                r.bx--;
                r.carry = true;
                return;
            }
            chkval_x();
        }

        private void chkval_x()
        {
            r.al = (byte)(r.bx >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[r.bx]);
            r.ah = 6;
            mucomsub.chknum();
            if (r.carry)
            {
                //chkval2:
                r.carry = false;
                return;
            }
            rednums();// 変数番号読み取り
            r.cl = 6;
            if (r.al > 9)
                error();

            r.al += 6;
            r.ah = r.al;
            //chkval2:
            r.carry = false;
            return;
        }

        //;･･･････････････････････････
        //;	総合音長の加算
        //;･･･････････････････････････

        public void add_tlen()
        {
            r.push(r.ax);
            r.push(r.bx);// AX = 加算する音長
            r.ah = 0;
            calc_alllen();
            uint ans = (uint)(alllen[r.bx/2] + alllen[r.bx/2 + 1] * 0x1_0000);
            ans += r.ax;
            alllen[r.bx/2] = (ushort)ans;
            alllen[r.bx/2 + 1] = (ushort)(ans >> 16);
            r.bx = r.pop();
            r.ax = r.pop();
            return;
        }

        public void init_looplen()
        {
            r.push(r.ax);
            r.push(r.bx);
            calc_alllen();
            r.ax = 0;
            alllen[r.bx/2] = r.ax;// ループ開始時の初期化 //熊:alllenはushort型
            alllen[r.bx/2 + 1] = r.ax;
            alllen[r.bx/2 + 2] = r.ax;
            alllen[r.bx/2 + 3] = r.ax;
            r.bx = r.pop();
            r.ax = r.pop();
            return;
        }

        public void exit_looplen()
        {
            r.push(r.ax);
            r.push(r.bx);
            calc_alllen();
            r.ax = alllen[r.bx / 2];// ループ脱出時に脱出までの音長を保存
            alllen[(r.bx + 4) / 2] = r.ax;
            r.ax = alllen[(r.bx+2) / 2];
            alllen[(r.bx + 6) / 2] = r.ax;
            r.bx=r.pop();
            r.ax=r.pop();
            return;
        }

        public void set_looplen()
        {
            r.push(r.ax);
            r.push(r.bx);
            r.push(r.dx);
            calc_alllen();
            r.dx = alllen[(r.bx + 4) / 2];// DX = ループ内の音長
            r.dx |= alllen[(r.bx + 6) / 2];
            int ans;
            if (r.dx != 0)
            {
                r.dx = alllen[(r.bx + 4) / 2];
                ans = alllen[(r.bx - 8) / 2] + r.dx;
                r.carry = ans > 0xffff;
                alllen[(r.bx - 8) / 2] = (ushort)ans;// 1つ前のネストへ加算
                r.dx = alllen[(r.bx + 6) / 2];
                alllen[(r.bx + 6) / 2] += (ushort)(r.dx + (r.carry ? 1 : 0));
                r.al--;
            }
            //looplen1:
            r.ah = 0;// AX = ループ回数
            r.push(r.ax);
            r.dx = alllen[(r.bx + 0) / 2];// [bx+2]は無視
            r.mul(r.dx);
            ans = alllen[(r.bx - 8) / 2] + r.ax;
            r.carry = ans > 0xffff;
            alllen[(r.bx - 8) / 2] = (ushort)ans;// 1つ前のネストへ加算
            alllen[(r.bx - 6) / 2] += (ushort)(r.dx + (r.carry ? 1 : 0));
            r.ax = r.pop();
            r.dx = alllen[(r.bx + 2) / 2];
            r.mul(r.dx);
            alllen[(r.bx - 6) / 2] += r.ax;

            r.dx = r.pop();
            r.bx = r.pop();
            r.ax = r.pop();
            return;
        }

        private void calc_alllen()
        {
            r.bx = 0;
            r.bl = nesting;
            r.bx <<= 3;
            r.bx += 0;//ofs:alllen
            return;
        }

    }
}