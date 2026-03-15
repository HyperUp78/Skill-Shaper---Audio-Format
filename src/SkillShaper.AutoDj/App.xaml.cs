namespace SkillShaper.AutoDj;

public partial class App : Application
{
    public App()
    {
        try
        {
            InitializeComponent();
            MainPage = new NavigationPage(new MainPage())
            {
                BarBackgroundColor = Color.FromArgb("#111318"),
                BarTextColor = Colors.White
            };
        }
        catch (Exception ex)
        {
            MainPage = new ContentPage
            {
                Padding = new Thickness(16),
                Content = new ScrollView
                {
                    Content = new Label
                    {
                        Text = $"Startup failure: {ex.GetType().Name}\n{ex.Message}\n\n{ex.StackTrace}",
                        TextColor = Colors.White
                    }
                },
                BackgroundColor = Color.FromArgb("#111318")
            };
        }
    }
}
