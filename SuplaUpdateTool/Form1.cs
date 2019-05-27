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
using System.Net;

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

        private async Task<HttpResponseMessage> postAsync(SuplaDevice device, HttpClient httpclient)
        {
            IEnumerable<KeyValuePair<string, string>> list = device.Fields.Select((s, i) => new KeyValuePair<string, string>(s.name, s.value));
            var content = new FormUrlEncodedContent(list);

            HttpResponseMessage responseMessage = null;
            try
            {
                responseMessage = await httpclient.PostAsync("http://192.168.4.1", content);
            }
            catch (Exception ex)
            {
                responseMessage = null;
            }
            return responseMessage;
        }


        private int checkAndClean(CancellationToken cancellationToken, bool doNotClean)
        {

            foreach (SuplaDevice device in devices)
                if (!device.Clean)
                {
                    device.State = "Setting the connection...";
                }

            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                dataGridView1.DataSource = null;
                dataGridView1.DataSource = devices;

            }), null);

            int updateCount = 0;
            int cleanCount = 0;

            foreach (SuplaDevice device in devices)
            {

                if ( !device.Clean )
                try
                {

                    device.wifiConnect();

                    if (!device.wifiWait(cancellationToken))
                    {
                        throw new Exception("Connection timeout");
                    }

                    device.Clean = true;
                    string OldFirmware = device.Firmware;

                    device.Load();

                    device.NewFirmware = device.Firmware;
                    device.Firmware = OldFirmware;

                        if (doNotClean)
                        {
                            if (device.Firmware.Equals(device.NewFirmware))
                            {
                                device.State = "!Not updated";
                            }
                            else
                            {
                                device.Updated = true;
                                device.State = "Updated";
                            }

                        } else
                        {
                            device.Fields.Clear();

                            device.Fields.Add(new FormField() { name = "sid", value = "" });
                            device.Fields.Add(new FormField() { name = "wpw", value = "" });
                            device.Fields.Add(new FormField() { name = "svr", value = "" });
                            device.Fields.Add(new FormField() { name = "eml", value = "" });
                            device.Fields.Add(new FormField() { name = "lid", value = "0" });
                            device.Fields.Add(new FormField() { name = "lid", value = "pwd" });

                            string result = device.postFields();

                            device.WlanIface.Disconnect();

                            if (result.IndexOf("Data saved", StringComparison.CurrentCultureIgnoreCase) >= 0)
                            {
                                if (device.Firmware.Equals(device.NewFirmware))
                                {
                                    device.State = "!Not updated but cleaned";
                                }
                                else
                                {
                                    device.Updated = true;
                                    device.State = "Updated and cleaned";
                                }

                            }
                            else
                            {
                                throw new Exception("Device configuration cannot be cleaned!");
                            }
                        }

                 
                
                    cancellationToken.ThrowIfCancellationRequested();

                }
                catch (Exception exception)
                {
                    device.Clean = false;
                    device.State = exception.Message;
                }

                if ( device.Updated )
                {
                    updateCount++;
                }

                if ( device.Clean )
                {
                    cleanCount++;
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
                btnCheckAndClean.Enabled = cleanCount != devices.Count();
                btnCheck.Enabled = btnCheckAndClean.Enabled;
                toolStripStatusLabel1.Text = "Result: " + updateCount.ToString() + "/" + devices.Count().ToString();

            }), null);

            return 1;
        }

        private int doUpdate(CancellationToken cancellationToken)
        {
            foreach (SuplaDevice device in devices)
                if (!device.TryConfig)
                {
                    device.State = "Setting the connection...";
                }

            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                dataGridView1.DataSource = null;
                dataGridView1.DataSource = devices;

            }), null);

            Boolean configSaved = false;

            foreach (SuplaDevice device in devices)
            {
                if ( !device.TryConfig )
                try
                {

                    device.wifiConnect();

                    if (!device.wifiWait(cancellationToken))
                    {
                        throw new Exception("Connection timeout");
                    }

                    device.TryConfig = true;

                    device.Load();

                    var upd = device.Fields.Where(n => n.name == "upd");
                    var sid = device.Fields.Where(n => n.name == "sid");
                    var wpw = device.Fields.Where(n => n.name == "wpw");
                    var svr = device.Fields.Where(n => n.name == "svr");

                    upd.First().value = "1";
                    sid.First().value = edWifiName.Text.Trim();
                    wpw.First().value = edWiFiPwd.Text.Trim();
                    svr.First().value = edSuplaServer.Text.Trim();

                    var eml = device.Fields.Where(n => n.name == "eml");
                    var lid = device.Fields.Where(n => n.name == "lid");
                    var pwd = device.Fields.Where(n => n.name == "pwd");

                    if (eml != null && eml.Count() == 1)
                    {
                        eml.First().value = edSuplaEmail.Text.Trim();
                    }
                    else if (lid != null && lid.Count() == 1 && pwd != null && pwd.Count() == 1)
                    {
                        lid.First().value = edSuplaLocationId.Text.Trim();
                        pwd.First().value = edSuplaLocationPwd.Text.Trim();
                    }
                    else
                    {
                        throw new Exception("Unsupported device");
                    }

                    string result = device.postFields();

                    device.WlanIface.Disconnect();

                    if (result.IndexOf("Data saved", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    {
                        device.State = "Please restart and update";
                        configSaved = true;
                    }
                    else
                    {
                        throw new Exception("Device configuration cannot be saved!");
                    }


                    cancellationToken.ThrowIfCancellationRequested();

                }
                catch (Exception exception)
                {
                    device.TryConfig = false;
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
                btnUpdate.Enabled = true;
                btnFind.Enabled = true;
                btnCheckAndClean.Enabled = configSaved;
                btnCheck.Enabled = btnCheckAndClean.Enabled;
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
                wlanIface.Disconnect();
                wlanIface.Scan();
            }

            System.Threading.Thread.Sleep(2000);

            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {

                Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in networks)
                {
                    string ssid = SuplaDevice.GetStringForSSID(network.dot11Ssid);

                    if (SuplaDevice.isSuplaNetworkName(ssid))
                    {

                        if (cancellationToken.IsCancellationRequested) break;

                        synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            IEnumerable<SuplaDevice> d = devices.Where(n => n.SSID.Equals(ssid));
                            if ( d == null || d.Count() == 0 )
                            {
                                SuplaDevice device = new SuplaDevice();
                                device.Network = network;
                                device.WlanIface = wlanIface;
                                device.SSID = ssid;

                                devices.Add(device);
                            }


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

        private void btnCheckAndClean_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            btnFind.Enabled = false;
            btnStop.Enabled = true;
            btnCheckAndClean.Enabled = false;
            btnCheck.Enabled = btnCheckAndClean.Enabled;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            Task<int> task = Task.Run(() => 
            checkAndClean(cancelationSource.Token, sender == btnCheck), cancelationSource.Token);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false;
            btnFind.Enabled = false;
            btnStop.Enabled = true;
            btnCheckAndClean.Enabled = false;
            btnCheck.Enabled = btnCheckAndClean.Enabled;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            Task<int> task = Task.Run(() => doUpdate(cancelationSource.Token), cancelationSource.Token);
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            btnFind.Enabled = false;
            btnStop.Enabled = true;
            btnCheckAndClean.Enabled = false;
            btnCheck.Enabled = btnCheckAndClean.Enabled;
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

        private void btnCheck_Click(object sender, EventArgs e)
        {

        }
    }
}
