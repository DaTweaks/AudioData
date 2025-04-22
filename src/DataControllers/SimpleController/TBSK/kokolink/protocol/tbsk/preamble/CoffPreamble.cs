

using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.streams;
using jp.nyatla.kokolink.types;

using jp.nyatla.kokolink.utils;
using jp.nyatla.kokolink.utils.math;
using jp.nyatla.kokolink.utils.math.corrcoef;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.compatibility;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;
using jp.nyatla.kokolink.protocol.tbsk.traitblockcoder;


namespace jp.nyatla.kokolink.protocol.tbsk.preamble
{

    public class PreambleBits : TraitBlockEncoder
    {
        public PreambleBits(TraitTone symbol,int cycle) :base(symbol)
        {
            var b = Functions.Repeat(cycle, 1);// [1]*self._cycle;
            var c = new List<int>();
            //var c =[i % 2 for i in range(this._cycle)];
            for (var i = 0; i< cycle; i++)
            {
                c.Add(i % 2);
            }
            //var d =[(1 + c[-1]) % 2, (1 + c[-1]) % 2, c[-1],];
            var d = new int[] { (1 + c.Last()) % 2, (1 + c.Last()) % 2, c.Last() };
            //var w = new BitStream(Functions.Flatten(new int[] { 0, 1 }, b, new int[] { 1 }, c, d), bitwidth: 1);
            //var w2=Functions.ToEnumerable(w);
            //Console.WriteLine(w2.Count());
            this.SetInput(new BitStream(Functions.Flatten(new int[] { 0, 1 }, b, new int[] { 1 }, c, d),bitwidth: 1));
            // return enc.SetInput(new BitStream([0, 1] + b +[1] + c + d, 1));
            // # return enc.setInput(BitStream([0,1]+[1,1]+[1]+[0,1]+[0,0,1],1))
            // # return enc.setInput(BitStream([0,1,1,1,1,0,1,0,0,1],1))
        }

    }
    // """ 台形反転信号プリアンブルです。
    // """
    public class CoffPreamble:Preamble
    {
        private double _threshold;
        readonly private TraitTone _symbol;
        private int _cycle; //#平坦部分のTick数
        private bool _asmethtod_lock;
        public const double DEFAULT_TH = 1.0;
        public const int DEFAULT_CYCLE = 4;

        public CoffPreamble(TraitTone tone, double threshold = DEFAULT_TH, int cycle= DEFAULT_CYCLE)
        {
            this._threshold=threshold;
            this._symbol=tone;
            this._cycle=cycle; //#平坦部分のTick数
            this._asmethtod_lock = false;
        }

        public IRoStream<double> GetPreamble(){
            return new PreambleBits(this._symbol,this._cycle);
        }

        public class WaitForSymbolAS : AsyncMethod<int?>{
            readonly private CoffPreamble _parent;
            readonly private BufferedIterator<double> _cof;
            readonly private AverageIterator _avi;
            readonly private int _sample_width;
            readonly private int _cofbuf_len;
            readonly private int _symbol_ticks;
            private RingBuffer<double> _rb;
            private double _gap;
            private int _nor;
            private double? _pmax;
            private int _co_step;
            private int? _result;
            private bool _closed;

