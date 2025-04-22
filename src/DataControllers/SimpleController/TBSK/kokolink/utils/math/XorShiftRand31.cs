using jp.nyatla.kokolink.types;
namespace jp.nyatla.kokolink.utils.math
{
    // """ https://ja.wikipedia.org/wiki/Xorshift
    // """
    class XorShiftRand31:IPyIterator<Int32>
    {
        private UInt64 _seed;
        public XorShiftRand31(UInt32 seed,int skip=0){
            this._seed=seed;
            
            for(var i=0;i<skip;i++){
                this.Next();
            }
        }
        public Int32 Next(){
            var y=this._seed;
            y = y ^ (y << 13);
            y = y ^ (y >> 17);
            y = y ^ (y << 5);
            y = y & 0x7fffffff;
            this._seed=y;
            return (Int32)y;
        }
        // def randRange(self,limit:int):
        //     """ 0<=n<limit-1の値を返します。
        //     """
        //     return next(self) % limit
        public Int32 RandRange(Int32 limit){
            return this.Next() %limit;
        }
    }
}


