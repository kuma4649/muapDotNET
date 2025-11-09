using muapDotNET.Common;
using musicDriverInterface;
using System;
using System.Collections.Generic;

namespace muapDotNET.Compiler
{
    public class MUCOMSUB
    {
        private x86Register r;
        public MUCOM2 mucom2;
        public muap98 muap98;
        public ushort[] labelAdrs = new ushort[40 * 17];// ラベルの番地を保存する領域(40個×17チャネル分)
        private Work work;

        public MUCOMSUB(x86Register r, MUCOM2 mucom2, muap98 muap98,Work work)
        {
            this.r = r;
            this.mucom2 = mucom2;
            this.muap98 = muap98;
            this.work = work;

            InitCmddata();
            InitCmdJump();
        }

        //;
        //;	Music Macro Assembler for MUAP98< for Extended command>
        //;		copyright(c) 1987,1989-1995 by Packen Software[dec.29.1995]
        //;

        //;========================================
        //;	@xxxxの拡張コマンド分岐処理
        //;========================================

        public void exp_cmd()
        {
            r.si = 0;//ofs:cmddata
            r.cl = 0;

            //cmd5:
            do
            {
                r.push(r.bx);

                //cmd2:
                do
                {
                    r.al = (byte)cmddata[r.si];
                    r.si++;

                    if (r.al == 255)
                    {
                        cmd3();// なかった
                        return;
                    }

                    if (r.al == (byte)' ')
                    {
                        cmd1();// 発見した
                        return;
                    }

                    r.dl = (byte)(r.bx >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[r.bx]);
                    r.bx++;

                    if (r.dl >= (byte)'a')
                    {// 大文字変換
                        r.dl -= (byte)' ';
                    }

                } while (r.al == r.dl);

                //cmd4:
                do
                {
                    r.al = (byte)cmddata[r.si];// 次の検索文字まで移動
                    r.si++;
                } while (r.al != ' ');

                r.bx = r.pop();// ソース番地を戻す
                r.cl++;
            } while (true);
        }

        private void cmd1()
        {
            r.dx = r.pop();
            r.al = r.cl;
            r.ah = 0;
            r.ax += r.ax;
            r.si = 0;//ofs:cmdjump
            r.si += r.ax;
            r.dh = 0;// 変音､嬰音用
            cmdjump[r.si / 2]();
        }

        private void cmd3()
        {
            r.bx = r.pop();
            tonex();// @xxコマンドへ
        }

        private string cmddata;
        private void InitCmddata()
        {
            cmddata = "V W JUMP CALL "//0
            + "RET LABEL XASM POR "//4
            + "## ++ -- _ "//8
            + "+ # - % "//12
            + "IF SI SO MANU& "//16
            + "TRN XTRN MTRN XMTRN "//20
            + "MOR XMOR CAD XCAD "//24
            + "IDM XIDM TRI TRS "//28
            + "SACF SACS XMMTRN ACS "//32
            + "ACF AMTRN XAMTRN RP "//36
            + "CODEIN CODEOUT HARM ARP "//40
            + "NOOUT CODE BASS XCOM "//44
            + "< > RT COM "//48
            + "SC ACCEL RIT DEBUG "//52
            + "STOP MAX START RS "//56
            + "LFO DT ENDIF PAN "//60
            + "HLS HL PA POP "//64
            + "DS INIT QS QL "//68
            + "QX CSI CSO CSC "//72
            + "SLS SL FON F+ "//76
            + "F- KM KD @ "//80
            + "SD / FO AV "//84
            + "PV SSG PCM Q "//88
            + "CH SRET REV "//92
            + "L M R AP " + (char)255;//95
        }

        private Action[] cmdjump;
        private void InitCmdJump()
        {
            cmdjump = new Action[]{
                finevol,kwait,jump_to,call_to,                      //0
                ret_to,label_to,mucom2.theend,porta,                //4
                setf4,setf4,setf5,set_flat,                         //8
                setf1,setf1,setf2,setf3,                            //12
                if_jump,slur_in,slur_out,manual_tie,                //16
                trn_ent,xtrn_ent,mtrn_ent,xmtrn_ent,                //20
                mor_ent,xmor_ent,cad_ent,xcad_ent,                  //24
                idm_ent,xidm_ent,tri_ent,trspeed,                   //28
                sacf_ent,sacs_ent,xmmtrn_ent,acc_ent,               //32
                xacc_ent,amtrn_ent,xamtrn_ent,rhythm_pat,           //36
                mucom2.codein,mucom2.codeout,harm,mucom2.arpeggio,  //40
                wait_mode,mucom2.code_exe,mucom2.bass,comment_mode, //44
                cresc,decresc,rhythm_set,mcomment,                  //48
                mucom2.same_code,accel,ritard,set_debug,            //52
                mucom2.stopm,mucom2.set_max,initia,rhythm_pan,      //56
                lfo_set,detune,mendif,pan,                          //60
                hlfo_speed,hlfo_data,pcm_adrs,pops,                 //64
                detune_shift,sinit,macro_small,macro_large,         //68
                macro_exit,comstepin,comstepout,comstepcut,         //72
                slide_set,slide,clear_mode,freq_add,                //76
                freq_sub,key_maskset,key_maskreset,ifch1,           //80
                sysdetune,if_abort,fade_out,acc_vol,                //84
                down_vol,ssgmode,pcmmode,subratio,                  //88
                channel,last_set,reverve,                           //92
                pan_left,pan_mono,pan_right,auto_pan,               //95
            };
        }

        //;=================================
        //;	音色設定コマンド処理
        //;=================================

        private void tonex()
        {
            mucom2.rednums();// テキストの数字を読み取る
            r.dl = r.al;// DL = 音色番号
            mucom2.chktxt();
            if (r.al == (byte)'=')// 置換指定か
            {
                tone_change();
                return;
            }
            if (r.al == 0x22)// ユーザPCMの指定か
            {
                pcm_load();
                return;
            }
            r.bx--;
            xchg_tone();// 音色番号の置換
            if (r.ch >= 4)
            {
                if (r.ch < 7)
                {
                    goto retssg;// SSG ならスキップ
                }
                if (r.ch == 10)
                {
                    save_rhythm();// リズムは@1-@63まで
                    return;
                }
                if (r.ch >= 10)
                {
                    if (r.ch <= 11)
                    {
                        r.cl = 6;
                        if (r.al > MUCOM2.MAXPCM - 1)
                        {// PCMは@0-@89まで
                            mucom2.error();
                            return;
                        }
                    }
                }
            }

            //nowfm_tone:
            r.test(mucom2.optimiz, 1);
            if (r.zero || r.al != mucom2.opt_tne)// 直前に設定した音色と同じか
            {
                // 同じなら設定しない
                //tonex1:
                mucom2.opt_tne = r.al;// 設定した音色番号を保存する
                mucom2.optimiz |= 1;// 設定したよフラグ
                r.ah = r.al;

                r.al = 0xeb;// 音色番号の設定 +++
                LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
                linePos.chip = work.crntChip;
                linePos.chipNumber = 0;
                linePos.ch = (byte)work.crntChannel;
                linePos.part = work.crntPart;
                MmlDatum md = new MmlDatum(r.al, enmMMLType.Instrument, linePos, new object[] { 0, r.ah });
                md = work.FlashLstMd(md);
                muap98.object_Buf[r.di] = md;

                muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
                r.di += 2;
                return;
            }

        retssg:
            if (mucom2.ssgpcmm == 0)//ssgexit:
            {
                return;
            }

            r.cl = 6;
            if (r.al > 19)
            {
                mucom2.error();
                return;
            }

            r.ah = r.al;// AH = 音色番号
            r.al = 0xf6;// nコマンドを流用
            muap98.object_Buf[r.di] = new MmlDatum(r.al);
            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
            r.di += 2;
            return;
        }

        private void save_rhythm()
        {
            r.cl = 6;
            if (r.al == 0 || r.al > 63)
            {
                mucom2.error();
                return;
            }

            mucom2.rhydata = r.al;// 次にKした時キーオンする
            return;
        }

        private void xchg_tone()
        {
            r.push(r.ds);
            r.push(r.si);
            r.ds = muap98.text;
            r.si = 0;//TONEOFS// DS:SI = 音色置換テーブルの番地
            r.dh = 0;
            r.si += r.dx;
            r.al = mucom2.TONEOFSbuf[r.si];// AL = 変換された音色番号
            r.si = r.pop();
            r.ds = r.pop();
            return;
        }

        //;････････････････････････････････
        //;	音色番号の置換指定
        //;････････････････････････････････
        private void tone_change()
        {
            mucom2.chktxt();
            if (r.al != (byte)'@')// @x=@y という記述も可能にする
            {
                r.bx--;
            }
            //tonec1:
            mucom2.rednums();// AL = 変更先の音色番号
            r.push(r.ds);
            r.push(r.si);
            r.ds = muap98.text;
            r.si = 0;// DS:SI = 音色置換テーブルの番地
            r.dh = 0;
            r.si += r.dx;// SI = 該当音色の番地
            mucom2.TONEOFSbuf[r.si] = r.al;
            r.si = r.pop();
            r.ds = r.pop();
            return;
        }

        //;･･･････････････････････････････････
        //;	SSG/PCMモードの切り換え
        //;･･･････････････････････････････････

        private void ssgmode()
        {
            mucom2.ssgpcmm = 0;
            r.ax = 0x80d0;
            mucom2.stoswObjBufAX2DI();
            return;
        }

