using CommunityToolkit.Mvvm.Messaging;

namespace OEMInfo_MAUI_Sample
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<string>(this, (r, m) =>
            {
                    MainThread.BeginInvokeOnMainThread(() => { lbDisplayDeviceId.Text += "\n" + m; });
            });
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
