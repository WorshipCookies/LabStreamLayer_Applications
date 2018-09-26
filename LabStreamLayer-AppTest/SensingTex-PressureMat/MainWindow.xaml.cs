using SensingTexAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SensingTex_PressureMat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private MattressDevice matSensor;

        // Characteristics of this particular the Sensing Mat used
        public const int rows = 16;
        public const int cols = 16;


        public MainWindow()
        {
            InitializeComponent();


        }
    }
}
