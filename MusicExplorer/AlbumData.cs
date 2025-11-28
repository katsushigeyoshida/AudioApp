using System.IO;
using WpfLib;

namespace AudioApp
{

    /// <summary>
    /// アーティストデータクラス
    /// </summary>
    public class ArtistData
    {
        public string Artist { get; set; }          //  リーダーアーティスト
        public string AlbumArtist { set; get; }     //  アルバムアーティスト
        public string UserArtist { set; get; }      //  ユーザ設定アーティスト
        public int AlbumCount { get; set; }         //  登録アルバム数

        /// <summary>
        /// アーティストデータの設定
        /// </summary>
        /// <param name="artist"></param>
        public ArtistData(string artist, string albumArtist, string userArtist)
        {
            Artist = artist == null ? "" : artist;
            AlbumArtist = albumArtist == null ? "" : albumArtist;
            UserArtist = userArtist == null ? (artist == null ? (albumArtist == null ? "" : albumArtist) : artist) : userArtist;
            AlbumCount = 1;
        }
    }

    /// <summary>
    /// アルバムデータクラス
    /// </summary>
    public class AlbumData
    {
        //  アルバム情報
        public string Album { get; set; }           //  アルバム名
        public string Artist { get; set; }          //  リーダーアーティスト
        public string AlbumArtist { set; get; }     //  アルバムアーティスト
        public string Year { get; set; }            //  年
        public string Genre { get; set; }           //  ジャンル
        public string Folder { get; set; }          //  フォルダ名
        public int TrackCount { get; set; }         //  曲数(トラック数)
        public long TotalTime { get; set; }         //  全演奏時間(秒)
        public string TotalTimeString { get; set; } //  全演奏時間(hh:mm:ss)
        public long AlbumSize { get; set; }          //  アルバムのデータサイズ
        public string FormatExt { get; set; }       //  ファイルの拡張子8フォーマット)
        //  手入力情報またはファイルから取り込む
        public string UserArtist { get; set; }      //  ユーザー設定アーティスト
        public string UserGenre { get; set; }       //  ユーザー設定ジャンル
        public string UserStyle { get; set; }       //  ユーザー設定サブジャンル(スタイル)
        public string OriginalMedia { get; set; }   //  元のメディア
        public string Label { get; set; }           //  レーベル
        public string SourceDate { get; set; }      //  入手日
        public string Source { get; set; }          //  入手元
        public long UnDisp {  get; set; }           //  非表示( < 0)

        private readonly int mDataCount = 11 + 8;   //  曲情報 + ユーザー設定情報

        private readonly string[] mTitle = {
            "Album","Artist","AlbumArtist","Year","Genre","Folder","TrackCount",
            "TotalTime","TotalTimeString", "FormatExtention",
            "UserArtist", "UserGenre", "UserStyle", "OriginalMedia",
            "Label", "SourceDate", "Source", "AlbumUnDisp","AlbumSize",
        };

        YLib ylib = new YLib();

        public AlbumData()
        {

        }

        /// <summary>
        /// アルバムデータの設定
        /// </summary>
        /// <param name="album">アルバム名</param>
        /// <param name="artist">アーティスト名</param>
        /// <param name="year">録音年</param>
        /// <param name="ganre">ジャンル</param>
        /// <param name="folder">フォルダ</param>
        /// <param name="playLength">演奏時間(sec)</param>
        /// <param name="ext">拡張子(.は含まない)</param>
        public AlbumData(string album, string artist,　string albumArtist, string year, string ganre, string folder, long playLength, string ext)
        {
            Album       = album == null ? "" : album;
            Artist      = artist == null ? "" : artist;
            AlbumArtist = albumArtist == null ? "" : albumArtist;
            Year        = year == null ? "" : year;
            Genre       = ganre == null ? "" : ganre;
            Folder      = folder == null ? "" : folder;
            TrackCount  = 1;
            TotalTime   = playLength;
            TotalTimeString = ylib.second2String(TotalTime, false);
            FormatExt   = ext;

            updateAlbumInfoData();
        }

