using System.Windows;
using BookShelfApp.Data;

namespace BookShelfApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DbInitializer.InitializeDatabase();
            base.OnStartup(e);
        }
    }
}
