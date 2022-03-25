using System;
using System.Collections.Generic;
using System.Linq;
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// アーティスト情報を管理する
    /// </summary>
    public class ArtistInfoList
    {
        private Dictionary<string, ArtistInfoData> mArtistInfoList = new Dictionary<string, ArtistInfoData>();
        private string mFilePath = "";      //  アーティスト情報リストの保存ファイルパス
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ArtistInfoList()
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">ファイル保存パス</param>
        public ArtistInfoList(string path)
        {
            mFilePath = path;
            loadData();         //  リストデータの読込
        }

        /// <summary>
        /// データの追加
        /// </summary>
        /// <param name="data">アーティスト情報データ</param>
        public void add(ArtistInfoData data)
        {
            mArtistInfoList.Add(data.mArtist, data);
        }

        /// <summary>
        /// アーティスト情報の取得
        /// </summary>
        /// <param name="artist">アーティスト名</param>
        /// <returns>アーティスト情報</returns>
        public ArtistInfoData getData(string artist)
        {
            if (mArtistInfoList.ContainsKey(artist))
                return mArtistInfoList[artist];
            else
                return null;
        }

        /// <summary>
        /// アーティスト名の検索
        /// 
        /// </summary>
        /// <param name="searchName"></param>
        /// <returns></returns>
        public List<ArtistInfoData> searchArtist(string searchName)
        {
            string upSearchName = searchName.ToUpper();
            List<ArtistInfoData> artistInfoDatas = new List<ArtistInfoData>();
            foreach (ArtistInfoData artistInfo in mArtistInfoList.Values) {
                string upArtist = artistInfo.mArtist.ToUpper();
                string upRealName = artistInfo.mRealName.ToUpper();
                string upArtistName = artistInfo.mArtistName.ToUpper();
                if (0 <= upArtist.IndexOf(upSearchName) || 0 <= upRealName.IndexOf(upSearchName) ||
                    0 <= upArtistName.IndexOf(upSearchName) || 0 <= artistInfo.mArtistJpName.IndexOf(searchName)) {
                    artistInfoDatas.Add(artistInfo);
                }
            }
            return artistInfoDatas;
        }

        /// <summary>
        /// ファイルデータを読み込む
        /// </summary>
        public void loadData()
        {
            mArtistInfoList.Clear();
            loadData(mFilePath);
        }

        /// <summary>
        /// ファイルを指定してからデータを取得
        /// </summary>
        /// <param name="filePath"></param>
        public void loadData(string filePath)
        {
            List<string[]> artistInfo = ylib.loadCsvData(filePath, ArtistInfoData.mDataFormat);
            if (artistInfo != null) {
                if (mArtistInfoList == null)
                    mArtistInfoList = new Dictionary<string, ArtistInfoData>();
                foreach (string[] datas in artistInfo) {
                    if (!mArtistInfoList.ContainsKey(datas[0]))
                        mArtistInfoList.Add(datas[0], new ArtistInfoData(datas));
                }
            }
        }

        /// <summary>
        /// ファイルにデータを保存
        /// </summary>
        public void saveData()
        {
            saveData(mFilePath);
        }

        /// <summary>
        /// ファイルにデータを保存
        /// </summary>
        /// <param name="filePath">保存ファイル名</param>
        public void saveData(string filePath)
        {
            List<string[]> artistInfo = new List<string[]>();
            foreach (ArtistInfoData datas in mArtistInfoList.Values)
                artistInfo.Add(datas.getStringData());
            ylib.saveCsvData(filePath, ArtistInfoData.mDataFormat, artistInfo);
        }

    }

    /// <summary>
    /// アーティスト情報データ
    /// </summary>
    public class ArtistInfoData
    {
        public string mArtist = "";         //  表記アーティスト名
        public string mRealName = "";       //  本名(空白の時は表記名が本名とする)
        public ARTISTTYPE mArtistType = ARTISTTYPE.Personal;    //  種別(個人/グループ)
        public string mArtistName = "";     //  英名アーティスト名(本名)/別名
        public string mArtistJpName = "";   //  日本語表記(外国人のカタカナ表記)
        public string mBorn = "";           //  生年月日
        public string mDied = "";           //  没年月日
        public string mHome = "";           //  出身地
        public string mGenre = "";          //  ジャンル
        public string mStyle = "";          //  スタイル
        public string mOccupatin = "";      //  職業
        public string mInstruments = "";    //  楽器
        public string mLabel = "";          //  レーベル
        public string mGroup = "";          //  Personal:所属グループ,Group:メンバー
        public string mLinkks = "";         //  参照URL,ファイル
        public string mComment = "";        //  コメント

        public enum ARTISTTYPE { Personal, Group };
        public static string[] mDataFormat = {
            "Artist",　"RealName", "Type", "ArtistName", "ArtistJpName", "Born", "Died", "Home", "Genre", "Style",
            "Occupatin", "Instruments", "Label", "Group/Member", "Links", "Comment",
        };

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ArtistInfoData()
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data">文字列配列データ</param>
        public ArtistInfoData(string[] data)
        {
            setStringData(data);
        }

        /// <summary>
        /// 文字列配列よりアーティスト情報データを設定
        /// </summary>
        /// <param name="data">アーティスト情報の文字列配列</param>
        public void setStringData(string[] data)
        {
            mArtist = data[0];
            mRealName = data[1];
            setArtistType(data[2]);
            mArtistName = data[3];
            mArtistJpName = data[4];
            mBorn = data[5];
            mDied = data[6];
            mHome = data[7];
            mGenre = data[8];
            mStyle = data[9];
            mOccupatin = data[10];
            mInstruments = data[11];
            mLabel = data[12];
            mGroup = data[13];
            mLinkks = data[14];
            mComment = data[15];
        }

        /// <summary>
        /// 文字列配列でアーティスト情報データを取得
        /// </summary>
        /// <returns>アーティスト情報の文字列配列</returns>
        public string[] getStringData()
        {
            string[] data = new string[mDataFormat.Length];
            data[0] = mArtist;
            data[1] = mRealName;
            data[2] = mArtistType.ToString();
            data[3] = mArtistName;
            data[4] = mArtistJpName;
            data[5] = mBorn;
            data[6] = mDied;
            data[7] = mHome;
            data[8] = mGenre;
            data[9] = mStyle;
            data[10] = mOccupatin;
            data[11] = mInstruments;
            data[12] = mLabel;
            data[13] = mGroup;
            data[14] = mLinkks;
            data[15] = mComment;

            return data;
        }

        /// <summary>
        /// アーティストの種別を設定
        /// </summary>
        /// <param name="type">(Pesonal/Group)</param>
        public void setArtistType(string type)
        {
            if (!Enum.TryParse(type, out mArtistType))
                mArtistType = ARTISTTYPE.Personal;
        }

        /// <summary>
        /// グループ/メンバ－リストをListで取得
        /// </summary>
        /// <returns>参照データ</returns>
        public List<string> getGroupList()
        {
            string[] groupArray = mGroup.Split('|');
            return groupArray.ToList<string>();
        }

        /// <summary>
        /// グループ/メンバーListを文字配列にして設定(セパレータに'|'を使用)
        /// </summary>
        /// <param name="group">グループ/メンバーリスト</param>
        public void setGroupList(List<string> group)
        {
            mGroup = "";
            if (0 < group.Count) {
                mGroup = group[0];
                for (int i = 1; i < group.Count; i++) {
                    mGroup += "|" + group[i];
                }
            }
        }

        /// <summary>
        /// 参照データ(URL)をListで取得
        /// </summary>
        /// <returns>参照データ</returns>
        public List<string> getLinkList()
        {
            string[] linkArray = mLinkks.Split('|');
            return linkArray.ToList<string>();
        }

        /// <summary>
        /// 参照データ(URL)を文字配列にして設定(セパレータに'|'を使用)
        /// </summary>
        /// <param name="links">参照データ(URL)</param>
        public void setLinkList(List<string> links)
        {
            mLinkks = "";
            if (0 < links.Count) {
                mLinkks = links[0];
                for (int i = 1; i < links.Count; i++) {
                    mLinkks += "|" + links[i];
                }
            }
        }
    }
}
