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
using System.Windows.Shapes;
using Model.Basic;

namespace GUI
{
    //people类
    public class people
    {
        public string tube { get; set; }
        public string row { get; set; }
        public string Pri { get; set; }
        public string Tri { get; set; }
        public string Hri { get; set; }
        public string Pro { get; set; }
        public string Tro { get; set; }
        public string Hro { get; set; }
        public string HTC { get; set; }
        public string Q { get; set; }
        public string mr { get; set; }
    }

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        //创建people数组
        List<people> peopleList = new List<people>();

        //①定义一个可读可写的公用的字符串：getName
        public string Tube_row { get; set; }
        public string Row { get; set; }


        public double[,] getName_tube { get; set; }
        public double[,] getName_row { get; set; }
        public double[,] getName_Pri { get; set; }
        public double[,] getName_Tri { get; set; }
        public double[,] getName_Hri { get; set; }
        public double[,] getName_Pro { get; set; }
        public double[,] getName_Tro { get; set; }
        public double[,] getName_Hro { get; set; }
        public double[,] getName_HTC { get; set; }
        public double[,] getName_Q { get; set; }
        public double[,] getName_mr { get; set; }

        

        //创建dataGrid数据
        private void LoadData(object sender, RoutedEventArgs e)
        {

            int Row_int = Convert.ToInt16(Row);
            int Tube_row_int = Convert.ToInt16(Tube_row);
            
            for (int i = 0; i < Tube_row_int; i++)
                for (int j = 0; j < Row_int; j++)
            {
                peopleList.Add(new people()
                {              
                    tube = Convert.ToString(i),
                    row = Convert.ToString(j),
                    Pri = Convert.ToString(getName_Pri[i, j]),
                    Tri = Convert.ToString(getName_Tri[i, j]),
                    Hri = Convert.ToString(getName_Hri[i, j]),
                    Pro = Convert.ToString(getName_Pro[i, j]),
                    Tro = Convert.ToString(getName_Tro[i, j]),
                    Hro = Convert.ToString(getName_Hro[i, j]),
                    HTC = Convert.ToString(getName_HTC[i, j]),
                    Q = Convert.ToString(getName_Q[i, j]),
                    mr = Convert.ToString(getName_mr[i, j]),
                });
            }

            ((this.FindName("dataGrid")) as DataGrid).ItemsSource = peopleList;
            
        }
    }
}
