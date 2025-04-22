using System;
using System.Diagnostics;
using jp.nyatla.kokolink.types;

namespace jp.nyatla.kokolink.utils.recoverable{

    // """ リカバリー可能なStopIterationです。
    //     イテレータチェーンのスループット調停などで、イテレータが再実行の機会を与えるために使います。
    //     この例外を受け取ったハンドラは、__next__メソッドを実行することで前回失敗した処理を再実行することができます。
    //     再実行を行わない場合はStopIterationと同じ振る舞いをしますが、異なる処理系ではセッションのファイナライズ処理が必要かもしれません。
    // """
    public class RecoverableStopIteration: PyStopIteration
    {
        public RecoverableStopIteration() : base() { }
        public RecoverableStopIteration(Exception innerException) : base(innerException) { }

    }
    abstract public class BasicRecoverableIterator<T>:BasicIterator<T>{
    }


    // """ リカバリー可能なメソッドが創出する例外です。
    //     送出元のメソッドはrecoverメソッドを呼び出すことで再実行できます。
    //     再実行した関数は、再びRecoverableExceptionをraiseする可能性もあります。

    //     再実行しない場合は、例外ハンドラでclose関数で再試行セッションを閉じてください。
    // """
    public class RecoverableException<T,V> : Exception, IDisposable where T :AsyncMethod<V>
    {
        private T? _recover_instance;
        public RecoverableException(T recover_instance)
        {
            this._recover_instance=recover_instance;
        }
        public T Detach()
        {
            if (this._recover_instance == null)
            {
                throw new Exception();

            }
            var r = this._recover_instance;
            this._recover_instance = null;
            return r;
        }

        // """ 関数を再試行します。再試行可能な状態で失敗した場合は、自分自信を返します。
        // """
        public void Dispose(){
            try
            {
                this.Close();
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }
        public void Close()
        {
            if (this._recover_instance != null)
            {
                this._recover_instance.Close();
            }
        }
    }


}
