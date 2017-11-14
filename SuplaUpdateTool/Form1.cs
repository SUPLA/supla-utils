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
using System.Threading;
using Microsoft.Win32;


namespace SuplaUpdateTool
{

    public partial class MainForm : Form
    {

        private List<SuplaDevice> devices = new List<SuplaDevice>();
        private CancellationTokenSource cancelationSource = null;
        private readonly SynchronizationContext synchronizationContext;

        const string regKeyName = @"HKEY_CURRENT_USER\Software\SUPLA.ORG\UpdateTool\Settings";

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            synchronizationContext = SynchronizationContext.Current;
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


        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            cancelTasks();
        }

        private void dataGridUpdate()
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                dataGridView1.DataSource = null;
                dataGridView1.DataSource = devices;

            }), null);
        }

        private int doUpdate(CancellationToken cancellatonToken)
        {


            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                foreach (SuplaDevice device in devices)
                {
                    device.State = "Waiting for connect...";
                }

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = devices;

            }), null);

            foreach (SuplaDevice device in devices)
            {

                try
                {

                    string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>", device.SSID);

                    device.WlanIface.Disconnect();
                    System.Threading.Thread.Sleep(500);
                    device.WlanIface.Connect(Wlan.WlanConnectionMode.TemporaryProfile, Wlan.Dot11BssType.Any, IntPtr.Zero, profileXml);

                    Boolean Connected = false;

                    DateTime now = new DateTime();
                    while (true)
                    {

                        cancellatonToken.ThrowIfCancellationRequested();

                        System.Threading.Thread.Sleep(1000);

                        try
                        {
                            if (device.WlanIface.CurrentConnection.profileName.Equals(device.SSID)
                                && device.WlanIface.CurrentConnection.isState == WlanInterfaceState.Connected)
                            {
                                Connected = true;
                                break;
                            }

                            if ((now - new DateTime()).TotalSeconds > 5)
                            {
                                break;
                            }
                        } catch (Win32Exception exception) { };

                    }

                    if ( !Connected )
                    {
                        throw new Exception("Connection timeout");
                    }

                    HttpClient httpclient = new HttpClient();
                    Task<string> task = httpclient.GetStringAsync("http://192.168.4.1");
                    task.Wait();

                    HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(task.Result);

                    if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Count() > 0)
                    {
                        throw new Exception("Html parse error");
                    }

                    if (htmlDoc.DocumentNode == null)
                    {
                        throw new Exception("DocumentNode is null");
                    }

                    IEnumerable<HtmlNode> inputs = htmlDoc.DocumentNode.Descendants("input");

                    if (inputs == null)
                    {
                        throw new Exception("Unsupported device");
                    }

                    foreach (HtmlNode input in inputs)
                    {
                        FormField field = new FormField();
                        field.name = input.Attributes["name"] == null ? "" : input.Attributes["name"].Value;
                        field.value = input.Attributes["value"] == null ? "" : input.Attributes["value"].Value;
                        device.Fields.Add(field);
                    }

                    IEnumerable<HtmlNode> selects = htmlDoc.DocumentNode.Descendants("select");

                    foreach (HtmlNode select in selects)
                    {
                        FormField field = new FormField();
                        field.name = select.Attributes["name"] == null ? "" : select.Attributes["name"].Value;

                        IEnumerable<HtmlNode> options = select.Descendants("option").Where(n => n.Attributes["selected"] != null);

                        if (options != null && options.Count() == 1)
                        {
                            HtmlNode option = options.First();
                            field.value = option.Attributes["value"] == null ? "" : option.Attributes["value"].Value;
                        }

                        device.Fields.Add(field);

                    }

                    var upd = device.Fields.Where(n => n.name == "upd");

                    if (upd != null && upd.Count() != 1)
                    {
                        throw new Exception("No update capabilities");
                    }
                    
                    var sid = device.Fields.Where(n => n.name == "sid");
                    var wpw = device.Fields.Where(n => n.name == "wpw");
                    var svr = device.Fields.Where(n => n.name == "svr");

                    string pattern = @"<h1>(.*)<\/h1><span>LAST STATE: (.*)<br>Firmware: (.*)<br>GUID: (.*)<br>MAC: (.*)<\/span>";
                    Match match = Regex.Match(task.Result, pattern);

                    if (match.Groups.Count != 6 || sid.Count() != 1 || wpw.Count() != 1 || svr.Count() != 1)
                    {
                        throw new Exception("Unsupported device");
                    }


                    synchronizationContext.Post(new SendOrPostCallback(o =>
                    {

                        device.Name = match.Groups[1].Value.Trim();
                        device.LastDeviceState = match.Groups[2].Value.Trim();
                        device.Firmware = match.Groups[3].Value.Trim();
                        device.GUID = match.Groups[4].Value.Trim();
                        device.MAC = match.Groups[5].Value.Trim();

                        device.State = "Waiting for restart...";

                    }), null);

                    device.WlanIface.Disconnect();

                    cancellatonToken.ThrowIfCancellationRequested();

                }
                catch (Exception exception)
                {
                    device.State = exception.Message;
                }


                synchronizationContext.Post(new SendOrPostCallback(o =>
                {

                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = devices;

                }), null);


            }

            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                btnStop.Enabled = false;
                btnFind.Enabled = true;
            }), null);


            return 1;
        }

        private int findDevices(CancellationToken cancellationToken)
        {

            synchronizationContext.Post(new SendOrPostCallback(o =>
            {

                devices.Clear();
                dataGridView1.DataSource = null;

            }), null);


            WlanClient client = new WlanClient();

            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {

                Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in networks)
                {
                    string ssid = GetStringForSSID(network.dot11Ssid);

                    if (isSuplaNetworkName(ssid))
                    {

                        if (cancellationToken.IsCancellationRequested) break;

                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            SuplaDevice device = new SuplaDevice();
                            device.Network = network;
                            device.WlanIface = wlanIface;
                            device.SSID = ssid;

                            devices.Add(device);

                        }), null);

                    }

                }

                
            }

            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                dataGridView1.DataSource = devices;
                btnFind.Enabled = true;
                btnUpdate.Enabled = devices.Count() > 0;
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripStatusLabel1.Text = "Device count: " + devices.Count().ToString();
                btnStop.Enabled = false;
            }), null);

            return 1;
        }

        private void cancelTasks()
        {
            if (cancelationSource != null)
            {
                cancelationSource.Cancel();
            }

            cancelationSource = new CancellationTokenSource();
        }



        private void btnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            btnFind.Enabled = false;
            btnStop.Enabled = true;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            Task<int> task = Task.Run(() => doUpdate(cancelationSource.Token), cancelationSource.Token);
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            btnFind.Enabled = false;
            btnStop.Enabled = true;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            cancelTasks();
            Task<int> task = Task.Run(() => findDevices(cancelationSource.Token), cancelationSource.Token);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

   
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 form = new Form2();
            form.ShowDialog();
        }

        private void settings_Changed(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnSave.Enabled = true;
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            string group = @"\WiFi";
            edWifiName.Text = (string)Registry.GetValue(regKeyName + group, "SSID", "");

            string pwd = (string)Registry.GetValue(regKeyName + group, "PWD", "");
            if ( pwd == null )
            {
                edWiFiPwd.Text = "";
            } 
            else
            {
                edWiFiPwd.Text = Encoding.UTF8.GetString(Convert.FromBase64String(pwd));
            }

            group = @"\Supla";

            edSuplaServer.Text = (string)Registry.GetValue(regKeyName + group, "Server", "");
            edSuplaEmail.Text = (string)Registry.GetValue(regKeyName + group, "Email", "");
            edSuplaLocationId.Text = (string)Registry.GetValue(regKeyName + group, "LocationId", "");

            pwd = (string)Registry.GetValue(regKeyName + group, "LocationPwd", "");
            if (pwd == null)
            {
                edSuplaLocationPwd.Text = "";
            }
            else
            {
                edSuplaLocationPwd.Text = Encoding.UTF8.GetString(Convert.FromBase64String(pwd));
            }

            btnSave.Enabled = false;
            btnUndo.Enabled = false;
        }


        private void btnSave_Click(object sender, EventArgs e)
        {

            btnSave.Enabled = false;
            btnUndo.Enabled = false;

            string group = @"\WiFi";
            
            Registry.SetValue(regKeyName+group, "SSID", edWifiName.Text);
            Registry.SetValue(regKeyName +group, "PWD", Convert.ToBase64String(Encoding.UTF8.GetBytes(edWiFiPwd.Text)));

            group = @"\Supla";

            Registry.SetValue(regKeyName + group, "Server", edSuplaServer.Text);
            Registry.SetValue(regKeyName + group, "Email", edSuplaEmail.Text);
            Registry.SetValue(regKeyName + group, "LocationId", edSuplaLocationId.Text);
            Registry.SetValue(regKeyName + group, "LocationPwd", Convert.ToBase64String(Encoding.UTF8.GetBytes(edSuplaLocationPwd.Text)));
        }
    }
}