        /// <summary>
        /// アルバムデータの設定
        /// </summary>
        /// <param name="musicFileData">アルバムデータ</param>
        public AlbumData(MusicFileData musicFileData)
        {
            Album       = musicFileData.Album;
            Artist      = musicFileData.Artist;
            AlbumArtist = musicFileData.AlbumArtist;
            Year        = musicFileData.Year;
            Genre       = musicFileData.Genre;
            Folder      = musicFileData.Folder;
            TrackCount  = 1;
            TotalTime   = musicFileData.PlayLength;
            TotalTimeString = ylib.second2String(TotalTime, false);
            FormatExt   = 0 < musicFileData.FileName.Length ? Path.GetExtension(musicFileData.FileName).Substring(1).ToUpper() : "";
            AlbumSize   = musicFileData.Size;

            updateAlbumInfoData();
        }

        /// <summary>
        /// 配列データからAlbumDataを作成
        /// </summary>
        /// <param name="data"></param>
        public AlbumData(string[] data)
        {
            if (data.Length < mDataCount)
                return;
            Album           = data[0];
            Artist          = data[1];
            AlbumArtist     = data[2];
            Year            = data[3];
            Genre           = data[4];
            Folder          = data[5];
            TrackCount = (int)ylib.string2double(data[6]);
            TotalTime = (long)ylib.string2double(data[7]);
            TotalTimeString = data[8];
            FormatExt       = data[9];

            UserArtist      = data[10];
            UserGenre       = data[11];
            UserStyle       = data[12];
            OriginalMedia   = data[13];
            Label           = data[14];
            SourceDate      = data[15];
            Source          = data[16];
            UnDisp = ylib.string2long(data[17]);
            AlbumSize = (long)ylib.string2double(data[18]);
        }

        /// <summary>
        /// アルバム情報データのみを更新する
        /// </summary>
        public void updateAlbumInfoData()
        {
            AlbumInfoData albumInfo = new AlbumInfoData(Folder);
            if (albumInfo.IsDataEnabled()) {
                UserArtist = albumInfo.getAlbumInfoData("UserArtist");
                if (UserArtist.Length <= 0)
                    UserArtist = 0 < AlbumArtist.Length ? AlbumArtist : Artist;
                UserGenre = albumInfo.getAlbumInfoData("Genre");
                UserStyle = albumInfo.getAlbumInfoData("Style");
                OriginalMedia = albumInfo.getAlbumInfoData("OriginalMedia");
                Label = albumInfo.getAlbumInfoData("Label");
                SourceDate = albumInfo.getAlbumInfoData("SourceDate");
                Source = albumInfo.getAlbumInfoData("Source");
                UnDisp = ylib.string2long(albumInfo.getAlbumInfoData("AlbumUnDisp"));
            } else {
                UserArtist = 0 < AlbumArtist.Length ? AlbumArtist : Artist;
                UserGenre = 0 < Genre.Length ? Genre : "";
                UserStyle = "";
                OriginalMedia = "";
                Label = "";
                SourceDate = "";
                Source = "";
                UnDisp = 0;
            }
        }

        /// <summary>
        /// 演奏時間(sec)、ファイルサイズ(byte)の累積
        /// </summary>
        /// <param name="musicFileData">音楽データ</param>
        public void addCount(MusicFileData musicFileData)
        {
            TrackCount++;
            TotalTime += musicFileData.PlayLength;
            TotalTimeString = ylib.second2String(TotalTime, false);
            AlbumSize += musicFileData.Size;
        }

        /// <summary>
        /// アルバムデータの種類名(タイトル名)
        /// </summary>
        /// <returns>タイトル名のの配列</returns>
        public string[] getTitle()
        {
            return mTitle;
        }

        /// <summary>
        /// アルバムデータを配列で取得
        /// </summary>
        /// <returns></returns>
        public string[] toArray()
        {
            string[] data = new string[mDataCount];
            data[0] = Album;
            data[1] = Artist;
            data[2] = AlbumArtist;
            data[3] = Year;
            data[4] = Genre;
            data[5] = Folder;
            data[6] = TrackCount.ToString();
            data[7] = TotalTime.ToString();
            data[8] = TotalTimeString;
            data[9] = FormatExt;

            data[10] = UserArtist;
            data[11] = UserGenre;
            data[12] = UserStyle;
            data[13] = OriginalMedia;
            data[14] = Label;
            data[15] = SourceDate;
            data[16] = Source;
            data[17] = UnDisp.ToString();
            data[18] = AlbumSize.ToString();
            return data;
        }
    }
}

