﻿using System;
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
using System.IO;

namespace MikrotikConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : NavigationWindow
    {
        //files we need to delete on start up:
        // script.rsc
        // info.txt
        // ros.npk
        // ipFile.txt
        //string[] files = { "\\script.rsc", "\\info.txt", "\\ros.npk", "\\ipFile.txt" };
        public MainWindow()
        {
            InitializeComponent();
            /*
            string path = directory.getcurrentdirectory();
            foreach(string file in files)
            {
                string filepath = path + file;
                if (file.exists(filepath))
                {
                    file.delete(filepath);
                }
            }
            */
        }
    }
}
