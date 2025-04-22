namespace jp.nyatla.tbaskmodem
{
	public class Version {
		public const String MODULE = "TBSKmodemForCShape";
		public const int MAJOR = 0;
		public const int MINER = 3;
		public const int PATCH = 7;
		public readonly String STRING = String.Format("{0}/{1}.{2}.{3}", Version.MODULE, Version.MAJOR, Version.MINER, Version.PATCH);
	}
}