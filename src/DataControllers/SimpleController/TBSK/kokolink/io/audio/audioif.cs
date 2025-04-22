using jp.nyatla.kokolink.types;


namespace jp.nyatla.kokolink.io.audioif
{


    //"""Audioメディアプレイヤーの操作インタフェイスを定義します。
    //"""
    public interface IAudioPlayer : IDisposable
    {
        //"""先頭から再生します。再生中の場合は失敗します。
        //"""
        void Play();
        //"""再生を停止します。既に停止している場合は無視します。
        //"""
        void Stop();
        //"""再生が終わるまでブロックします。既に停止中なら何もしません。
        //"""
        void Wait();

        //"""セッションを閉じます。
        //"""
        void Close();
    }

    //"""Audioデバイスからサンプリング値を取り込むイテレータです。
    //サンプリング値は[-1, 1] 範囲のfloatです。
    //"""
    public interface IAudioInputIterator : IDisposable, IPyIterator<double>
    {
        //"""データの取り込みを開始します。
        //取り込みキューは初期化されます。
        //"""
        void Start();
        //"""データの取り込みを停止します。
        //待機している__next__は直ちに例外を発生させて停止します。
        //Startで再開できます。
        //"""
        void Stop();

        //"""デバイスの利用を終了します。以降、デバイスの再利用はできません。
        //"""
        void Close();
    }
}