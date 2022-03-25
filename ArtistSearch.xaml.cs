using System.Collections.Generic;
using System.Windows;

namespace AudioApp
{
    /// <summary>
    /// ArtistSearch.xaml の相互作用ロジック
    /// </summary>
    public partial class ArtistSearch : Window
    {
        private ArtistInfoList mArtistList;             //  アーティスト情報リスト
        private List<ArtistInfoData> mArtistInfoData;   //  検索されたアーティスト情報のリスト
        public ArtistInfoData mArtist;                  //  検索結果のアーティスト情報
        public string mArtistName = "";                 //  登録アーティスト名

        public ArtistSearch(string artistName, ArtistInfoList artistList)
        {
            InitializeComponent();

            TbSearchName.Text = artistName;
            mArtistList = artistList;
        }

        /// <summary>
        /// [検索]ボタン  アーティスト情報リストからアーティストを検索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtSearch_Click(object sender, RoutedEventArgs e)
        {
            //  アーティスト名の検索(部分一致)
            mArtistInfoData = mArtistList.searchArtist(TbSearchName.Text);
            if (0 < mArtistInfoData.Count) {
                //  検索結果
                LbSearchData.Items.Clear();
                foreach (ArtistInfoData artistInfoData in mArtistInfoData) {
                    string buf = "[" + artistInfoData.mArtist + "][" + artistInfoData.mRealName + "][" + artistInfoData.mArtistName + "][" + artistInfoData.mArtistJpName + "]";
                    LbSearchData.Items.Add(buf);
                }
            } else {
                MessageBox.Show("アーティスト名が見つかりませんでした。", "検索結果");
            }
        }

        /// <summary>
        /// [OK]ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            if (0 <= LbSearchData.SelectedIndex)
                mArtist = mArtistInfoData[LbSearchData.SelectedIndex];
            mArtistName = TbRegistName.Text;

            DialogResult = true;
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
        /// [検索結果]のダブルクリック
        /// 選択して終了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbSearchData_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (0 <= LbSearchData.SelectedIndex) {
                mArtist = mArtistInfoData[LbSearchData.SelectedIndex];
                TbRegistName.Text = mArtist.mArtist;
                mArtistName = mArtist.mArtist;
            }
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// [件様結果]の選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LbSearchData_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (0 <= LbSearchData.SelectedIndex) {
                mArtist = mArtistInfoData[LbSearchData.SelectedIndex];
                TbRegistName.Text = mArtist.mArtist;
            }
        }
    }
}
