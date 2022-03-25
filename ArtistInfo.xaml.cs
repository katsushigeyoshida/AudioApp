using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfLib;

namespace AudioApp
{
    /// <summary>
    /// ArtistInfo.xaml の相互作用ロジック
    /// </summary>
    public partial class ArtistInfo : Window
    {
        ArtistInfoList mArtistInfoList;     //  アーティスト情報リスト
        private string mArtist = "";        //  編集対象アーティスト名
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ArtistInfo()
        {
            InitializeComponent();

            mArtistInfoList = new ArtistInfoList();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="artist">アーティスト名</param>
        /// <param name="artistInfoList">アーティスト情報リスト</param>
        public ArtistInfo(string artist, ArtistInfoList artistInfoList)
        {
            InitializeComponent();

            mArtist = artist;
            mArtistInfoList = artistInfoList;
            //  アーティスト情報を画面に表示
            LbArtist.Content = mArtist;
            ArtistInfoData artistInfoData = mArtistInfoList.getData(mArtist);
            if (artistInfoData == null) {
                //  登録されていないデータ
                artistInfoData = new ArtistInfoData();
                artistInfoData.mArtist = mArtist;
            }
            if (0 < artistInfoData.mRealName.Length) {
                //  登録名がある場合,登録名データを取得し、データを上書き表示
                ArtistInfoData artistInfoDataReal = mArtistInfoList.getData(artistInfoData.mRealName);
                setData(artistInfoDataReal);
                LbRealName.Content = artistInfoData.mRealName.ToString();   //  登録名(本名)
            } else {
                setData(artistInfoData);
            }
            setTitle();
        }

        /// <summary>
        /// コントロールにデータを設定
        /// </summary>
        /// <param name="artistInfodata">アーティスト情報</param>
        private void setData(ArtistInfoData artistInfodata)
        {
            ChkGroup.IsChecked = artistInfodata.mArtistType == ArtistInfoData.ARTISTTYPE.Group;
            LbRealName.Content = artistInfodata.mRealName;   //  登録名(本名)
            TbName.Text = artistInfodata.mArtistName;
            TbJpName.Text = artistInfodata.mArtistJpName;
            if (0 < artistInfodata.mBorn.Length)
                TbBorn.Text = artistInfodata.mBorn;
            else
                TbBorn.Text = "yyyy/mm/dd";
            if (0 < artistInfodata.mDied.Length)
                TbDied.Text = artistInfodata.mDied;
            else
                TbDied.Text = "yyyy/mm/dd";
            TbHome.Text = artistInfodata.mHome;
            TbGenre.Text = artistInfodata.mGenre;
            TbStyle.Text = artistInfodata.mStyle;
            TbOccupation.Text = artistInfodata.mOccupatin;
            TbInstrument.Text = artistInfodata.mInstruments;
            TbLabel.Text = artistInfodata.mLabel;
            LbGroupMember.Items.Clear();
            List<string> groupList = artistInfodata.getGroupList();
            foreach (string group in groupList) {
                if (0 < group.Length)
                    LbGroupMember.Items.Add(group.Trim());
            }
            LbURL.Items.Clear();
            List<string> linkList = artistInfodata.getLinkList();
            foreach (string link in linkList) {
                if (0 < link.Length)
                    LbURL.Items.Add(Uri.UnescapeDataString(link.Trim()));
            }
            TbComment.Text = ylib.strControlCodeRev(artistInfodata.mComment);
            LbAge.Content = "年齢 " + ylib.subYear(TbBorn.Text, TbDied.Text).ToString();
        }

        /// <summary>
        /// コントロールからデータの取得しデータを更新する
        /// </summary>
        /// <param name="artist">artist名(keyデータ)</param>
        private void updateData(string artist)
        {
            ArtistInfoData artistInfodata = mArtistInfoList.getData(artist);
            if (artistInfodata == null) {
                artistInfodata = new ArtistInfoData();
                artistInfodata.mArtist = mArtist;
                mArtistInfoList.add(artistInfodata);
            }
            if (artist.CompareTo(LbRealName.Content.ToString()) != 0) {
                artistInfodata.mRealName = LbRealName.Content.ToString();
            } else {
                artistInfodata.mRealName = "";
            }
            artistInfodata.mArtistType = ChkGroup.IsChecked == true ? ArtistInfoData.ARTISTTYPE.Group : ArtistInfoData.ARTISTTYPE.Personal;
            artistInfodata.mArtistName = TbName.Text;
            artistInfodata.mArtistJpName = TbJpName.Text;
            artistInfodata.mBorn = TbBorn.Text;
            artistInfodata.mDied = TbDied.Text;
            artistInfodata.mHome = TbHome.Text;
            artistInfodata.mGenre = TbGenre.Text;
            artistInfodata.mStyle = TbStyle.Text;
            artistInfodata.mOccupatin = TbOccupation.Text;
            artistInfodata.mInstruments = TbInstrument.Text;
            artistInfodata.mLabel = TbLabel.Text;
            List<string> groupList = new List<string>();
            foreach (string group in LbGroupMember.Items)
                groupList.Add(group.Trim());
            artistInfodata.setGroupList(groupList);
            List<string> linkList = new List<string>();
            foreach (string link in LbURL.Items)
                linkList.Add(Uri.UnescapeDataString(link.Trim()));
            artistInfodata.setLinkList(linkList);
            artistInfodata.mComment = ylib.strControlCodeCnv(TbComment.Text);
        }

        /// <summary>
        /// [演奏者リスト]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtMusicianList_Click(object sender, RoutedEventArgs e)
        {
            FindPlayer dlg = new FindPlayer();
            dlg.mCloseBottonVisible = true;
            dlg.TbSearch.Text = mArtist.Trim();
            var result = dlg.ShowDialog();
            if (result == true) {
                setMusicianData(dlg.mMusicianData);
                setTitle();
            }
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            //  アーティスト情報リストの更新保存
            updateData(mArtist);
            if (0 < LbRealName.Content.ToString().Length) {
                updateData(LbRealName.Content.ToString());
            }
            mArtistInfoList.saveData();
            Close();
        }

        /// <summary>
        /// [Cancel]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// [リンク]のダブルクリック(開く)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbURL_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (0 <= LbURL.SelectedIndex) {
                //  リンク先を開く
                System.Diagnostics.Process p =
                        System.Diagnostics.Process.Start(LbURL.Items[LbURL.SelectedIndex].ToString());
            }
        }