            public WaitForSymbolAS(CoffPreamble parent,IRoStream<double> src):base()
            {
                var symbol_ticks = parent._symbol.Count;
                //#後で見直すから10シンボル位記録しておく。
                var cofbuf_len = symbol_ticks * (6 + parent._cycle * 2);
                //# cofbuf_len=symbol_ticks*10
                this._parent = parent;
                this._cof = new BufferedIterator<double>(AlgorithmSwitch.CreateSelfCorrcoefIterator(symbol_ticks, src, symbol_ticks), cofbuf_len, 0);
                this._avi = new AverageIterator(this._cof, symbol_ticks);
                var sample_width = parent._cycle + 1;
                //# rb=RingBuffer(symbol_ticks*3,0)
                this._sample_width = sample_width;
                this._cofbuf_len = cofbuf_len;
                this._symbol_ticks = symbol_ticks;
                this._rb = new RingBuffer<double>(symbol_ticks * sample_width, 0);
                this._gap = 0; //#gap
                this._nor = 0; //#ストリームから読みだしたTick数
                //this._pmax;
                this._co_step = 0;
                this._result = null;
                this._closed = false;

            }
            override public int? Result
            {
                get{
                    Debug.Assert(this._co_step >= 4);
                    return this._result;
                }
            }
            override public void Close()
            {
                if (!this._closed)
                {
                    this._parent._asmethtod_lock = false;
                    this._closed = true;
                }
            }
            override public bool Run()
            {
                Debug.Assert(!this._closed);
                //# print("wait",self._co_step)
                if (this._closed)
                {
                    return true;
                }
                //# ローカル変数の生成
                var avi = this._avi;
                var cof = this._cof;
                var rb = this._rb;
                try
                {
                    while (true)
                    {
                        //# ギャップ探索
                        if (this._co_step == 0)
                        {
                            this._gap = 0;
                            this._co_step = 1;
                        }
                        //# ASync #1
                        if (this._co_step == 1)
                        {
                            while (true)
                            {
                                try
                                {
                                    rb.Append(avi.Next());
                                    //# print(rb.tail)
                                    this._nor = this._nor + 1;
                                    this._gap = rb.Top - rb.Tail;
                                    if (this._gap < 0.5)
                                    {
                                        continue;
                                    }
                                    if (rb.Top < 0.1)
                                    {
                                        continue;
                                    }
                                    if (rb.Tail > -0.1)
                                    {
                                        continue;
                                    }
                                    break;
                                }
                                catch (RecoverableStopIteration)
                                {
                                    return false;
                                }
                            }
                            this._co_step = 2; //#Co進行
                        }
                        if (this._co_step == 2)
                        {
                            //# print(1,self._nor,rb.tail,rb.top,self._gap)
                            //# ギャップ最大化
                            while (true)
                            {
                                try
                                {
                                    rb.Append(avi.Next());
                                    this._nor = this._nor + 1;
                                    var w = rb.Top - rb.Tail;
                                    if (w >= this._gap)
                                    {
                                        //# print(w,self._gap)
                                        this._gap = w;
                                        continue;
                                    }
                                    break;
                                }
                                catch (RecoverableStopIteration)
                                {
                                    return false;
                                }
                            }
                            //# print(2,nor,rb.tail,rb.top,self._gap)
                            if (this._gap < this._parent._threshold)
                            {
                                this._co_step = 0;// #コルーチンをリセット
                                continue;
                            }
                            //# print(3,nor,rb.tail,rb.top,self._gap)
                            //# print(2,nor,self._gap)
                            this._pmax = rb.Tail;
                            this._co_step = 3;
                        }
                        if (this._co_step == 3)
                        {
                            //#同期シンボルピーク検出
                            while (true)
                            {
                                try
                                {
                                    var n = avi.Next();
                                    this._nor = this._nor + 1;
                                    if (n > this._pmax)
                                    {
                                        this._pmax = n;
                                        continue;
                                    }
                                    if (this._pmax > 0.1)
                                    {
                                        break;
                                    }
                                }
                                catch (RecoverableStopIteration)
                                {
                                    return false;
                                }
                            }
                            this._co_step = 4; //#end
                            var symbol_ticks = this._symbol_ticks;
                            var sample_width = this._sample_width;
                            var cofbuf_len = this._cofbuf_len;
                            var cycle = this._parent._cycle;


                            //# #ピーク周辺の読出し
                            //# [next(cof) for _ in range(symbol_ticks//4)]
                            //# バッファリングしておいた相関値に3値平均フィルタ
                            var buf = Functions.ToArray(cof.Buf.SubIter(cof.Buf.Length - symbol_ticks, symbol_ticks));//buf = cof.buf[-symbol_ticks:]


                            var b = new List<ValueTuple<int, double>>();
                            for (var i = 0; i < buf.Length - 2; i++)
                            {
                                b.Add((i + this._nor - symbol_ticks + 1, buf[i] + buf[i + 1] + buf[i+2]));
                            }
                            //var b.sort(key = lambda x: x[1], reverse = True);
                            b.Sort((a, b) => a.Item2 == b.Item2 ? 0 : (a.Item2 < b.Item2 ? 1 : -1));



                            //#ピークを基準に詳しく様子を見る。
                            var peak_pos = b[0].Item1;//b[0][0];
                            //# print(peak_pos-symbol_ticks*3,(self._nor-(peak_pos+symbol_ticks*3)))

                            //# Lレベルシンボルの範囲を得る
                            var s = peak_pos - symbol_ticks * sample_width - (this._nor - cofbuf_len);

                            var lw = Functions.ToArray(cof.Buf.SubIter(s, cycle * symbol_ticks));

                            Array.Sort(lw);//cof.buf[s: s + cycle * symbol_ticks]
                            // 実装ミスで機能してないからコメントアウト
                            //lw = lw.Take(lw.Length * 3 / 2 + 1).ToArray(); //lw[:len(lw) * 3 / 2 + 1];
                            if (lw.Sum() / lw.Length > lw[0] * 0.66)
                            {
                                this._co_step = 0;//#co_step0からやり直す。
                                continue;// #バラツキ大きい
                            }
                            //#Hレベルシンボルの範囲を得る
                            //# s=peak_pos-symbol_ticks*6-(self._nor-cofbuf_len)
                            s = peak_pos - symbol_ticks * sample_width * 2 - (this._nor - cofbuf_len);
                            var lh = Functions.ToArray(cof.Buf.SubIter(s, cycle * symbol_ticks));

                            Array.Sort(lh);
                            Array.Reverse(lh);
                            // 実装ミスで機能してないからコメントアウト
                            //lh = lh.Take(lh.Length * 3 / 2 + 1).ToArray(); //lh = lh[:len(lh) * 3 / 2 + 1]

                            if (lh.Sum() / lh.Length < lh[0] * 0.66)
                            {
                                //# print("ERR(H",peak_pos+src.pos,sum(lh)/len(lh),max(lh))
                                this._co_step = 0;// #co_step0からやり直す。
                                continue; //#バラツキ大きい
                            }
                            //#値の高いのを抽出してピークとする。
                            //# print(peak_pos)
                            this._result = peak_pos - this._nor;//#現在値からの相対位置
                            this.Close();
                            return true;
                        }
                        throw new Exception("Invalid co_step");
                    }
                }
                catch (PyStopIteration) {
                    this._co_step = 4; //#end
                    this.Close();
                    this._result = null;
                    return true;
                    //# print("END")
                } catch (Exception) {
                    this._co_step = 4; //#end
                    this.Close();
                    throw;
                }
            }
        }
        //""" 尖形のピーク座標を返します。座標は[0:-1],[1:1],[2:1],[3:-1]の[2:1]の末尾に同期します。
        //    値はマイナスの事もあります。
        //    @raise
        //        入力からRecoverableStopInterationを受信した場合、RecoverableExceptionを送出します。
        //        呼び出し元がこの関数を処理しない限り, 次の関数を呼び出すことはできません。
        //        終端に到達した場合は、Noneを返します。
        //"""
        public int? WaitForSymbol(IRoStream<double> src)
        {
            Debug.Assert(!this._asmethtod_lock);
            var asmethtod = new WaitForSymbolAS(this, src);
            if (asmethtod.Run())
            {
                return asmethtod.Result;
            }
            else
            {
                //# ロックする（解放はASwaitForSymbolのclose内で。）
                this._asmethtod_lock = true;
                throw new RecoverableException<WaitForSymbolAS, int?>(asmethtod);
            }
        }
    }
}