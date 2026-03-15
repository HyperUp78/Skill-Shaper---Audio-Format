namespace SkillShaper.AutoDj;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(CreateSafeLauncherPage());
    }

    private Page CreateSafeLauncherPage()
    {
        var statusLabel = new Label
        {
            Text = "Initializing...",
            TextColor = Colors.White,
            FontSize = 12
        };

        var launchButton = new Button
        {
            Text = "Launch Mixer"
        };

        async Task LaunchAsync()
        {
            try
            {
                var page = new NavigationPage(new MainPage())
                {
                    BarBackgroundColor = Color.FromArgb("#111318"),
                    BarTextColor = Colors.White
                };

                if (Windows.Count > 0)
                {
                    Windows[0].Page = page;
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Startup failure: {ex.GetType().Name} - {ex.Message}";
            }
        }

        launchButton.Clicked += async (_, _) => await LaunchAsync();

        var launcher = new ContentPage
        {
            Title = "SkillShaper AutoDJ",
            BackgroundColor = Color.FromArgb("#111318"),
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "SkillShaper AutoDJ",
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 24,
                        TextColor = Colors.White
                    },
                    new Label
                    {
                        Text = "Safe startup mode is active.",
                        TextColor = Color.FromArgb("#CBD5E0")
                    },
                    launchButton,
                    statusLabel
                }
            }
        };

        return launcher;
    }
}
