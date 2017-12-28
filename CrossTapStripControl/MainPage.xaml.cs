using System;

namespace CrossTapStripControl
{
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void GotoNormalPage(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new NormalTabStripPage());
        }

        private void GotoImgaePage(object sender, EventArgs e)
        {
            this.Navigation.PushAsync(new ImageTabStripPage());
        }
    }
}