using System.Diagnostics;
using jp.nyatla.kokolink.compatibility;


namespace jp.nyatla.kokolink.utils.wavefile.riffio
{
    public class Chunk
    {
        protected MemBuffer _buf = new MemBuffer();
        private int _name;
        private int _size;
        public Chunk(String name, int size)
        {
            this._name = this._buf.WriteBytes(name, 4);
            this._size = this._buf.WriteInt32LE(size);
        }
        public Chunk(IBinaryReader s)
        {
            this._name = this._buf.WriteBytes(s, 4);
            this._size = this._buf.WriteBytes(s, 4);
        }
        public String Name
        {
            get
            {
                return this._buf.AsStr(this._name, 4);
            }
        }
        public int Size
        {
            get
            {
                return this._buf.AsInt32LE(this._size);
            }
        }

        virtual public int Dump(IBinaryWriter writer)
        {
            return this._buf.Dump(writer);
        }
    }
    public class RawChunk : Chunk
    {
        private int _data;
        public RawChunk(String name, byte[] data) : base(name, data.Length)
        {
            this._data = this._buf.WriteBytes(data, data.Length % 2);
        }
        public RawChunk(String name, int size, IBinaryReader fp) : base(name, size)
        {
            this._data = this._buf.WriteBytes(fp, this.Size + this.Size % 2);
        }

        public List<byte> Data
        {
            get { return this._buf.GetRange(this._data, this._buf.Count - this._data); }
        }

    }



    public class FmtChunk : Chunk
    {
        public const int CHUNK_SIZE = 2 + 2 + 4 + 4 + 2 + 2;
        private const int WAVE_FORMAT_PCM = 0x0001;
        public FmtChunk(int size, IBinaryReader fp) : base("fmt ", size)

        {
            this._buf.WriteBytes(fp, 2);
            this._buf.WriteBytes(fp, 2);
            this._buf.WriteBytes(fp, 4);
            this._buf.WriteBytes(fp, 4);
            this._buf.WriteBytes(fp, 2);
            this._buf.WriteBytes(fp, 2);
        }
        public FmtChunk(int framerate, int samplewidth, int nchannels) : base("fmt ", FmtChunk.CHUNK_SIZE)
        {
            this._buf.WriteInt16LE(WAVE_FORMAT_PCM); //+0
            this._buf.WriteInt16LE(nchannels);//+2
            this._buf.WriteInt32LE(framerate);//+4
            this._buf.WriteInt32LE(nchannels * framerate * samplewidth);//+8
            this._buf.WriteInt16LE(nchannels * samplewidth);//+12
            this._buf.WriteInt16LE(samplewidth * 8);//+14
        }
        public int Nchannels
        {
            get
            {
                return this._buf.AsInt16LE(2 + 8);
            }
        }

        public int Framerate
        {
            get
            {
                return this._buf.AsInt32LE(4 + 8);
            }
        }

        public int Samplewidth
        {
            get
            {
                return this._buf.AsInt16LE(14 + 8);
            }
        }
    }

    public class DataChunk : RawChunk
    {

        public DataChunk(int size, IBinaryReader fp) : base("data", size, fp)

        {
        }
        public DataChunk(byte[] data) : base("data", data)
        {
        }
    }





public class ChunkHeader : Chunk
    {
        private int _form;
        public ChunkHeader(IBinaryReader fp) : base(fp)
        {
            this._form = this._buf.WriteBytes(fp, 4);
        }
        public ChunkHeader(String name, int size, IBinaryReader fp) : base(name, size)
        {
            this._form = this._buf.WriteBytes(fp, 4);
        }
        public ChunkHeader(String name, int size, String form) : base(name, size)
        {
            this._form = this._buf.WriteBytes(form, 4);
        }
        public String Form 
        {
            get
            {
                return this._buf.AsStr(this._form, 4);
            }
        }
    }

    public class RiffHeader : ChunkHeader
    {

        public RiffHeader(IBinaryReader fp) : base(fp)
        {
            if (this.Name.CompareTo("RIFF") != 0)
            {
                throw new IOException("Invalid RIFF header");
            }
        }
        public RiffHeader(int size, String form) : base("RIFF", size, form)
        {
        }
    }


    public class RawListChunk : ChunkHeader
    {
        private int _payload;
        private int _payload_len;
        public RawListChunk(int size, IBinaryReader fp) : base("LIST", size, fp)

        {
            this._payload_len = this.Size - 4;
            this._payload = this._buf.WriteBytes(fp, this._payload_len);
        }
        public RawListChunk(String form, Byte[] payload, int payload_len) : base("LIST", payload_len + 4, form)
        {
            this._payload_len = this.Size - 4;
            this._payload = this._buf.WriteBytes(payload);
        }
        public byte[] Payload
        {
            get
            {
                return this._buf.AsBytes(this._payload, this._payload_len);

            }
        }
    }