        private void pcmmode()
        {
            r.al = mucom2.ope_no;// AL = 1～17
            r.cl = 41;
            if (r.al == 11)
            {
                mucom2.error(); return;
            }
            if (r.al <= 3)
            {
                pcm4_mode();
                return;
            }
            if (r.al > 6)
            {
                pcm4_mode();
                return;
            }
            r.al = muap98.source_Buf[r.bx];
            chknum();// 数字チェック
            if (!r.carry)
            {
                pcm4_mode();
                return;
            }
            mucom2.ssgpcmm = 1;// SSGPCM
            r.ax = 0x81d0;
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;････････････････････････････････
        //;	拡張PCMへの切り換え
        //;････････････････････････････････

        private void pcm4_mode()
        {
            mucom2.rednums();
            r.cl = 6;
            if (r.al > 6)
            {
                // @PCM0,1～16
                mucom2.error(); return;
            }
            r.ah = r.al;
            r.al = 0xd0;
            mucom2.stoswObjBufAX2DI();
            if (r.ah != 0)
            {
                //pcm4mode1
                mucom2.sendch = 11;// PCMとしてアセンブルする
                return;
            }
            r.al = mucom2.ope_no;// 元に戻す
            mucom2.sendch = r.al;
            return;
        }

        //;･･････････････････････････････
        //;	DSPコマンドの指定
        //;･･････････････････････････････

        private void reverve()
        {
            r.zero = mucom2.sendch == 11;// PCMモード?
            r.cl = 32;
            if (!r.zero)
            {
                mucom2.error(); return;
            }
            r.al = 0xf2;
            mucom2.stosbObjBufAL2DI();
            r.dl = 3;// 値の範囲
            read_check();// DSPモード
            chkcm();
            r.dl = 127;
            read_check();// レベル
            chkcm();
            r.dl = (byte)(mucom2.MAXBUF - 2);
            read_check();// 遅延時間
        }

        private void read_check()
        {
            mucom2.rednums();
            r.cl = 6;
            if (r.al > r.dl)
            {
                mucom2.error(); return;
            }
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;･･････････････････････････････
        //;	@Qコマンドの処理
        //;･･････････････････････････････

        private void subratio()
        {
            mucom2.rednums();
            mucom2.ratdata = r.al;
            mucom2.ratmode |= 1;
            r.al = mucom2.chglen;
            mucom2.setrat();
        }

        //;---------------------------------
        //;	１文字単位置換の指定
        //;---------------------------------

        private void macro_small()
        {
            r.al = 1;// 小文字変換モード
            mac_set();
        }

        private void macro_large()
        {
            r.al = 2;// 大文字変換モード
            mac_set();
        }

        private void macro_exit()
        {
            r.al = 0;// 解除
            mac_set();
        }

        private void mac_set()
        {
            mucom2.mac_mod = r.al;
            return;
        }

        //;･･･････････････････････････････････
        //;	アクセントレベルの設定
        //;･･･････････････････････････････････

        private void acc_vol()
        {
            mucom2.rednums();
            mucom2.accbase = r.al;
            return;
        }

        //;････････････････････････････････････
        //;	一時音量下げレベルの設定
        //;････････････････････････････････････

        private void down_vol()
        {
            mucom2.rednums();
            mucom2.dwnbase = r.al;
            return;
        }

        //;---------------------------------------
        //;	@V コマンド（音量の細設定）
        //;---------------------------------------

        private void finevol()
        {
            mucom2.rednums();// テキストの数字を読み取る
            r.carry = r.ax < 128;// 範囲チェック
            r.cl = 6;
            if (!r.carry)
            {
                //error5:
                mucom2.error();return;
            }
            r.ah = r.al;
            mucom2.retvol2();
        }

        //;----------------------------
        //;	@W コマンド処理
        //;----------------------------

        private void kwait()
        {
            mucom2.set_symbol2();
            cres_check();// クレッシェンドチェック
            check_tiemode();// 自動タイ指定か
            if (r.zero)
            {
                r.al = 0xe1;// タイコマンドを格納 +++
                mucom2.stosbObjBufAL2DI();
            }
            //kwtie0:
            //+++ mucom2.dionpu = r.di;// 音符の演奏番地を保存
            mucom2.tnelnmx();// テキストから音符長を読みだし、変換
            mucom2.tnelnx();
            kwait0();
        }
        private void kwait0()
        {
            r.al = 0xfb;// +++
            mucom2.stosbObjBufAL2DI();// ダミーデータ
            return;
        }

        //;--------------------------------
        //;	@HARM コマンドの処理
        //;--------------------------------

        private void harm()
        {
            mucom2.rednums();// 和音スキップ数の獲得
            r.cl = 10;
            if (r.al > 15)
            {
                //error5:
                mucom2.error();return;
            }
            mucom2.harmno = r.al;// 和音スキップデータの保存
            return;
        }

        //;-------------------------------
        //;	PCM読みだしの指定
        //;	entry	DL = 音色番号
        //;-------------------------------

        private void pcm_load()
        {
            r.si = 0x24;// ES:SI = UsrPCMのヘッダ番地
            if ((byte)muap98.object_Buf[r.di - 1].dat == 0x88)
            {
                // 直前に指定してあるか
                //_load1:
                r.di--;// 連続指定の場合は識別コードをつぶす
                r.si = (ushort)((byte)muap98.object_Buf[r.si].dat + ((byte)muap98.object_Buf[r.si + 1].dat << 8));
                ushort ans = (ushort)((byte)muap98.object_Buf[r.si - 2].dat + ((byte)muap98.object_Buf[r.si - 1].dat << 8));
                ans += 14;
                muap98.object_Buf[r.si - 2] = new MmlDatum((byte)ans);
                muap98.object_Buf[r.si - 1] = new MmlDatum((byte)(ans >> 8));
            }
            else
            {
                r.ax = 0x13ea;// @jump命令を格納
                mucom2.stoswObjBufAX2DI();
                r.al = 0;
                mucom2.stosbObjBufAL2DI();
                muap98.object_Buf[r.si] = new MmlDatum((byte)r.di);
                muap98.object_Buf[r.si + 1] = new MmlDatum((byte)(r.di >> 8));// コマンド識別コード開始番地を格納
                r.al = 0x88;
                mucom2.stosbObjBufAL2DI();// 識別コードを格納
            }
            //_load10:
            r.al = r.dl;// AL = 読みだし音色番号(0～19,50～69)
            r.cl = 6;
            if (r.al >= 20 && r.al < 50)
            {
                mucom2.error(); return;
            }
            else if (r.al > 89)
            {
                // 範囲チェック
                mucom2.error(); return;
            }
            //_load11:
            mucom2.stosbObjBufAL2DI();
            r.cx = 8;
            //_load3:
            do
            {
                mucom2.chktxt();
                if (!r.carry)
                {
                    xsmall();// 漢字以外は小文字変換
                }
                if (r.al <= (byte)' ')
                {
                    r.cl = 39;
                    mucom2.error(); return;
                }
                if (r.al == 0x22)
                {
                    // 指定終了か
                    //_load5
                    r.cx += 3;// 拡張子の分も含める
                    _load7();
                    return;
                }
                if (r.al == (byte)'.')
                {
                    // 以降拡張子か
                    //_load2:
                    do
                    {
                        r.al = 0x20;
                        mucom2.stosbObjBufAL2DI();// ファイル名の8文字に不足する部分をうめる
                        r.cx--;
                    } while (r.cx != 0);
                    _load4();
                    return;
                }
                mucom2.stosbObjBufAL2DI();
                r.cx--;
            } while (r.cx != 0);

            mucom2.chktxt();
            if (r.al != (byte)'.')
            {
                r.bx--;
            }
            _load4();
        }

        private void _load4()
        {
            r.cx = 3;
            //_load8:
            do
            {
                mucom2.chktxt();
                if (!r.carry)
                {
                    xsmall();// 漢字以外は小文字変換
                }
                if (r.al <= (byte)' ')
                {
                    r.cl = 39;
                    mucom2.error(); return;
                }
                if (r.al == 0x22)
                {
                    // 指定終了か
                    _load7();
                    return;
                }
                mucom2.stosbObjBufAL2DI();
                r.cx--;
            } while (r.cx != 0);

            mucom2.chktxt();
            if (r.al != 0x22)
            {
                r.cl = 32;
                mucom2.error(); return;
            }
            _load9();
            return;
        }

        private void _load7()
        {
            r.al = 0x20;
            //_load6:
            do
            {
                mucom2.stosbObjBufAL2DI();// 拡張子なしの場合後半を全てうめる
                r.cx--;
            } while (r.cx != 0);

            _load9();
        }

        private void _load9()
        {
            mucom2.chktxt();
            r.bx--;
            r.zero = (r.al == (byte)',');// 音量の指定があるか（SSGPCM用）
            r.al = 16;// デフォルトの音量値
            if (r.zero)
            {
                r.bx++;
                mucom2.rednums();// AX = 音量値(0～127)
                r.cl = 6;
                r.al += mucom2.volsft;
                if (r.al > 127)
                {
                    mucom2.error(); return;
                }
            }

            //_load12:
            r.ah = 0;
            r.ax <<= 7;
            if (r.dl >= 20)
            {
                // SSGPCM以外は256
                r.ax = 256;
            }
            mucom2.stoswObjBufAX2DI();// 音量を格納
            r.al = 0x88;
            mucom2.stosbObjBufAL2DI();// 最後の識別子を格納
            return;
        }

        //;------------------------------------
        //;	"/" による和音指定とばし
        //;------------------------------------

        public void harm_onpu()
        {
            do
            {
                r.al = muap98.source_Buf[r.bx];
                r.bx++;
                r.cl = 32;
                if (r.al >= 0xfe)
                {
                    // CR,LFなら error end
                    //error24:
                    mucom2.error(); return;
                }
                if (r.al == (byte)':')
                {
                    //harmon1:
                    return;
                }
                if (r.al == (byte)'@')
                {
                    r.push(mucom2.octdata);
                    mucom2.mode[0] |= 4;
                    exp_cmd();// @ 系コマンドの実行(@+,@-,@%)
                    mucom2.octdata = (byte)r.pop();
                }
            } while (true);
        }

        //;	&の自動タイ指定禁止

        private void manual_tie()
        {
            mucom2.mode[0] |= 8;
            return;
        }

        //;-----------------------------
        //;	周波数加算・減算
        //;-----------------------------

        private void freq_add()
        {
            mucom2.rednum();
            r.dx = 0;
            fadd3();
        }

        private void fadd3()
        {
            r.ch = mucom2.sendch;
            if (r.ch == 10)
            {
                //fadd2:
                return;
            }
            if (r.ch >= 4)
            {
                if (r.ch < 7)
                {
                    r.ax = (ushort)-r.ax;// SSGの場合のみ反転する
                    r.dx = (ushort)~r.dx;
                }
            }
            //fadd1:
            r.push(r.ax);
            r.al = 0xf8;// +++
            mucom2.stosbObjBufAL2DI();
            r.ax = r.pop();
            mucom2.stoswObjBufAX2DI();
            r.al = r.dl;
            r.ah = 0xe1;// タイも格納 +++
            mucom2.stoswObjBufAX2DI();
            //fadd2:
            return;
        }

        private void freq_sub()
        {
            mucom2.rednum();
            r.dx = 0;
            r.dx--;
            r.ax = (ushort)-r.ax;
            fadd3();
        }

        //;-----------------------------
        //;	演奏スタックのpop
        //;-----------------------------

        private void pops()
        {
            r.al = 0xd2;
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;--------------------------------
        //;	演奏スタックの初期化
        //;--------------------------------

        private void sinit()
        {
            r.al = 0xe5;
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;----------------------------------
        //;	送信チャネルの切り換え
        //;----------------------------------

        private void channel()
        {
            mucom2.rednums();
            r.cl = 6;
            if (r.al > 17)
            {
                mucom2.error();return;
            }
            if (r.al == 0)
            {
                mucom2.error(); return;
            }
            mucom2.sendch = r.al;// 送信チャネルを保存(1～17)
            muap98.object_Buf[r.di] = new MmlDatum(0xcf);
            r.di++;
            mucom2.stosbObjBufAL2DI();
            mucom2.mode[1] |= 8;// 音符の前に音色番号を毎回出力する
            return;
        }

        //;--------------------------------------
        //;	直前の音色・音量に切り換え
        //;--------------------------------------

        private void last_set()
        {
            r.al = 0xce;
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;=========================================
        //;	@if jump/call/then/exitの処理
        //;=========================================

        private void if_jump()
        {
            mucom2.chktxt();
            if (r.al == (byte)'#')
            {
                // チャネル条件か
                if_channel();
                return;
            }
            r.bx--;
            mucom2.chkval();// 変数チェック
            if (r.carry)
            {
                if_norm();// AH = 6-F (変数領域指定)
                return;
            }
            mucom2.chktxt();
            r.cl = 32;
            r.dh = 0;
            if (r.al == (byte)'=')
            {
                if_match();
                return;
            }
            r.dh = 0x10;
            if (r.al == (byte)'>')
            {
                if_match();
                return;
            }
            r.dh = 0x20;
            if (r.al == (byte)'<')
            {
                if_match();
                return;
            }
            r.dh = 0x30;
            if (r.al == (byte)'!')
            {
                if_match();
                return;
            }

            mucom2.error(); return;
        }

        private void if_match()
        {
            mucom2.ifflag |= 1;// @if･･の実行（総合音長用）
            r.push(r.ax);
            mucom2.rednums();// AL = 変数条件値
            r.dl = r.al;
            r.ax = r.pop();
            r.ah |= r.dh;

            if_value();
        }

        private void if_norm()
        {
            mucom2.ifflag |= 1;// @if･･の実行（総合音長用）
            mucom2.rednums();// AL = 条件値
            r.dl = r.al;
            r.ah = mucom2.nesting;// ネストの値
            r.carry = r.ah < 1;
            r.ah -= 1;
            r.cl = 25;// ()外にif命令あり
            if (r.carry)
            {
                mucom2.error(); return;
            }
            if_value();
        }

        private void if_value()
        {
            mucom2.chktxt();// 次の文字を獲得
            xsmall();// ALの大文字変換
            r.cl = 32;
            if (r.al == (byte)'J')
            {
                // @IF ･･･ JUMP の処理
                if_jump0();
                return;
            }
            if (r.al == (byte)'C')
            {
                // @IF ･･･ CALL の処理
                if_call0();
                return;
            }
            if (r.al == (byte)'T')
            {
                // @IF ･･･ THEN の処理
                if_then0();
                return;
            }
            if (r.al == (byte)'E')
            {
                // @IF ･･･ EXIT の処理
                if_exit0();
                return;
            }
            //error6:
            mucom2.error(); return;
        }

        private void if_jump0()
        {
            mucom2.ifflag |= 1;// @if･･の実行（総合音長用）
            r.al = 0xe4;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            r.al = r.dl;// ジャンプ条件値
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            chgax("UMP");
            if_quit();
        }

        private void if_quit()
        {
            get_val();// ラベル番号
            if_quit1();
        }

        private void if_quit1()
        {
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            return;
        }

        private void if_call0()
        {
            mucom2.ifflag |= 1;// @if･･の実行（総合音長用）
            r.al = 0xe3;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            r.al = r.dl;// ジャンプ条件値
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            chgax("ALL");
            if_quit();
        }

        private void if_then0()
        {
            mucom2.ifflag |= 1;// @if･･の実行（総合音長用）
            r.al = 0x80;// AL = @if then 識別子(パス2でe4に書き換える)
            if_sub();
            r.ah ^= 0x30;
            muap98.object_Buf[r.di - 2] = new MmlDatum(r.ah);// 条件値を反転
            r.ah &= 0x30;
            if (r.ah == 0x10)
            {
                muap98.object_Buf[r.di - 1].dat = (byte)(muap98.object_Buf[r.di - 1].dat - 1);
            }
            if (r.ah == 0x20)
            {
                muap98.object_Buf[r.di - 1].dat = (byte)(muap98.object_Buf[r.di - 1].dat + 1);
            }
            chgax("HEN");
            r.dh = mucom2.nesting;
            r.dl = 1;// @if then モード
            pushif();
            if_quit1();
        }

        private void if_exit0()
        {
            r.al = 0x81;// AL = @if exit識別子(パス2でd3に書き換える)
            if_sub();
            chgax("XIT");
            if_exit1();
        }

        private void if_exit1()
        {
            r.dh = mucom2.nesting;
            if (r.dh == 0)
            {
                // ループの外はエラーにする
                //error16:
                mucom2.error(); return;
            }
            r.dl = 2;// @if exit モード
            pushif();
            mucom2.exit_looplen();
            if_quit1();
        }

        private void if_sub()
        {
            r.cl = 35;
            if (mucom2.jumpnes >= 32)
            {
                //error16:
                mucom2.error(); return;
            }
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            r.al = r.dl;// ジャンプ条件値
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            return;
        }

        //;･･････････････････････････････
        //;	チャネル条件分岐
        //;･･････････････････････････････

        private void if_channel()
        {
            r.dl = 0;// DL = 一致フラグ
        ifch5:
            mucom2.rednums();
            r.dh = r.al;
            if (r.al == mucom2.ope_no)
            {
                r.dl = 1;
            }
            //ifch2:
            do
            {
                mucom2.chktxt();
                if (r.al == (byte)',')
                {
                    // 複数指定
                    goto ifch5;
                }
                if (r.al != (byte)'-')
                {
                    ifch3();
                    return;
                }
                mucom2.rednums();// 連続指定
                if (r.al < mucom2.ope_no)
                {
                    continue;
                }
                if (r.dh > mucom2.ope_no)
                {
                    continue;
                }
                r.dl = 1;
            } while (true);
        }

        private void ifch3()
        {
            r.bx--;
            if (r.dl != 0)
            {
                // 現在のチャネルと一致か
                return;
            }
            //ifch4:
            do
            {
                mucom2.chktxt();// @@の検索
                if (r.al != (byte)'@')
                {
                    continue;
                }
                r.al = muap98.source_Buf[r.bx];
                if (r.al != (byte)'@')
                {
                    continue;// @@まではアセンブルしない
                }
                break;
            } while (true);
            r.bx++;
            //ifch1:
            return;
        }

        private void ifch1()
        {
            return;
        }

        //;･････････････････････････････････････････
        //;	最後にループ脱出する処理（@/）
        //;･････････････････････････････････････････

        private void if_abort()
        {
            r.al = 0x81;// AL = @if exit識別子(パス2でd3に書き換える)
            if_sub();
            r.push(r.bx);
            r.al = mucom2.nesting;
            r.ah = 0;
            r.ax--;
            r.ax <<= 2;
            r.bx = 2;//ofs:stttbl+2
            r.bx += r.ax;
            r.ax = r.di;
            r.ax--;
            mucom2.stttbl[r.bx/2] = r.ax; // カウンタのループ値を格納する番地を格納
            r.bx = r.pop();
            if_exit1();
        }

        //;---------------------------------------------
        //;	Jump,Call,Then,Exitの文法チェック
        //;---------------------------------------------

        private void chgax(string str)
        {
            r.si = 0;//ofs str
            r.al = muap98.source_Buf[r.bx];
            r.ah = muap98.source_Buf[r.bx + 1];// AX,DL の大文字変換
            r.cl = 32;
            r.bx += 2;
            r.dl = muap98.source_Buf[r.bx];
            r.bx++;
            xsmall();// ALの大文字変換
            if (r.ah >= (byte)'a')
            {
                r.ah -= (byte)' ';
            }
            if (r.dl >= (byte)'a')
            {
                r.dl -= (byte)' ';
            }
            if (r.al != str[r.si] || r.ah != str[r.si + 1])
            {
                //error16:
                mucom2.error(); return;
            }
            r.si += 2;
            if (r.dl != str[r.si])
            {
                //error16:
                mucom2.error(); return;
            }
            r.si++;
            //	push	si //熊:不要
            return;
        }

        //;------------------------------
        //;	IFスタックのpush
        //;	entry	DL = モード
        //;		DH = ネスト値
        //;		DI = 番地
        //;------------------------------

        private void pushif()
        {
            r.push(r.ds);
            r.push(r.bx);
            r.bx = 0;
            r.bl = mucom2.jumpnes;
            r.bx <<= 2;
            r.bx += 0;//ofs IFSTACK	; DS:BX = ifスタックの番地
            r.ds = muap98.text;
            mucom2.IFSTACKbuf[r.bx] = r.dl;
            mucom2.IFSTACKbuf[r.bx + 1] = r.dh;
            mucom2.IFSTACKbuf[r.bx + 2] = (byte)r.di;
            mucom2.IFSTACKbuf[r.bx + 3] = (byte)(r.di >> 8);
            r.bx = r.pop();
            r.ds = r.pop();
            mucom2.jumpnes++;
            return;
        }

        //;=============================================
        //;	@jump,@call,@ret,@label,の処理
        //;=============================================

        private void jump_to()
        {
            get_val2();
            r.ah = r.al;
            r.al = 0xea;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            r.di++;// オフセット値の所に変数番号を入れておく
            return;
        }

        private void call_to()
        {
            get_val2();
            r.ah = r.al;
            r.al = 0xe9;
            mucom2.stoswObjBufAX2DI();
            r.di++;
            r.al = mucom2.chglen;// 前回の音長、割合をセットしておく
            mucom2.setrat();
            mucom2.optimiz = 0;// 最適化フラグをクリア
            return;
        }

        private void ret_to()
        {
            r.al = 0xe8;
            mucom2.stosbObjBufAL2DI();
            mucom2.optimiz = 0;// 最適化フラグをクリア
            return;
        }

        private void label_to()
        {
            get_val2();
            r.push(r.si);
            r.si = 0;//ofs:bufbuf
            labelAdrs[r.al+(r.ch-1)*40]=r.di;// ラベルの番地を保存
            r.ax += r.ax;
            r.si += r.ax;
            muap98.bufbuf[r.si] = (byte)r.di;// 開始番地を格納
            muap98.bufbuf[r.si + 1] = (byte)(r.di >> 8);
            r.si = r.pop();
            r.al = mucom2.chglen;// 前回の音長、割合をセットしておく
            mucom2.setrat();
            mucom2.optimiz = 0;// 最適化フラグをクリア
            return;
        }

        private void get_val2()
        {
            mucom2.rednums();// ラベル変数獲得(絶対jump用)
            r.zero = r.al == 39;
            r.carry = (r.al < 39);
            getv1();
        }

        private void getv1()
        {
            r.cl = 6;
            if (!r.zero && !r.carry)
            {
                mucom2.error(); return;
            }
            return;
        }

        private void get_val()
        {
            mucom2.rednums();// ラベル変数獲得(@if用)
            r.zero = r.al == 31;
            r.carry = r.al < 31;
            getv1();
        }

        //;-----------------------
        //;	パン設定
        //;-----------------------

        private void pan()
        {
            mucom2.chktxt();
            xsmall();
            if (r.al == (byte)'L')
                pan_left();
            else if (r.al == (byte)'R')
                pan_right();
            else if (r.al == (byte)'M')
                pan_mono();
            else
            {
                r.cl = 32;
                mucom2.error();
            }
        }

        private void pan_left()
        {
            check_panm();
            r.ah = 0x80;// @L
            if (!r.carry)
            {
                r.ah = 0x82;// @LM
                if (r.zero)
                {
                    r.ah = 6;// @LK
                }
            }
            pan_set();
        }

        private void pan_right()
        {
            check_panm();
            r.ah = 0x40;// @R
            if (!r.carry)
            {
                r.ah = 0x41;// @RM
                if (r.zero)
                {
                    r.ah = 9;// @RK
                }
            }
            pan_set();
        }

        private void pan_mono()
        {
            check_panm();
            r.ah = 0xc0;// @M
            if (!r.carry)
            {
                r.ah = 0xc4;// @MK
                if (r.zero)
                {
                    r.ah = 0xcc;// @MM
                }
            }
            pan_set();
        }

        //;	パンのビット定義
        //;	b0 = left 1/2	b1 = right 1/2
        //;	b2 = left neg	b3 = right neg
        //;	b7 = left out   b6 = right out
        private void pan_set()
        {
            if (r.ch < 4)
            {
                //pan1:
                set_pan();
                //pan0:
                pan_init();// オートパンの初期化
                return;
            }
            if (r.ch < 7)
            {
                // SSGは無視
                //pan0:
                pan_init();// オートパンの初期化
                return;
            }
            if (r.ch == 10)
            {
                // リズム用
                pan2();
                return;
            }

            //pan1:
            set_pan();
            //pan0:
            pan_init();// オートパンの初期化
            return;
        }

        //;	パン指定のチェック
        //;	exit	@L = CY
        //;		@LM = NC,NZ
        //;		@LK = NC,Z
        private void check_panm()
        {
            if (mucom2.sendch != 11)
            {
                // PCMモードの場合のみ許可
                //chkm3:
                r.zero = false;
                r.carry = true;
                return;
            }
            r.al = muap98.source_Buf[r.bx++];

            if (muap98.source_Buf[r.bx - 2] >= (byte)'a')
            {
                // 直前の文字が小文字?
                r.al -= (byte)' ';
            }

            if (r.al == (byte)'M')
            {
                // @LM
                //chkm1:
                r.carry = false;
                r.zero = r.al == 0;
                return;
            }

            if (r.al == (byte)'K')
            {
                // @LK
                //chkm2:
                r.al = 0;
                //chkm1:
                r.carry = false;
                r.zero = r.al == 0;
                return;
            }

            r.bx--;
            //chkm3:
            r.zero = false;
            r.carry = true;
            return;
        }

        //;	リズム音源用パン指定

        private void pan2()
        {
            chkcm();// カンマチェック
            mucom2.chktxt();// 1文字取り出し
            xsmall();// 大文字変換
            r.dl = r.ah;
            setrt();// リズム種類のチェック
            if (r.al == 0)
            {
                //	je	error21
            }
            r.ch = 0;
            r.si = 0;//ofs:rhyvol	; SI = リズム音量バッファ
            r.si += r.cx;
            r.dl |=mucom2.rhyvol[r.si];// DL = パン＋音量
            r.cl += 0x18;// CL = 出力するアドレス
            r.al = r.dl;
            r.ah = r.cl;
            set_rhythmpan();// リズムパンコマンドの格納
        }

        //;･････････････････････････････････････
        //;	パンの設定（最適化つき）
        //;	entry	AH = パンデータ
        //;･････････････････････････････････････

        private void set_pan()
        {
            r.test(mucom2.optimiz, 2);//パン設定済みか
            if (r.zero || r.ah != mucom2.opt_pan)
            {
                //span1:
                mucom2.opt_pan = r.ah;// 設定したパンを保存
                mucom2.optimiz |= 2;

                r.al = 0xf6;// パン指定コマンドを格納
                LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
                linePos.chip = work.crntChip;
                linePos.chipNumber = 0;
                linePos.ch = (byte)work.crntChannel;
                linePos.part = work.crntPart;
                MmlDatum md = new MmlDatum(r.al, enmMMLType.Pan, linePos, new object[] { (int)r.ah });
                md = work.FlashLstMd(md);
                muap98.object_Buf[r.di++] = md;// 休符を格納
                //muap98.object_Buf[r.di++] = new MmlDatum(r.al);
                muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
                return;
            }

            // 直前に設定したパンと同じか
            // 同じならコマンド格納しない
            //span2:
            return;
        }

        //;･･･････････････････････････････････
        //;	リズム音源用パン設定
        //;	entry	AX = パラメータ
        //;･･･････････････････････････････････

        private void set_rhythmpan()
        {
            r.test(mucom2.optimiz, 8);// リズムパン設定済みか
            if (!r.zero)
            {
                if (r.ax == mucom2.opt_rhy)
                {
                    // 直前に指定したパンと同じか
                    // 同じならコマンドを格納しない
                    //srpan2:
                    return;
                }
            }
            //srpan1:
            mucom2.opt_rhy = r.ax;// 設定したパンを保存
            mucom2.optimiz |= 8;
            r.push(r.ax);
            r.al = 0xee;
            mucom2.stosbObjBufAL2DI();// コマンドの指定
            r.ax = r.pop();
            mucom2.stoswObjBufAX2DI();
            //srpan2:
            return;
        }

        //;------------------------------
        //;	オートパンの初期化
        //;------------------------------

        public void pan_init()
        {
            r.si = 0;//ofs:pandata
            mucom2.panadrs = r.si;
            mucom2.pandata[r.si] = 0;
            return;
        }

        //;------------------------------
        //;	オートふりふりパン
        //;------------------------------

        private void auto_pan()
        {
            r.ch = mucom2.sendch;
            if (r.ch >= 4)
            {
                if (r.ch < 7)
                {
                    //apan_skip:
                    return;
                }
                if (r.ch == 10)
                {
                    //apan_skip:
                    return;
                }
            }
            //apan_exe:
            mucom2.chktxt();
            if (r.al != (byte)'(')
            {
                //error21:
                r.cl = 32;
                //error22:
                mucom2.error(); return;
            }
            pan_init();// SI = パンパターン格納番地
            //apan_loop:
            do
            {
                mucom2.chktxt();
                xsmall();
                r.ah = 0x80;
                if (r.al != (byte)'L')
                {
                    r.ah = 0xc0;
                    if (r.al != (byte)'M')
                    {
                        r.ah = 0x40;
                        if (r.al != (byte)'R')
                        {
                            if (r.al == (byte)')')
                            {
                                //apan_exit:
                                mucom2.pandata[r.si] = 0;// 終了コードを格納

                                //apan_skip:
                                return;
                            }
                            //error21:
                            r.cl = 32;
                            //error22:
                            mucom2.error(); return;
                        }
                    }
                }
            apan_set:
                mucom2.pandata[r.si] = r.ah;
                r.si++;
            } while (r.si < 16);//ofs:pandata+16 // 16パターンまで指定可能

            r.cl = 37;
            mucom2.error(); return;
        }

        //;--------------------------------
        //;	オートパンのチェック
        //;--------------------------------
        public void pan_check()
        {
            r.push(r.si);
            r.push(r.ax);
            r.si = mucom2.panadrs;
            r.al = mucom2.pandata[r.si];
            if (r.al == 0)
            {
                r.si = 0;//ofs:pandata
                mucom2.panadrs = r.si;// パンパターン番地の初期化
            }
            //panchk1:
            r.al = mucom2.pandata[r.si];
            if (r.al != 0)
            {
                r.si++;
                mucom2.panadrs = r.si;
                r.ah = r.al;
                set_pan();// パンの指定
            }
            //panchk2:
            r.ax = r.pop();
            r.si = r.pop();
            return;
        }

        //;-------------------------
        //;	歌詞表示機能
        //;-------------------------

        private void mcomment()
        {
            mucom2.chkval();// 変数チェック

            if (!r.carry)
            {
                mucom2.chktxt();
                r.zero = (r.al == (byte)'=');// @com x= の指定
                r.cl = 32;
                if (!r.zero)
                {
                    mucom2.error();
                }

                r.push(r.ax);
                mucom2.rednums();
                r.dl = r.al;
                r.ax = r.pop();
                mucom2.chktxt();
                r.bx--;
                goto comment5;
            }

            //com_norm:
            chknum();// 数字チェック
            if (r.carry)
            {
                //comment1:
                r.ah = 0xff;// 条件指定なしの場合
                goto comment5;
            }
            mucom2.rednums();// 引き数読み込み
            r.dl = r.al;
            mucom2.chktxt();
            r.bx--;
            //comment2:
            r.ah = mucom2.nesting;// コマンド(DBH),ネスティング(FFH,0-5)
            r.ah--;

        comment5:
            r.cl = 32;
            if (r.al != 0x22)
            {
                mucom2.error();
            }
            r.bx++;
            if ((mucom2.commode & 1) == 0)
            {
                // 歌詞出力禁止か
                comment_set();
                return;
            }
            r.push(r.di);
            comment_set();
            r.di = r.pop();// 演奏番地を戻す
            return;
        }

        private void comment_set()
        {
            if ((mucom2.commode & 2) != 0)
            {
                // 色変わりモードか
                mucom2.comcnt = 0;
                r.push(r.ax);
                r.ax = 0xdd;
                // 桁位置を初期化
                muap98.object_Buf[r.di] = new MmlDatum(r.al);
                muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
                r.di += 2;
                r.ax = r.pop();
            }

            //comment6:
            r.al = 0xdb;
            muap98.object_Buf[r.di] = new MmlDatum(r.al);
            muap98.object_Buf[r.di + 1] = new MmlDatum(r.ah);
            r.di += 2;

            r.al = r.dl;// 条件データ
            muap98.object_Buf[r.di] = new MmlDatum(r.al);
            r.di++;
            r.dx = r.di;// ES:[DX] = 文字列長格納番地
            r.di++;

            r.ah = 0;// 文字列長
            r.cl = 33;// 歌詞データエラー

            work.compilerInfo ??= new CompilerInfo(); // インスタンスがない場合は作成する
            work.compilerInfo.addtionalInfo ??= new GD3Tag();// インスタンスがない場合は作成する
            if (!((GD3Tag)work.compilerInfo.addtionalInfo).dicItem.ContainsKey(enmTag.Lyric))
                ((GD3Tag)work.compilerInfo.addtionalInfo).dicItem.Add(enmTag.Lyric, new string[] { "MUS:UseLyric" });// 歌詞使用フラグを立てる

            //comment4:
            do
            {
                r.al = (byte)(r.bx >= muap98.source_Buf.Length ? 0 : muap98.source_Buf[r.bx]);
                r.bx++;
                if (r.al < (byte)' ')
                {
                    mucom2.error();// コントロールコードは不可
                }
                if (r.al == 0x22)
                {
                    //comment3:
                    r.push(r.di);
                    r.di = r.dx;
                    r.al = r.ah;
                    // 文字列長を格納
                    muap98.object_Buf[r.di] = new MmlDatum(r.al);
                    r.di++;
                    r.di = r.pop();
                    return;
                }
                muap98.object_Buf[r.di] = new MmlDatum(r.al);
                r.di++;
                r.ah++;
            } while (r.ah != 73); // 72文字まで

            //error7:
            mucom2.error();
        }

        //;---------------------------
        //;	歌詞出力の禁止
        //;---------------------------

        private void comment_mode()
        {
            mucom2.commode |= 1;
            return;
        }

        //;-----------------------------------
        //;	色変わり歌詞表示の設定
        //;-----------------------------------

        private void comstepin()
        {
            mucom2.commode |= 2;// 色変わり歌詞モードon
            return;
        }

        private void comstepout()
        {
            mucom2.commode &= 0xfd;
            stepcut2();
        }

        private void comstepcut()
        {
            mucom2.chktxt();
            chknum();
            if (r.carry)
            {
                //stepcut1:
                r.bx--;
                stepcut2();
                return;
            }
            r.bx--;
            mucom2.rednums();
            r.al += mucom2.comcnt;
            mucom2.comcnt = r.al;
            r.ah = r.al;
            r.al = 0xdd;// 指定値だけ加算する
            mucom2.stoswObjBufAX2DI();
            return;
        }

        private void stepcut2()
        {
            r.ax = 0xffdd;// 一気に白にする
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;--------------------------------
        //;	@_D- 変音調号セット
        //;--------------------------------

        private void set_flat()
        {
            r.al = muap98.source_Buf[r.bx];
            r.bx++;
            xsmall();// ALの大文字変換
            if (r.al == (byte)'I')
            {
                // 初期化用
                flat_init();
                return;
            }

            flat_param();
            r.push(r.bx);
            r.bx = 0;//ofs:flatdata
            r.ah = 0;
            r.al = r.dl;// 音符データ(0-6)
            r.bx += r.ax;
            mucom2.flatdata[r.bx] = r.dh;// 0,1,-1 をセット
            r.bx = r.pop();
            return;
        }

        //;	現チャネルの調号をクリア

        private void flat_init()
        {
            r.push(r.bx);
            r.bx = 0;//ofs:flatdata
            r.cx = 7;
            //finit1:
            do
            {
                mucom2.flatdata[r.bx] = 0;
                r.bx++;
                r.cx--;
            } while (r.cx != 0);
            r.bx = r.pop();
            return;
        }

        //;-----------------------------
        //;	@+,@-,@% 臨時変音
        //;-----------------------------

        private void setf5()
        {
            r.dh++;// 重変音(DH=4)
            setf4();
        }


        private void setf4()
        {
            r.dh++;// 重嬰音(DH=3)
            setf2();
        }

        private void setf2()
        {
            r.dh++;// 変音(DH=2)
            setf1();
        }


        private void setf1()
        {
            r.dh++;// 嬰音(DH=1)
            setf3();
        }

        private void setf3()
        {
            // ナチュラル(DH=0)
            r.test(mucom2.mode[0], 4);// <>を実行するか否か(和音､トリル用)
            if (!r.zero)
            {
                oct_exe();
                return;
            }
            r.push(r.bx);
            r.push(mucom2.octdata);
            mucom2.chktxt();
            if (r.al != (byte)'{')
            {
                // @+ { cde }4 のチェック
                r.bx--;
            }
            skipoct();// AL = 音符コード
            r.al -= (byte)'A';
            r.dl = r.al;
            calc_fpara();// 臨時変音コードの出力
            mucom2.octdata = (byte)r.pop();
            r.bx = r.pop();
            return;
        }

        private void oct_exe()
        {
            skipoct();// オクターブ変移を実行してしまう
            r.bx--;
            r.push(r.bx);
            r.al -= (byte)'A';
            r.dl = r.al;
            calc_fpara();
            r.bx = r.pop();
            mucom2.mode[0] &= 0xfb;
            return;
        }

        //;	<>を飛ばして音符コードの獲得

        public void skipoct()
        {
            do {
                mucom2.chktxt();
                if (r.al == (byte)'<')
                {
                    //skipoct1:
                    mucom2.octdown();
                    continue;
                }
                if (r.al == (byte)'>')
                {
                    //skipoct2:
                    mucom2.octup();
                    continue;
                }
                break;
            } while (true);
            xsmall();// ALの大文字変換
            return;
        }

        //;------------------------------------------
        //;	臨時変音生成コードの作成
        //;	entry	DH = 変音データ(0,1,2,3,4)
        //;		DL = 音符データ(0-6)
        //;------------------------------------------

        private void calc_fpara()
        {
            r.dh |= 0x80;// b7 = 1
            r.bx = 0;//ofs:flatdata2
            r.al = mucom2.octdata;// AL = 現在のオクターブ値
            r.ah = 7;
            r.mul(r.ah);
            r.bx += r.ax;
            r.al = r.dl;// AL = 音符データ(0-6)
            // 生成コード(b7:臨時あり,
            r.bx += r.ax;//  b2-b0:000=%,001=#,010=-,011=##,100=--)
            mucom2.flatdata2[r.bx] = r.dh;// 0,1,-1 をセット
            return;
        }

        //;--------------------------------------
        //;	@_のパラメータチェック
        //;	entry	AL = テキスト文字
        //;	exit	DH = %,#,- : 0,1,-1
        //;		DL = ABCDEFG : 0-6
        //;--------------------------------------

        private void flat_param()
        {
            r.al -= (byte)'A';
            r.dl = r.al;// DL = 音符データ(0-6)
            r.carry = (r.al < 7);
            r.cl = 28;
            if (!r.carry)
            {
                mucom2.error(); return;
            }

            r.al = muap98.source_Buf[r.bx];// 次のコードをチェック
            r.bx++;
            r.dh = 0xff;// 初期フラット
            if (r.al == (byte)'-')
            {
                //findflat:
                return;
            }
            r.dh = 1;// 初期シャープ
            if (r.al == (byte)'+')
            {
                //findflat:
                return;
            }
            if (r.al == (byte)'#')
            {
                //findflat:
                return;
            }
            r.dh = 0;// 元に戻す
            if (r.al == (byte)'%')
            {
                //findflat:
                return;
            }
            r.bx--;// 違えばテキスト番地を戻す
            //findflat:
            return;
        }

        //;--------------------------------------
        //;	小節終了時の臨時調号クリア
        //;--------------------------------------

        private void flat_clear()
        {
            r.push(r.es);
            r.push(r.di);
            r.push(r.cx);
            r.es = r.cs;
            r.cx = 56 / 2;
            r.ax = 0;
            r.di = 0;//ofs:flatdata2
            do
            {
                mucom2.flatdata2[r.di + 0] = r.al;
                mucom2.flatdata2[r.di + 1] = r.ah;
                r.di += 2;
                r.cx--;
            } while (r.cx != 0);
            r.cx = r.pop();
            r.di = r.pop();
            r.es = r.pop();
            return;
        }

        //;-------------------------------
        //;	クレッシェンド処理
        //;-------------------------------

        private void cresc()
        {
            r.ah = (byte)'<';
            cres_ent();
            mucom2.creslen = r.ax;
            r.ax = 0;
            mucom2.crescnt = r.ax;// 音長カウンタをクリア
            mucom2.dcrelen = r.ax;
            return;
        }

        //;--------------------------------
        //;	デクレッシェンド処理
        //;--------------------------------

        private void decresc()
        {
            r.ah = (byte)'>';
            cres_ent();
            mucom2.dcrelen = r.ax;
            r.ax = 0;
            mucom2.crescnt = r.ax;// 音長カウンタをクリア
            mucom2.creslen = r.ax;
            return;
        }

        //;･･････････････････････････････････････
        //;	クレッシェンドの初期処理
        //;	entry	AH = 比較記号(<>)
        //;･･････････････････････････････････････

        private void cres_ent()
        {
            r.al = mucom2.volsave;
            //r.al += mucom2.volsft;//+++ V=+の加味
            r.cl = 31;
            if (r.al > 127)
            {
                mucom2.error(); return;
            }
            mucom2.volstt = r.al;// 開始音量の保存
            r.dl = 1;// 音量の上昇分
            //cres2:
            do
            {
                r.al = muap98.source_Buf[r.bx];
                if (r.al != r.ah)
                {
                    break;
                }
                r.bx++;
                r.dl++;
            } while (true);
            //cres1:
            r.dh = r.dl;
            r.dl += r.dl;
            r.dl += r.dh;
            mucom2.cresvol = r.dl;// 3倍した値
            r.cl = 14;
            chknum();
            if (r.carry)
            {
                mucom2.error();return;
            }
            mucom2.tnelnm();// 音長解析(AX)
        }

        //;--------------------------------------
        //;	クレッシェンドのチェック
        //;--------------------------------------

        public void cres_check()
        {
            rit_check();// @ACC,@RITのチェック
            r.push(r.cx);
            r.cx = mucom2.creslen;
            if (r.cx != 0)
            {
                exe_cresc();
                return;
            }
            r.cx = mucom2.dcrelen;
            if (r.cx != 0)
            {
                exe_decre();
                return;
            }
            r.cx = r.pop();
            return;
        }

        private void exe_cresc()
        {
            r.push(r.ax);
            cres_main();
            if (r.carry)
            {
                //cres0:
                r.ax = r.pop();
                r.cx = r.pop();
                return;
            }
            r.al = mucom2.volstt;// 開始時の音量値

            //r.ah = mucom2.sendch;
            //if (r.ah < 4)
            //{
            //;	jb	cres6
            //}
            //if (r.ah < 7)// SSGは除外
            //{
            //;	jb	cres5
            //}
            //if (r.ah == 11)// PCMは除外
            //{
            //;	je	cres5
            //}
            //;cres6:
            //if (r.al == 0)
            //{
            // V0ならV1-@Vの値を代入
            //r.al = (byte)mucom2.VOLBASE;
            //}
            //;cres5:

            r.al += r.cl;
            if (r.al > 127)
            {
                r.al = 127;
            }
            //cres4:
            r.zero = r.al == mucom2.volsave;// 今の音量と一致するか
            r.ah = r.al;
            if (!r.zero)
            {
                mucom2.retvol3();// 音量の指定
            }
            //cres0:
            r.ax = r.pop();
            r.cx = r.pop();
            return;
        }

        private void exe_decre()
        {
            r.push(r.ax);
            cres_main();
            if (r.carry)
            {
                //cres0:
                r.ax = r.pop();
                r.cx = r.pop();
                return;
            }
            r.al = mucom2.volstt;
            r.al -= r.cl;
            if (r.carry)
            {
                r.al = 0;
            }

            //r.ah = mucom2.sendch;
            //if (r.ah < 4)
            //{
            //;	jb	cres7
            //}
            //if (r.ah < 7)// SSGは除外
            //{
            //;	jb	cres4
            //}
            //if (r.ah == 11)// PCMは除外
            //{
            //;	je	cres4
            //}
            //;cres7:
            //if (r.al == VOLBASE)
            //{
            // V1-@V以下になるとV0とする
            //r.al = 0;
            //}
            //cres4:
            r.zero = r.al == mucom2.volsave;// 今の音量と一致するか
            r.ah = r.al;
            if (!r.zero)
            {
                mucom2.retvol3();// 音量の指定
            }
            //cres0:
            r.ax = r.pop();
            r.cx = r.pop();
            return;
        }

        //;----------------------------------------
        //;	クレッシェンドサブルーチン
        //;	entry	CX = 全体の音長
        //;		chglen = 音符の長さ
        //;	exit	CX = 今回の音量変移値(@v)
        //;		CY = もう終わったぞ
        //;----------------------------------------

        private void cres_main()
        {
            r.push(r.dx);
            r.ah = 0;// CX = クレッシェンドの長さ
            r.al = mucom2.chglen;// AX = 今の音符の長さ
            r.ax += mucom2.crescnt;// AX = 開始からの音長
            mucom2.crescnt = r.ax;
            if (r.ax >= r.cx)
            {
                // 音長期間外かチェック
                //cres_end:
                init_cres();
                r.carry = true;
                r.dx = r.pop();
                return;
            }
            r.mul(mucom2.cresvol);
            r.div(r.cx);// AX = 現時点での音量変移値
            r.ax++;// 1つ加算しておく
            r.cx = r.ax;
            r.dx = r.pop();
            r.carry = false;
            return;
        }

        public void init_cres()
        {
            r.push(r.ax);
            r.ax = 0;
            mucom2.creslen = r.ax;
            mucom2.dcrelen = r.ax;
            r.ax = r.pop();
            return;
        }

        //;===============================
        //;	アチェレランド処理
        //;===============================

        private void accel()
        {
            mucom2.rednum();// AX = 増加するテンポの値
            accel1();
        }
        private void accel1()
        {
            mucom2.tmpdata = r.ax;
            chkcm();// カンマチェック
            mucom2.tnelnm();// AX = 音長期間(max=65535)
            mucom2.tmplen = r.ax;
            r.ax = mucom2.tempos;
            mucom2.tmpstt = r.ax;// 開始時のテンポを保存
            r.ax = 0;
            mucom2.tmpcnt = r.ax;// カウンタを初期化
            return;
        }

        //;-----------------------------
        //;	リタルダンド処理
        //;-----------------------------

        private void ritard()
        {
            mucom2.rednum();
            r.ax = (ushort)-r.ax;// 負の値にする
            accel1();
        }

        //;--------------------------------------------------
        //;	アチェレランド,リタルダンドのチェック
        //;--------------------------------------------------

        private void rit_check()
        {
            r.push(r.cx);
            r.cx = mucom2.tmplen;// CX = 全体の音長
            if (r.cx != 0)
            {
                exe_rit();
                return;
            }
            r.cx = r.pop();
            return;
        }

        private void exe_rit()
        {
            r.push(r.ax);
            rit_main();
            if (!r.carry)
            {
                r.ax = mucom2.tmpstt;// 開始時のテンポの値(T120など)
                r.ax += r.cx;
                if (r.ax < 16)
                {
                    r.ax = 16;
                }
                if (r.ax > 3907)
                {
                    r.ax = 3907;
                }
                r.push(r.ax);
                mucom2.calct();// AL = 3907/AX
                r.cx = r.ax;
                r.ax = mucom2.tempos;
                mucom2.calct();
                r.zero = r.ax == r.cx;// 内部的にテンポが一致するか
                r.ax = r.pop();
                if (!r.zero)
                {
                    mucom2.calc_tempo();// テンポ指定コマンド(CDxxxx)
                }
            }
            //rit_abort:
            r.ax = r.pop();
            r.cx = r.pop();
            return;
        }

        //;------------------------------------------
        //;	@ACC,@RITチェックサブルーチン
        //;	entry	CX = 全体の音長
        //;		chglen = 音符の長さ
        //;	exit	CX = 今回のテンポ変移値(+-)
        //;		CY = もう終わったぞ
        //;------------------------------------------

        private void rit_main()
        {
            r.push(r.dx);
            r.ah = 0;// CX = クレッシェンドの長さ
            r.al = mucom2.chglen;// AX = 今の音符の長さ
            r.ax += mucom2.tmpcnt;// AX = 開始からの音長
            mucom2.tmpcnt = r.ax;
            if (r.ax >= r.cx)
            {
                // 音長期間外かチェック
                //rit_end:
                mucom2.tmplen = 0;// @ACC,@RITの中止
                r.carry = true;
                r.dx = r.pop();
                return;
            }

            short a = (short)r.ax;
            short b = (short)mucom2.tmpdata;
            int ans = a * b;
            r.dx = (ushort)(ans >> 16);
            r.ax = (ushort)(short)ans;
            short quo = (short)(ans / (short)r.cx);
            short re = (short)(ans % (short)r.cx);
            r.ax = (ushort)quo;
            r.dx = (ushort)re;// AX = 現時点でのテンポ変移値
            r.cx = r.ax;
            r.dx = r.pop();
            r.carry = false;
            return;
        }

        //;-----------------------------
        //;	LFOデータの設定
        //;-----------------------------

        private void lfo_set()
        {
            mucom2.chktxt();
            xsmall();
            r.cl = 32;
            if (r.al == (byte)'P')
            {
                // @LFO P
                lfo_pmd();
                return;
            }
            if (r.al == (byte)'A')
            {
                // @LFO A
                lfo_amd();
                return;
            }
            if (r.al == (byte)'S')
            {
                // @LFO S
                lfo_stop();
                return;
            }
            if (r.al == (byte)'R')
            {
                // @LFO R
                lfo_reset();
                return;
            }
            //lerror:
            mucom2.error();
        }


        private void lfo_stop()
        {
            r.ax = 0x00d8;
            mucom2.stoswObjBufAX2DI(); // LFOの停止
            return;
        }
        private void lfo_reset()
        {
            r.ax = 0x03d8;// LFOカウンタのリセット
            mucom2.stoswObjBufAX2DI();
            return;
        }

        private void lfo_pmd()
        {
            r.ax = 0x02d8;
            lfo1();
        }

        private void lfo_amd()
        {
            r.ax = 0x01d8;
            lfo1();
        }
        private void lfo1()
        {
            mucom2.stoswObjBufAX2DI();// D8 xx
            r.al = muap98.source_Buf[r.bx];
            if (r.al != (byte)',')
            {
                //lfo0:
                return;
            }
            r.bx++;
            mucom2.rednums();// SYNCスイッチ(0/1/2/3)
            r.cl = 6;
            if (r.al > 3)
            {
                mucom2.error();
                return;
            }
            r.ah = r.al;
            r.al = 0xd9;
            mucom2.stoswObjBufAX2DI();// D9 SYNC
            chkcm();
            r.dh = 0;
            r.al = muap98.source_Buf[r.bx];
            if (r.al == (byte)'-')
            {
                r.dh++;
                r.bx++;
            }

            //lfo2:
            mucom2.rednums();// SPEED
            r.cl = 6;
            if (r.al > 127)
            {
                mucom2.error();
                return;
            }
            if (r.dh != 0)
            {
                r.al = (byte)-r.al;
            }
            r.dh = r.al;// AH = -128～127
            chkcm();
            mucom2.rednums();// LEVEL
            r.carry = (byte)muap98.object_Buf[r.di - 3].dat < 1;
            if ((byte)muap98.object_Buf[r.di - 3].dat != 1)// LFOAの場合については互換性を考えて
            {
                if (mucom2.mmlver < 0x26)
                {
                    // V2.6より下は奇数カットを行なう
                    r.al &= 0xfe;
                }
                r.carry = r.al < 128;
            }
            //lfo_amd1:
            bool cf = r.carry;
            //	pushf			; 0～127はLFOベースを-1して出力する
            if (!r.carry)
            {
                r.al >>= 1;// 半分にする(V2.12以降用)
            }
            r.ah = r.dh;
            mucom2.stoswObjBufAX2DI();
            r.cl = 1;// CL = スピードベース
            r.dx = 0x200;// DL = インクリメント値
            r.al = muap98.source_Buf[r.bx];// DH = ベース値（デフォルト）
            if (r.al == (byte)',')
            {
                // 以降のパラメータを省略してあるか
                r.bx++;
                r.al = muap98.source_Buf[r.bx];
                if (r.al != (byte)',')// ディレイパラメータはあるか
                {
                    //lfo5:
                    mucom2.rednums();// DELAY
                }
                else
                {
                    r.al = 0;// ディレイのみ省略時
                }
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
                r.al = muap98.source_Buf[r.bx];
                if (r.al == (byte)',')// 以降のパラメータを省略してあるか
                {
                    r.bx++;
                    r.al = muap98.source_Buf[r.bx];
                    if (r.al != (byte)',')// インクリメントパラメータはあるか
                    {
                        //lfo7:
                        mucom2.rednums();// INCREMENT
                        if (r.al > 15)
                        {
                            // 0～15まで
                            r.cl = 6;
                            mucom2.error();
                            return;
                        }
                    }
                    else
                    {
                        r.al = 0;// パラメータ省略時の処理
                    }

                    //lfo8:
                    r.dl = r.al;// DL = インクリメント値
                    r.al = muap98.source_Buf[r.bx];
                    if (r.al == (byte)',')// ベース値のパラメータを省略してあるか
                    {
                        r.bx++;
                        r.al = muap98.source_Buf[r.bx];
                        if (r.al != (byte)',')// ベース値のパラメータは存在するか
                        {
                            mucom2.rednums();
                            if (r.al > 6)
                            {
                                // ベース値は0～6まで
                                r.cl = 6;
                                mucom2.error();
                                return;
                            }
                            r.dh = r.al;
                        }
                    }
                }
            }
            else
            {
                //lfo3:
                r.al = 0;// 以降全て省略時、ディレイを格納
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            }

        lfo9:;
            r.carry = cf;//	popf// CY = 1 : レベル0～127指定
            r.al = r.dl;
            if (r.carry)
            {
                r.dh++;
            }
            r.dh <<= 4;
            r.al |= r.dh;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);// インクリメント値(b0～b3)・ベース値(b4～b6)
            r.al = muap98.source_Buf[r.bx];
            r.ah = muap98.source_Buf[r.bx + 1];

            if (r.al == (byte)',')// 以降の値を省略してあるか
            {
                r.bx++;
                if (r.ah != (byte)',')// スピードベース値のパラメータを省略してあるか
                {
                    mucom2.rednums();
                    if (r.al > 3)
                    {
                        // スピードベース値は0～3まで
                        r.cl = 6;
                        mucom2.error();
                        return;
                    }
                    r.cl = r.al;
                }
                //lfo10:
                r.al = muap98.source_Buf[r.bx];
                if (r.al == (byte)',')// LFOくり返し回数が指定されているか
                {
                    r.bx++;
                    mucom2.rednums();
                    if (r.al > 63)
                    {
                        r.cl = 6;
                        mucom2.error();
                        return;
                    }
                    r.al <<= 2;
                    r.cl |= r.al;// スピードベース値にORする
                }
            }
            //lfo11:
            r.al = r.cl;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);

            //lfo0:
            return;

        }

        //;--------------------------------
        //;	ハードLFO速度の設定
        //;--------------------------------

        private void hlfo_speed()
        {
            mucom2.rednums();
            if (skip_ssg()) return;
            if (r.al == 0)
            {
                //hlfo_stop:
                r.ax = 0xef;
                //hlfo_set:
                mucom2.stoswObjBufAX2DI();
                return;
            }
            r.carry = r.al > 8;//ja
            r.cl = 6;
            if (r.carry)
            {
                //error17:
                r.cl = 6;
                mucom2.error();return;
            }
            r.al--;
            r.al |= 8;
            r.ah = r.al;
            r.al = 0xef;

            //hlfo_set:
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;----------------------------------
        //;	ハードLFOデータの設定
        //;----------------------------------

        private void hlfo_data()
        {
            mucom2.rednums();
            if (r.al > 7)
            {
                //error17:
                r.cl = 6;
                mucom2.error();return;
            }
            r.dh = r.al;// DH = PMS
            chkcm();
            mucom2.rednums();
            if (r.al > 3)
            {
                //error17:
                r.cl = 6;
                mucom2.error(); return;
            }
            r.dl = r.al;// DL = AMS
            chkcm();
            mucom2.rednums();
            if (r.al > 15)
            {
                //error17:
                r.cl = 6;
                mucom2.error(); return;
            }
            if (skip_ssg()) return;// SSGでは無視して戻る
            r.push(r.ax);
            r.dl <<= 4;
            r.dl |= r.dh;
            r.ah = r.dl;
            r.al = 0xee;
            mucom2.stoswObjBufAX2DI();
            r.ax = r.pop();
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;-------------------------
        //;	スラーの開始
        //;-------------------------

        private void slur_in()
        {
            r.cl = 12;
            if (mucom2.slurmod != 0)
            {
                //error12:
                mucom2.error(); return;
            }
            r.al = mucom2.ratdata;// 元のQを保存する
            r.al |= 0x80;
            mucom2.slurmod = r.al;
            r.al = 0;// Q8に設定する
            mucom2.chgrat();
        }

        //;------------------------
        //;	スラーの終了
        //;------------------------

        private void slur_out()
        {
            r.cl = 12;
            r.al = mucom2.slurmod;
            if (r.al == 0)
            {
                //error12:
                mucom2.error(); return;
            }
            r.al &= 0x7f;
            mucom2.chgrat();// Qを元に戻す
            mucom2.slurmod = 0;// @siモード解除
            return;
        }

        //;-----------------------------
        //;	@SL スライド処理
        //;-----------------------------

        private void slide_set()
        {
            mucom2.chktxt();
            if (r.al == (byte)'-')
            {
                //slneg1:
                mucom2.rednums();
                r.al = (byte)-r.al;
            }
            else
            {
                r.bx--;
                mucom2.rednums();
            }

            //slneg2:
            mucom2.slbase = r.al;
            chkcm();
            mucom2.tnelnmx();
            mucom2.slspeed = r.al;
            return;
        }

        private void slide()
        {
            r.ch = mucom2.sendch;
            r.al = 1;
            mucom2.setrat();// L192 にする
            r.push(r.di);
            if (r.ch == 11)
            {
                // PCMの場合周波数コードが3バイト
                r.di++;
            }
            r.di += 16;// freq,&(@f+&)n Len
            porta_sub();
            mucom2.porta2 = r.ax;// 終了周波数の保存
            if (r.ch == 11)
            {
                (r.al, r.ah) = (r.ah, r.al);
            }
            mucom2.por_end = r.di;// 終了番地
            r.di = r.pop();
            r.dx = r.ax;
            r.ah = mucom2.slbase;// AH = 変移する値
            mucom2.freq_lfo();
            r.ax += r.dx;
            if (r.ch == 11)
            {
                (r.al, r.ah) = (r.ah, r.al);
                r.push(r.ax);
                r.al = 0xd5;
                mucom2.stosbObjBufAL2DI();
                r.ax = r.pop();
            }

            //slide1:
            mucom2.porta1 = r.ax;// 開始周波数の保存
            (r.al, r.ah) = (r.ah, r.al);
            mucom2.stoswObjBufAX2DI();// 開始周波数を格納
            r.al = mucom2.slspeed;
            mucom2.porcnt = r.al;// 分割回数
            porta_main();// ポルタメント処理に任せる
            r.di = r.ax;// 演奏番地は戻さない
            mucom2.tnelnmx();// 音長データの獲得(AL)
            mucom2.add_tlen();
            r.al++;
            r.cl = 15;
            r.carry = r.al < mucom2.slspeed;
            r.al -= mucom2.slspeed;
            if (r.carry)
            {
                //error12:// 総合音長超過
                mucom2.error();return;
            }
            mucom2.setrat();
            r.di = mucom2.por_end;
            return;
        }

        //;===================================
        //;	@POR ポルタメント処理
        //;===================================

        public void porta()
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
            md = work.FlashLstMd(md);
            work.md = md;

            r.ch = mucom2.sendch;
            mucom2.chktxt();
            chknum();// ディレイ値が指定してあるか
            r.bx--;
            if (r.carry)
            {
                //portas1:
                r.al = 1;
            }
            else
            {
                mucom2.tnelnmx();
                r.ax++;// AL = ディレイ時間
            }

            //portas2:
            r.push(r.ax);
            r.push(mucom2.ratdata);
            mucom2.ratdata = 0;// Q8にする
            mucom2.setrat();// ディレイ時間+L192 待機する
            mucom2.ratdata = (byte)r.pop();
            porta_sub();
            work.md = null;
            mucom2.porta1 = r.ax;// 開始周波数の保存
            r.dx = r.pop();

            r.push(r.dx);
            r.push(r.di);
            if (r.dl != 1)
            {
                r.di += 3;
            }
            r.di += 11;// &(@f+&)n の分11バイト加算
            porta_sub();
            mucom2.porta2 = r.ax;// 終了周波数の保存
            mucom2.tnelnmx();// 音長データの獲得(AL)
            mucom2.add_tlen();
            mucom2.por_end = r.di;// 終了番地
            r.di = r.pop();
            r.dx = r.pop();

            r.dl--;// DL = ディレイ時間
            if (r.dl != 0)
            {
                r.push(r.ax);
                r.al = 1;
                mucom2.setrat();
                r.ax = r.pop();
            }

            //portas3:
            bool flg = r.al <= r.dl;
            r.al -= r.dl;
            if (flg)
            {
                //por_error:
                r.cl = 18;// ポルタメントの範囲が広すぎる
                mucom2.error(); return;
            }
            mucom2.porcnt = r.al; // 分割回数
            porta_main();
        }

        private void porta_main()
        {
            if (r.ch <= 3)
            {
                por_2203();
                return;
            }
            if (r.ch <= 6)
            {
                por_ssg();
                return;
            }
            r.cl = 22;
            if (r.ch == 10)// リズムの場合は不許可
            {
                //error17:
                r.cl = 6;
                mucom2.error(); return;
            }
            if (r.ch == 11)
            {
                por_pcm();
                return;
            }
            por_2203();
            return;
        }

        //;---------------------------------
        //;	YM2203 ポルタメント
        //;---------------------------------

        private void por_2203()
        {
            pre_por();
            r.ax = mucom2.porta1;
            calc_exp1();// 開始周波数を展開する
            mucom2.freqsv1 = r.ax;
            mucom2.freqsv2 = r.dx;
            r.ax = mucom2.porta2;
            calc_exp1();// 終了周波数を展開する
            r.carry = r.ax < mucom2.freqsv1;
            r.ax -= mucom2.freqsv1;
            ushort ans = (ushort)(mucom2.freqsv2 + (r.carry ? 1 : 0));
            r.carry = r.dx < ans;
            r.dx -= ans;// DXAX = 終了－開始周波数
            if (r.carry)
            {
                //por_fm1:
                r.ax = (ushort)-r.ax;
                r.dx = (ushort)~r.dx;
                div64();
                r.dx = (ushort)~r.dx;
                r.ax = (ushort)-r.ax;
            }
            else
            {
                div64();
            }

            //por_fm2:
            after_por();
        }

        //;--------------------------------
        //;	SSG のポルタメント
        //;--------------------------------

        private void por_ssg()
        {
            pre_por();
            r.ax = mucom2.porta2;
            r.carry = r.ax < mucom2.porta1;
            r.ax -= mucom2.porta1;// AX = 終了 - 開始
            if (r.carry)
            {
                //por_ssg1:
                r.ax = (ushort)-r.ax;
                div32();
                r.dx = (ushort)~r.dx;
                r.ax = (ushort)-r.ax;
            }
            else
            {
                div32();
            }

            //por_ssg2:
            after_por();
        }

        //;-----------------------------
        //;	PCM ポルタメント
        //;-----------------------------

        private void por_pcm()
        {
            pre_por();
            r.cx = 1;// n の値
            r.dx = mucom2.porta1;
            (r.dl, r.dh) = (r.dh, r.dl);// DX = 開始DELTA-N
            r.ax = mucom2.porta2;
            (r.al, r.ah) = (r.ah, r.al);// AX = 終了DELTA-N
            r.carry = r.ax < r.dx;
            r.ax -= r.dx;// AX = 終了－開始DELTA-N
            if (r.carry)
            {
                //por_pcm1:
                r.ax = (ushort)-r.ax;
                div32();
                r.dx = (ushort)~r.dx;
                r.ax = (ushort)-r.ax;
            }
            else
            {
                div32();
            }
            //por_pcm2:
            after_por();

            //after_por_main();
            //r.al=0xd5;
            //mucom2.stosbObjBufAL2DI();
            //r.ax=mucom2.porta2;// AX = 最終周波数
            //(r.al, r.ah) = (r.ah, r.al);
            //mucom2.stoswObjBufAX2DI();
            //return;
        }

        //;----------------------------------------------
        //;	YM2203用周波数展開・復元ルーチン
        //;	entry	DXAX = 2203周波数データ
        //;	exit	DXAX = 実周波数データ
        //;----------------------------------------------

        private void calc_exp1()
        {
            r.push(r.cx);
            r.cx = r.ax;// AX = オクターブ以下の周波数成分
            r.ax &= 0x7ff;
            r.cx >>= 11;// CX = オクターブ(0-7)
            r.dx = 0;
            if (r.cx == 0)
            {
                //por5:
                r.cx = r.pop();
                return;
            }

            //por1:
            do
            {
                r.carry = (r.ax & 0x8000) != 0;
                r.ax <<= 1;
                r.dx = r.rcl(r.dx, 1);
                r.cx--;
            } while (r.cx != 0);// DX AX = 展開した周波数データ

            //por5:
            r.cx = r.pop();
            return;
        }

        //;････････････････････････････････････････････
        //;	ポルタメント用除算
        //;	entry	DXAX = 差分データ（正のみ）
        //;	exit	DXAX = AX*256/(porcnt)
        //;････････････････････････････････････････････

        private void div32()
        {
            r.dx = 0;
            div64();
        }

        private void div64()
        {
            r.push(r.ax);
            r.al = r.ah;
            r.ah = r.dl;
            r.dl = r.dh;
            r.dh = 0;
            r.div(mucom2.porcnt);// 上位16bitを除算
            r.cx = r.ax;
            r.ax = r.pop();
            r.ah = r.al;
            r.al = 0;
            r.div(mucom2.porcnt);// 下位16bitを除算
            r.dx = r.cx;// DXAX = 商
            return;
        }

        //;･･･････････････････････････････
        //;	ポルタメント前処理
        //;･･･････････････････････････････

        private void pre_por()
        {
            r.ax = 0xe0e1;// ループ開始コマンド +++
            mucom2.stoswObjBufAX2DI();
            r.al = 0xf8;// 周波数加算 +++
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;････････････････････････････････
        //;	ポルタメント後始末
        //;	entry	DLAX = 差分値
        //;････････････････････････････････

        private void after_por()
        {
            after_por_main();
            r.ax = r.di;
            r.di = mucom2.por_end;// 最後の番地に戻す
            return;
        }

        private void after_por_main()
        {
            mucom2.stoswObjBufAX2DI();
            r.al = r.dl;
            r.ah = 0xe1;// +++
            mucom2.stoswObjBufAX2DI();// 変化量とタイを格納
            r.ax = 0x5f7;
            mucom2.stoswObjBufAX2DI();// ループの終了
            r.ax = 0;
            r.ah = (byte)mucom2.porcnt;
            r.carry = r.ah <= 2;//jbe
            r.ah -= 2;
            if (r.carry)
            {
                // L96以下は不可能
                //por_error:
                r.cl = 18;// ポルタメントの範囲が広すぎる
                mucom2.error();return;
            }
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;-----------------------------------------
        //;	ポルタメント処理サブルーチン
        //;-----------------------------------------

        private void porta_sub()
        {
        porta_sub:
            skipoct();// 次のテキストを読み<>の実行
            if (r.al == (byte)'@')
            {
                // @拡張コマンドも許可
                //por_set:
                mucom2.mode[0] |= 4;// <> を実行するフラグ
                exp_cmd();// @xx 拡張コマンドの実行
                goto porta_sub;
            }
            r.cl = 17;
            check_onpu();// 次は音符でなければならない
            r.push(r.ax);// AL = 音符データ(A-G)
            r.al = muap98.source_Buf[r.bx];
            mucom2.gethenon();// #+-%チェックをDLに返す
            r.ax = r.pop();
            read();// 周波数データを格納
            r.al = (byte)muap98.object_Buf[r.di - 2].dat;
            r.ah = (byte)muap98.object_Buf[r.di - 1].dat;
            (r.al, r.ah) = (r.ah, r.al);
            return;
        }

        //;=================================
        //;	@DEBUG コマンドの処理
        //;=================================

        private void set_debug()
        {
            mucom2.tnelnmx();// 休符音長の獲得
            mucom2.debug = r.al;
            return;
        }

        //;-------------------------------------------
        //;	改行時の臨時調号クリアチェック
        //;-------------------------------------------

        private void clear_mode()
        {
            mucom2.mode[0] |= 0x10;
            return;
        }

        public void check_flatclear()
        {
            if ((mucom2.mode[0] & 0x10) == 0)
            {
                return;
            }
            if ((mucom2.mode[1] & 1) == 0)
            {
                // []外はチェックしない
                return;
            }

            //;====================================
            //;	データ揃えマーク処理(')
            //;====================================
            wait_r();
        }

        public void wait_r()
        {
            flat_clear();// 臨時調号のクリア
            init_rhythm();// リズムテーブル番地の初期化

            if ((mucom2.mode[0] & 0x40) != 0)
            {
                // @NOOUTが効いているか
                return;
            }
            r.al = mucom2.debug;
            if (r.al != 0)
            {
                // 休符出力か
                mucom2.tnelnx();// 音長の格納
                r.al = 0xff;
                //	stosb			; 休符の格納(圧縮しない)
            }

            //wait_rr:
            r.al = 0xf3;
            //	stosb
            //nop_wait:
            return;
        }

        private void wait_mode()
        {
            mucom2.mode[0] |= 0x40;// '非出力モードにする
            return;
        }

        //;---------------------------------
        //;	フェードアウトの実行
        //;---------------------------------

        private void fade_out()
        {
            r.al = 0xd1;
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;=================================
        //;	停止チャネル有効にする
        //;=================================

        private void initia()
        {
            r.si = 0;
            r.dx = 0;// DXSI = ANDするデータ
            mucom2.chktxt();
            r.bx--;// テキストを戻す
            chknum();// 数字以外ならCY=1(全チャネル復活)
            if (!r.carry)
            {
                r.si--;
                r.dx--;//DXSI = FFFFFFFF

                //initi2:
                do
                {
                    mucom2.rednums();
                    r.cl = 6;
                    r.al--;// AL = 0-16
                    if (r.al >= 17)
                    {
                        //	jnb	error23
                    }
                    r.ch = r.al;
                    r.push(r.dx);
                    mucom2.calcbit();
                    r.ax = (ushort)~r.ax;
                    r.si &= r.ax;
                    r.al = r.dl;
                    r.al = (byte)~r.al;
                    r.dx = r.pop();
                    r.dl &= r.al;

                    mucom2.chktxt();
                    r.cl = 32;
                } while (r.al == (byte)',');
                // @init3,7,9,10 のカンマチェック

                r.bx--;
            }

            //initi1:
            r.al = 0xdc;
            mucom2.stosbObjBufAL2DI();// "***"によるチャネル停止をクリア
            r.ax = r.si;
            mucom2.stoswObjBufAX2DI();// ANDするデータ(24bit)を格納
            r.al = r.dl;
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;-----------------------------------------
        //;	リズム各音色のパン・音量指定
        //;-----------------------------------------

        private void rhythm_pan()
        {
            mucom2.chktxt();
            xsmall();
            setrt();
            if (r.al == 0)// AH = 指定リズムビット(1～20h)
            {
                r.cl = 32;
                //error23:
                mucom2.error(); return;
                // CL = リズム番号(0～5)
            }
            //rpan1:
            r.si = 0;//ofs:rhyvol
            r.ch = 0;
            r.si += r.cx;// SI = 音量保存バッファ
            r.dh = r.cl;
            r.dh += 0x18;// DH = 出力するアドレス

            chkcm();
            mucom2.chktxt();
            xsmall();
            r.dl = 0x80;
            if (r.al != (byte)'L')// 左指定
            {
                r.dl = 0x40;
                if (r.al != (byte)'R')// 右指定
                {
                    r.dl = 0xc0;
                    if (r.al != (byte)'M')//モノ指定
                    {
                        r.cl = 32;
                        mucom2.error(); return;
                    }
                }
            }

            //rpan_set:
            chkcm();// カンマチェック
            mucom2.rednums();
            r.carry = r.al > 31;// 音量の範囲チェック
            r.cl = 6;
            if (r.carry)
            {
                //error23:
                mucom2.error(); return;
            }
            mucom2.rhyvol[r.si] = r.al;// リズム音量バッファに保存する
            r.al |= r.dl;
            r.ah = r.dh;
            set_rhythmpan();// リズムパンコマンドの格納
        }

        //;----------------------------------
        //;	リズムパータンの展開
        //;----------------------------------

        public void rhyexp()
        {
            mucom2.set_symbol2();
            mucom2.tnelnmx();// 音長の解析
            mucom2.rtm_max = r.al;// AL = 音長
            rhyexp_code();
        }
        public void rhyexp_code()
        {
            r.push(r.ax);
            r.al = mucom2.rhydata;// @によって指定されているか
            if (r.al != 0)
            {
                set_dump();
                return;
            }
            r.ax = r.pop();

            //rhyexp1:
            do
            {
                r.dl = mucom2.rtm_max;
                getrp_table();// AL = 音長
                if (r.dl >= r.al)
                {
                    reend5();
                    return;
                }
                r.dl -= r.al;
                mucom2.rtm_max = r.dl;// 残りの音長
                reend4();
            } while (true);
        }

        private void reend5()
        {
            r.al = r.dl;// 全体の残りを音長にする
            reend4();
        }

        private void reend4()
        {
            mucom2.tnelnx();// 音長の格納
            if (r.cl == 255)
            {
                // パターン未定義
                //reend6:
                r.al = 0xf9;// +++
                mucom2.stosbObjBufAL2DI();// コマンド終了符号を出力
                mucom2.onpucnt++;
                //re_abort:
                return;
            }
            mucom2.dionpu = r.di;
            if (r.cx == 0)
            {
                // キーオン・ダンプの無指定
                //reend8:
                r.al = 0xff;// 休符を出力
                mucom2.stosbObjBufAL2DI();
                mucom2.onpucnt++;
                return;
            }
            if (r.ch != 0)
            {
                r.ah = r.ch;
                r.al = 0xef;// ダンプ
                mucom2.stoswObjBufAX2DI();
            }
            //reend7:
            if (r.cl != 0)
            {
                r.ah = r.cl;
                r.al = 0xf0;// キーオン
                mucom2.stoswObjBufAX2DI();
            }
            //reend6:
            r.al = 0xf9;// +++
            mucom2.stosbObjBufAL2DI();// コマンド終了符号を出力
            mucom2.onpucnt++;
            //re_abort:
            return;
        }

        private void set_dump()
        {
            r.dl = r.al;
            r.ax = r.pop();
            mucom2.tnelnx();// 音長の格納
            mucom2.dionpu = r.di;
            r.ah = r.dl;
            r.al = 0xf0;// キーオン
            if (work.md == null)
            {
                mucom2.stoswObjBufAX2DI();
            }
            else
            {
                work.md.args[work.mdArgsStep + 0] = (int)r.ah;
                work.md.args[work.mdArgsStep + 1] = work.otoLength;
                mucom2.stoswObjBufAX2DI(work.md);
            }
            r.al = 0xf9;// +++
            mucom2.stosbObjBufAL2DI();
            mucom2.onpucnt++;
            return;
        }

        private void getrp_table()
        {
            getrp_main();
            if (r.cl != 255)
            {
                return;
            }
            //getrp_init:
            init_rhythm();
            getrp_main();
        }

        private void getrp_main()
        {
            r.push(r.si);
            r.si = mucom2.rhyadrs;// SI = リズムテーブルの番地
            r.cl = mucom2.rhythmdta[r.si];
            r.ch = mucom2.rhythmdta[r.si + 1];// CX = キーオン・ダンプデータ
            r.al = mucom2.rhythmdta[r.si + 2];// AL = リズム音長
            if (r.cl != 255)
            {
                r.si += 3;
                mucom2.rhyadrs = r.si;
            }
            //getrp1:
            r.si = r.pop();
            return;
        }

        //;----------------------------------------------
        //;	リズムパターンの設定(リズム音源用)
        //;----------------------------------------------

        private void rhythm_pat()
        {
            if (r.ch != 10)
            {
                return;// リズム音源のみ可能
            }
            mucom2.rhydata = 0;// リズム音色をクリア
            mucom2.chktxt();
            r.zero = r.al == (byte)'(';
            r.cl = 32;
            if (!r.zero)
            {
                mucom2.error(); return;
            }
            r.si = 0;//ofs:rhythmdta
            r.dl = 0;// DL = 全体のデータ数
            //rp_loop:
            do
            {
                r.ax = 0;
                mucom2.rhythmdta[r.si] = r.al;
                mucom2.rhythmdta[r.si + 1] = r.ah;// [si] = キーオン・ダンプするリズムのビット
                rp_next:
                do
                {
                    mucom2.chktxt();
                    xsmall();
                    if (r.al == (byte)'*')
                    {
                        // 直前のパターン指定
                        //rp_same:
                        r.ax = mucom2.lastrp;// AX = 直前のキーオン・ダンプデータ
                        mucom2.rhythmdta[r.si] = r.al;
                        mucom2.rhythmdta[r.si + 1] = r.ah;
                        goto rp_next;
                    }
                    if (r.al == (byte)'-')
                    {
                        // ダンプ指定
                        break;
                    }
                    setrt();// キーオンするビットを格納
                    if (r.al == 0)
                    {
                        goto rp_length;
                    }
                    mucom2.rhythmdta[r.si] |= r.ah;
                } while (true);

                //rp_dump:
                mucom2.chktxt();
                xsmall();
                setrt();// ダンプするビットを格納
                if (r.al != 0)
                {
                    mucom2.rhythmdta[r.si + 1] |= r.ah;
                    goto rp_next;
                }

                rp_length:
                r.bx--;
                mucom2.tnelnmx();// AL = 音長データ(無指定の時はデフォルト)
                mucom2.rhythmdta[r.si + 2] = r.al;
                r.al = mucom2.rhythmdta[r.si];
                r.ah = mucom2.rhythmdta[r.si + 1];
                mucom2.lastrp = r.ax;// 直前のデータとして保存
                r.si += 3;
                r.dl++;
                r.carry = r.dl < 16;
                r.cl = 8;// リズムパターンバッファ不足
                if (!r.carry)
                {
                    mucom2.error(); return;
                }
                mucom2.chktxt();
                if (r.al == (byte)')')
                {
                    //rp_exit:
                    mucom2.rhythmdta[r.si] = 0xff;// 終了コードを書き込む
                    init_rhythm();//リズムテーブル番地の初期化
                    //rp_abort:
                    return;
                }
            } while (r.al == (byte)',');
            r.cl = 32;
            mucom2.error(); return;
        }

        private void setrt()
        {
            r.ah = 1;
            r.cl = 0;

            //'B' バスドラム
            //'S' スネアドラム
            //'C' シンバル
            //'H' ハイハット
            //'T' タム
            //'R' リムショット
            string ptn = "BSCHTR";
            int p = ptn.IndexOf((char)r.al);
            if (p < 0)
            {
                r.cl = 5;
                r.al = 0;
                return;
            }
            r.cl = (byte)p;

            //rp_get:
            r.ah <<= r.cl;// AH = 音色のビットコード
            return;
        }

        //;------------------------------------------------
        //;	リズムパターンの設定(コードネーム用)
        //;------------------------------------------------

        private void rhythm_set()
        {
            mucom2.chktxt();
            r.zero = r.al == (byte)'(';
            r.cl = 32;
            if (!r.zero)
            {
                mucom2.error(); return;
            }
            r.si = 0;//ofs:rhythmdta
            r.dl = 0;// 全体のデータ数

            //rhy_loop:
            do
            {
                mucom2.chktxt();
                if (r.al != (byte)'0')
                {
                    xsmall();
                    if (r.al == (byte)'R')
                    {
                        // 休符の指定か(FFxx)
                        mucom2.rhythmdta[r.si] = 0xff;
                        r.si++;
                        r.dl++;
                        mucom2.chktxt();
                    }

                    //rhyrest:
                    r.bx--;
                    mucom2.tnelnmx();// リズム音長の獲得(AL)

                    //rhy2:
                    mucom2.rhythmdta[r.si] = r.al;
                    r.si++;
                }

            lbl:
                // モードのリセット
                //rhy0:
                mucom2.chktxt();
                if (r.al == (byte)'?')
                {
                    // 逆アクセント指定
                    r.si--;
                    r.al = mucom2.rhythmdta[r.si];
                    mucom2.rhythmdta[r.si] = 0xfd;
                    r.si++;

                    //rhy2:
                    mucom2.rhythmdta[r.si] = r.al;
                    r.si++;
                    goto lbl;
                }

                //rhy3:
                if (r.al == (byte)0x22)
                {
                    // スタカート指定
                    r.si--;
                    r.al = mucom2.rhythmdta[r.si];
                    mucom2.rhythmdta[r.si] = 0xfc;
                    r.si++;

                    //rhy2:
                    mucom2.rhythmdta[r.si] = r.al;
                    r.si++;
                    goto lbl;
                }

                //rhy4:
                if (r.al == (byte)'!')
                {
                    // アクセント指定
                    r.si--;
                    r.al = mucom2.rhythmdta[r.si];
                    mucom2.rhythmdta[r.si] = 0xfe;
                    r.si++;

                    //rhy2:
                    mucom2.rhythmdta[r.si] = r.al;
                    r.si++;
                    goto lbl;
                }

                //rhy1:
                if (r.al == (byte)')')
                {
                    //rhythm_end:
                    mucom2.rhythmdta[r.si] = 0x00;
                    return;
                }
                r.bx--;
                chkcm();// ","チェック
                r.dl--;
                r.carry = r.dl < 48;
                r.cl = 8;
            } while (r.carry);

            //error20:
            mucom2.error(); return;

        }

        public void init_rhythm()
        {
            r.push(r.ax);
            r.ax = 0;//ofs:rhythmdta
            mucom2.rhyadrs = r.ax;// リズム番地の初期化
            r.ax = r.pop();
            return;
        }

        //;----------------------------
        //;	@ENDIF の処理
        //;----------------------------

        private void mendif()
        {
            r.cl = 35;
            r.al = mucom2.jumpnes;
            if (r.al == 0)
            {
                mucom2.error(); return;
            }
            r.dl = 1;// if then のチェック用
            set_exit();
        }

        //;---------------------------------------
        //;	鍵盤表示のマスク設定・解除
        //;---------------------------------------

        private void key_maskset()
        {
            r.ax = 0x1ec;
            mucom2.stoswObjBufAX2DI();
            return;
        }

        private void key_maskreset()
        {
            mucom2.chktxt();
            chknum();
            r.bx--;
            if (r.carry)
            {
                //maskr1:
                r.ax = 0xec;
                //maskr2:
                mucom2.stoswObjBufAX2DI();
                return;
            }
            mucom2.rednums();// AL = 色コード(1～6)
            r.cl = 6;
            if (r.al == 0)
            {
                mucom2.error();return;
            }
            if (r.al > 6)
            {
                mucom2.error(); return;
            }
            r.al <<= 5;
            r.ah = r.al;
            r.al = 0xec;
            //maskr2:
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;===========================================
        //;	（）内繰り返しループ命令処理
        //;===========================================

        public void nloop1()
        {
            r.push(r.bx);
            mucom2.nesting++;// ネスト数を+1
            r.al = mucom2.nesting;
            r.carry = r.al > 15;// ネストのオーバーフローチェック
            r.cl = 3;
            if (r.carry)
            {
                mucom2.error();
                return;
            }

            r.al--;
            r.push(r.ax);
            r.al = 0xe0;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);// E0 : ループカウンタをクリアする
            r.ax = r.pop();
            r.bx = 0;//ofs:stttbl	; "(" が始まった番地を格納
            r.ah = 0;
            r.ax <<= 2;
            r.bx += r.ax;
            mucom2.stttbl[r.bx/2] = r.di;
            r.ax = 0;
            mucom2.stttbl[(r.bx + 2)/2] = r.ax;//熊:stttblはushort型です
            r.bx = r.pop();
            r.al = mucom2.chglen;// 前回の音長、割合をセットしておく
            mucom2.setrat();
            mucom2.optimiz = 0;// 最適化フラグをクリア
            mucom2.init_looplen();
        }

        //;--------------------------------
        //;	（）ループ終了処理
        //;--------------------------------

        public void nloop2()
        {
            mucom2.rednums();// 繰り返し回数を読み取る
            r.zero = r.al == 0;
            r.cl = 1;// 0ならエラー
            if (r.zero)
            {
                mucom2.error(); return;
            }
            mucom2.set_looplen();
            r.push(r.bx);
            r.push(r.ax);
            r.al = mucom2.nesting;// ネスト数を-1
            r.carry = r.al < 1;
            r.al--;
            mucom2.nesting = r.al;
            r.cl = 13;// "(" より ")" の方が多い
            if (r.carry)
            {
                mucom2.error(); return;
            }
            r.bx = 0;//ofs:stttbl
            r.ah = 0;
            r.ax <<= 2;
            r.bx += r.ax;
            r.dx = mucom2.stttbl[r.bx / 2];
            r.dx -= r.di;// 現在の番地からループ開始番地の差を計算
            r.dx = (ushort)-r.dx;
            r.al = 0xf7;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            r.ax = r.dx;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);// 戻り先までのオフセット値をセット
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            r.ax = r.pop();
            r.bx = mucom2.stttbl[(r.bx + 2) / 2];// BX = @if exit番地
            if (r.bx != 0)
            {
                muap98.object_Buf[r.bx] = new MmlDatum(r.al); // 最後の番号を格納する
            }
            r.bx = r.pop();
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);// ループ回数をセット
            r.al = mucom2.nesting;
            r.ax++;
            r.dh = r.al;// DH = ネスト値
            r.cl = 35;
            r.al = mucom2.jumpnes;// if exitありか?
            if (r.al == 0)
            {
                //nloop4:
                return;
            }
            r.dl = 2;// if exit のチェック用
            set_exit();
        }

        private void set_exit()
        {
            r.push(r.ds);
            r.push(r.bx);
            r.ah = 0;
            r.ax--;
            r.bx = r.ax;
            r.bx <<= 2;
            r.bx += 0;// MUCOM2.IFSTACK;
            r.ds = muap98.text;//	mov	ds,text		; DS:BX = IFスタックの番地
            r.ax = (ushort)(mucom2.IFSTACKbuf[r.bx] | (mucom2.IFSTACKbuf[r.bx + 1] << 8));
            if (r.al == r.dl)
            {
                bool flg = false;
                if (r.dl != 1)// @if thenの時はネストレベルチェックをしない
                {
                    if (r.ah != r.dh)// ネストと一致するか
                    {
                        flg = true;
                    }
                }
                if (!flg)
                {
                    //nloop5:
                    r.ax = r.di;
                    r.si = (ushort)(mucom2.IFSTACKbuf[r.bx + 2] | (mucom2.IFSTACKbuf[r.bx + 3] << 8));
                    r.bx = r.pop();
                    r.ds = r.pop();
                    r.ax -= r.si;
                    r.ax++;
                    muap98.object_Buf[r.si] = new MmlDatum(r.al);
                    muap98.object_Buf[r.si + 1] = new MmlDatum(r.ah);
                    mucom2.jumpnes--;
                    r.al = mucom2.chglen;// 前回の音長、割合をセットしておく
                    mucom2.setrat();
                    mucom2.optimiz = 0;// 最適化フラグをクリア
                    return;
                }
            }
            //nloop3:
            r.bx = r.pop();
            r.ds = r.pop();
            //nloop4:
            return;
        }

        //;=================================
        //;	SSGチャネルチェック
        //;=================================

        private bool check_ssg()
        {
            if (mucom2.sendch < 4)
            {
                //nopexe:
                //r.ax = r.pop();// チャネル4-6以外なら無視してリターン
                return false;
            }
            if (mucom2.sendch < 7)
            {
                return true;
            }
            //nopexe:
            //r.ax = r.pop();// チャネル4-6以外なら無視してリターン
            //ssgok:
            return false;
        }

        private bool skip_ssg()
        {
            if (mucom2.sendch < 4)
            {
                return false;
            }
            if (mucom2.sendch >= 7)
            {
                // チャネル4～6なら無視してリターン
                return false;
            }
            //r.ax = r.pop();
            //cmdexe:
            return true;
        }

        //;==================================
        //;	ノイズ周波数のセット
        //;==================================

        public void noise()
        {
            mucom2.rednums();// テキストの数字を読み取る
            if (!check_ssg()) return;// SSG 以外は無視

            r.carry = r.al < 32;// 範囲チェック
            r.cl = 6;
            if (!r.carry)
            {
                //error13:
                mucom2.error();return;
            }
            r.ah = r.al;
            r.al = 0xf6;
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;==================================
        //;	エンベロープ形状設定
        //;==================================

        public void env_type()
        {
            mucom2.rednums();// テキストの数字を読み取る
            if (!check_ssg()) return;// SSG以外は無視
            r.carry = r.al < 16;
            r.cl = 6;
            if (!r.carry)
            {
                //error13:
                mucom2.error();return;
            }
            r.ah = r.al;
            r.al = 0xed;
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;=================================
        //;	エンベロープ速度設定
        //;=================================

        public void env_speed()
        {
            mucom2.rednum();// テキストの数字を読み取る
            if (!check_ssg()) return;
            muap98.object_Buf[r.di] = new MmlDatum(0xee);
            r.di++;
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;============================================
        //;	エンベロープ使用、未使用処理 (Px)
        //;============================================

        public void envelope()
        {
            r.al = muap98.source_Buf[r.bx];
            r.bx++;
            xsmall();// ALの大文字変換
            if (r.al == (byte)'S')
            {
                // PSxx,xx,xx コマンド
                sdecay();
                return;
            }
            if (r.al == (byte)'M')
            {
                // PMx コマンド
                mixer();
                return;
            }
            if (r.al == (byte)'A')
            {
                // PAxx,xx コマンド
                attack();
                return;
            }
            r.bx--;
            mucom2.rednums();// P0/P1 の数字
            if (!check_ssg()) return;// SSG以外は無視
            r.carry = r.al < 2;// 範囲チェック
            r.cl = 6;
            if (!r.carry)
            {
                //error13:
                mucom2.error(); return;
            }
            r.ah = r.al;
            r.cl = 4;
            r.ah <<= r.cl;
            r.al = 0xef;// ENV On/Off (16/0)
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;============================================
        //;	SSG トーン、ノイズ選択命令 'PMx'
        //;============================================

        private void mixer()
        {
            mucom2.rednums();// テキストの数字を読み取る
            if (!check_ssg()) return;
            r.cl = 6;// 範囲チェック
            if (r.al >= 3)
            {
                mucom2.error(); return;
            }
            r.dl = r.al;
            r.al &= 1;// b0 = 1 TONE Calcel
            r.dl &= 2;// b1 = 1 NOISE Cancel
            r.dl <<= 1;
            r.dl <<= 1;
            r.al |= r.dl;// AL = b4,b0 data exsist
            r.al |= 0b1111_0110;// 不要ビットをマスク

            r.cl = mucom2.sendch;
            r.cl -= 4;
            r.al=r.rol(r.al, r.cl);// チャネルによって回す
            r.al &= 0b1011_1111;// b6=0 とする
            r.ah = r.al;
            r.al = 0xeb;// +++
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;-----------------------------------------
        //;	SSG 初期ディケイ速度設定 'PSx'
        //;-----------------------------------------

        private void sdecay()
        {
            mucom2.rednums();// テキストの数字を読み取る
            if (!check_ssg()) return;
            r.ah = r.al;
            r.al = 0xf2;
            mucom2.stoswObjBufAX2DI();
            chkcm();
            mucom2.rednums();
            mucom2.stosbObjBufAL2DI();
            chkcm();
            mucom2.rednums();
            mucom2.stosbObjBufAL2DI();
            return;
        }

        //;--------------------------------------------
        //;	開始音量・アタックの指定 'PAx,x'
        //;--------------------------------------------

        private void attack()
        {
            mucom2.rednums();// AL = 開始音量値
            r.cl = 6;
            if (r.al != 255)
            {
                if (r.al > 127)
                {
                    //error27:		; 0～127,255のみ許可
                    mucom2.error(); return;
                }
            }
            //attack1:
            r.ah = r.al;
            r.al = 0xd5;
            mucom2.stoswObjBufAX2DI();
                chkcm();
            mucom2.rednums();
            mucom2.stosbObjBufAL2DI();
            mucom2.chktxt();
            if (r.al == (byte)',')
            {
                sdecay();
                return;
            }
            r.bx--;
            return;
        }

        //;========================================
        //;	レジスタに直接データ出力命令
        //;========================================

        public void reg()
        {
            mucom2.rednums();// テキストの数字を読み取る
            r.ch = r.al;// CH = アドレス値
            chkcm();// ","チェック
            mucom2.rednums();// DL = データ値
            r.dl = r.al;
            set16();
        }

        //;	直接出力16進数版

        public void reghex()
        {
            get2hex();// 16進数2桁データの読み取り
            r.ch = r.dl;// CH = アドレス
            chkcm();// ","チェック
            get2hex();// DL = データ
            set16();
        }

        private void set16()
        {
            r.al = 0xf1;// DH = ADRS , DL = DATA
            mucom2.stosbObjBufAL2DI();
            r.al = r.dl;
            r.ah = r.ch;
            if (r.ah == 7)
            {
                // Y7,の指定か
                r.al |= 0x80;
                r.al &= 0xbf;// 強制的に b7,b6 = 10 にする
            }
            //set17:
            mucom2.stoswObjBufAX2DI();
            return;
        }

        private void get2hex()
        {
            gethex();// テキストからの16進数データ読み込み
            r.dl <<= 4;// 0x to x0
            r.dh = r.dl;
            gethex();
            r.dl |= r.dh;// DL = get xx
            return;
        }

        private void gethex()
        {
            r.dl = muap98.source_Buf[r.bx];
            r.bx++;
            if (r.dl >= (byte)'a')
            {
                r.dl -= (byte)' ';
            }
            r.dl -= (byte)'0';
            if (r.dl >= 10)
            {
                r.dl -= 7;
            }
            return;
        }

        //;------------------------
        //;	PCM番地指定
        //;------------------------

        private void pcm_adrs()
        {
            r.al = 0xd4;
            mucom2.stosbObjBufAL2DI();
            mucom2.chktxt();
            r.bx--;
            set4hex();
            chkcm();
            set4hex();
        }

        private void set4hex()
        {
            get2hex();// DL = 16進数2桁データ
            r.ah = r.dl;
            get2hex();
            r.al = r.dl;
            mucom2.stoswObjBufAX2DI();// 開始番地を格納
            return;
        }

        //;====================================================
        //;	YM-2203ユーザ音色設定
        //;	Z@x,@x(:),[I,][E,FB,],CN(:),
        //;		     AR,DR,SR,RR,SL,TL,KR,MP,DT(:),
        //;			(same data op2-op4)
        //;====================================================

        public void usr_tone()
        {
            gettpara();// AL = @xxの音色番号
            mucom2.from_no = r.al;// 複写元音色番号
            chkcm();// ","チェック
            gettpara();
            mucom2.to_no = r.al;// 複写先音色番号
            r.push(r.cx);
            r.push(r.es);
            copy_tone();// 音色パラメータ複写
            r.si = r.dx;// ES:SI = 変更する音色番号番地

            chkcm2();// ",:"チェック
            if (r.carry)
            {
                goto cut_param;//以降省略
            }

            //;-----------------------------------
            //;	反転モード(I)のチェック
            //;-----------------------------------
            mucom2.mode[0] &= 0xdf;// 反転モードのクリア
            mucom2.chktxt();
            r.bx--;
            xsmall();// ALの大文字変換
            if (r.al == (byte)'I')// n88basic(86)モードか(I)
            {
                r.bx++;
                chkcm();
                mucom2.mode[0] |= 0x20;// 反転モードの設定
                //next_param1:
                r.bp = 0;// オペレータ番号
                r.dx = 24;// 格納offset(SI+BP+DL)､反転なし
                r.cx = 0;// OR DATA=0(CH),SHIFT=0(CL)
            }
            //;-------------------------------
            //;	FB,CNの分離指定処理
            //;-------------------------------
            //next_param:
            else if (r.al != (byte)'E')// FB,CN分離指定
            {
                //next_param1:
                r.bp = 0;// オペレータ番号
                r.dx = 24;// 格納offset(SI+BP+DL)､反転なし
                r.cx = 0;// OR DATA=0(CH),SHIFT=0(CL)
            }
            else
            {
                r.bx++;
                chkcm();
                r.bp = 0;// OP番号
                r.dx = 24;// 格納offset(SI+BP+DL),DH=0
                r.cx = 0x703;// CH=or data,CL=shift counts
                paramain();
                chkcm();
                r.cx = 0x3800;// CN
            }

            //;------------------------------------
            //;	コネクションデータの獲得
            //;------------------------------------
            //next_param2:
            paramain();// パラメータ処理
            r.cx = 4;// オペレータ1-4のループ

            //getloop:
            do
            {
                chkcm2();
                if (r.carry)
                {
                    goto cut_param;// 以降省略
                }
                r.push(r.cx);

                //;---------------------------
                //;	AR,DR,SRの獲得
                //;---------------------------
                r.cx = 3;
                r.push(r.si);
                //setar:
                do
                {
                    r.push(r.cx);
                    r.dx = 0x1f08;// 格納offset(SI+BP+DL),DH=反転データ
                    r.cx = 0xe000;// OR DATA(CH),SHIFT(CL)
                    paramain();// パラメータ処理
                    chkcm();
                    r.si += 4;// $50,$60,$70と続く
                    r.cx = r.pop();
                    r.cx--;
                } while (r.cx != 0);
                r.si = r.pop();
                //;-------------------------------
                //;	RELEASE RATEの獲得
                //;-------------------------------
                r.dx = 0xf14;// 格納offset(SI+BP+DL),DH=反転データ
                r.cx = 0xf000;// OR DATA(CH),SHIFT(CL)
                paramain();// パラメータ処理
                chkcm();

                //;-------------------------------
                //;	SUSTAIN LEVELの獲得
                //;-------------------------------
                r.dx = 0xf14;// 格納offset(SI+BP+DL),DH=反転データ
                r.cx = 0xf04;// OR DATA(CH),SHIFT(CL)
                paramain();// パラメータ処理
                chkcm();

                //;------------------------------
                //;	TOTAL LEVELの獲得
                //;------------------------------
                r.dx = 0x7f04;// 格納offset(SI+BP+DL),DH=反転データ
                r.cx = 0;// OR DATA(CH),SHIFT(CL)
                paramain();// パラメータ処理
                chkcm();

                //;--------------------------------
                //;	KEY SCALE RATEの獲得
                //;--------------------------------

                r.dx = 8;// 格納offset(SI+BP+DL),DH=反転データ
                r.cx = 0x3f06;// OR DATA(CH),SHIFT(CL)
                paramain();// パラメータ処理
                chkcm();

                //;---------------------------
                //;	MULTIPLEの獲得
                //;---------------------------
                r.dx = 0;// 格納offset(SI+BP+DL),DH=反転データ
                r.cx = 0xf000;// OR DATA(CH),SHIFT(CL)
                paramain();// パラメータ処理
                chkcm();

                //;-------------------------
                //;	DETUNEの獲得
                //;-------------------------
                mucom2.chktxt();
                if (r.al == (byte)'-')
                {
                    getnum();// 負のデータ
                    r.al |= 4;
                    //minusdta:
                    r.al <<= 4;// b6-b4 に移動
                    r.al |= 0xf;// b3-b0 はそのまま
                    muap98.toneBuff[r.si + r.bp] |= 0xf0;
                    muap98.toneBuff[r.si + r.bp] &= r.al;// DTのみ変更
                }
                else
                {
                    //plusdta:
                    r.bx--;
                    getnum();
                    if (!r.carry)
                    {
                        //minusdta:
                        r.al <<= 4;// b6-b4 に移動
                        r.al |= 0xf;// b3-b0 はそのまま
                        muap98.toneBuff[r.si + r.bp] |= 0xf0;
                        muap98.toneBuff[r.si + r.bp] &= r.al;// DTのみ変更
                    }
                }
                //skipdt:
                r.cx = r.pop();
                r.bp++;// 次のオペレータへ
                r.cx--;
            } while (r.cx != 0);

            chkcm3();// 最後のカンマはどっちでも
                     //;	演奏データとして格納

        cut_param:
            r.dx = r.es;// 音色データのセグメントを保存
            r.es = r.pop();
            r.al = 0xe6;
            r.ah = mucom2.to_no;// 複写先番号
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);// 27bytes命令
            r.cx = 25;// 音色データ量
            r.push(r.ds);
            r.ds = r.dx;// DS:SI = 設定する音色データ番地
            do
            {
                muap98.object_Buf[r.di++] = new MmlDatum(muap98.toneBuff[r.si++]); // ES:DI = 演奏データ番地
                r.cx--;
            } while (r.cx != 0);

            r.ds = r.pop();
            r.cx = r.pop();
            return;
        }

        //;------------------------------------
        //;	テキストのカンマチェック
        //;------------------------------------

        private void chkcm2()
        {
            r.carry = false;
            mucom2.chktxt();// "," ":" のチェック
            if (r.al == (byte)':')
            {
                //syorya:
                r.carry = true;// 以降のパラメータ省略(:)ならCY
                return;
            }
            if (r.al != (byte)',')
            {
                //zerr:
                r.cl = 29;
                mucom2.error(); return;
            }
            return;
        }

        private void chkcm3()
        {
            mucom2.chktxt();// 最後のチェック用
            if (r.al == (byte)',')
            {
                //chkcm4:
                return;
            }
            if (r.al == (byte)':')
            {
                //chkcm4:
                return;
            }
            r.bx--;
            //chkcm4:
            return;
        }

        //;------------------------------------------------
        //;	パラメータの省略チェック
        //;	exit	CY = 1 : パラメータなし
        //;		AX = あったときの数字データ
        //;------------------------------------------------

        private void getnum()
        {
            mucom2.chktxt();
            r.bx--;
            if (r.al == (byte)',')
            {
                //no_num:
                r.carry = true;
                return;
            }
            if (r.al == (byte)':')
            {
                //no_num:
                r.carry = true;
                return;
            }
            mucom2.rednums();
            r.carry = false;
            return;
        }

        //;-----------------------------------
        //;	@xxの音色番号獲得(AL)
        //;-----------------------------------

        private void gettpara()
        {
            mucom2.chktxt();// 次の文字を獲得
            if (r.al != (byte)'@')
            {
                //zerr:
                r.cl = 29;
                mucom2.error(); return;
            }
            mucom2.rednums();// 音色番号
        }

        //;----------------------------------------
        //;	その他のパラメータ獲得、格納
        //;	entry	CL = left shift level
        //;		CH = or data
        //;		DL = SI offset
        //;		DH = 0:反転チェックなし
        //;		   <>0:反転用最大値
        //;		ES:SI = tone param address
        //;		BP = operator number(0-)
        //;----------------------------------------

        private void paramain()
        {
            getnum();
            if (r.carry)
            {
                // パラメータ省略
                return;
            }
            r.push(r.si);
            r.push(r.dx);
            r.push(r.cx);

            // 反転チェックがいるか
            if (r.dh != 0)
            {
                r.test(mucom2.mode[0], 0x20);// 反転モードか
                if (!r.zero)
                {
                    r.dh -= r.al;
                    r.al = r.dh;// ALのデータを反転させる
                }
            }

            //parainv:
            r.dh = 0;
            r.si += r.dx;// SIにオフセット加算
            r.al <<= r.cl;
            r.al |= r.ch;
            r.ch = (byte)~r.ch;
            muap98.toneBuff[r.si + r.bp] |= r.ch;
            muap98.toneBuff[r.si + r.bp] &= r.al;
            r.cx = r.pop();
            r.dx = r.pop();
            r.si = r.pop();

            //skip_para:
            return;
        }

        //;------------------------------------------
        //;	音色パラメータの複写(YM2203)
        //;	entry	from_no = 複写元番号
        //;		to_no   = 複写先
        //;	exit	ES:DX = 複写先パラ番地
        //;------------------------------------------

        private void copy_tone()
        {
            r.push(r.ax);
            r.push(r.bx);
            r.push(r.cx);
            r.push(r.si);
            r.push(r.di);

            r.push(r.ds);
            r.al = mucom2.from_no;
            byte[] tonebuf = mucom2.tone_adrs();// DS:SI = 所望の音色データ番地
            r.si = r.bx;
            r.al = mucom2.to_no;
            mucom2.tone_adrs();
            r.di = r.bx;// DS:DI = 複写先データ番地(DI,DX)
            r.dx = r.di;
            r.push(r.ds);// DS = 音色データセグメント
            r.es = r.pop();
            r.cx = 25;
            do
            {
                tonebuf[r.di] = tonebuf[r.si];
                r.di++;
                r.si++;
                r.cx--;
            } while (r.cx != 0);// 音色データを転送する
            r.ds = r.pop();

            r.di = r.pop();
            r.si = r.pop();
            r.cx = r.pop();
            r.bx = r.pop();
            r.ax = r.pop();

            return;
        }

        //;---------------------------------
        //;	１文字マクロの展開
        //;	entry	AL = 文字
        //;---------------------------------

        public void macro_exec()
        {
            mucom2.mode[1] |= 2;// 1文字マクロフラグ
            mucom2.set_symbol2();
            mucom2.symbol2 |= 2;// 1文字マクロ内部は出力しない
            r.push(r.di);// 文字列の探索、格納
            mucom2.macroflg = 0;// 変数指定フラグをクリア
            mucom2.wordbuf[0] = r.al;// 検索文字を格納する
            r.dl = 1;// 文字列長の指定
            r.al = muap98.source_Buf[r.bx];
            if (r.al == (byte)',')
            {
                getword4();
                return;
            }
            chknum();// マクロ変数が指定されているか
            if (!r.carry)
            {
                getword4();// 変数の格納
                return;
            }
            getword2();// 検索の実行へ
        }

        //;======================================
        //;	$word ソース置換命令処理
        //;======================================

        public void dtcall()
        {
            mucom2.mode[1] &= 0xfd;// 1文字マクロフラグクリア
            r.push(r.di);// 文字列の探索、格納
            mucom2.macroflg = 0;// 変数指定フラグをクリア
            r.di = 0;//ofs:wordbuf
            r.dl = 0;// 文字列の長さ

            //getword1:
            do
            {
                r.ax = muap98.source_Buf[r.bx];
                r.bx++;
                if (r.al == (byte)'$')
                {
                    // 区切り文字発見か
                    getword2();
                    return;
                }
                if (r.al == (byte)' ')
                {
                    getword2();
                    return;
                }
                if (r.al == (byte)9)
                {
                    getword2();
                    return;
                }
                if (r.al == 0xfe)
                {
                    getword9();
                    return;
                }
                if (r.al == (byte)',')
                {
                    // マクロ変数指定のチェック
                    getword4();
                    return;
                }
                r.cl = 21;// error code
                r.dl++;
                if (r.dl == 33)
                {
                    // 文字列は32文字まで
                    break;
                }
                mucom2.wordbuf[r.di] = r.al;// バッファに文字列格納
                r.di++;
            } while (true);
            //derror:
            mucom2.error();return;
        }

        //;･････････････････････････････････
        //;	マクロ変数のチェック
        //;･････････････････････････････････

        private void getword4()
        {
            mucom2.macrov[18 / 2] = r.bx;// パラメータテキスト開始番地を保存する
            r.push(r.si);
            r.push(r.dx);
            r.dx = 1;// SI = 変数値格納番地
            r.si = 0;//ofs:macrov	; SI+18 = 変数の指定されたテキスト番地格納先
            getword6:
            chkcall();
            r.bx--;
            if (r.al != (byte)',')// パラメータが省略されていたか
            {
                mucom2.macrov[(r.si + 18) / 2] = r.bx;// テキスト開始番地を格納する
                mucom2.rednum();// 設定する変数値を読み出す
                mucom2.macroflg |= r.dx;// 指定したフラグを立てる
                mucom2.macrov[(r.si) / 2] = r.ax;
                skiplen();// 音長パラメータのスキップ
            }
            //getword7:
            r.si++;
            r.si++;// 次の変数番号へ
            r.dx <<= 1;
            if (r.si < 18)//	cmp	si,ofs:macrov+18
            {
                chkcall();
                if (r.al == (byte)',')
                {
                    // 次の変数はあるか
                    goto getword6;
                }
                r.bx--;
            }
            //getword5:
            r.dx = r.pop();
            r.si = r.pop();
            getword8();
        }

        private void getword9()
        {
            check_flatclear();// 改行時の臨時調号クリア
            getword2();
        }

        private void getword2()
        {
            mucom2.macrov[18 / 2] = r.bx; // パラメータテキスト開始番地を保存する
            getword8();
        }
        private void getword8()
        {
            r.di = r.pop();
            mucom2.cal_num = r.dl;// 文字列長の保存
            skiplen();// 音長パラメータのスキップ
            mucom2.nest2++;
            r.carry = mucom2.nest2 > 10;// ネストは10レベルまで
            r.cl = 20;
            if (r.carry)
            {
                //derror
                mucom2.error(); return;
            }

            //;･･･････････････････････････････････････････
            //;	1文字マクロキャッシュのチェック
            //;･･･････････････････････････････････････････
            r.push(mucom2.linedta);// 行番号とソース位置を保存
            int oldcol = work.col;
            r.push(r.bx);
            r.test(mucom2.mode[1], 2);// 1文字マクロか
            if (!r.zero)
            {
                r.push(r.ds);
                r.push(r.bx);
                calc_macache();
                r.al = mucom2.MACACHEbuf[r.bx];
                r.ah = mucom2.MACACHEbuf[r.bx + 1];// AX = キャッシュデータ
                r.bx = r.pop();
                r.ds = r.pop();
                if (r.ax != 0)
                {
                    // ヒットしているか
                    r.bx = r.ax;
                    goto chk_dc5;// 検索を飛ばして実行する（ヒット）
                }
            }

            //cache1:
            mucom2.linedta = 1;// 先頭からコール先をサーチ
            work.row=1;
            work.oldbx = 0;
            r.bx = 0;
        chk_dc1:
            do
            {
                chkcall();
            } while (r.al != (byte)'$');// 置換もとのデータがあったか

            r.push(r.di);// 文字列の比較
            r.dl = mucom2.cal_num;// DL = 文字列の長さ
            r.di = 0;//ofs:wordbuf
            //chk_dc4:
            do
            {
                r.al = muap98.source_Buf[r.bx];
                r.bx++;
                if (r.al == 0xff || r.bx >= muap98.buflens)// EOF か // ソースバッファ超過か
                {
                    chk_err0();
                    return;
                }

                if (r.al != mucom2.wordbuf[r.di])
                {
                    // 置換文字列との比較
                    //chk_dc3:
                    r.di = r.pop();
                    goto chk_dc1;
                }
                r.di++;
                r.dl--;
            } while (r.dl != 0);

            r.di = r.pop();
            chkcall();
            if (r.al != (byte)'[')// 最終文字($,[)のチェック
            {
                if (r.al != (byte)'$')
                {
                    goto chk_dc1;
                }
                chkcall();
                if (r.al != (byte)'[')
                {
                    goto chk_dc1;
                }
            }

            //chk_dc2:
            r.test(mucom2.mode[1], 2);// 1文字マクロか
            if (!r.zero)
            {
                r.push(r.ds);
                r.push(r.bx);
                r.dx = r.bx;
                calc_macache();
                mucom2.MACACHEbuf[r.bx] = r.dl;
                mucom2.MACACHEbuf[r.bx + 1] = r.dh;//キャッシュデータとして保存
                r.bx = r.pop();
                r.ds = r.pop();
            }

        chk_dc5:
            do
            {
                chkcall();
                if (r.al == (byte)']')// 音符データ終了マークか
                {
                    break;
                }

                r.push(r.cx);
                r.push(mucom2.macroflg);// 指定したよフラグのみ保存する
                r.push(mucom2.macrov[0]);// 変数は３つまで保存
                r.push(mucom2.macrov[2 / 2]);
                r.push(mucom2.macrov[4 / 2]);
                r.push(mucom2.macrov[18 / 2]);
                mucom2.com_main();// 置換用データの変換作業
                mucom2.macrov[18 / 2] = r.pop();
                mucom2.macrov[4 / 2] = r.pop();
                mucom2.macrov[2 / 2] = r.pop();
                mucom2.macrov[0 / 2] = r.pop();
                mucom2.macroflg = r.pop();

                r.push(r.ax);
                r.ax = muap98.bufleno;// 演奏バッファが超過か
                r.ax -= 0x10;
                r.carry = r.di < r.ax;
                r.cl = 2;
                r.ax = r.pop();
                if (!r.carry)
                {
                    mucom2.error(); return;
                }
                r.cx = r.pop();
            } while (true);

            //chk_end0:
            r.bx = r.pop();// ソースアドレス復活
            mucom2.linedta = r.pop();
            work.row = mucom2.linedta;
            work.col = oldcol;
            mucom2.symbol2 &= 0xfd;
            mucom2.nest2--;
            return;

        }

        //;････････････････････････････････････････････････
        //;	マクロキャッシュ番地の計算
        //;	exit	DS:BX = マクロキャッシュ番地
        //;････････････････････････････････････････････････

        private void calc_macache()
        {
            r.ax = 0;
            r.al = mucom2.wordbuf[0];
            xsmall();// AL = 41～5Ah
            r.al -= 0x41;
            r.ax += r.ax;
            r.ds = muap98.text;
            r.bx = r.ax;
            r.bx += 0;// MUCOM2.MACACHE;//	add	bx,MACACHE	; DS:BX = マクロキャッシュ番地
            return;
        }

        //;･････････････････････････････････････････････
        //;	音長パラメータのスキップ処理
        //;	entry	DS:BX = ソーステキスト番地
        //;	exit	DS:BX = スキップ後の番地
        //;	break	AL
        //;･････････････････････････････････････････････

        private void skiplen()
        {
            do
            {
                chkcall();// テキスト読みだし
                if (r.al == (byte)'.') continue;
                if (r.al == (byte)'^') continue;
                if (r.al == (byte)'=') continue;
                chknum();
                if (r.carry) break;
            } while (true);

            r.bx--;
            return;
        }

        //;･･･････････････････････････････････････････
        //;	置換用テキスト読み取りルーチン
        //;	entry	DS:BX = テキスト番地
        //;	exit	DS:BX = 次のテキスト番地
        //;		AL = 取り出した文字
        //;･･･････････････････････････････････････････

        private void chkcall()
        {
            do
            {
                if (r.bx >= muap98.buflens)
                {
                    // source buffer END?
                    chk_err0();
                    return;
                }
                r.al = muap98.source_Buf[r.bx];
                r.bx++;
                if (r.al == 0xff)
                {
                    //EOF か
                    chk_err0();
                    return;
                }

                if (r.al == (byte)' ')
                {
                    continue;
                }
                if (r.al == (byte)9)
                {
                    continue;
                }
                if (r.al != 0xfe)
                {
                    // CR,LF コードなら行カウンタ+1
                    //dtcall3:
                    if (r.al == (byte)';')
                    {
                        // コメント文か
                        //dtcall4:
                        do
                        {
                            r.al = muap98.source_Buf[r.bx];// skip REM
                            r.bx++;
                            if (r.al == 0xff)
                            {
                                // EOFチェック
                                chk_err0();
                                return;
                            }
                        } while (r.al != 0xfe); // CR,LF まで無視
                    }
                    else
                    {
                        return;
                    }
                }
                //chkcall1:
                mucom2.linedta++;
                work.row++;
                work.oldbx = r.bx;
            } while (true);
        }

        private void chk_err0()
        {
            r.ax = r.pop();
            r.bx = r.pop();
            mucom2.linedta = r.pop();
            work.row = mucom2.linedta;
            work.col = 1;
            r.cl = 19;// 置換データが見つからなかった
            mucom2.error(); return;
        }

        //;----------------------------
        //;	トリルの速度指定
        //;----------------------------

        private void trspeed()
        {
            trspeed0();
            mucom2.trildef = r.al;
            return;
        }

        private void trspeed0()
        {
            mucom2.tnelnmx();
            r.zero = (r.al == 0);
            r.cl = 6;
            if (r.zero)
            {
                mucom2.error();return;
            }
            return;
        }

        //;=================================
        //;	トリラーコマンド処理
        //;=================================

        private int trillsub()
        {
            mucom2.tridta1 = 0;
            mucom2.tridta2 = 0;// トリル中の+-指定をクリア

            r.al = muap98.source_Buf[r.bx];
            if (r.al == (byte)'%')
            {
                mucom2.tridta1 = 0x03;
                mucom2.tridta2 = 0x03;// ナチュラルにする
                r.bx++;
                r.al = muap98.source_Buf[r.bx];
            }
            //trill0:
            if (r.al == (byte)'-')
            {
                mucom2.tridta1 = 2;// 1つ上の音符にフラット指定する
                r.bx++;
                r.al = muap98.source_Buf[r.bx];
            }
            //trill1:
            if (r.al == (byte)'+')
            {
                mucom2.tridta2 = 1;// 1つ下の音符にシャープ指定する
                r.bx++;
                r.al = muap98.source_Buf[r.bx];
            }
            //trill2:
            chknum();// 数字チェック
            if (r.carry)
            {
                r.al = mucom2.trildef;// デフォルトのトリラーはL32
            }
            else
            {
                //num_set:
                trspeed0();// トリル音長を獲得
            }
            //num_not:
            mucom2.trillen = r.al;

        //;	<>､音符のチェック

        trill3:
            skipoct();// 次のテキストを読み<>の実行
            if (r.al == (byte)'@')// @拡張コマンドも許可
            {
                //tr_set:
                mucom2.mode[0] |= 4;// <> を実行するフラグ
                exp_cmd();// @xx 拡張コマンドの実行
                goto trill3;
            }

            r.cl = 17;
            check_onpu();// 次は音符でなければならない
            mucom2.trionpu = r.al;// 音符の保存
            save_oct();// オクターブを保存する

            //;	変音記号､音長のチェック

            r.al = muap98.source_Buf[r.bx];
            mucom2.gethenon();// #+-%チェックをDLに返す
            mucom2.tridta0 = r.dl;
            mucom2.tnelnmx();// 音長データの獲得(AL)
            mucom2.add_tlen();
            mucom2.totalen = r.al;
            if (mucom2.harmno != 0)
            {
                // 和音モードかチェック
                int ret=tr_harm_mode();
                if (ret != 0) return 1;
            }
            r.al = mucom2.trillen;// トリル音長を指定
            mucom2.setrat();
            return 0;

        }

        public void check_onpu()
        {
            mucom2.set_symbol2();// ソース番地の格納
            if (r.al < (byte)'A')
            {
                // 音符があるかチェック
                mucom2.error();return;
            }
            if (r.al > (byte)'G')
            {
                mucom2.error(); return;
            }
            return;
        }

        //;------------------------------
        //;	トリル時の和音処理
        //;------------------------------

        private int tr_harm_mode()
        {
            mucom2.harm_main();// 和音の存在チェック
            if (!r.carry)
            {
                mucom2.get_harm();// 和音の音符獲得(@+,<>の実行)
                mucom2.trionpu = r.al;
                r.al = muap98.source_Buf[r.bx];
                mucom2.gethenon();
                mucom2.tridta0 = r.dl;// 調音記号の指定
                harm_onpu();// ":"の次へ飛ばす
                return 0;
            }

            //tr_noharm:
            r.dl=mucom2.totalen;// 和音が無いので休符を指定する
            mucom2.kyufu();
            r.ax = r.pop();// ※注意：トリル処理ではtrill_subを直接
            r.ax = r.pop();//	　コールすること。
            return 1;
        }

        //;	オクターブの保存､復活

        private void save_oct()
        {
            r.push(r.ax);
            r.al = mucom2.octdata;
            mucom2.octsave = r.al;
            r.al = mucom2.ratdata;
            mucom2.slursav = r.al;// ratioの保存
            r.al = 0;
            mucom2.chgrat();// Q8に設定する
            r.ax = r.pop();
            return;
        }

        private void load_oct()
        {
            r.push(r.ax);
            r.al = mucom2.octsave;
            mucom2.octdata = r.al;
            r.al = mucom2.slursav;
            mucom2.chgrat();// Qを戻す
            r.ax = r.pop();
            return;
        }

        //;---------------------------------
        //;	1つ上下の音符を演奏
        //;	entry	AL = 音符コード
        //;		DL = 変音指定
        //;---------------------------------

        private void addonpu()
        {
            r.al++;
            if (r.al == (byte)'C')
            {
                // B-C なら1オクターブUP
                mucom2.octdata++;
            }
            if (r.al == (byte)'H')
            {
                r.al = (byte)'A';
            }
            read();
        }

        private void subonpu()
        {
            r.al--;
            if (r.al == (byte)'B')
            {
                // C-Bなら1オクターブDOWN
                mucom2.octdata--;
            }
            if (r.al == (byte)'@')
            {
                r.al = (byte)'G';
            }
            read();
        }

        //;-----------------------------------------
        //;	全体の音長から残った音長を指定
        //;	entry	DL = トリル発音回数
        //;-----------------------------------------

        private void calcrest()
        {
            r.push(r.ax);
            r.al = mucom2.trillen;
            r.mul(r.dl);
            r.dl = r.al;
            r.al = mucom2.totalen;
            r.carry = (r.al <= r.dl);
            r.al -= r.dl;
            r.cl = 15;// 音長が残らない
            if (r.carry)
            {
                mucom2.error();return;
            }
            mucom2.setrat();// 残りの音長指定
            r.ax = r.pop();
            return;
        }

        //;------------------------------
        //;	@SACF 短前打音処理
        //;------------------------------

        private void sacf_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.al = mucom2.trionpu;// 基音
            triexe(new byte[] { 0x85 });// 1,add

            r.dl = 1;// 回数
            calcrest();// 残りの音長を指定
            triexe(new byte[] { 0x82 });// 0,sub
            load_oct();// オクターブの復活
            return;
        }

        //;--------------------------------
        //;	@SACS 短前打音処理
        //;--------------------------------

        private void sacs_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.al = mucom2.trionpu;// 基音
            triexe(new byte[] { 0x8a });// 2,sub

            r.dl = 1;// 回数
            calcrest();// 残りの音長を指定
            triexe(new byte[] { 0x81 });// 0,add
            load_oct();// オクターブの復活
            return;
        }

        //;--------------------------
        //;	@TRN ターン処理
        //;--------------------------

        private void trn_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.al = mucom2.trionpu;//	mov	al,trionpu	; 基音
            triexe(new byte[] { 5, 2, 0x8a });// 1,add  0,sub  2,sub

            r.dl = 3;// 回数
            calcrest();// 残りの音長を指定
            triexe(new byte[] { 0x81 });// 0,add
            load_oct();// オクターブの復活
            return;
        }

        //;--------------------------------
        //;	@XTRN 逆ターン処理
        //;--------------------------------

        private void xtrn_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.al = mucom2.trionpu;// 基音
            triexe(new byte[] { 0x0a, 0x01, 0x85 });// 2,sub  0,add  1,add

            r.dl = 3;// 回数
            calcrest();// 残りの音長指定
            triexe(new byte[] { 0x82 });// 0,sub
            load_oct();// オクターブの復活
            return;
        }

        //;--------------------------------
        //;	@MTRN 中間ターン処理
        //;--------------------------------

        private void mtrn_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.dl = 4;
            calcrest();// 残りの音長指定
            r.al = mucom2.trionpu;
            triexe(new byte[] { 0x80 });// 0,read
            r.push(r.ax);
            r.al = mucom2.trillen;
            mucom2.setrat();// トリル音長指定
            r.ax = r.pop();
            triexe(new byte[] { 0x05, 0x02, 0x0a, 0x81 });// 1,add 0,sub 2,sub 0,add
            load_oct();// オクターブの復活
            return;
        }

        //;------------------------------------
        //;	@XMTRN 中間逆ターン処理
        //;------------------------------------

        private void xmtrn_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.dl = 4;
            calcrest();// 残りの音長指定
            r.al = mucom2.trionpu;
            triexe(new byte[] { 0x80 });// 基音を発音
            // 0,read
            r.push(r.ax);
            r.al = mucom2.trillen;
            mucom2.setrat();// トリル音長指定
            r.ax = r.pop();
            triexe(new byte[] { 0x0a, 0x01, 0x05, 0x82 });// 2,sub 0,add 1,add 0,sub
            load_oct();// オクターブの復活
            return;
        }

        //;--------------------------------
        //;	@MOR モルデント処理
        //;--------------------------------

        private void mor_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.al = mucom2.trionpu;// 基音
            triexe(new byte[] { 0x00, 0x85 });// 0,read  1,add
            r.dl = 2;// 回数
            calcrest();// 残りの音長指定
            triexe(new byte[] { 0x82 });// 0,sub
            load_oct();// オクターブの復活
            return;
        }

        //;----------------------------------
        //;	@XMOR 逆モルデント処理
        //;----------------------------------

        private void xmor_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.al = mucom2.trionpu;// 基音
            triexe(new byte[] { 0x00, 0x8a });// 0,read  2,sub
            r.dl = 2;// 回数
            calcrest();// 残りの音長指定
            triexe(new byte[] { 0x81 });// 0,add
            load_oct();// オクターブの復活
            return;
        }

        //;-----------------------------
        //;	@CAD カデンス処理
        //;-----------------------------

        private void cad_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            calc_tri();// トリる回数を計算
            if (r.cl < 2)
            {
                //terror:
                r.cl = 15;//余らない
                mucom2.error(); return;
            }
            r.cl--;// 最初と最後の音符の分を減らす
            set_tri1();
            //rest_cad:
            triexe(new byte[] { 0x82 });// 0,sub
            rest_tri();
            return;
        }

        //;--------------------------------
        //;	@XCAD 逆カデンス処理
        //;--------------------------------

        private void xcad_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            calc_tri();// トリる回数を計算
            if (r.cl < 3)
            {
                //terror:
                r.cl = 15;//余らない
                mucom2.error(); return;
            }
            r.cl -= 2;
            set_tri1();
            //rest_xcad:
            triexe(new byte[] { 0x02, 0x0a, 0x81 });// 0,sub 2,sub 0,add
            rest_tri();
            return;
        }

        //;-----------------------------
        //;	@IDM アイデム処理
        //;-----------------------------

        private void idm_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            calc_tri();// トリる回数を計算
            if (r.cl < 3)
            {
                //terror:
                r.cl = 15;//余らない
                mucom2.error(); return;
            }
            r.cl -= 2;
            set_tri2();
            //rest_cad:
            triexe(new byte[] { 0x82 });// 0,sub
            rest_tri();
            return;
        }

