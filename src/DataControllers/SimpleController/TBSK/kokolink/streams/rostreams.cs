using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.types;
using jp.nyatla.kokolink.utils.recoverable;

namespace jp.nyatla.kokolink.streams.rostreams
{
    abstract public class BasicRoStream<T>: BasicIterator<T>,IRoStream<T>
    {
        readonly private Queue<T> _savepoint;
        // """ IRoStreamの基本実装です。
        // __next__,posメソッドを実装することで機能します。seek,getsはgetをラップしてエミュレートします。
        // __next__メソッドの中でself#_posを更新してください。
        // """
        public BasicRoStream()
        {
            this._savepoint=new Queue<T>();
        }
        public T Get(){
            if(this._savepoint.Count>0){
                // #読出し済みのものがあったらそれを返す。
                var r=this._savepoint.Dequeue();
                // this._savepoint=self._savepoint[1:]
                // if(this._savepoint.Length==0){
                //     self._savepoint=null;
                // }
                return r;
            }
            return this.Next();
        }
        public IEnumerable<T> Gets(int maxsize,bool fillup=false){
            var r=this._savepoint;
            try{
                int len= maxsize - r.Count;
                for (var i=0;i<len;i++){
                    r.Enqueue(this.Next());
                }
            }catch(RecoverableStopIteration e){
                throw e;
                // self._savepoint=r
                // raise RecoverableStopIteration(e)
            }catch(PyStopIteration e){
                if(fillup || r.Count==0){
                    throw e;
                }
            }
            Debug.Assert(r.Count<=maxsize);
            var ret=new List<T>();
            while(r.Count>0){
                ret.Add(r.Dequeue());
            }
            return ret;

        }
        public void Seek(int size){
            try{
                this.Gets(size,true);
            }catch(RecoverableStopIteration e){
                throw e;
            }catch(PyStopIteration e){
                throw e;
            }
            return;
        }
        abstract public Int64 Pos
        {
            get;
        }
    }

// class FlattenRoStream(BasicRoStream[T],Generic[T]):
//     """T型の再帰構造のIteratorを直列化するストリームです。
//     最上位以外にあるインレータは値の取得時に全て読み出されます。
//     T型はIterator/Iterable/Noneな要素ではないことが求められます。
//     """
//     def __init__(self,src:Union[Iterator[T],Iterable[T]]):
//         super().__init__()
//         self._pos=0
//         def toIterator(s):
//             if isinstance(s,Iterable):
//                 return iter(s)
//             else:
//                 return s
//         def rextends(s:Union[Iterator[T],Iterable[T]]):
//             while True:
//                 try:
//                     i=next(s)
//                 except RecoverableStopIteration:
//                     yield None
//                     continue
//                 except StopIteration:
//                     break
//                 if isinstance(i,(Iterable,Iterator)) and not isinstance(i,(str,bytes)):
//                     yield from rextends(toIterator(i))
//                     continue
//                 else:
//                     yield i
//                     continue

//         self._gen=rextends(toIterator(src))
//     def __next__(self):
//         r=next(self._gen)
//         if r is None:
//             raise RecoverableStopIteration()
//         self._pos+=1 #posのインクリメント
//         return r
//     @property
//     def pos(self):
//         return self._pos



// class PeekRoStream(BasicRoStream[T],Generic[T]):
//     """PeekableStreamをラップしてPeekを使ったRoStreamを生成します。
//     ラップしているストリームを途中で操作した場合、このインスタンスの取得値は影響を受けます。
//     """
//     def __init__(self,src:IPeekableStream):
//         self._src=src
//         self._pos=0
//     def __next__(self)->T:
//         try:
//             r=self._src.peek(self._pos)
//         except RecoverableStopIteration as e:
//             raise RecoverableStopIteration(e)
//         self._pos+=1
//         return r
//     def pos(self)->int:
//         return self._pos
        



        
}














