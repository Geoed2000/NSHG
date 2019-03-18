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
using NSHG;

namespace XMLEditor
{
    /// <summary>
    /// Interaction logic for AdapterEditor.xaml
    /// </summary>
    public partial class AdapterEditor : Window
    {
        Adapter a;
        public AdapterEditor(Adapter a)
        {
            this.a = a;
            InitializeComponent();
            NameIn.Text = a.Name;
            MacIn.Text = a.MyMACAddress.ToString();
            ConnectedIn.IsChecked = a.Connected;
            if ((bool)ConnectedIn.IsChecked)
            {
                OtherEndIn.Text = a.OtherendID.ToString();
                LocalIPIn.Text = a.LocalIP.ToString();
                SubnetIn.Text = a.SubnetMask.ToString();
                DefaultGatewayIn.Text = a.DefaultGateway.ToString();
                OtherEndIn.Focusable = true;
                LocalIPIn.Focusable = true;
                SubnetIn.Focusable = true;
                DefaultGatewayIn.Focusable = true;
            }
            else
            {
                OtherEndIn.Focusable = false;
                LocalIPIn.Focusable = false;
                SubnetIn.Focusable = false;
                DefaultGatewayIn.Focusable = false;
            }
        }

        private void NameIn_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NameIn.Text.Trim() != "")
            {
                a.Name = NameIn.Text; 
            }
        }

        private void MacIn_LostFocus(object sender, RoutedEventArgs e)
        {
            if (MacIn.Text.Trim() != "")
            {
                MAC NewMAC;
                if (MAC.TryParse(MacIn.Text, out NewMAC))
                {
                    a.MyMACAddress = NewMAC;
                }
                else
                {
                    MacIn.Text = "Error Parsing data current MAC: " + a.MyMACAddress.ToString();
                }
            }
        }

        private void ConnectedIn_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((bool)ConnectedIn.IsChecked)
            {
                OtherEndIn.Focusable = true;
                LocalIPIn.Focusable = true;
                SubnetIn.Focusable = true;
                DefaultGatewayIn.Focusable = true;
            }
            else
            {
                OtherEndIn.Focusable = false;
                LocalIPIn.Focusable = false;
                SubnetIn.Focusable = false;
                DefaultGatewayIn.Focusable = false;
            }
        }

        private void OtherEndIn_LostFocus(object sender, RoutedEventArgs e)
        {
            uint tmpId;
            if (uint.TryParse(OtherEndIn.Text, out tmpId))
            {
                a.OtherendID = tmpId;
            }
            else OtherEndIn.Text = "Invalid ID: "+OtherEndIn.Text;
        }

        private void LocalIPIn_LostFocus(object sender, RoutedEventArgs e)
        {
            IP tmpIP;
            if (IP.TryParse(LocalIPIn.Text, out tmpIP))
            {
                a.LocalIP = tmpIP;
            }
            else LocalIPIn.Text = "Invalid IP: " + LocalIPIn.Text;
        }

        private void SubnetIn_LostFocus(object sender, RoutedEventArgs e)
        {
            IP tmpIP;
            if (IP.TryParse(SubnetIn.Text, out tmpIP))
            {
                a.SubnetMask= tmpIP;
            }
            else SubnetIn.Text = "Invalid IP: " + SubnetIn.Text;
        }

        private void DefaultGatewayIn_LostFocus(object sender, RoutedEventArgs e)
        {
            IP tmpIP;
            if (IP.TryParse(DefaultGatewayIn.Text, out tmpIP))
            {
                a.DefaultGateway = tmpIP;
            }
            else DefaultGatewayIn.Text = "Invalid IP: " + DefaultGatewayIn.Text;
        }
    }
}
