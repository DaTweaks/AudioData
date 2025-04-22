using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.recoverable;
namespace jp.nyatla.kokolink.utils.math.corrcoef
{

    // from math import sqrt
    // from typing import Union



    // from ....types import Deque, Iterable, Iterator
    // from ...recoverable import RecoverableIterator, RecoverableStopIteration

    // """ src[:]とsrc[shift:]の相関を返すストリームです。
    //     n番目に区間[n,n+window]と[n+shift,n+shift+window]の相関値を返します。
    //     開始からwindow-shift個の要素は0になります。
    // """

    class SelfCorrcoefIterator : ISelfCorrcoefIterator
    {
        readonly private double[]?[] xyi;
        private int c;
        private int n;
        private double sumxi;
        private double sumxi2;
        private double sumyi;
        private double sumyi2;
        private double sumxiyi;
        readonly private IPyIterator<double> _srcx;
        readonly private Queue<double> _srcy;

        public SelfCorrcoefIterator(int window, IPyIterator<double> src, int shift = 0)
        {

            this.xyi = new double[window][];// Functions.Create2dArray<double?>(window,2,null); //#Xi
            for (var i = 0; i < window; i++)
            {
                this.xyi[i] = null;
            }

            this.c = 0;//#エントリポイント
            this.n = 0;//#有効なデータ数
            this.sumxi = 0;
            this.sumxi2 = 0;
            this.sumyi = 0;
            this.sumyi2 = 0;
            this.sumxiyi = 0;
            this._srcx = src;
            this._srcy = new Queue<double>();
            // """初期化
            // """
            for (var i = 0; i < shift; i++)
            {
                this._srcy.Enqueue(0);//extend([0]);
            }
            return;
        }
        public double Next()
        {
            var c = this.c;
            var l = this.xyi.Length;
            var m = c % l;
            double vx;
            try
            {
                vx = this._srcx.Next();
            }
            catch (RecoverableStopIteration e)
            {
                throw e;
            }


            this._srcy.Enqueue(vx);
            var vy = this._srcy.Dequeue();

            if (this.xyi[m] == null)
            {
                // #値追加
                this.n += 1 - 0;
                this.sumxi += vx - 0;
                this.sumxi2 += vx * vx - 0;//vx**2-0;
                this.sumyi += vy - 0;
                this.sumyi2 += vy * vy - 0;// vy**2-0;
                this.sumxiyi += vx * vy - 0;
                this.xyi[m] = new double[] { vx, vy };
            }
            else
            {
                // #値削除
                var lxi = this.xyi[m]![0];
                var lyi = this.xyi[m]![1];
                this.n += 1 - 1;
                this.sumxi += vx - lxi;
                this.sumxi2 += vx * vx - lxi * lxi;
                this.sumyi += vy - lyi;
                this.sumyi2 += vy * vy - lyi * lyi;
                this.sumxiyi += vx * vy - lxi * lyi;
                this.xyi[m]![0] = vx;
                this.xyi[m]![1] = vy;
            }

            this.c += 1;
            Debug.Assert(this.n > 0);
            // if(this.n==0){
            //     return null;
            // }
            if (this.n == 1)
            {
                return 1;
            }

            var sumxi = this.sumxi;
            var meanx_ = sumxi / (this.n);
            var sumxi2 = this.sumxi2;
            var v = (sumxi2 + (meanx_ * meanx_) * this.n - 2 * meanx_ * sumxi);
            if (v < 0)
            {
                v = 0;
            }
            var stdx = Math.Sqrt(v / (this.n - 1));

            var sumyi = this.sumyi;
            var meany_ = sumyi / (this.n);
            var sumyi2 = this.sumyi2;
            v = (sumyi2 + (meany_ * meany_) * this.n - 2 * meany_ * sumyi);
            if (v < 0)
            {
                v = 0;
            }
            var stdy = Math.Sqrt(v / (this.n - 1));

            v = this.sumxiyi + this.n * meanx_ * meany_ - meany_ * sumxi - meanx_ * sumyi;
            var covxy = v / (this.n - 1);
            var r = stdx * stdy == 0 ? 0 : covxy / (stdx * stdy);
            return r > 1 ? 1f : (r < -1 ? -1 : r);
        }
    }

}





// # if __name__ == '__main__':
// #     import math
// #     import numpy as np


// #     def original(l,d1,d2):
// #         r=[]
// #         for i in range(len(d1)-l):
// #             r.append(np.corrcoef(d1[i:i+l],d2[i:i+l])[0][1])
// #         return r
// #     def manual(l,d1,d2):
// #         r=[]
// #         for i in range(len(d1)-l):
// #             # s1=np.std(d1[i:i+l],ddof=1)
// #             # s2=np.std(d2[i:i+l],ddof=1)
// #             s=np.cov(d1[i:i+l],d2[i:i+l])[0][1]
// #             # r.append(s/(s1*s2))
// #             r.append(s)
// #         return r

// #     def optimized(l,d1,d2):
// #         r=[]
// #         c=CorrcoefStream(l,iter(d1),iter(d2))
// #         # c=SelfCorrcoefStream(l,iter(d1),90)
// #         c=[i for i in c]
// #         print(len(c))
// #         return c
// #     def optimizeds(l,d1,s):
// #         r=[]
// #         c=SelfCorrcoefStream(l,iter(d1),s)
// #         c=[i for i in c]
// #         print(len(c))
// #         return c
// #     src=[math.sin(math.pi*2/360*i) for i in range(3600)]
// #     d1=src[20:]
// #     d2=src[:-20]
// #     # [d*abs(math.cos(math.pi*2/360*i)) for i,d in enumerate(d1)]

// #     import matplotlib.pyplot as plot


// #     fig = plot.figure()
// #     ax1 = fig.add_subplot(4, 1, 1)
// #     ax2 = fig.add_subplot(4, 1, 2)
// #     ax3 = fig.add_subplot(4, 1, 3)
// #     ax4 = fig.add_subplot(4, 1, 4)
// #     ax1.plot(d1)
// #     ax1.plot(d2)
// #     # ax2.plot(optimized(10,d1,d2))
// #     ax2.plot(original(10,d1,d2))

// #     import time
// #     time_sta = time.perf_counter()
// #     original(10,d1,d2)
// #     time_end = time.perf_counter()
// #     print("original",time_end- time_sta)

// #     time_sta = time.perf_counter()
// #     ax3.plot(optimized(10,d1,d2))
// #     time_end = time.perf_counter()
// #     print("oprimized",time_end- time_sta)

// #     time_sta = time.perf_counter()
// #     ax3.plot(optimizeds(10,d1,20))
// #     time_end = time.perf_counter()
// #     print("oprimizeds",time_end- time_sta)

// #     plot.draw()
// #     plot.show()