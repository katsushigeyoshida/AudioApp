using System.Collections.Generic;
using System.IO;
using WpfLib;

namespace AudioApp
{
    public class AlbumInfoData
    {
        public string mFolder;                                     //  アルバムファイル保存フォルダ
        private string mFileName = "AlbumInfo.csv";                 //  アルバム情報ファイル名
        private Dictionary<string, string[]> mAlbumInfo = new Dictionary<string, string[]>();   //  アルバム情報リストデータ

        private string[] mDataTitle = {                             //  アルバム情報リストタイトル
            "Title", "Data", "Data1", "Data2", "Data3", "Data4", "Data5", "Data6", "Data7", "Data8", "Data9" };
        public string[] mDataCategory = {                          //  アルバム情報分類タイトル
            "Album", "Artist", "UserArtist", "Personal", "RecordDate", "OriginalMedia", "Label", "RecordNo",
            "Source", "SourceDate", "RefURL", "AtachedFile", "Comment", "Genre", "Style", "[Tune]" };

        private string[] mGenreTitle = {                            //  ユーザー設定用ジャンルデータ
            "Jazz", "R&B", "Pop", "Jpop", "Folk", "Rock", "Classic", "EasyListing", "Blues", "歌謡曲", "SoundTrack",
            "MechanicalSound", "AnimalSound", "Biophony(生物音)", "Geophony(自然音)", "AudioBook", "" };
        private string[] mStyleTitle = {                            //  ユーザー設定用スタイルデータ
            "Vocal", "Instrumental", "Fusion", "Free", "Modal", "HardBop", "Bop", "Contemporary", "Funk", "Soul", "AvantGarde",
            "PostBop", "Latin", "Afro", "Cape", "Bossa", "Rock", "Smooth", "Cool", "Swing", "Dixieland", "BigBand",
            "CrossOver","EasyListening","Classical", "Improvisation", "Folk", "NewAge", "A Cappella",
            "" };
        private string[] mMediaType = { "LP", "CD", "DVD", "TAPE", "MP3", "FLAC", "YouTube" }; //  メディアタイプデータ

        private YLib ylib = new YLib();

        public AlbumInfoData()
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="folder">フォルダ</param>
        public AlbumInfoData(string folder)
        {
            mFolder = folder;
            loadData();
        }

        /// <summary>
        /// 空白データを除いてデータの上書きをする
        /// ArtistListの更新の有無を判定するためUserArtistが変更された時trueを返す
        /// </summary>
        /// <param name="albumInfoData">上書きデータ</param>
        /// <returns>UserArtistが変更されるとtrue</returns>
        public bool mergeData(AlbumInfoData albumInfoData)
        {
            bool result = false;
            foreach (KeyValuePair<string, string[]> data in albumInfoData.mAlbumInfo) {
                if (1 < data.Value.Length && data.Value[1] != null && 0 < data.Value[1].Length &&
                    data.Key.ToString().CompareTo("Album") != 0 && data.Key.ToString().CompareTo("Artist") != 0) {
                    if (this.mAlbumInfo.ContainsKey(data.Key)) {
                        this.mAlbumInfo[data.Key] = new string[data.Value.Length];
                    } else {
                        this.mAlbumInfo.Add(data.Key, new string[data.Value.Length]);
                    }
                    for (int i = 0; i < data.Value.Length; i++) {
                        this.mAlbumInfo[data.Key][i] = data.Value[i];
                    }
                    if (data.Key.ToString().CompareTo("UserArtist") == 0)
                        result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// アルバム情報フォルダの取得
        /// </summary>
        /// <returns></returns>
        public string getFolder()
        {
            return mFolder;
        }

        /// <summary>
        /// ユーザー設定ジャンルデータの取得
        /// </summary>
        /// <returns></returns>
        public string[] getGenreData()
        {
            return mGenreTitle;
        }

        /// <summary>
        /// ユーザー設定スタイルデータの取得
        /// </summary>
        /// <returns></returns>
        public string[] getStyleData()
        {
            return mStyleTitle;
        }

        /// <summary>
        /// メディアデータの取得
        /// </summary>
        /// <returns></returns>
        public string[] getMediaType()
        {
            return mMediaType;
        }

        /// <summary>
        /// データサイズ
        /// </summary>
        /// <returns></returns>
        public int getDataSize()
        {
            return mDataTitle.Length;
        }

        /// <summary>
        /// データが取得できているか
        /// </summary>
        /// <returns></returns>
        public bool IsDataEnabled()
        {
            return 0 < mAlbumInfo.Count ? true : false;
        }

        /// <summary>
        /// アルバム情報の取得(単独)
        /// </summary>
        /// <param name="category">取得データの分類名</param>
        /// <returns></returns>
        public string getAlbumInfoData(string category)
        {
            if (mAlbumInfo != null) {
                if (mAlbumInfo.ContainsKey(category))
                    return mAlbumInfo[category][1];
            }
            return "";
        }

        /// <summary>
        /// アルバム情報の取得(複数データ)
        /// </summary>
        /// <param name="category">取得データの分類名</param>
        /// <returns></returns>
        public string[] getAlbumInfoDatas(string category)
        {
            if (mAlbumInfo != null) {
                if (mAlbumInfo.ContainsKey(category))
                    return mAlbumInfo[category];
            }
            return null;
        }

        /// <summary>
        /// アルバム情報データを設定(単独データ)
        /// </summary>
        /// <param name="category">分類名</param>
        /// <param name="data">データ</param>
        public void setAlbumInfoData(string category, string data)
        {
            if (mAlbumInfo == null)
                mAlbumInfo = new Dictionary<string, string[]>();
            string[] datas = new string[mDataTitle.Length];
            datas[0] = category;
            datas[1] = data;

            if (mAlbumInfo.ContainsKey(datas[0])) {
                mAlbumInfo[datas[0]] = datas;
            } else {
                mAlbumInfo.Add(datas[0], datas);
            }
        }

        /// <summary>
        /// アルバム情報データを設定(複数データ)
        /// </summary>
        /// <param name="category">分類名</param>
        /// <param name="data">配列データ</param>
        public void setAlbumInfoData(string category, string[] data)
        {
            if (mAlbumInfo == null)
                mAlbumInfo = new Dictionary<string, string[]>();
            string[] datas = new string[mDataTitle.Length];
            datas[0] = category;
            for (int i = 0; i < data.Length && i < mDataTitle.Length - 1; i++)
                datas[i + 1] = data[i];

            if (mAlbumInfo.ContainsKey(datas[0])) {
                mAlbumInfo[datas[0]] = datas;
            } else {
                mAlbumInfo.Add(datas[0], datas);
            }
        }

        /// <summary>
        /// ファイルからデータを取得
        /// </summary>
        public void loadData()
        {
            List<string[]> albumInfo = ylib.loadCsvData(Path.Combine(mFolder, mFileName), mDataTitle);
            if (albumInfo != null) {
                if (mAlbumInfo == null)
                    mAlbumInfo = new Dictionary<string, string[]>();
                foreach (string[] datas in albumInfo) {
                    if (!mAlbumInfo.ContainsKey(datas[0]))
                        mAlbumInfo.Add(datas[0], datas);
                }
            }
        }

        /// <summary>
        /// ファイルにデータを保存
        /// </summary>
        public void saveData()
        {
            List<string[]> albumInfo = new List<string[]>();
            albumInfo.Add(mDataTitle);
            foreach (string[] datas in mAlbumInfo.Values)
                albumInfo.Add(datas);
            ylib.saveCsvData(Path.Combine(mFolder, mFileName), albumInfo);
        }
    }
}