        //;-------------------------------
        //;	@XIDM 逆アイデム処理
        //;-------------------------------

        private void xidm_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            calc_tri();// トリる回数を計算
            if (r.cl < 4)
            {
                //terror:
                r.cl = 15;//余らない
                mucom2.error(); return;
            }
            r.cl -= 3;
            set_tri2();
            //rest_xcad:
            triexe(new byte[] { 0x02, 0x0a, 0x81 });// 0,sub 2,sub 0,add
            rest_tri();
            return;
        }

        //;--------------------------
        //;	@TRI トリル処理
        //;--------------------------

        private void tri_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            calc_tri();//CL = トリる回数 , CH = 余りの音長
            set_tri();// 基音とその上の音をCL回繰り返す
            rest_tri();// 残りの音長処理
            return;
        }

        //;-----------------------------------
        //;	トリラー回数計算
        //;	exit	CL = トリる回数
        //;		CH = 余りの音長
        //;		AL = trionpu
        //;-----------------------------------

        private void calc_tri()
        {
            r.al = mucom2.totalen;// 全体の音長
            r.cl = mucom2.trillen;// トリル音長
            r.cl += r.cl;
            r.ah = 0;
            r.div(r.cl);// AL=AX/CL,トリル回数
            r.cx = r.ax;// AH=余りの音長
            if (r.cl == 0)
            {
                //terror:
                r.cl = 15;//余らない
                mucom2.error();return;
            }
            r.al = mucom2.trionpu;// 基音
            return;
        }

        //;-------------------------------------
        //;	基音・その上の音繰り返し
        //;	entry	CL = 繰り返し回数
        //;-------------------------------------

        private void set_tri2()
        {
            triexe(new byte[] { 0x05, 0x82 });// 1,add 0,sub (@idm,@xidm)
            set_tri1();
        }
        private void set_tri1()
        {
            triexe(new byte[] { 0x0a, 0x01, 0x85 });// 2,sub 0,add 1,add (@cad,@xcad)
            do
            {
                //tri0:
                r.cl--;
                if (r.cl == 0) return;
                //tri_loop1:
                triexe(new byte[] { 0x02, 0x85 });// 2回目以降はここを使用する
            } while (true);
        }

        private void set_tri()
        {
            triexe(new byte[] { 0x00, 0x85 });// 0,read  1,add (@tri)
            do
            {
                //tri0:
                r.cl--;
                if (r.cl == 0) return;
                //tri_loop1:
                triexe(new byte[] { 0x02, 0x85 });// 2回目以降はここを使用する
            } while (true);
        }

        //;---------------------------------
        //;	トリル残りの音長処理
        //;	entry	CH = 残りの音長
        //;---------------------------------

        private void rest_tri()
        {
            if (r.ch == 0)
            {
                //tri_end:
                load_oct();// オクターブの復活
                return;
            }
            r.al = r.ch;
            mucom2.setrat();// 余った音長を設定
            kwait0();// @Wコマンドを格納
            //tri_end:
            load_oct();// オクターブの復活
        }

        //;---------------------------------------------------
        //;	@XMMTRN 逆モルデントと中間ターンの組合せ
        //;---------------------------------------------------

        private void xmmtrn_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.al = mucom2.trionpu;
            r.dl= mucom2.tridta0;
            read();//基音を発音
            triexe(new byte[] { 0x8a });
            r.dl = 6;

            calcrest();//残りの音長指定
            triexe(new byte[] { 0x81 });//0,add

            r.push(r.ax);
            r.al = mucom2.trillen;
            mucom2.setrat();
            r.ax = r.pop();

            triexe(new byte[] { 0x05, 0x02, 0x0a, 0x81 });//1,add  0,sub  2,sub  0,add
            load_oct();//オクターブの復活
            return;
        }

        //;-----------------------------------------
        //;	@ACS 前長打音(accent steigend)
        //;-----------------------------------------

        private void acc_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            accsub();// 最初の音長を設定
            r.al = mucom2.trionpu;
            triexe(new byte[] { 0x8a });//2,sub
            r.dl = 1;

            calcrest();//残りの音長指定
            triexe(new byte[] { 0x81 });//0,add
            load_oct();// オクターブの復活
            return;
        }

        //;------------------------------------------
        //;	@ACF 長前打音(accent fallend)
        //;------------------------------------------

        private void xacc_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            accsub();// 最初の音長を設定
            r.al = mucom2.trionpu;
            triexe(new byte[] { 0x85 });//1,add
            r.dl = 1;

            calcrest();//残りの音長指定
            triexe(new byte[] { 0x82 });//0,sub
            load_oct();// オクターブの復活
            return;
        }

        //;	長前打音サブルーチン

        private void accsub()
        {
            r.al = mucom2.totalen;// 全音長
            r.dl = r.al;
            r.dh = 9;
            r.ah = 0;
            r.div(r.dh);// 符点音符か
            r.al = r.dl;
            if (r.ah == 0)
            {
                //futen:
                r.dh = 3;
                r.ah = 0;
                r.div(r.dh);
                r.al += r.al;// 2/3の長さにする
            }
            else
            {
                r.al >>= 1;// 普通の音符なら半分にする
            }

            //accsub1:
            mucom2.trillen = r.al;
            mucom2.setrat();
            r.al = 0xdf;// +++
            mucom2.stosbObjBufAL2DI();// Q8にする
            return;
        }

        //;----------------------
        //;	@AMTRN 処理
        //;----------------------

        private void amtrn_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.dl = 3;
            calcrest();//残りの音長指定
            r.al = mucom2.trionpu;
            triexe(new byte[] { 0x85 });//1,add

            r.push(r.ax);
            r.al = mucom2.trillen;
            mucom2.setrat();// トリル音長指定
            r.ax = r.pop();

            triexe(new byte[] { 0x02, 0x05, 0x82 });// 0,sub  1,add  0,sub
            load_oct();// オクターブの復活
        }

        //;------------------------
        //;	@XAMTRN 処理
        //;------------------------

        private void xamtrn_ent()
        {
            int ret = trillsub();// パラメータチェック(トリル音長指定)
            if (ret != 0) return;
            r.dl = 3;
            calcrest();//残りの音長指定
            r.al = mucom2.trionpu;
            triexe(new byte[] { 0x8a });//2,sub

            r.push(r.ax);
            r.al = mucom2.trillen;
            mucom2.setrat();// トリル音長指定
            r.ax = r.pop();

            triexe(new byte[] { 0x01, 0x0a, 0x81 });// 0,add  2,sub  0,add
            load_oct();// オクターブの復活
        }

        //;----------------------------------------
        //;	トリリングサブルーチン
        //;	entry	AL = 音符コード
        //;		call	triexe
        //;		db	･････,b7=1
        //;	データ	b3,b2 : 00 = 調音No.0
        //;			01 =     No.1
        //;			10 =     No.2
        //;		b1,b0 : 00 = 同じ音
        //;			01 = 上の音
        //;			10 = 下の音
        //;----------------------------------------

        private void triexe(byte[] dat)
        {
            r.si = 0;// r.pop();// リターン番地の獲得
            r.push(r.cx);

            //triexe1:
            do
            {
                r.cl = dat[r.si];
                r.cl &= 0x0c;// b3,b2を保存
                r.dl = mucom2.tridta0;
                if (r.cl == 4)
                {
                    // 00 = 基音, 01 = 上, 10 = 下
                    r.dl = mucom2.tridta1;
                }
                if (r.cl == 8)
                {
                    r.dl = mucom2.tridta2;
                }

                r.cl = dat[r.si];
                r.cl &= 3;// b1,b0を保存
                if (r.cl == 0)
                {
                    read();
                }
                r.cl--;
                if (r.cl == 0)
                {
                    addonpu();
                }
                r.cl--;
                if (r.cl == 0)
                {
                    subonpu();
                }
                r.si++;
                r.test(dat[r.si - 1], 0x80);
            } while (r.zero);// パラメータの次の番地へ戻る

            r.cx = r.pop();
            //r.push(r.si);
            return;
        }

        //;=========================================
        //;	移調コマンド処理
        //;	entry	BX = ソース番地
        //;	exit	ichosav = 移調データ
        //;=========================================

        public void icho()
        {
            r.dl = 0;
            //icho4:
            do
            {
                do
                {
                    r.al = muap98.source_Buf[r.bx];
                    r.ah = muap98.source_Buf[r.bx + 1];
                    r.bx++;
                    if (r.al != (byte)'<')
                    {
                        break;
                    }
                    r.dl -= 12;// 1オクターブ下に移調する
                } while (true);
                //icho5:
                if (r.al != (byte)'>')
                {
                    break;
                }
                r.dl += 12;// 1オクターブ上に移調する
            } while (true);
            //icho6:
            xsmall();// ALの大文字変換
            r.al -= (byte)'A';// 移調音符の読み取り
            r.carry = r.al < 7;
            r.cl = 23;
            if (!r.carry)
            {
                //error28:
                mucom2.error(); return;
            }

            r.push(r.bx);
            r.push(r.ax);
            r.bx = 0;//ofs:ichodta	; 音符から実際にずらす値を獲得
            r.ah = 0;
            r.bx += r.ax;
            r.dl += mucom2.ichodta[r.bx];// 音符による移調データを加算
            r.ax = r.pop();
            r.bx = r.pop();

            if (r.ah == (byte)'-')
            {
                // フラットのチェック
                r.bx++;
                r.dl--;
                //icho2:
                mucom2.ichosav = r.dl;// 移調データの保存
                return;
            }
            //icho1:
            if (r.ah != (byte)'#')
            {
                // シャープのチェック
                if (r.ah != (byte)'+')
                {
                    //icho2:
                    mucom2.ichosav = r.dl;// 移調データの保存
                    return;
                }
            }
            //icho3:
            r.bx++;
            r.dl++;
            //icho2:
            mucom2.ichosav = r.dl;// 移調データの保存
            return;
        }

        public void MakeDatum(enmMMLType type)
        {
            LinePos linePos = new LinePos(null, work.sourceFileName, work.row, work.col);
            linePos.chip = work.crntChip;
            linePos.chipNumber = 0;
            linePos.ch = (byte)work.crntChannel;
            linePos.part = work.crntPart;
            int clk = 0;
            List<object> args = new List<object>();
            MmlDatum md = new MmlDatum(type, args, linePos, clk);
            work.md = md;
        }

        //;--------------------------------
        //;	ディチューンの設定
        //;--------------------------------

        private void detune()
        {
            MakeDatum(enmMMLType.Detune);

            r.si = 0;//ofs:dtdata
            get_detune();
            mucom2.dtdata[r.si] = r.al;

            work.md.args.Add("D");//通常のディチューン
            work.md.args.Add((int)(sbyte)r.al);//ディチューン値(int)
            //熊:@DTはコンパイラ側で管理しているので次のコマンドへ情報を委託する
            work.lstMd.Add(work.Copy(work.md, -1));

            r.si++;
            mucom2.chktxt();
            if (r.al == (byte)',')
            {
                r.cl = 16;
                check_314();
                if (!r.zero)
                {
                    // 複数パラメータは3,14chのみで使用可能
                    mucom2.error(); return;
                }

                r.dl = 2;
                //detune2:
                do
                {
                    get_detune();
                    mucom2.dtdata[r.si] = r.al;
                    r.si++;
                    chkcm();
                    r.dl--;
                } while (r.dl!=0);

                get_detune();
                mucom2.dtdata[r.si] = r.al;
                mucom2.dt2mode = 1;// 複数ディチューンの指定
                if (mucom2.codemod == 1)
                {
                    // コード出力モードは不可
                    mucom2.codemod = 0;
                }
                r.ax = 0x40ed;
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
                muap98.object_Buf[r.di++] = new MmlDatum(r.ah);// 効果音モードにする
                return;
            }

            //detune1:
            mucom2.dt2mode = 0;// 標準ディチューン
            r.bx--;
            check_314();
            if (!r.zero)
            {
                //detune3:
                return;
            }
            r.ax = 0x00ed;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);// 標準モードにする
            //detune3:
            return;
        }

        private void get_detune()
        {
            mucom2.chktxt();
            if (r.al != (byte)'-')
            {
                //deplus:
                r.bx--;
                mucom2.rednums();
                return;
            }
            mucom2.rednums();
            r.ax = (ushort)-r.ax;
            return;
        }

        //;---------------------------------------
        //;	システムディチューンの設定
        //;---------------------------------------

        private void sysdetune()
        {
            r.cl = 38;
            if (r.ch == 10)
            {
                // PCM,リズムでは指定禁止
                //error28:
                mucom2.error(); return;
            }
            if (r.ch == 11)
            {
                //error28:
                mucom2.error(); return;
            }
            get_detune();
            r.ah = r.al;
            r.al = 0xf0;
            mucom2.stoswObjBufAX2DI();
            return;
        }

        //;･････････････････････････････････････
        //;	ch3,14のみのチェック
        //;	entry	CH = ch番号(1～17)
        //;	exit	Z = ch3,14である
        //;･････････････････････････････････････

        public void check_314()
        {
            if (r.ch == 3)
            {
                r.zero = true;
                return;
            }
            r.zero = (r.ch == 14);
            //cch1:
            return;
        }

        //;---------------------------------------
        //;	ディチューンシフト値の設定
        //;---------------------------------------

        private void detune_shift()
        {
            r.si = 0;//ofs:dtshift
            detune_sub();
            mucom2.chktxt();
            if (r.al == (byte)',')
            {
                check_314();
                if (!r.zero)
                {
                    //error28:// 3,14ch以外は@ds op4,3,2,1指定はできない
                    mucom2.error(); return;
                }
                r.dl = 2;
                //_detunes2:
                do
                {
                    detune_sub();
                    chkcm();
                    r.dl--;
                } while (r.dl!=0);
                detune_sub();
                return;
            }
            //_detunes1:
            r.bx--;
            return;
        }

        private void detune_sub()
        {
            mucom2.rednums();
            r.cl = 6;
            if (r.al > 31)
            {
                mucom2.error();return;
            }
            mucom2.dtshift[r.si] = r.al;
            r.si++;
            return;
        }

        private void calcdt()
        {
            int p = r.si + 4;
            r.cl = p < 4 ? mucom2.dtdata[p] : mucom2.dtshift[p - 4];
            if (r.cl >= 16)
            {
                // @ds16～31は増える方向にする
                //calcdt1:
                r.cl -= 16;
                r.ax <<= r.cl;
                return;
            }

            r.ax = (ushort)((short)r.ax >> r.cl);//	sar	ax,cl
            return;
        }

        //;======================================================
        //;	音符データより実際の出力値を計算
        //;
        //;	DL = para data (0,1,2,3,4,5)=( ,#,-,%,##,--)
        //;	DI = object offset
        //;	AL = music chr code (CDEFGAB)
        //;======================================================

        public void read()
        {
            cres_check();// クレシェンドのチェック
            pan_check();// オートパンのチェック
            read_main();// 変換のメイン
            r.push(r.ax);
            r.push(r.dx);
            check_tiemode();
            if (!r.zero)
            {
                goto autotie0;
            }
            r.dx = mucom2.lastfrq;// 前回の周波数と一致するか
            r.zero = (r.dx == (ushort)((byte)muap98.object_Buf[r.di - 2].dat | ((byte)muap98.object_Buf[r.di - 1].dat << 8)));// 今回の周波数と比較
            if (!r.zero)
            {
                goto autotie0;
            }
            r.ax = r.di;
            r.ax -= 2;
            // AX = ただ今音符を格納した番地
            r.dx = 1;
            mucom2.move_obj();// 演奏データの移動(1byte)
            r.push(r.di);
            r.di = r.ax;
            r.al = 0xe1;// そこにタイコマンドを格納 +++
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            r.di = r.pop();
        autotie0:
            r.dx = r.pop();
            r.ax = r.pop();
            return;
        }

        //;･･････････････････････････････････････
        //;	自動タイのチェック
        //;	exit	Z = 自動タイモード
        //;･･････････････････････････････････････

        private void check_tiemode()
        {
            r.push(r.ax);
            r.al = mucom2.mode[0];
            mucom2.mode[0] &= 0x7f;// 自動タイフラグクリア
            if (mucom2.sendch == 11)
            {
                r.ax = 0;
                r.ax++;
                r.zero = false;
                // PCMのみ自動タイ禁止
            }
            else
            {
                //ctie1:
                r.al ^= 0x80;
                r.test(r.al, 0x88);// 自動タイ指定か(Zero)
            }
            //ctie2:
            r.ax = r.pop();
            return;
        }

        //;------------------------------------
        //;	音符中間コード解析処理
        //;------------------------------------

        private void read_main()
        {
            mucom2.dionpu = r.di;// 音符の演奏番地を保存
            r.push(r.si);
            r.push(r.dx);
            r.push(r.cx);
            r.push(r.bx);
            r.push(r.ax);

            r.al -= (byte)'A';
            r.bx = 0;//ofs:musdata
            r.ah = 0;
            r.bx += r.ax;// 音符から中間コードに変換
            r.cl = mucom2.musdata[r.bx];// CDEFGAB to 0-11
            if (r.dl == 3)
            {
                // ナチュラル(%)
                natural();
                return;
            }
            if (r.dl != 0)
            {
                // #,-が指定されている時は調号を無視
                natural();
                return;
            }

            r.bx = 0;//ofs:flatdata	; 中間コードに調号を加味
            r.bx += r.ax;
            r.al = mucom2.octdata;
            r.ah = 7;
            r.mul(r.ah);
            r.si = r.ax;// SI = オクターブ*7
            int p = r.bx + r.si + 7;
            r.al = p < 7 ? mucom2.flatdata[p] : mucom2.flatdata2[p - 7];
            if (r.al == 0)
            {
                // 臨時調号が存在するか(0ならなし)
                nature1();
                return;
            }

            r.al &= 7;// b2-b0のみ残す
            if (r.al == 2)
            {
                r.al = 0xff;// 変音に変換
            }
            if (r.al == 3)
            {
                r.al = 2;// 重嬰音に変換
            }
            if (r.al == 4)
            {
                r.al = 0xfe;// 重変音に変換
            }
            r.cl += r.al;// AL = 0,1,-1(%,#,-)

            // 臨時調号のみ加味
            natural();
            return;
        }

        //;-----------------------------------------
        //;	コード変換用エントリ
        //;	entry	AL = 中間コード(0-11)
        //;-----------------------------------------

        public void read2()
        {
            r.push(r.si);
            r.push(r.dx);
            r.push(r.cx);
            r.push(r.bx);
            r.push(r.ax);
            natent();
        }

        private void nature1()
        {
            r.cl += mucom2.flatdata[r.bx];// 調号を加味
            natural();
        }

        private void natural()
        {
            r.al = r.cl;
            r.dl--;
            if (r.dl == 0) r.al++;// DL=1 : 嬰音
            r.dl--;
            if (r.dl == 0) r.al--;// DL=2 : 変音
            r.dl--;
            r.dl--;
            if (r.dl == 0) r.al += 2;// DL=4 : 重嬰音
            r.dl--;
            if (r.dl == 0) r.al -= 2;// DL=5 : 重変音
            natent();
        }
        private void natent()
        {
            r.push((ushort)(mucom2.octdata | (mucom2.octsave << 8)));
            r.al += mucom2.ichosav;// 中間コードに移調を加味した値
            //icho_loop:
            do
            {
                if (r.al < 12)
                {
                    // 1オクターブ上のデータとなった
                    break;
                }
                r.test(r.al, 0x80);// 1オクターブ下のデータとなった
                if (r.zero)
                {
                    //icho_octup:
                    r.al -= 12;
                    mucom2.octdata++;
                    continue;
                }
                r.al += 12;
                mucom2.octdata--;
            } while (true);

            //icho_oct:
            r.carry = mucom2.octdata < 9;// オクターブの値が範囲外か
            r.cl = 24;
            if (!r.carry)
            {
                mucom2.error();
                return;
            }
            r.ah = 0;

            //熊:r.axに最終的な音程決定(0～11)
            work.ontei = r.ax;
            work.oct = mucom2.octdata;

            r.ax += r.ax;

            mucom2.onpucnt++;// 連符数カウンタ
            r.si = 0;//ofs:dtdata	; SI = ディチューン値格納番地
            r.ch = mucom2.sendch;
            if (r.ch < 4)
            {
                // FM/SSG のチェック
                readf();
                return;
            }
            if (r.ch < 7)
            {
                reads();
                return;
            }
            if (r.ch < 10)
            {
                readf();
                return;
            }
            if (r.ch == 10)
            {
                read_exit();
                return;
            }
            if (r.ch >= 12)
            {
                readf();
                return;
            }

            //;	PCM 用変換ルーチン
            //;	entry	AX = 0-22 (CDEFGAB*2)
            //;		octdata = オクターブ値

            r.bx = 0;//ofs:data3	; BX = PCM O4 delta-N tabel
            r.bx += r.ax;
            r.al = mucom2.octdata;// AL = octave(0-7)
            ushort ans = r.pop();
            mucom2.octdata = (byte)ans;
            mucom2.octsave = (byte)(ans >> 8);
            r.dx = (ushort)(mucom2.data3[r.bx] | (mucom2.data3[r.bx + 1] << 8));// DX = DELTA-N data
            if (r.al > 7)
            {
                //error18:
                r.cl = 36;
                mucom2.error();
                return;
            }
            if (r.al != 7)
            {
                r.cl = 6;
                r.cl -= r.al;
                r.dx >>= r.cl;
            }
            else
            {
                //octave5:
                r.carry = (r.dx << 1) > 0xffff;
                r.dx <<= 1;
                if (r.carry)
                {
                    //error18:
                    r.cl = 36;
                    mucom2.error();
                    return;
                }
            }

            //readpcm1:
            r.ah = mucom2.dtdata[0];// ディチューンの設定
            mucom2.freq_lfo();// 周波数による補正
            calcdt();
            r.dx += r.ax;
            r.al = 0xd5;

            if (work.md == null)
            {
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            }
            else
            {
                work.md.dat = r.al;
                work.md.args[work.mdArgsStep + 0] = work.ontei + work.oct * 12;
                work.md.args[work.mdArgsStep + 1] = work.otoLength;
                work.md.linePos.chip = work.crntChip;
                work.md.linePos.chipNumber = 0;
                work.md.linePos.ch = (byte)work.crntChannel;
                work.md.linePos.part = work.crntPart;
                muap98.object_Buf[r.di++] = work.md;
            }

            r.ax = r.dx;
            muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            read_exit();
        }

        //;	FM OPN 用変換ルーチン

        private void readf()
        {
            r.bx = 0;//ofs:data1
            r.bx += r.ax;// BX =  music data offset
            r.al = mucom2.octdata;// oct data
            ushort ans = r.pop();
            mucom2.octdata = (byte)ans;
            mucom2.octsave = (byte)(ans >> 8);

            // O9の場合の例外処理
            if (r.al == 8)
            {
                r.al = 0x38;
                r.dx = (ushort)(mucom2.data1[r.bx] | (mucom2.data1[r.bx + 1] << 8));
                r.dx += r.dx;
                r.dh += r.al;
                if (r.dx > 0x3fff)
                {
                    r.dx = 0x3fff;
                }
            }
            else
            {
                //not_o9:
                r.al <<= 3;
                r.al += mucom2.data1[r.bx + 1];// set F-Number1 & Block
                r.dh = r.al;
                r.dl = mucom2.data1[r.bx];
            }

            //set_o9:
            if (mucom2.dt2mode != 0)
            {
                // 複数ディチューンの指定か
                r.al = 0xfa;// +++
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);// 効果音モード用に出力
                r.cx = 3;
                //multi_dt:
                do
                {
                    r.push(r.dx);
                    r.push(r.cx);
                    set_dtfreq();
                    r.si++;
                    r.cx = r.pop();
                    r.dx = r.pop();
                    r.cx--;
                } while (r.cx != 0);
            }
            //norm_dt:
            set_dtfreq();
            read_exit();
        }

        private void read_exit()
        {
            r.ax = r.pop();
            r.bx = r.pop();
            r.cx = r.pop();
            r.dx = r.pop();
            r.si = r.pop();
            return;
        }

        private void set_dtfreq()
        {
            r.push(r.dx);
            r.dh &= 7;
            r.ah = mucom2.dtdata[r.si];// ディチューンの設定
            mucom2.freq_lfo();// 周波数の補正
            calcdt();
            r.ax += r.dx;
            r.dx = r.pop();
            r.dx &= 0xf800;
            r.ax += r.dx;
            (r.al, r.ah) = (r.ah, r.al);

            if (work.md == null)
            {
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            }
            else
            {
                work.md.dat = r.al;
                work.md.args[work.mdArgsStep + 0] = work.ontei + work.oct * 12;
                work.md.args[work.mdArgsStep + 1] = work.otoLength;
                work.md.linePos.chip = work.crntChip;
                work.md.linePos.chipNumber = 0;
                work.md.linePos.ch = (byte)work.crntChannel;
                work.md.linePos.part = work.crntPart;
                muap98.object_Buf[r.di++] = work.md;
            }

            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            // set F-Number2
            return;
        }

        //;	SSG 用変換ルーチン

        private void reads()
        {
            r.bx = 0;//ofs:data2
            r.bx += r.ax;
            r.dx = (ushort)(mucom2.data2[r.bx] | (mucom2.data2[r.bx + 1] << 8));

            r.al = mucom2.octdata;// load oct
            ushort ans = r.pop();
            mucom2.octdata = (byte)ans;
            mucom2.octsave = (byte)(ans >> 8);

            //loop16:
            while (r.al != 0)
            {
                r.dx >>= 1;
                r.al--;
            }

            //exit8:
            r.ah = mucom2.dtdata[0];// ディチューンの設定
            mucom2.freq_lfo();// 周波数による補正
            calcdt();
            r.dx -= r.ax;
            r.al = r.dh;
            r.ah = r.dl;

            if(work.md == null)
            {
                muap98.object_Buf[r.di++] = new MmlDatum(r.al);
            }
            else
            {
                work.md.dat = r.al;
                work.md.args[work.mdArgsStep + 0] = work.ontei + work.oct * 12;
                work.md.args[work.mdArgsStep + 1] = work.otoLength;
                work.md.linePos.chip = work.crntChip;
                work.md.linePos.chipNumber = 0;
                work.md.linePos.ch = (byte)work.crntChannel;
                work.md.linePos.part = work.crntPart;
                muap98.object_Buf[r.di++] = work.md;
            }

            muap98.object_Buf[r.di++] = new MmlDatum(r.ah);
            read_exit();
        }

        //;-----------------------------------
        //;	ALの小文字－大文字変換
        //;-----------------------------------

        public void xsmall()
        {
            if (r.al > (byte)'z')
            {
                return;
            }
            if (r.al >= (byte)'a')
            {
                r.al -= (byte)' ';
            }
            //nxsmall:
            return;
        }

        //;-----------------------------------
        //;	ALの数字チェック(\ ok)
        //;	exit	CY = 数字以外
        //;-----------------------------------

        public void chknum()
        {
            if (r.al != (byte)'\\')// マクロ変数か
            {
                chknum2();
                return;
            }
            r.push(r.ax);
            r.al = (byte)(
                (r.bx + 1) >= muap98.source_Buf.Length
                ? 0
                : muap98.source_Buf[r.bx + 1]
                );// 次の文字をチェック
            chknum2();
            if (r.carry)
            {
                // 範囲外の数字はだめ
                //chknum3:
                r.ax = r.pop();
                return;
            }
            r.carry = r.al < (byte)'1';
            r.al -= (byte)'1';// AL = マクロ変数番号(0～8)
            if (r.carry)
            {
                //chknum3:
                r.ax = r.pop();
                return;
            }
            r.push(r.cx);
            r.cl = r.al;
            r.ax = 1;
            r.ax <<= r.cl;// AX = 変数の指定したよビット値
            r.zero = ((mucom2.macroflg & r.ax) != 0);// 格納されているか
            r.cx = r.pop();
            r.carry = false;
            if (r.zero)
            {
                r.bx += 2;// 指定されなかった場合は指定キャラクタを削除
                r.carry = true;
            }

            //chknum3:
            r.ax = r.pop();
            return;
        }

        private void chknum2()
        {
            if (r.al < (byte)'0')
            {
                r.carry = true;
                return;
            }
            r.carry = (r.al < (byte)'9' + 1);
            r.carry = !r.carry;
            //chknum1:
            return;
        }

        //;----------------------------
        //;	カンマチェック
        //;----------------------------

        private void chkcm()
        {
            mucom2.chktxt();
            r.zero = r.al == (byte)',';
            r.cl = 32;
            if (!r.zero)
            {
                mucom2.error();
            }
            return;
        }

    }
}