        /// <summary>
        /// [グループ/メンバ] ダブルクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbGroupMember_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (0 <= LbGroupMember.SelectedIndex) {
                //  アーティスト情報表示
                string artist = LbGroupMember.Items[LbGroupMember.SelectedIndex].ToString();
                artist = ylib.stripBrackets(artist, '(', ')');
                artist = ylib.stripBrackets(artist, '（', '）');
                ArtistInfo dlg = new ArtistInfo(artist, mArtistInfoList);
                var result = dlg.ShowDialog();
                if (result == true) {

                }
            }
        }

        /// <summary>
        /// [グループ/メンバーリスト] コンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupAddMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("GroupSearchMenu") == 0) {
                //  グループまたはメンバー検索
                if (0 <= LbGroupMember.SelectedIndex) {
                    searchArtist(LbGroupMember.Items[LbGroupMember.SelectedIndex].ToString());
                }
            } else if (menuItem.Name.CompareTo("GroupEditMenu") == 0) {
                //  グループまたはメンバー編集
                editGroupMember();
            } else if (menuItem.Name.CompareTo("GroupPasteMenu") == 0) {
                //  貼付け
                LbGroupMember.Items.Add(Clipboard.GetText());
            } else if (menuItem.Name.CompareTo("GroupCopyMenu") == 0) {
                //  コピー
                if (0 <= LbGroupMember.SelectedIndex) {
                    Clipboard.SetText(LbGroupMember.Items[LbGroupMember.SelectedIndex].ToString());
                }
            } else if (menuItem.Name.CompareTo("GroypRemoveMenu") == 0) {
                //  削除
                if (0 <= LbGroupMember.SelectedIndex) {
                    LbGroupMember.Items.Remove(LbGroupMember.Items[LbGroupMember.SelectedIndex]);
                }
            }
        }

        /// <summary>
        /// [グループ]チェックボックスのクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkGroup_Click(object sender, RoutedEventArgs e)
        {
            setTitle();
        }

        /// <summary>
        /// Wikipediaの検索データを表示に反映
        /// </summary>
        /// <param name="musicianData"></param>
        private void setMusicianData(string[] musicianData)
        {
            bool jpnData = 0 <= musicianData[14].IndexOf("ja.wikipedia") ? true : false;    //  URLで日本Wikiかを判定
            if (0 < musicianData[1].Length)
                TbComment.Text = musicianData[1];           //  [1]:コメント
            if (jpnData) {
                if (0 < musicianData[3].Length)
                    TbName.Text = musicianData[3];          //  [3]:別名
                if (0 < musicianData[2].Length)
                    TbJpName.Text = musicianData[2];        //  [2]:出生名
                else
                    TbJpName.Text = musicianData[0];        //  [0]:タイトル
            } else {
                if (0 < musicianData[2].Length)
                    TbName.Text = musicianData[2];          //  [2]:出生名
                if (0 < musicianData[3].Length)
                    TbJpName.Text = musicianData[3];        //  [3]:別名
            }
            if (0 < musicianData[5].Length)
                TbBorn.Text = pickupYear(musicianData[5]);  //  [5]:生誕
            if (0 < musicianData[6].Length)
                TbDied.Text = pickupYear(musicianData[6]);  //  [6]:死亡
            if (0 < musicianData[4].Length)
                TbHome.Text = musicianData[4];              //  [4]:出身地
            if (0 < musicianData[7].Length)
                TbGenre.Text = musicianData[7];             //  [7]:ジャンル
            if (0 < musicianData[8].Length)
                TbOccupation.Text = musicianData[8];        //  [8]:職業
            if (0 < musicianData[9].Length)
                TbInstrument.Text = musicianData[9];        //  [9]:楽器
            if (0 < musicianData[11].Length)
                TbLabel.Text = musicianData[11];            //  [11]:レーベル
            //  メンバまたは共同作業者を追加
            string[] members = null;
            if (0 < musicianData[13].Length) {
                members = splitMember(musicianData[13]);    //  [13]:メンバ
            } else if (0 < musicianData[12].Length) {
                members = splitMember(musicianData[12]);    //  [12]:共同作業者
            }
            if (members != null) {
                LbGroupMember.Items.Clear();
                foreach (string buf in members)
                    LbGroupMember.Items.Add(buf.Trim());
            }
            LbURL.Items.Add(musicianData[14].Trim());   //  [14]:URL
        }

        /// <summary>
        /// Wikipediaから検索する
        /// </summary>
        /// <param name="artist"></param>
        private void searchArtist(string artist)
        {
            artist = ylib.stripBrackets(artist, '(', ')');
            artist = ylib.stripBrackets(artist, '（', '）');
            FindPlayer dlg = new FindPlayer();
            dlg.mCloseBottonVisible = true;
            dlg.TbSearch.Text = artist.Trim();
            var result = dlg.ShowDialog();
        }

        /// <summary>
        /// グループメンバをテキスト編集ダイヤログで編集する
        /// </summary>
        private void editGroupMember()
        {
            if (0 <= LbGroupMember.SelectedIndex) {
                InputBox dlg = new InputBox();
                dlg.Title = mArtist;
                dlg.mWindowSizeOutSet = true;
                dlg.mWindowWidth  = 400.0;
                dlg.mWindowHeight = 300.0;
                dlg.mMultiLine = true;
                string buf = "";
                for (int i = 0; i < LbGroupMember.Items.Count; i++)
                    buf += LbGroupMember.Items[i] + (i < LbGroupMember.Items.Count - 1 ? "\n" : "");
                dlg.mEditText = buf;
                var result = dlg.ShowDialog();
                if (result == true) {
                    char[] trimChar = new char[] { '\r', ' ' };
                    string[] text = dlg.mEditText.Split('\n');
                    LbGroupMember.Items.Clear();
                    for (int i = 0; i < text.Length; i++)
                        LbGroupMember.Items.Add(text[i].Trim(trimChar));
                }
            }
        }

        /// <summary>
        /// [リンク]リストボックスのコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void URLMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("URLPasteMenu") == 0) {
                //  リンク先き貼付け
                if (LbURL.Items.Count < 10)
                    LbURL.Items.Add(Clipboard.GetText());
            } else if (menuItem.Name.CompareTo("URLCopyMenu") == 0) {
                //  リンク先コピー
                if (0 <= LbURL.SelectedIndex) {
                    Clipboard.SetText(LbURL.Items[LbURL.SelectedIndex].ToString());
                }
            } else if (menuItem.Name.CompareTo("URLRemoveMenu") == 0) {
                //  リンク先削除
                if (0 <= LbURL.SelectedIndex) {
                    LbURL.Items.Remove(LbURL.Items[LbURL.SelectedIndex]);
                }
            }
        }

        /// <summary>
        /// [Artist名]のコンテキストメニュー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArtistMenuClick(object sender, RoutedEventArgs e)
        {
            string searchWord = "";
            MenuItem menuItem = (MenuItem)e.Source;
            if (menuItem.Name.CompareTo("LbArtistCopyMenu") == 0) {
                //  アーティスト名コピー
                Clipboard.SetText(LbArtist.Content.ToString());
            } else if (menuItem.Name.CompareTo("LbArtistGoogleMenu") == 0) {
                //  アーティスト名でGoogle検索
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("googleJpn", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistGoogleComMenu") == 0) {
                //  アーティスト名でGoogle(米)検索
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("google", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistBingMenu") == 0) {
                //  アーティスト名でBing検索
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("Bing", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistDiscogsMenu") == 0) {
                //  アーティスト名でDiscogs検索
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("Discogs", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistWikiMenu") == 0) {
                //  アーティスト名でWikipedia検索
                searchWord = LbArtist.Content.ToString();
                ylib.WebSerach("Wikipedia", searchWord);
            } else if (menuItem.Name.CompareTo("LbArtistSearchMenu") == 0) {
                //  登録名検索
                ArtistSearch dlg = new ArtistSearch(mArtist, mArtistInfoList);
                var result = dlg.ShowDialog();
                if (result == true && dlg.mArtist != null) {
                    setData(dlg.mArtist);
                    setTitle();
                    if (0 == dlg.mArtist.mRealName.Length)
                        LbRealName.Content = dlg.mArtist.mArtist;
                }
            } else if (menuItem.Name.CompareTo("LbArtistRemoveMenu") == 0) {
                //  登録名削除
                if (MessageBox.Show("すべてのデータをクリアしますか?","削除確認",MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                    ArtistInfoData artistInfoData = new ArtistInfoData();
                    setData(artistInfoData);
                    setTitle();
                }
                LbRealName.Content = "";
            } else if (menuItem.Name.CompareTo("LbArtistPasteMenu") == 0) {
                //  クリップボードのデータを反映する
                clipDataSet();
            }
        }

        /// <summary>
        /// クリップボードのデータを反映する
        /// </summary>
        private void clipDataSet()
        {
            string[] buf = Clipboard.GetText().Split('\n');
            if (1 < buf.Length) {
                string[] title = ylib.seperateString(buf[0]);
                string[] data = ylib.seperateString(buf[1]);
                if (title[0].CompareTo("タイトル")==0 && title[1].CompareTo("コメント")==0) {
                    TbComment.Text = data[1];
                    TbName.Text = data[2];
                    TbBorn.Text = pickupYear(data[4]);
                    TbDied.Text = pickupYear(data[5]);
                    //TbHome.Text
                    TbGenre.Text = data[6];
                    TbOccupation.Text = data[7];
                    TbInstrument.Text = data[8];
                    TbLabel.Text = data[10];
                    LbGroupMember.Items.Clear();
                    LbGroupMember.Items.Add(data[11]);
                    LbURL.Items.Add(data[12]);
                }
            }
        }

        /// <summary>
        /// 日付の抽出
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string pickupYear(string data)
        {
            if (0 < data.Length) {
                int m = data.IndexOf("(");
                int n = data.IndexOf(")");
                if (0 <= m && 0 <= n) {
                    return data.Substring(m + 1, n - m - 1).Trim();
                } else {
                    return ylib.cnvDateFormat(data);
                    //return ylib.JulianDay2DateYear(ylib.date2JulianDay(data), 0);
                }
            }
            return "";
        }

        /// <summary>
        /// メンバリストを',' または' '区切りで配列にする
        /// </summary>
        /// <param name="text">メンバリスト文字列</param>
        /// <returns></returns>
        private string[] splitMember(string text)
        {
            string[] members = null;
            if (0 < text.IndexOf(',')) {
                members = text.Split(',');
            } else if (0 < text.IndexOf('、')) {
                members = text.Split('、');
            } else if (0 < text.IndexOf(' ')) {
                List<string> memberList = new List<string>();
                string buf = "";
                int i = 0;
                text = text.Replace("  ", " ");
                while (i < text.Length) {
                    if (text[i] == ' ') {
                        if (i == text.Length - 1 ||
                           (i < text.Length + 1 && (text[i + 1] != '(' && text[i + 1] != '（'))) {
                            memberList.Add(buf);
                            buf = "";
                        }
                    } else if (text[i] == '(' || text[i] == '（') {
                        while (i < text.Length && (text[i] != ')' && text[i] != '）')) {
                            buf += text[i];
                            i++;
                        }
                        if (i < text.Length) {
                            buf += text[i];
                        }
                    } else {
                        buf += text[i];
                    }
                    i++;
                }
                if (0 < buf.Length)
                    memberList.Add(buf);
                members = memberList.ToArray();
            }
            return members;
        }

        /// <summary>
        /// タイトルの設定
        /// </summary>
        private void setTitle()
        {
            if (ChkGroup.IsChecked == true) {
                LbBornTitle.Content = "結成";
                LbDiedTitle.Content = "解散";
                LbGroupTitle.Content = "グループ";
                LbMemberTitle.Content = "メンバー";
            } else {
                LbBornTitle.Content = "誕生";
                LbDiedTitle.Content = "死亡";
                LbGroupTitle.Content = "共同";
                LbMemberTitle.Content = "作業者";
            }
        }
    }
}
