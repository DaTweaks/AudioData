using System;
using System.Collections.Generic;
using jp.nyatla.kokolink.interfaces;
using jp.nyatla.kokolink.utils.recoverable;
using jp.nyatla.kokolink.types;
using System.Diagnostics;
using System.Text;
// T=TypeVar("T")

namespace jp.nyatla.kokolink.utils
{

	/**
     * 微妙に壊れた文字を逐次確定するUTF-8デコーダ
     * @author nyatla
     *
     */
	public class BrokenTextStreamDecoder
	{
		private Encoding _decoder;
		static public int MAX_CHARACTOR_BYTES = 8;
		private byte[] _a = new byte[MAX_CHARACTOR_BYTES];
		private int _len = 0;
		public BrokenTextStreamDecoder(String encoding)
		{
			this._decoder = System.Text.Encoding.GetEncoding(encoding, new EncoderExceptionFallback(), new DecoderExceptionFallback());
		}
		public Char? _Decode(int length)
		{
			try
			{
				char[] r = this._decoder.GetChars(this._a, 0, length);
				//Debug.Assert(r.Length == 1); 2文字返ってくるときは一体
				return r[0];
			}
			catch (DecoderFallbackException e)
			{
				return null;
			}
		}
		/**
		 * 解析キューの先頭からsizeバイト取り除きます。
		 */
		public void Shift(int size)
		{
			Debug.Assert(size > 0);
			if (this._len == 0)
			{
				return;
			}
			var a = this._a;
			for (var i = size; i < a.Length; i++)
			{
				a[i - size] = a[i];
			}
			this._len -= size;
			return;
		}
		/**
		 * 解析キューを変更せずに、先頭から1バイトを読み出します。
		 * @return
		 * 読みだした値。値がないときはnull
		 */
		public byte? PeekFront()
		{
			if (this._len > 0)
			{
				return this._a[0];
			}
			return null;
		}


		/**
		 * 先頭から文字コードを構成する文字数を返す。
		 * @return
		 * 0	解析キューが空<br/>
		 * -1	解析キューの文字コードは存在しない。<br/>
		 * n	文字コードの長さ	<br/>
		 */
		public int Test()
		{
			if (this._len == 0)
			{
				return 0;
			}
			for (var i = 1; i <= this._len; i++)
			{
				var r = this._Decode(i);
				if (r != null)
				{
					return i;
				}
			}
			return -1;
		}
		/**
		 * バッファに文字を追記してから、先頭から文字コードを構成する文字数を返す。
		 * @return
		 * -1	解析キューの文字コードは存在しない。<br/>
		 * n	文字コードの長さ	<br/>
		 */
		public int Test(byte d)
		{
			var a = this._a;
			//シフト
			if (this._len >= a.Length)
			{
				this.Shift(1);
			}
			//追記
			a[this._len] = d;
			this._len = this._len + 1;
			//テスト
			return this.Test();
		}

		/**
		 * 新規入力をともなうアップデート.
		 * キューがいっぱいの場合は、先頭1バイトを"?"と仮定して出力します。
		 * @param d
		 * @return
		 * char	変換した文字<br/>
		 * null	変換できない<br/>
		 */
		public Char? Update(byte d)
		{
			int len = this.Test(d);
			switch (len)
			{
				case -1:
					if (this._len == this._a.Length)
					{
						return '?';//キューがいっぱいならシフトが起きてる。
					}
					else
					{
						return null;
					}
				case 0:
					return null;
				default:
					Char? r = this._Decode(len);
					this.Shift(len);
					return r;
			}
		}
		/**
		 * 新規入力のないアップデート。キューの内容が変換できない場合は'?'を返す。
		 * @return
		 * null	解析キューにデータが無い<br/>
		 * char	変換した文字コード<br/>
		 */
		public Char? Update()
		{
			int len = this.Test();
			switch (len)
			{
				case -1:
					//文字コードが見つからない
					this.Shift(1);
					return '?';
				case 0:
					return null;
				default:
					Char? r = this._Decode(len);
					this.Shift(len);
					return r;
			}
		}
		/**
		 * 解析バッファのデータサイズを返します。
		 * @return
		 */
		public int HoldLen()
		{
			return this._len;
		}
		public bool IsBufferFull()
		{
			return this._len == this._a.Length;
		}
	}
}