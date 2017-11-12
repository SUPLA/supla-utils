/*
 Copyright (C) AC SOFTWARE SP. Z O.O.
 This program is free software; you can redistribute it and/or
 modify it under the terms of the GNU General Public License
 as published by the Free Software Foundation; either version 2
 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.
 You should have received a copy of the GNU General Public License
 along with this program; if not, write to the Free Software
 Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NativeWifi;
using System.Text.RegularExpressions;
using static NativeWifi.Wlan;
using System.Runtime.InteropServices;
using System.Net.Http;
using HtmlAgilityPack;

namespace SuplaUpdateTool
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string GetStringForSSID(Wlan.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }

        private Boolean isSuplaNetworkName(string ssid)
        {
            Regex r = new Regex(@"-[A-Fa-f0-9]{12}$", RegexOptions.IgnoreCase);
            return r.Match(ssid).Success && ( ssid.StartsWith("SUPLA-") || ssid.StartsWith("ZAMEL-") || ssid.StartsWith("NICE-") );
        }



        private void button1_Click(object sender, EventArgs e)
        {

            SuplaDevices devices = new SuplaDevices();
       

            WlanClient client = new WlanClient();
           
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
 
                Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in networks)
                {
                    string ssid = GetStringForSSID(network.dot11Ssid);

                    if ( isSuplaNetworkName(ssid) )
                    {
                        Console.WriteLine("Found WEP network with SSID {0}.", ssid);

                        SuplaDevice device = new SuplaDevice();
                        device.network = network;
                        device.wlanIface = wlanIface;
                        device.ssid = ssid;
                        devices.Add(device);
                    }

                }

            }


            foreach(SuplaDevice device in list)
            {
                string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>", device.ssid);

                device.wlanIface.Disconnect();
                System.Threading.Thread.Sleep(500);              
                device.wlanIface.Connect(Wlan.WlanConnectionMode.TemporaryProfile, Wlan.Dot11BssType.Any, IntPtr.Zero, profileXml);

                bool Connected = false;
                DateTime now = new DateTime();
   
                while(true)
                {
                    
                    System.Threading.Thread.Sleep(1000);

                    try
                    {
                        if (device.wlanIface.CurrentConnection.profileName.Equals(device.ssid)
                            && device.wlanIface.CurrentConnection.isState == WlanInterfaceState.Connected )
                        {
                            Connected = true;
                            break;
                        }

                        if ((now - new DateTime()).TotalSeconds > 5)
                        {
                            break;
                        }

                    } catch(Win32Exception exception) { };
                    
                }

                Console.WriteLine("Connected {0}.", Connected);

                if ( Connected )
                {
                    HttpClient httpclient = new HttpClient();
                    Task<string> task = httpclient.GetStringAsync("http://192.168.4.1");
                    task.Wait();

                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(task.Result);

                    if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
                    {
                        throw new Exception("Html parse error");
                    }

                    if (htmlDoc.DocumentNode == null )
                    {
                        throw new Exception("DocumentNode is null");
                    }

                    IEnumerable<HtmlNode> inputs = htmlDoc.DocumentNode.Descendants("input");

                    if (inputs == null)
                    {
                        throw new Exception("Unsupported device");
                    }

                    IEnumerable<HtmlNode> selects = htmlDoc.DocumentNode.Descendants("select");

                    var upd = selects == null ? null : selects.Where(n => n.Attributes["name"] != null && n.Attributes["name"].Value == "upd");

                    if (upd != null && upd.Count() != 1 )
                    {
                        throw new Exception("No update capabilities");
                    }

                    var sid = inputs.Where(n => n.Attributes["name"] != null && n.Attributes["name"].Value == "sid");
                    var wpw = inputs.Where(n => n.Attributes["name"] != null && n.Attributes["name"].Value == "wpw");
                    var svr = inputs.Where(n => n.Attributes["name"] != null && n.Attributes["name"].Value == "svr");

                    string pattern = @"<h1>(.*)<\/h1><span>LAST STATE: (.*)<br>Firmware: (.*)<br>GUID: (.*)<br>MAC: (.*)<\/span>";
                    Match match = Regex.Match(task.Result, pattern);

                    if ( match.Groups.Count != 6 || sid.Count() != 1 || wpw.Count() != 1 || svr.Count() != 1 )
                    {
                        throw new Exception("Unsupported device");
                    }
                    
                    device.name = match.Groups[1].Value.Trim();
                    device.lastState = match.Groups[2].Value.Trim();
                    device.firmware = match.Groups[3].Value.Trim();
                    device.guid = match.Groups[4].Value.Trim();
                    device.mac = match.Groups[5].Value.Trim();



                }


            }

        }
    }
}
