using System;
using System.Windows;
using JNN.Stair.Views;

namespace JNN.Stair.Preview
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new Application();
                var window = new StairDesignerWindow();
                app.Run(window);
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("error.log", ex.ToString());
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
