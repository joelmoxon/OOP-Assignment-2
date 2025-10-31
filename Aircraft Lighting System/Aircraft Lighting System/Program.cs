namespace AircraftLightsGUI
{
    internal static class Program
    {
        public static MainForm? MainFormInstance { get; private set; }

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            MainFormInstance = new MainForm();
            Application.Run(MainFormInstance);
        }

    }
}