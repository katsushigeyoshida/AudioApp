using AudioLib;
using System;
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// 音楽ファイルデータ
    /// </summary>
    public class MusicFileData : FileData
    {
        //  タグ情報 (ID3V1/V2TAG)
        public int TitleNo { get; set; }            //  曲のNo
        public string Title { get; set; }           //  曲名タイトル
        public string Artist { get; set; }          //  リーダーアーティスト/グループ [TP1/TPE1]
        public string AlbumArtist { get; set; }     //  バンド/グループ[TP2/TPE2]
        public string Album { get; set; }           //  アルバム名
        public string Year { get; set; }            //  年
        public string Comment { get; set; }         //  コメント
        public string Genre { get; set; }           //  ジャンル
        public string Track { get; set; }           //  トラック
        public string DiscNum { get; set; }         //  ディスクNo (Part of aset)
        public int PictureCount { get; set; }       //  添付画像数
        public string Id3Tag { get; set; }          //  タグバージョン
        public long TagSize { get; set; }           //  タグのサイズ
        //  音情報
        public long PlayLength { get; set; }        //  演奏時間(秒)
        public string PlayLengthString { get; set; }    //  演奏時間(hh:mm:ss)
        public int SampleRate { get; set; }         //  サンプル周波数(Hz)
        public int SampleBits { get; set; }         //  量子ビット(bits)
        public int SampleBitsRate { get; set; }     //  ビットレート(bps)
        public int Channels { get; set; }           //  チャンネル数
        public double Volume { get; set; }          //  ボリューム

        private const int mDataCount = 4 + 12 + 7;  //  = 20 (ファイルデータ数 + タグデータ数　+ 音情報数)

        public bool UpdateFlag { get; set; }        //  データ更フラグ新

        //  配列に変換したときの配列のタイトル名
        private static readonly string[] mTitle = {
            "TitleNo",    "FileName", "Folder",     "Date",    "Size",       "Title",
            "Artist" ,    "Album",    "AlbumArtist","Year",    "Comment",    "Genre",
            "Track",      "Picture",  "ID3Tag",     "TagSize", "PlayLength", "SampleRate",
            "SampleBits", "SampleBitsRate", "Channels", "PlayLengthString", "Volume",
        };

        private bool mWaveDataInfo = true;          //  NAudioでWaveFormatを取得可

        YLib ylib = new YLib();

        public MusicFileData()
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fileName">音楽データファイル名</param>
        /// <param name="folder">フォルダ</param>
        /// <param name="date">ファイル日付</param>
        /// <param name="size">ファイルサイズ</param>
        public MusicFileData(string fileName, string folder, DateTime date, long size)
        {
            TitleNo = trackNo2int(fileName);
            FileName = fileName;
            Folder = folder;
            Date = date;
            Size = size;
            //  タグ情報
            Title = "";
            Artist = "";
            Album = "";
            AlbumArtist = "";
            Year = "";
            Comment = "";
            Genre = "";
            Track = "";
            PictureCount = 0;
            Id3Tag = "";
            TagSize = 0;
            //  音情報
            PlayLength = 0;
            PlayLengthString = ylib.second2String(PlayLength, false);
            SampleRate = 0;
            SampleBits = 0;
            SampleBitsRate = 0;
            Channels = 0;
            Volume = 0.8;
            UpdateFlag = false;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="musicFileData">音楽データ</param>
        public MusicFileData(MusicFileData musicFileData)
        {
            //  ファイル情報
            TitleNo = musicFileData.TitleNo;
            FileName = musicFileData.FileName;
            Folder = musicFileData.Folder;
            Date = musicFileData.Date;
            Size = musicFileData.Size;
            //  タグ情報
            Title = musicFileData.Title;
            Artist = musicFileData.Artist;
            Album = musicFileData.Album;
            AlbumArtist = musicFileData.AlbumArtist;
            Year = musicFileData.Year;
            Comment = musicFileData.Comment;
            Genre = musicFileData.Genre;
            Track = musicFileData.Track;
            PictureCount = musicFileData.PictureCount;
            Id3Tag = musicFileData.Id3Tag;
            TagSize = musicFileData.TagSize;
            //  音情報
            PlayLength = musicFileData.PlayLength;
            PlayLengthString = ylib.second2String(PlayLength, false);
            SampleRate = musicFileData.SampleRate;
            SampleBits = musicFileData.SampleBits;
            SampleBitsRate = musicFileData.SampleBitsRate;
            Channels = musicFileData.Channels;
            Volume = musicFileData.Volume;
            UpdateFlag = false;
        }

        /// <summary>
        /// 配列のタイトルの取得
        /// </summary>
        /// <returns></returns>
        public string[] getTitle()
        {
            return mTitle;
        }

        /// <summary>
        /// 配列からデータを設定する
        /// </summary>
        /// <param name="data">配列データ</param>
        public MusicFileData(string[] data)
        {
            if (data.Length < mDataCount)
                return;
            //  ファイル情報
            setTileNo(data[0]);
            FileName = data[1];
            Folder = data[2];
            setDate(data[3]);
            setSize(data[4]);
            //  タグ情報
            Title = data[5];
            Artist = data[6];
            Album = data[7];
            AlbumArtist = data[8];
            Year = data[9];
            Comment = data[10];
            Genre = data[11];
            Track = data[12];
            setPictureCount(data[13]);
            Id3Tag = data[14];
            setTagSize(data[15]);
            //  音情報
            setPlayLength(data[16]);
            setSampleRate(data[17]);
            setSampleBits(data[18]);
            setSampleBitsRate(data[19]);
            setChannels(data[20]);
            PlayLengthString = data[21];
            setVolume(data[22]);
        }

        /// <summary>
        /// データの配列化
        /// </summary>
        /// <returns></returns>
        public string[] toArray()
        {
            string[] data = new string[mDataCount];
            data[0] = getTitleNo();
            data[1] = FileName;
            data[2] = Folder;
            data[3] = getDate();
            data[4] = getSize();
            data[5] = Title;
            data[6] = Artist;
            data[7] = Album;
            data[8] = AlbumArtist;
            data[9] = Year;
            data[10] = Comment;
            data[11] = Genre;
            data[12] = Track;
            data[13] = getPictureCount();
            data[14] = Id3Tag;
            data[15] = getTagSize();
            data[16] = getPlayLength();
            data[17] = getSampleRate();
            data[18] = getSampleBits();
            data[19] = getSampleBitsRate();
            data[20] = getChannels();
            data[21] = PlayLengthString;
            data[22] = getVolume();

            return data;
        }

        /// <summary>
        /// タグデータの主要項目の取得
        /// </summary>
        public void AddTagData()
        {
            FileTagReader fileTagReader = new FileTagReader(getPath());
            Id3Tag = fileTagReader.getVersion();
            TagSize = fileTagReader.getTagSize();
            DiscNum = "";
            //  キーワードで取得
            Title   = fileTagReader.getTagDataConvetKey("TITLE");
            Artist  = fileTagReader.getTagDataConvetKey("ARTIST");
            AlbumArtist = fileTagReader.getTagDataConvetKey("ALBUMARTIST");
            Album   = fileTagReader.getTagDataConvetKey("ALBUM");
            Year    = fileTagReader.getTagDataConvetKey("YEAR");
            Comment = fileTagReader.getTagDataConvetKey("COMMENT");
            Track   = fileTagReader.getTagDataConvetKey("TRACKNUMBER");
            DiscNum = fileTagReader.getTagDataConvetKey("DISCNUMBER");  //  FLAC,ASFにはない
            Genre   = fileTagReader.getTagDataConvetKey("GENRE");
            //  コメントのコントロールコード除去
            Comment = Comment.Replace("\r\n", " ");
            Comment = Comment.Replace("\n", " ");
            Comment = Comment.Replace("\r", " ");

            //  演奏時間はタグにない場合が多いので使わない
            //PlayLength = fileTagReader.getPlayLength() / 1000;        //  演奏時間 msc → sec
            //PlayLengthString = ylib.second2String(PlayLength, false);
            int n = trackNo2int(Track, DiscNum);                        //  トラック番号
            if (0 < n)
                TitleNo = n;                                            //  タイトルNo(トラックNoがない時はファイル名の先頭番号で代用)
            PictureCount = fileTagReader.getImageDataCount();           //  画像データ数

            //  WMAはMediaPlayerでCODEC情報を取得できないのでタグデータから補完する
            if (mWaveDataInfo == false && Id3Tag.CompareTo("ASF") == 0) {
                PlayLength = fileTagReader.getPlaylength() / 1000;      //  seconds
                SampleRate = fileTagReader.getSampleRate();             //  Hz
                SampleBits = fileTagReader.getBitsPerSample();          //  bits
                SampleBitsRate = fileTagReader.getSampleBitsPerRate();  //  bps
                Channels = fileTagReader.getChannels();                 //  チャンネル数
                PlayLengthString = ylib.second2String(PlayLength, false);
            }
        }

        /// <summary>
        /// タグ情報だけをコピー
        /// </summary>
        /// <param name="musicFileData"></param>
        public void setTagData(MusicFileData musicFileData)
        {
            Title = musicFileData.Title;
            Artist = musicFileData.Artist;
            Album = musicFileData.Album;
            AlbumArtist = musicFileData.AlbumArtist;
            Year = musicFileData.Year;
            Comment = musicFileData.Comment;
            Genre = musicFileData.Genre;
            Track = musicFileData.Track;
            PictureCount = musicFileData.PictureCount;
            Id3Tag = musicFileData.Id3Tag;
            TagSize = musicFileData.TagSize;
        }

        /// <summary>
        /// MP3データ(TAGデータ以外)から演奏時間、サンプル周波数,量子ビット,チャンネル数の取得
        /// </summary>
        public void AddWaveFormat()
        {
            AudioLib.AudioLib audioLib = new AudioLib.AudioLib();
            if (audioLib.Open(getPath())) {
                PlayLength = (long)audioLib.mTotalTime.TotalSeconds;        //  seconds
                SampleRate = audioLib.getSampleRate();                      //  Hz
                SampleBits = audioLib.getBitsPerSample();                   //  bits
                SampleBitsRate = audioLib.getAverageBytesPerSecond();       //  bps
                Channels = audioLib.getChannels();                          //  チャンネル数
                PlayLengthString = ylib.second2String(PlayLength, false);   //  再生時間文字列
                mWaveDataInfo = true;
            } else {
                mWaveDataInfo = false;
            }
            audioLib.dispose();
        }

        /// <summary>
        /// Wave情報だけをコピー
        /// </summary>
        /// <param name="musicFileData"></param>
        public void setWaveFormat(MusicFileData musicFileData)
        {
            PlayLength = musicFileData.PlayLength;
            PlayLengthString = ylib.second2String(PlayLength, false);
            SampleRate = musicFileData.SampleRate;
            SampleBits = musicFileData.SampleBits;
            SampleBitsRate = musicFileData.SampleBitsRate;
            Channels = musicFileData.Channels;
        }


        /// <summary>
        /// データの数の取得
        /// </summary>
        /// <returns></returns>
        public int DataCount()
        {
            return mDataCount;
        }

        /// <summary>
        /// 曲タイトルのNo(Track No)の取得
        /// </summary>
        /// <returns></returns>
        public string getTitleNo()
        {
            return TitleNo.ToString();
        }

        /// <summary>
        /// タイトルNoの設定(タイトルNo文字列を数値に変換))
        /// </summary>
        /// <param name="titleNo"></param>
        public void setTileNo(string titleNo)
        {
            int no;
            TitleNo = int.TryParse(titleNo, out no) ? no : 0;
        }

        /// <summary>
        /// タグデータのサイズの取得
        /// </summary>
        /// <returns></returns>
        public string getTagSize()
        {
            return TagSize.ToString();
        }

        /// <summary>
        /// タグデータサイズの設定
        /// </summary>
        /// <param name="tagSize"></param>
        public void setTagSize(string tagSize)
        {
            long size;
            TagSize = long.TryParse(tagSize, out size) ? size : 0;
        }

        /// <summary>
        /// 添付画像数を文字列で取得
        /// </summary>
        /// <returns></returns>
        public string getPictureCount()
        {
            return PictureCount.ToString();
        }

        /// <summary>
        /// 添付画像数の設定
        /// </summary>
        /// <param name="pictureCount"></param>
        public void setPictureCount(string pictureCount)
        {
            int count;
            PictureCount = int.TryParse(pictureCount, out count) ? count : 0;
        }

        /// <summary>
        /// 演奏時間の取得
        /// </summary>
        /// <returns>(秒の文字列)</returns>
        public string getPlayLength()
        {
            return PlayLength.ToString();
        }

        /// <summary>
        /// 演奏時間の設定
        /// </summary>
        /// <param name="playLength">(秒の文字列)</param>
        public void setPlayLength(string playLength)
        {
            long length;
            PlayLength = long.TryParse(playLength, out length) ? length : 0;
        }

        /// <summary>
        /// サンプル周波数(Hz)を文字列で取得
        /// </summary>
        /// <returns></returns>
        public string getSampleRate()
        {
            return SampleRate.ToString();
        }

        /// <summary>
        /// サンプル周波数の設定
        /// </summary>
        /// <param name="sampleRate"></param>
        public void setSampleRate(string sampleRate)
        {
            int rate;
            SampleRate = int.TryParse(sampleRate, out rate) ? rate : 0;
        }

        /// <summary>
        /// 量子ビットの取得
        /// </summary>
        /// <returns></returns>
        public string getSampleBits()
        {
            return SampleBits.ToString();
        }

        /// <summary>
        /// 量子ビットの設定
        /// </summary>
        /// <param name="sampleBits"></param>
        public void setSampleBits(string sampleBits)
        {
            int bits;
            SampleBits = int.TryParse(sampleBits, out bits) ? bits : 0;
        }

        /// <summary>
        /// ビットレートの取得
        /// </summary>
        /// <returns></returns>
        public string getSampleBitsRate()
        {
            return SampleBitsRate.ToString();
        }

        /// <summary>
        /// ビットレートの設定
        /// </summary>
        /// <param name="sampleBitsRate"></param>
        public void setSampleBitsRate(string sampleBitsRate)
        {
            int bits;
            SampleBitsRate = int.TryParse(sampleBitsRate, out bits) ? bits : 0;
        }

        /// <summary>
        /// チャンネル数の取得
        /// </summary>
        /// <returns></returns>
        public string getChannels()
        {
            return Channels.ToString();
        }

        /// <summary>
        /// チャンネル数を設定
        /// </summary>
        /// <param name="channels"></param>
        public void setChannels(string channels)
        {
            int n;
            Channels = int.TryParse(channels, out n) ? n : 0;
        }

        /// <summary>
        /// ボリュームの取得
        /// </summary>
        /// <returns></returns>
        public string getVolume()
        {
            return Volume.ToString();
        }

        /// <summary>
        /// ボリュームの設定
        /// </summary>
        /// <param name="vol"></param>
        public void setVolume(string vol)
        {
            double v;
            Volume = double.TryParse(vol, out v) ? v : 0.8;
        }

        /// <summary>
        /// ファイル名またはトラックNoの文字列からタイトルNoを求める
        /// Discが複数枚にわたるとき 1-xx, 2-xxとなる場合,'-'を削除して
        /// 1xx,2xxなどとする
        /// </summary>
        /// <param name="trackNo"></param>
        /// <returns></returns>
        private int trackNo2int(string trackNo)
        {
            trackNo = trackNo.Replace("-", "");
            return (int)ylib.string2double(trackNo);
        }

        /// <summary>
        /// TrackNoとDiscNo(Part of aset)からTrackNoを求める
        /// DiscNo(1/2) + TrackNo(2/8) = xxx (102等)
        /// </summary>
        /// <param name="trackNo"></param>
        /// <param name="discNum"></param>
        /// <returns></returns>
        private int trackNo2int(string trackNo, string discNum)
        {
            if (0 < trackNo.IndexOf("-")) {
                trackNo = trackNo.Replace("-", "");
                return (int)ylib.string2double(trackNo);
            } else {
                if (0 < discNum.Length) {
                    if (0 < discNum.IndexOf("/")) {
                        int discSize = (int)ylib.string2double(discNum.Substring(discNum.IndexOf("/") + 1));
                        if (1 < discSize) {
                            int tracNum = (int)ylib.string2double(trackNo);
                            return (int)ylib.string2double(discNum.Substring(0, discNum.IndexOf("/")) + string.Format("{0:00}", tracNum));
                        }
                    } else {
                        int tracNum = (int)ylib.string2double(trackNo);
                        return (int)ylib.string2double(ylib.intParse(discNum).ToString() + string.Format("{0:00}", tracNum));
                    }
                }
            }
            trackNo = trackNo.Replace("-", "");
            return (int)ylib.string2double(trackNo);
        }
    }
}
