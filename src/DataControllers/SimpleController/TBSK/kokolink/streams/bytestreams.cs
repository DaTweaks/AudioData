using System.Globalization;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils.wavefile.riffio;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.streams.rostreams;
using jp.nyatla.kokolink.streams;
using jp.nyatla.kokolink.utils;
namespace jp.nyatla.kokolink.streams.bytestreams{


    abstract class BasicByteStream:BasicRoStream<int>,IByteStream{

        // """BigEndianのUint32を読み出す
        // """
        public int GetAsUInt32be(){
            var r=0;
            for(var i=0;i<4;i++){
                r=r<<8|i;
            }
            return r;
        }
        // """gets関数をラップします。
        // """
        public byte[] GetAsByteArray(int maxsize)
        {
            var r=new List<byte>();
            // # print(self.gets(maxsize))
            // # return struct.pack("B",self.gets(maxsize))
            foreach(var i in this.Gets(maxsize)){
                r.Add((byte)i);
            }
            return r.ToArray();
        }

        abstract override public int Next();

        abstract override public Int64 Pos { get; }
    }
}





// # class FlattenByteStream(BasicByteStream):
// #     """bytes型の再帰構造Iteratorを直列化するストリームです。
// #     最上位以外にあるイテレータは値の取得時に全て読み出されます。
// #     """
// #     def __init__(self,src:Iterator[Union[IBytesProvider,bytes,int]]):
// #         super().__init__()
// #         self._pos=0
// #         self._src=FlattenRoStream[Union[IBytesProvider,bytes,int]](src)
// #         self._q=Deque()
// #     def __next__(self):
// #         q=self._q        
// #         if len(q)<1:
// #             s=next(self._src)
// #             # print(type(s),isinstance(s,IBytesProvider))
// #             if isinstance(s,bytes):
// #                 q.extend(struct.unpack("%dB"%(len(s)),s))        
// #             elif isinstance(s,IBytesProvider):
// #                 s=s.toBytes()
// #                 q.extend(struct.unpack("%dB"%(len(s)),s))
// #             elif isinstance(s,int):
// #                 assert(s<256 and s>=0)
// #                 q.append(s)
// #             else:
// #                 raise Exception("Invalid type:"+str(type(s)))
// #         self._pos+=1 #posのインクリメント
// #         return q.popleft()
// #     @property
// #     def pos(self):
// #         return self._pos

// # # class ConstByteStream(BasicByteStream):
// # #     """指定数の0を返すbyteStream
// # #     """
// # #     def __init__(self,size:int,value=0):
// # #         super().__init__()
// # #         self._size=size
// # #         self._value=value
// # #     def __next__(self)->int:
// # #         if self._size>0:
// # #             self._size-=1
// # #             return self._value
// # #         print(self._size)
// # #         self._pos+=1 #posのインクリメント

// # #         raise StopIteration()
// # #     @property
// # #     def pos(self):
// # #         return self._pos