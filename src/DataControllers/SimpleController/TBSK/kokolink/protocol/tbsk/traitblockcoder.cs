using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.utils.math.corrcoef;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.math;
using jp.nyatla.kokolink.protocol.tbsk.toneblock;


namespace jp.nyatla.kokolink.protocol.tbsk.traitblockcoder
{


    // """ ビット列をTBSK変調した振幅信号に変換します。出力レートは入力のN倍になります。
    //     N=len(trait_block)*2*len(tone)
    //     このクラスは、toneを一単位とした信号とtrail_blockとの積を返します。
    // """
    public class TraitBlockEncoder: BasicRoStream<double>,IEncoder<TraitBlockEncoder,IBitStream, double>{
        private Int64 _pos;
        private readonly List<int> _sblock;
        private TraitTone _btone;
        private Queue<double> _tone_q;
        private IBitStream? _src;
        private bool _is_eos;

        // @overload
        // """
        //     Kビットは,N*K+1個のサンプルに展開されます。
        // """
        public TraitBlockEncoder(TraitTone tone):base()
        {
            this._sblock=new List<int>(new int[]{-1});
            this._btone=tone;
            this._tone_q=new Queue<double>();
        }
        public TraitBlockEncoder SetInput(IBitStream src)
        {
            if (src == null)
            {
                this._is_eos = true;
            }
            else
            {
                this._is_eos = false;//True if src is None else False
            }
            this._tone_q.Clear();//=new Queue<float>();
            // # print(len(self._tone_q))
            this._pos = 0;
            this._src = src;
            return this;
        }

        override public double Next(){
            if(this._tone_q.Count==0){
                if(this._is_eos){
                    throw new PyStopIteration();
                }
                try{
                    var sign=this._src!.Next()!=0?1:-1;//1 if next(self._src)!=0 else -1
                    foreach(var i in this._sblock){
                        foreach(var j in this._btone){
                            this._tone_q.Enqueue(sign*i*j);
                        }
                    }
                }catch(PyStopIteration e){
                    this._is_eos=true;
                    throw e;
                }
            }
            var r=this._tone_q.Dequeue();
            this._pos+=1; //#posのインクリメント
            return r;
        }
        // @property
        override public Int64 Pos
        {
            get=>this._pos;
        }
    }

    // """ シンボル幅がNのTBSK相関信号からビットを復調します。

    // """
    public class TraitBlockDecoder: BasicRoStream<int>,IBitStream,IDecoder<TraitBlockDecoder,IRoStream<double>,int>
    {
        private readonly int _trait_block_ticks;
        private AverageIterator? _avefilter;
        readonly private double _threshold;
        private bool _is_eos;
        private Int64 _pos;
        readonly private List<double> _samples;
        private ISelfCorrcoefIterator? _cof;
        private double _last_data;
        private int _preload_size;
        private int _block_skip_size;
        private int _block_skip_counter;
        private int _shift;
        public TraitBlockDecoder(int trait_block_ticks,double threshold=0.2)
        {
            this._is_eos=true;
            this._trait_block_ticks=trait_block_ticks;
            this._threshold=threshold;
            this._samples = new List<double>();// #観測値
        }

        // """ 
        //     Args:
        //         src
        //             TBSK信号の開始エッジにポインタのあるストリームをセットします。
        // """
        public TraitBlockDecoder SetInput(IRoStream<double>? src)
        {
            if (src == null)
            {
                this._is_eos = true;
            }
            else
            {
                this._is_eos = false;
                this._cof = AlgorithmSwitch.CreateSelfCorrcoefIterator(this._trait_block_ticks, src, this._trait_block_ticks);
                var ave_window = Math.Max((int)(this._trait_block_ticks * 0.1), 2);// #検出用の平均フィルタは0.1*len(tone)//2だけずれてる。個々を直したらtbskmodem#TbskModulatorも直せ
                this._avefilter = new AverageIterator(this._cof, ave_window);
                this._last_data = 0;

                this._preload_size = this._trait_block_ticks + ave_window / 2 - 1;    //#平均値フィルタの初期化サイズ。ave_window/2足してるのは、平均値の遅延分.
                this._block_skip_size = this._trait_block_ticks - 1 - 2; //#スキップ数
                this._block_skip_counter = this._block_skip_size; //#スキップ数
                this._samples.Clear();// = new List<double>();// #観測値
                this._shift = 0;
            }
            // # try:
            // #     [next(self._avefilter) for i in range(self._trait_block_ticks+ave_window//2)]
            // # except StopIteration:
            // #     self._is_eos=True
            this._pos = 0;
            return this;
        }
        override public int Next()
        {
            if(this._is_eos){
                throw new PyStopIteration();
            }
            try{
                // #この辺の妙なカウンターはRecoverableStopInterationのため
                var lavefilter = this._avefilter!;

                // #平均イテレータの初期化(初めの一回目だけ)
                while (this._preload_size>0){
                    lavefilter.Next();
                    this._preload_size=this._preload_size-1;
                }
                // #ブロックヘッダの読み出し(1ブロック読出しごとにリセット)
                while(this._block_skip_counter>0){
                    lavefilter.Next();
                    this._block_skip_counter=this._block_skip_counter-1;
                }
                while(this._samples.Count<3){
                    this._samples.Add(lavefilter.Next());
                }




                var samples=this._samples;
                var r=samples[1];
                if (samples[0] * samples[1] < 0 || samples[1] * samples[2] < 0) {
                    // #全ての相関値が同じ符号でなければ何もしない
                    this._block_skip_counter = this._block_skip_size; //#リセット
                } else {
                    var asample = new double[samples.Count];
                    // #全ての相関値が同じ符号
                    for (var i = 0; i < samples.Count; i++) {
                        asample[i] = Math.Abs(samples[i]);
                    }
                    // #一番大きかったインデクスを探す
                    if (asample[1] > asample[0] && asample[1] > asample[2]) {
                        // #遅れも進みもしてない
                        // pass
                    } else if (asample[0] > asample[2]) {
                        // #探索場所が先行してる
                        this._shift = this._shift - 1;
                    } else if (asample[0] < asample[2]) {
                        // #探索場所が遅行してる
                        this._shift = this._shift + 1;
                    } else {
                        // #不明
                        // pass
                    }

                    if (this._shift > 10) {
                        this._shift = 0;
                        // # print(1)
                        this._block_skip_counter = this._block_skip_size + 1;
                    } else if (this._shift < -10) {
                        this._shift = 0;
                        // # print(-1)
                        this._block_skip_counter = this._block_skip_size - 1;
                    } else {
                        this._block_skip_counter = this._block_skip_size;
                    }
                }


                this._samples.Clear();

                // # print(self._src.pos,r)
                var th=this._threshold;
                this._pos=this._pos+1;

                if(r>th){
                    // # print(1,1)
                    this._last_data=r;
                    return 1;
                }else if(r<-th){
                    // # print(2,0)
                    this._last_data=r;
                    return 0;
                }else if(this._last_data-r>th){
                    // # print(3,1)
                    this._last_data=r;
                    return 1;
                }else if(r-this._last_data>th){
                    // # print(4,0)
                    this._last_data=r;
                    return 0;
                }else{
                    this._is_eos=true;
                    throw new PyStopIteration();
                }
            }catch(RecoverableStopIteration e){
                throw e;
            }catch(PyStopIteration e){
                this._is_eos=true;
                throw e;
            }
        }
        // @property
        override public Int64 Pos
        {
            get=>this._pos;
        }
    }
}
        

