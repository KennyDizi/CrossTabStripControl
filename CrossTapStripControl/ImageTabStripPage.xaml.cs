using System;

namespace CrossTapStripControl
{
	public partial class ImageTabStripPage
	{
		public ImageTabStripPage ()
		{
			InitializeComponent ();
		}

	    private void OnTabActivated(object sender, EventArgs e)
	    {
            System.Diagnostics.Debug.WriteLine($"Tab active index: {this.TabControl.TabActiveIndex}");
	    }
	}
}