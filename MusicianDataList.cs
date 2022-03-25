using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// 演奏者データ(Wikipediaから抽出するデータ)
    /// </summary>
    public class MusicianData
    {
        public string mTitle { get; set; } = "";          //  タイトル名
        public string mComment { get; set; } = "";        //  コメント
        public string mUrl { get; set; } = "";            //  URL
        public string mBirthName { get; set; } = "";      //  出生名
        public string mAlsoKnownAs { get; set; } = "";    //  別名
        public string mBirthPlace { get; set; } = "";     //  出身地
                                                          //  学歴
        public string mBorn { get; set; } = "";           //  生誕
        public string mDied { get; set; } = "";           //  死没
        public string mGenres { get; set; } = "";         //  ジャンル
        public string mOccupations { get; set; } = "";    //  職業
        public string mInstruments { get; set; } = "";    //  担当楽器
        public string mYearsActive { get; set; } = "";    //  活動期間
        public string mLabels { get; set; } = "";         //  レーベル
        public string mAssociatedActs { get; set; } = ""; //  共同作業者
        public string mMember { get; set; } = "";         //  
        public string mListTitle { get; set; } = "";      //  親リストのタイトル
        public string mListUrl { get; set; } = "";        //  親リストのURL

        public static string[] mDataFormat = {
            "タイトル", "コメント", "出生名", "別名", "出身地", "生誕", "死没", "ジャンル", "職業", "担当楽器",
            "活動期間", "レーベル", "共同作業者", "メンバー", "URL", "親リストタイトル", "親リストURL"
        };
        //private string[,] mDataTitle = new string[,] {
        //    { "出生名", "別名", "出身地", "生誕", "死没", "ジャンル", "職業", "担当楽器", "活動期間", "レーベル", "共同作業者" },
        //    { "BirthName", "AlsoKnownAs", "BirthPlace", "Born", "Died", "Genres", "Occupations", "Instruments", "Years Active", "Labels", "Associated Acts" },
        //};

        private RegexOptions mRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline; //  正規表現検索オプション
        private string mPattern = "<table class=\"infobox(.*?)</table>";    //  テーブルデータ抽出正規表現パターン
        private string mHtml;               //  ダウンロードししたHTMLソース
        private int mEncordType = 0;        //  UTF8
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MusicianData()
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="title">タイトル(演奏者名)</param>
        /// <param name="comment">コメント</param>
        /// <param name="url">演奏者WebページのURL</param>
        /// <param name="infoData">演奏者データ取得可否</param>
        public MusicianData(string title, string comment, string url, string listTitle="", string listUrl="", bool infoData=false)
        {
            mTitle = title;
            mComment = comment;
            mUrl = url;
            mListTitle = listTitle;
            mListUrl = listUrl;
            //  演奏者データ
            if (infoData)
                getInfoData();
        }

        /// <summary>
        /// 演奏者データの更新
        /// </summary>
        public void updateInfoData()
        {
            getInfoData();
        }

        /// <summary>
        /// 演奏者データを文字列配列に変換
        /// </summary>
        /// <returns>文字列配列</returns>
        public string[] getStringData()
        {
            string[] data = new string[mDataFormat.Length];
            data[0] = mTitle;
            data[1] = mComment;
            data[2] = mBirthName;
            data[3] = mAlsoKnownAs;
            data[4] = mBirthPlace;
            data[5] = mBorn;
            data[6] = mDied;
            data[7] = mGenres;
            data[8] = mOccupations;
            data[9] = mInstruments;
            data[10] = mYearsActive;
            data[11] = mLabels;
            data[12] = mAssociatedActs;
            data[13] = mMember;
            data[14] = mUrl.Replace("\n", "\\n");
            data[15] = mListTitle;
            data[16] = mListUrl.Replace("\n", "\\n");
            return data;
        }

        /// <summary>
        /// 文字列配列データを演奏者データに設定
        /// </summary>
        /// <param name="data">文字列配列</param>
        public void setStringData(string[] data)
        {
            mTitle = data[0];
            mComment = data[1];
            mBirthName = data[2];
            mAlsoKnownAs = data[3];
            mBirthPlace = data[4];
            mBorn = data[5];
            mDied = data[6];
            mGenres = data[7];
            mOccupations = data[8];
            mInstruments = data[9];
            mYearsActive = data[10];
            mLabels = data[11];
            mAssociatedActs = data[12];
            mMember = data[13];
            mUrl = data[14].Replace("\\n", "\n");
            mListTitle = data[15];
            mListUrl = data[16].Replace("\\n", "\n");
        }

        /// <summary>
        /// Wikipediaの演奏者Webページのデータから演奏者データを抽出する
        /// </summary>
        public void getInfoData()
        {
            ylib.mRegexOption = mRegexOptions;
            if (mUrl == null || mUrl.Length == 0)
                return;

            //  Webデータの取得
            mHtml = ylib.getWebText(mUrl, mEncordType);
            if (mHtml == null || mHtml.Length == 0)
                return;

            //  データの冒頭(前書き)部分を抽出
            mComment = getFirstComment(mComment) + " " + getIntroduction(mHtml);

            //  基本情報のテーブルデータ(<table> </table>)を正規表現で抽出
            List<string[]> musicianData = ylib.getPattern(mHtml, mPattern);
            if (musicianData == null || musicianData.Count == 0)
                return;

            //  HTMLデータをタグ単位でリスト化
            List<string> tagList = ylib.getHtmlTagList(musicianData[0][0]);

            //  更新時は一部のデータの初期化要
            mBirthPlace = "";
            mMember = "";
            for (int i = 0; i < tagList.Count; i++) {
                //  タグデータの取得
                string tagName = ylib.getHtmlTagName(tagList[i]);
                if (0 < tagName.Length) {
                    //  テーブルの行データからデータの取得
                    if (tagName.CompareTo("tr") == 0) {
                        string label = "";
                        string data = "";
                        (label, data, i) = getTagTr(tagList, i);
                        data = ylib.cnvHtmlSpecialCode(data);   //  HTML特殊コード変換
                        data = stringStripCode(data);           //  改行コードをスペースにして除く
                        data = ylib.stripBrackets(data);        //  大括弧内の文字列を括弧ごと除く
                        if (label.CompareTo("出生名") == 0 || label.CompareTo("本名") == 0 || 
                            label.CompareTo("Birth name") == 0) {
                            mBirthName = data;
                        } else if (label.CompareTo("別名") == 0 || label.CompareTo("Also known as") == 0) {
                            mAlsoKnownAs = data;
                        } else if (label.CompareTo("出身地") == 0 || label.CompareTo("Origin") == 0) {
                            mBirthPlace = data;
                        } else if (label.CompareTo("生誕") == 0 || label.CompareTo("生年月日") == 0 ||
                            label.CompareTo("Born") == 0) {
                            mBorn = data;
                        } else if (label.CompareTo("死没") == 0 || label.CompareTo("Died") == 0) {
                            mDied = data;
                        } else if (label.CompareTo("ジャンル") == 0 || label.CompareTo("Genres") == 0) {
                            mGenres = data;
                        } else if (label.CompareTo("職業") == 0 || label.CompareTo("Occupation(s)") == 0) {
                            mOccupations = data;
                        } else if (label.CompareTo("担当楽器") == 0 || label.CompareTo("Instruments") == 0) {
                            mInstruments = data;
                        } else if (label.CompareTo("活動期間") == 0 || label.CompareTo("Years active") == 0) {
                            mYearsActive = data;
                        } else if (label.CompareTo("レーベル") == 0 || label.CompareTo("Labels") == 0) {
                            mLabels = data;
                        } else if (label.CompareTo("共同作業者") == 0 || label.CompareTo("Associated acts") == 0) {
                            mAssociatedActs = data;
                        } else if (label.CompareTo("メンバー") == 0 || label.CompareTo("旧メンバー") == 0 ||
                            label.CompareTo("Members") == 0 || label.CompareTo("Past members") == 0) {
                            if (mMember == null || mMember.Length == 0)
                                mMember = data;
                        }
                    }
                }
            }
            //  出身地が入っていない時は Born から出身地を抽出
            if (mBirthPlace.Length == 0 && 0 < mBorn.Length) {
                int n = 0;
                if (0 < (n = mBorn.LastIndexOf(','))) {
                    mBirthPlace = mBorn.Substring(n + 1).Trim();
                    mBirthPlace = mBirthPlace.Any(c => char.IsDigit(c)) ? "" : mBirthPlace;
                } else if (0 < (n = mBorn.LastIndexOf(' '))) {
                    mBirthPlace = mBorn.Substring(n + 1).Trim();
                    mBirthPlace = mBirthPlace.Any(c => char.IsDigit(c)) ? "" : mBirthPlace;
                }
            }
        }

        /// <summary>
        /// 一覧抽出で取得したコメント('['']'で囲まれた部分)のみを抽出
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        private string getFirstComment(string comment)
        {
            if (comment.IndexOf("[") == 0 && 0 < comment.IndexOf("]")) {
                return comment.Substring(0, comment.IndexOf("]") + 1);
            } else {
                return "";
            }
        }

        /// <summary>
        /// データの前書き部分を抽出
        /// 最初の段落(<p> ～　</p>)部分から文字列デーを抽出する
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string getIntroduction(string html)
        {
            //  基本情報のテーブルを終わりを検索
            int bs = html.IndexOf("<table class=\"infobox");
            int be = 0;
            if (0 <= bs)
                be = html.IndexOf("</table>", bs);
            //  段落の検出
            int n = html.IndexOf("<p>", be);
            if (n < 0)
                return "";
            int m = html.IndexOf("</p>", n);
            if (m < 0)
                return "";
            //  段落データの抽出
            string introData = html.Substring(n, m - n);
            //  データを抽出してリスト化
            List<string> tagList = ylib.getHtmlTagDataAll(introData);

            string data = string.Join(" ", tagList);
            data = ylib.cnvHtmlSpecialCode(data);   //  HTML特殊コード変換
            data = stringStripCode(data);           //  改行コードをスペースにして除く
            return ylib.stripBrackets(data);        //  大括弧内の文字列を括弧ごと除く
        }

        /// <summary>
        /// 改行コードと連続スペースを除く
        /// </summary>
        /// <param name="code">ソースデータ</param>
        /// <returns>変換データ</returns>
        private string stringStripCode(string code)
        {
            string buffer;
            buffer = code.Replace("\r\n", " ");     //  \r\n→\\\n
            buffer = buffer.Replace("\n", " ");     //  \n→\\\n
            buffer = buffer.Replace("\r", " ");     //  \r→\\\n
            buffer = buffer.Replace("  ", " ");     //  '  ' → ' ' (重複スペースを単スペースにする)
            return buffer;
        }

        /// <summary>
        /// HTMLのテーブルデータの一行から見出しとデータを取得
        /// </summary>
        /// <param name="tagList">タグリストデータ</param>
        /// <param name="pos">データ位置</param>
        /// <returns>(label, data, pos) (データタイトル,データ, 位置)</returns>
        private (string, string, int) getTagTr(List<string> tagList, int pos)
        {
            string label = "";
            string data = "";
            while (pos < tagList.Count - 1 && tagList[pos].IndexOf("</tr>") < 0) {
                string tagName = ylib.getHtmlTagName(tagList[pos]);
                if (tagName.CompareTo("th") == 0) {
                    //  ヘッダー(見出し)
                    while (pos < tagList.Count - 1 && tagList[pos].IndexOf("</th>") < 0 && tagList[pos].IndexOf("</tr>") < 0) {
                        tagName = ylib.getHtmlTagName(tagList[pos]);
                        if (tagName.Length == 0) {
                            label = tagList[pos];
                        }
                        pos++;
                    }
                } else if (tagName.CompareTo("td") == 0) {
                    //  セルのデータ
                    while (pos < tagList.Count - 1 && tagList[pos].IndexOf("</td>") < 0 && tagList[pos].IndexOf("</tr>") < 0) {
                        //  <style >タグを除去
                        pos = nextTagPos(tagList, "style", pos);
                        //if (0 <= tagList[pos].IndexOf("<style")) {
                        //    if (tagList[pos].IndexOf("/>") < 0) {
                        //        while (pos < tagList.Count - 1 && tagList[pos].IndexOf("</style>") < 0) {
                        //            pos++;
                        //        }
                        //    }
                        //    pos++;
                        //}
                        tagName = ylib.getHtmlTagName(tagList[pos]);
                        if (tagName.Length == 0) {
                            //  データの取得
                            if (0 < data.Length)
                                data += " ";
                            data += tagList[pos];
                        } else if (tagName.CompareTo("br") == 0 ||
                            (tagList[pos].CompareTo("</li>") == 0 && tagList[pos + 1].CompareTo("<li>") == 0)) {
                            //  メンバーなどの区切りのためにカンマを追加
                            data += ",";
                        }
                        pos++;
                    }
                }
                pos++;
            }
            return (label, data, pos);
        }

        private int nextTagPos(List<string> tagList, string tagName, int pos)
        {
            if (0 <= tagList[pos].IndexOf("<" + tagName)) {
                if (tagList[pos].IndexOf("/>") < 0) {
                    while (pos < tagList.Count - 1 && tagList[pos].IndexOf("</" + tagName + ">") < 0) {
                        pos++;
                    }
                }
                pos++;
            }
            return pos;
        }

    }

    /// <summary>
    /// 演奏者データのリスト
    /// Wikipediaの演奏者一覧のWebページから演奏者リストを抽出する
    /// </summary>
    class MusicianDataList
    {
        public List<MusicianData> mMusicianList = new List<MusicianData>();

        //  演奏者リストを抽出するための正規表現パターン
        private string pattern1 = 
            "<li><a href=\"(.*?)\".*?title=\"(.*?)\">(.*?)</a>(.*?)(</li>|<sup|<a href.*?>(.*?)</a>)";  //  演奏者
            //  data        [2]               [3]      [4]      [5]                          [7]
        private string pattern2 = "<span class=\"(.*?)\" ?id=\"(.*?)\">(.*?)</span>";           //  小タイトル(楽器名)
                               // data            [8]           [9]     [10]
        private string mPattern = "";       //  抽出パターン

        YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MusicianDataList()
        {
            //  抽出パターン設定
            mPattern = "(" + pattern1 + ")|(" + pattern2 + ")";

        }

        /// <summary>
        /// 演奏者リストをWebページから読み込む
        /// </summary>
        /// <param name="url">WebページのURL</param>
        /// <param name="infoData">演奏者データの読込可否</param>
        /// <returns>演奏者リスト</returns>
        public List<MusicianData> getPlayerList(string listTitle, string url, bool infoData)
        {
            string baseUrl = url.Substring(0, url.IndexOf('/', 10));
            string html = ylib.getWebText(url);
            if (html != null) {
                List<string[]> musicianList = ylib.getPattern(html, mPattern);
                mMusicianList.Clear();
                foreach (string[] data in musicianList) {
                    string title = ylib.cnvHtmlSpecialCode(data[4].Trim());          //  タイトル
                    string comment = "";
                    string urlAddress = Uri.UnescapeDataString(baseUrl + data[2]);  //  URL
                    if (0 < data[1].Length) {
                        if (0 < data[7].Length)             //  コメント
                            comment = ylib.cnvHtmlSpecialCode(data[7].Trim());
                        else
                            comment = ylib.cnvHtmlSpecialCode(data[5].Trim());       //  コメント
                        if (0 < comment.Length)
                            comment = "[" + comment + "]";
                        mMusicianList.Add(new MusicianData(title, comment, urlAddress, listTitle, url, infoData));
                    } else {
                        //  抽出データ読込終了
                        if (data[10].CompareTo("脚注") == 0 || data[10].CompareTo("References") == 0)
                            break;
                        mMusicianList.Add(new MusicianData(data[10], "", "", listTitle, url, false));
                    }
                }
                return mMusicianList;
            }
            return null;
        }

        /// <summary>
        /// 演奏者の検索(次検索)
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns>検索位置</returns>
        public int nextSearchPlayer(string searchText, int searchIndex)
        {
            if (mMusicianList == null || mMusicianList.Count < 1)
                return -1;
            for (int i = Math.Max(searchIndex + 1, 0); i < mMusicianList.Count; i++) {
                if (0 <= mMusicianList[i].mTitle.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ||
                    0 <= mMusicianList[i].mComment.IndexOf(searchText, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 演奏者の検索(前検索)
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns>検索位置</returns>
        public int prevSearchPlayer(string searchText, int searchIndex)
        {
            if (mMusicianList == null || mMusicianList.Count < 1)
                return -1;
            for (int i = Math.Min(searchIndex - 1, mMusicianList.Count - 1); 0 <= i; i--) {
                if (0 <= mMusicianList[i].mTitle.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ||
                    0 <= mMusicianList[i].mComment.IndexOf(searchText, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 全ファイルの中から検索する
        /// </summary>
        /// <param name="searchText">検索文字列</param>
        /// <param name="dataFoleder">検索ファイルのフォルダ</param>
        public void getSearchAllPlayer(string searchText, string dataFoleder)
        {
            string[] fileList = ylib.getFiles(dataFoleder + "\\*.csv");
            if (fileList != null) {
                mMusicianList.Clear();
                foreach (string path in fileList) {
                    mMusicianList.AddRange(getSerchPlayerFile(searchText, path));
                }
            }
        }

        /// <summary>
        /// 検索ファイルから用語を検索しListに保存
        /// </summary>
        /// <param name="searchText">検索文字列</param>
        /// <param name="filePath">検索ファイル名</param>
        /// <returns></returns>
        public List<MusicianData> getSerchPlayerFile(string searchText, string filePath)
        {
            List<string[]> musicianList = ylib.loadCsvData(filePath, MusicianData.mDataFormat);
            if (musicianList != null) {
                List<MusicianData> playerList = new List<MusicianData>();
                foreach (string[] data in musicianList) {
                    if (data[0].CompareTo(MusicianData.mDataFormat[0]) != 0) {
                        for (int i = 0; i <data.Length; i++) {
                            if (0 <= data[i].IndexOf(searchText, StringComparison.OrdinalIgnoreCase)) {
                                MusicianData musicianData = new MusicianData();
                                musicianData.setStringData(data);
                                playerList.Add(musicianData);
                                break;
                            }
                        }
                    }
                }
                return playerList;
            }
            return null;
        }

        /// <summary>
        /// 演奏者リストをファイルから読み込む
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        public void loadData(string filePath, string listTitle)
        {
            List<string[]> musicianList = ylib.loadCsvData(filePath, MusicianData.mDataFormat);
            if (musicianList != null) {
                int listTitleNo = Array.IndexOf(MusicianData.mDataFormat, "親リストタイトル");
                mMusicianList.Clear();
                foreach (string[] data in musicianList) {
                    if (data[0].CompareTo(MusicianData.mDataFormat[0]) != 0) {
                        if (0 < listTitleNo && listTitleNo < data.Length) {
                            data[listTitleNo] = listTitle;
                        }
                        MusicianData musicianData = new MusicianData();
                        musicianData.setStringData(data);
                        mMusicianList.Add(musicianData);
                    }
                }
            }
        }

        /// <summary>
        /// 演奏者リストをファイルに保存
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        public void saveData(string filePath)
        {
            List<string[]> musicianList = new List<string[]>();
            foreach (MusicianData data in mMusicianList)
                musicianList.Add(data.getStringData());
            ylib.saveCsvData(filePath, MusicianData.mDataFormat, musicianList);
        }
    }
}
