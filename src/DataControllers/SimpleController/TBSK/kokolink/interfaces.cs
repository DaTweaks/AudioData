// """
// このモジュールには、ライブラリ全体で頻繁に使用する共通のインタフェイスを定義します。
// """


using System.Collections;
using System;
using jp.nyatla.kokolink.types;

namespace jp.nyatla.kokolink.interfaces
{
    
    public interface IRecoverableIterator<T> : IPyIterator<T>
    {
    }
    public interface IRoStream<T>: IRecoverableIterator<T>
    {
        // @abstractmethod
        // def seek(self,size:int):
        //     """ストリームの先頭を移動します。
        //     終端に到達するとStopIterationをスローします。
        //     """
        //     pass
        void Seek(int size);
        // @abstractmethod
        // def get(self)->T:
        //     """ストリームの先頭から1要素を要素を取り出します。
        //     終端に到達するとStopIterationをスローします。
        //     """
        //     pass
        IEnumerable<T> Gets(int size,bool fillup=false);

        // @abstractproperty
        // def pos(self)->int:
        //     """ストリーム上の現在の読み出し位置です。get/getsで読み出した要素数+seekで読み出した要素数の合計です。
        //     """
        //     pass
        Int64 Pos{
            get;
        }
    }
    // class IPeekableStream(IRoStream[T],Generic[T],ABC):
    //     """Peek/Seek、及び任意サイズのPeek/Seek機能を備えたストリームインタフェイスです。
    //     Iteratorはgetのラッパーとして機能します。
    //     このストリームは内部状態を持つストリームに使わないでください。
    //     """
    //     @abstractmethod
    //     def peek(self,skip:int=0)->int:
    //         """読み出し位置を移動せずに1要素を取り出します。
    //         Args:
    //             skip 読み出し位置までのスキップ数です。
    //         """
    //         pass
    //     @abstractmethod
    //     def peeks(self,size:int,skip=0,fillup:bool=False):
    //         """読み出し位置を移動せずに最大でsize個の要素を取り出します。戻り値はsizeに満たないこともあります。
    //         Args:
    //             size 読み出しサイズです。
    //             skip 読み出し位置までのスキップ数です。
    //             fillup 戻り値のサイズをsizeに強制するフラグです。

    //         """
    //         pass


    // """Byte値のストリームです。返却型はbyteの値範囲を持つintです。
    // """
    public interface IByteStream:IRoStream<int>{
        // def getAsUInt32be(self)->int:
        //     """byteStreamから32bit unsigned int BEを読み出します。
        //     """
        //     pass
        int GetAsUInt32be();
        // def getAsByteArray(self,size)->bytearray:
        //     """byteStreamから指定サイズのbytearrayを読み出します。
        //     """
        //     pass
        byte[] GetAsByteArray(int maxsize);
    }

    // """ Bit値を返すストリームです。返却型は0,1の値範囲を持つintです。
    // """
    public interface IBitStream :IRoStream<int>{}

    // class IReader(Generic[T],ABC):
    //     """Tから値を読み出します。このクラスは分類であり関数を持ちません。
    //     """

    // class IScanner(Generic[T],ABC):
    //     """Tの読み出し位置を変更せずに値を抽出します。このクラスは分類であり関数を持ちません。
    //     """






    //     """入力と出力の型が異なるストリームです。
    //     ソース値のプロバイダSからD型の値を生成します。
    //     """
    public interface IFilter<IMPL,S,D> : IRoStream<D> where IMPL : IFilter<IMPL, S, D>
    {
        //     @abstractmethod
        //     def setInput(self,src:S)->"IFilter[S,D]":
        //         """新しいソースをインスタンスにセットします。
        //         関数が終了した時点で、インスタンスの状態は初期状態に戻ります。
        //         内部キューがある場合、既に読み出された入力側の内部キューと、未出力の出力側のキューがある場合は値を破棄します。 

        //         ・望ましい実装

        //             Noneがセットされた場合は、インスタンスは終端状態になり、ブロックしているget(s)/peek(s)関数があればStopInterationを発生させて終了するべきです。
        //         """
        //         pass
        IMPL SetInput(S src) ;
    }


    // class IEncoder(IFilter[S,D],Generic[S,D],ABC):
    //     """より物理値に近い情報を出力するストリームです。
    //     """
    public interface IEncoder<IMPL,S, D>:IFilter<IMPL,S, D> where IMPL : IEncoder<IMPL, S, D> { }


    // class IDecoder(IFilter[S,D],Generic[S,D],ABC):
    //     """より抽象値に近い情報を出力するストリームです。
    //     """
    public interface IDecoder<IMPL,S, D> :IFilter<IMPL,S, D> where IMPL : IDecoder<IMPL, S, D> { }

    // class IGenerator(IRoStream[T],Generic[T],ABC):
    //     """T型のストリームを生成するクラスです。
    //     ストリームクラスの亜種ですが、主にパラメーターから数列を生成するクラスの分類です。
    //     """
    //     pass
    public interface IGenerator<T>:IRoStream<T>{}



    // T=TypeVar('T')
    // class IConverter(Generic[T]):
    //     """ 同一型で値交換をする関数を定義します。
    //     """
    //     @abstractmethod
    //     def convert(self,src:T)->T:
    //         """ srcを同一型の異なる値に変換します。
    //         """




    // class IBytesProvider(ABC):
    //     """インスタンスの値をbytesにシリアライズできるクラスです。廃止予定。
    //     """
    //     def toBytes(self)->bytes:
    //         pass
}