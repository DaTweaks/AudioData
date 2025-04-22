using jp.nyatla.kokolink.utils.math;
using jp.nyatla.kokolink.types;


namespace jp.nyatla.kokolink.protocol.tbsk.toneblock
{
    public class TraitTone:List<double>{
        public TraitTone(IEnumerable<double> d):base(d)
        {
        }
        // """ 信号強度をv倍します。
        // """
        public TraitTone Mul(double v){
            for (var i = 0; i < this.Count; i++)
            {
                this[i] = this[i] * v;
            }
            return this;
        }
    }

    // """ Sin波形のトーン信号です。
    //     このトーン信号を使用したTBSKはDPSKと同じです。
    // """
    public class SinTone:TraitTone{
        static private IEnumerable<double> _constructor_init(int points, int cycle)
        {
            var s = Math.PI * 2 / points * 0.5;
            var d1 = new List<double>();
            for (var i = 0; i < points; i++)
            {
                d1.Add((double)Math.Sin(s + i * Math.PI * 2 / points));
            }
            var d2 = new List<double>();
            for (var i = 0; i < cycle; i++)
            {
                d2.AddRange(d1);
            }
            return d2;

        }
        public SinTone(int points,int cycle=1):base(SinTone._constructor_init(points,cycle))
        {
        }
    }

    // """ トーン信号を巡回符号でBPSK変調した信号です。
    //     2^bits-1*len(base_tone)の長さです。
    // """
    public class MSeqTone:TraitTone{
        readonly private IEnumerable<int> _sequence;
        static private IEnumerable<double> _constructor_init(MSequence mseq,TraitTone? base_tone)
        {
            var tone = base_tone != null ? base_tone : new SinTone(20, 1);
            var a = new List<double>();
            foreach (var i in mseq.GenOneCycle())
            {
                foreach (var j in tone)
                {
                    a.Add(j * (i * 2 - 1));
                }
            }
            return a;
        }
        public MSeqTone(MSequence mseq, TraitTone? base_tone = null) : base(_constructor_init(mseq, base_tone))
        {
            this._sequence = mseq.GenOneCycle();
        }

        public MSeqTone(int bits,int tap,TraitTone? base_tone=null):this(new MSequence(bits, tap), base_tone)
        {
        }
        public IEnumerable<int> Sequence
        {
            get=>this._sequence;
        }
    }

    public class PnTone:TraitTone{
        static private IEnumerable<double> _constructor_init(uint seed, int interval, TraitTone? base_tone)
        {
            var tone = base_tone != null ? base_tone : new SinTone(20,10);
            var pn = new XorShiftRand31(seed, skip: 29);
            var c = 0;
            int f=0;
            var d = new List<double>();
            foreach (var i in tone)
            {
                if (c % interval == 0)
                {
                    f = (pn.Next() & 0x02) - 1;
                }
                c = c + 1;
                d.Add(i * f);
            }
            return d;
        }
        // """ トーン信号をPN符号でBPSK変調した信号です。
        //     intervalティック単位で変調します。
        // """
        public PnTone(uint seed,int interval=2,TraitTone? base_tone = null) : base(_constructor_init(seed, interval, base_tone))
        {
        }
    }



    // """ Sin波形をXPSK変調したトーン信号です。
    //     1Tick単位でshiftイテレータの返す値×2pi/divの値だけ位相をずらします。
    // """
    public class XPskSinTone:TraitTone{
        class DefaultIter : IPyIterator<int>
        {
            private XorShiftRand31 _pn;
            public DefaultIter()
            {
                this._pn = new XorShiftRand31(999, skip: 299);
            }
            public int Next()
            {
                return ((this._pn.Next() & 0x01) * 2 - 1);
            }
        }
        // """
        //     Args:
        //     shift   -1,0,1の3値を返すイテレータです。省略時は乱数値です。
        // """

        static private IEnumerable<double> _constructor_init(int points, int cycle, int div, IPyIterator<int>? shift)
        {
            var delta = Math.PI * 2 / points;
            var lshift = (shift != null) ? shift:new DefaultIter();
            var s = delta * 0.5;
            var d = new List<double>();
            for (var i = 0; i<points * cycle; i++)
            {
                s = s + delta + lshift.Next() * (Math.PI * 2 / div);
                d.Add(Math.Sin(s));
            }
            return d;
        }
        public XPskSinTone(int points,int cycle=1,int div=8, IPyIterator<int>? shift = null) : base(_constructor_init(points, cycle, div, shift))
        {

        }
    }
}