    public class WaveFile : RiffHeader
    {
        private List<Chunk> _chunks;
        public WaveFile(IBinaryReader fp) : base(fp)
        {
            this._chunks = new List<Chunk>();
            Debug.Assert(this.Form.CompareTo("WAVE") == 0);
            var chunk_size = this.Size;
            chunk_size -= 4;//fmt分
            while (chunk_size > 8)
            {
                String name = System.Text.ASCIIEncoding.ASCII.GetString(fp.ReadBytes(4));

                int size = fp.ReadInt32LE();
                chunk_size -= 8 + size + (size % 2);
                if (name.CompareTo("fmt ") == 0)
                {
                    this._chunks.Add(new FmtChunk(size, fp));
                }
                else if (name.CompareTo("data") == 0)
                {
                    this._chunks.Add(new DataChunk(size, fp));
                }
                else if (name.CompareTo("LIST") == 0)
                {
                    this._chunks.Add(new RawListChunk(size, fp));
                }
                else
                {
                    this._chunks.Add(new RawChunk(name, size, fp));
                }
            }
        }
        public static int ToSize(int frames_len, List<Chunk>? extchunks)
        {
            int s = 4;//form
            s = s + FmtChunk.CHUNK_SIZE + 8;
            s = s + frames_len + 8;
            if (extchunks != null)
            {
                for (int i = 0; i < extchunks.Count; i++)
                {
                    var cs = extchunks[i].Size;
                    s = s + cs + cs % 2 + 8;
                }
            }
            return s;
        }
        public WaveFile(int samplerate, int samplewidth, int nchannel, Byte[] frames, List<Chunk>? extchunks) : base(ToSize(frames.Length, extchunks), "WAVE")

        {
            this._chunks = new List<Chunk>();
            this._chunks.Add(new FmtChunk(samplerate, samplewidth, nchannel));
            this._chunks.Add(new DataChunk(frames));
            if (extchunks != null)
            {
                for (var i = 0; i < extchunks.Count; i++)
                {
                    this._chunks.Add(extchunks[i]);
                }
            }
        }




        public DataChunk? Data
        {
            get
            {
                var ret = this.GetChunk("data");
                if (ret == null)
                {
                    return null;
                }
                return (DataChunk)ret;

            }
        }
        public FmtChunk? Fmt
        {
            get
            {
                var ret = this.GetChunk("fmt ");
                if (ret == null)
                {
                    return null;
                }
                return (FmtChunk)ret;

            }

        }


        public Chunk? GetChunk(String name)
        {
            for (var i = 0; i < this._chunks.Count; i++)
            {
                if (this._chunks[i].Name.CompareTo(name) == 0)
                {
                    return this._chunks[i];
                }
            }
            return null;
        }
        override public int Dump(IBinaryWriter writer)
        {
            int ret = 0;
            ret += base.Dump(writer);
            for (var i = 0; i < this._chunks.Count; i++)
            {
                ret += this._chunks[i].Dump(writer);
            }
            return ret;
        }
    }

// if __name__ == '__main__':
//     with open("cat1.wav","rb") as f:
//         src=f.read()
//         r=WaveFile(BytesIO(src))
//         print(r)
//         dest=r.toChunkBytes()
//         print(src==dest)
//         for i in range(len(src)):
//             if src[i]!=dest[i]:
//                 print(i)
//         with open("ssss.wav","wb") as g:
//             g.write(dest)
//         n=WaveFile(44100,2,2,r.chunk(b"data").data)
//         with open("ssss2.wav","wb") as g:
//             g.write(n.toChunkBytes())

//         n=WaveFile(44100,2,2,r.chunk(b"data").data,[
//             InfoListChunk([
//                     (b"IARL",b"The location where the subject of the file is archived"),
//                     (b"IART",b"The artist of the original subject of the file"),
//                     (b"ICMS",b"The name of the person or organization that commissioned the original subject of the file"),
//                     (b"ICMT",b"General comments about the file or its subject"),
//                     (b"ICOP",b"Copyright information about the file (e.g., 'Copyright Some Company 2011')"),
//                     (b"ICRD",b"The date the subject of the file was created (creation date)"),
//                     (b"ICRP",b"Whether and how an image was cropped"),
//                     (b"IDIM",b"The dimensions of the original subject of the file"),
//                     (b"IDPI",b"Dots per inch settings used to digitize the file"),
//                     (b"IENG",b"The name of the engineer who worked on the file"),
//                     (b"IGNR",b"The genre of the subject"),
//                     (b"IKEY",b"A list of keywords for the file or its subject"),
//                     (b"ILGT",b"Lightness settings used to digitize the file"),
//                     (b"IMED",b"Medium for the original subject of the file"),
//                     (b"INAM",b"Title of the subject of the file (name)"),
//                     (b"IPLT",b"The number of colors in the color palette used to digitize the file"),
//                     (b"IPRD",b"Name of the title the subject was originally intended for"),
//                     (b"ISBJ",b"Description of the contents of the file (subject)"),
//                     (b"ISFT",b"Name of the software package used to create the file"),
//                     (b"ISRC",b"The name of the person or organization that supplied the original subject of the file"),
//                     (b"ISRF",b"The original form of the material that was digitized (source form)"),
//                     (b"ITCH",b"The name of the technician who digitized the subject file"),]
//                     )])
//         with open("ssss3.wav","wb") as g:
//             g.write(n.toChunkBytes())
//         with open("ssss3.wav","rb") as g:
//             r=WaveFile(g)
//             print(r)

//     with open("ssss2.wav","rb") as f:
//         src=f.read()
//         r=WaveFile(BytesIO(src))
//         print(r)

}