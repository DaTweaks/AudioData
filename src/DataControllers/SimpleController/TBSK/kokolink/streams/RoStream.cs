using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.streams.rostreams;
namespace jp.nyatla.kokolink.streams{


    // """T型のRoStreamです。
    // """
    class RoStream<T>:BasicRoStream<T>{
        readonly private IPyIterator<T> _src;
        private Int64 _pos;
        public RoStream(IPyIterator<T> src):base()
        {
            this._src=src;
            this._pos=0;
        }
        override public T Next(){
            T r;
            try{
                r= this._src.Next(); //#RecoverableStopInterationを受け取っても問題ない。
            }catch(RecoverableStopIteration e){
                throw e;
            }
            this._pos+=1;
            return r;
        }
        override public Int64 Pos
        {
            get=>this._pos;
        }
    }

}


