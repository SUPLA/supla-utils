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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeWifi;
using System.Collections;
using System.Net.Http;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Threading;
using static NativeWifi.Wlan;

namespace SuplaUpdateTool
{

    class FormField
    {
        private string _name;
        public string name
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _name == null ? null : String.Copy(_name);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _name = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _value;
        public string value
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _value == null ? null : String.Copy(_value);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _value = value == null ? null : String.Copy(value);
                }
            }
        }
    }

    class SuplaDevice
    {
        public List<FormField> Fields = new List<FormField>();

        public WlanClient.WlanInterface WlanIface;
        public Wlan.WlanAvailableNetwork Network;

        private string _SSID;
        public string SSID
        {
            get
            {
                string result;
                lock(this)
                {
                    result = _SSID == null ? null : String.Copy(_SSID);
                }

                return result;
            }

            set
            {
                lock(this)
                {
                    _SSID = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _State;
        public string State
        {
            get
            {
                string result;

                lock (this)
                {
                    result = _State == null ? null : String.Copy(_State);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _State = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _Name;
        public string Name
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _Name == null ? null : String.Copy(_Name);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _Name = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _LastDeviceState;
        public string LastDeviceState
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _LastDeviceState == null ? null : String.Copy(_LastDeviceState);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _LastDeviceState = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _Firmware;
        public string Firmware
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _Firmware == null ? null : String.Copy(_Firmware);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _Firmware = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _NewFirmware;
        public string NewFirmware
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _NewFirmware == null ? null : String.Copy(_NewFirmware);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _NewFirmware = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _GUID;
        public string GUID
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _GUID == null ? null : String.Copy(_GUID);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _GUID = value == null ? null : String.Copy(value);
                }
            }
        }

        private string _MAC;
        public string MAC
        {
            get
            {
                string result;
                lock (this)
                {
                    result = _MAC == null ? null : String.Copy(_MAC);
                }

                return result;
            }

            set
            {
                lock (this)
                {
                    _MAC = value == null ? null : String.Copy(value);
                }
            }
        }

        public Boolean Clean;
        public Boolean Updated;
        public Boolean TryConfig;

        public static string GetStringForSSID(Wlan.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }

        public static Boolean isSuplaNetworkName(string ssid)
        {
            Regex r = new Regex(@"-[A-Fa-f0-9]{12}$", RegexOptions.IgnoreCase);
            return r.Match(ssid).Success && (ssid.StartsWith("SUPLA-") || ssid.StartsWith("ZAMEL-") || ssid.StartsWith("NICE-"));
        }

        public void wifiConnect()
        {
  
            if (WlanIface == null)
            {
                return;
            }

            string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>", SSID);

            WlanIface.Disconnect();
            System.Threading.Thread.Sleep(500);
            WlanIface.Connect(Wlan.WlanConnectionMode.TemporaryProfile, Wlan.Dot11BssType.Any, IntPtr.Zero, profileXml);
        }

        public Boolean wifiWait(CancellationToken cancellationToken)
        {

            if (WlanIface == null)
            {
                return false;
            }

            Boolean Connected = false;

            DateTime then = DateTime.Now;
            while (true)
            {

                cancellationToken.ThrowIfCancellationRequested();

                System.Threading.Thread.Sleep(1000);

                try
                {
                    if (WlanIface.CurrentConnection.profileName.Equals(SSID)
                        && WlanIface.CurrentConnection.isState == WlanInterfaceState.Connected)
                    {
                        Connected = true;
                        break;
                    }
                }
                catch (Win32Exception exception) { };


                if ((DateTime.Now - then).TotalSeconds > 5)
                {
                    break;
                }
            }

            return Connected;
        }

        public void Load()
        {
            Fields.Clear();

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
                Fields.Add(field);
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

                Fields.Add(field);

            }

            var upd = Fields.Where(n => n.name == "upd");

            if (upd != null && upd.Count() != 1)
            {
                throw new Exception("No update capabilities");
            }
       
            var sid = Fields.Where(n => n.name == "sid");
            var wpw = Fields.Where(n => n.name == "wpw");
            var svr = Fields.Where(n => n.name == "svr");

            string pattern = @"<h1>(.*)<\/h1><span>LAST STATE: (.*)<br>Firmware: (.*)<br>GUID: (.*)<br>MAC: (.*)<\/span>";
            Match match = Regex.Match(task.Result, pattern);

            if (match.Groups.Count != 6 || sid.Count() != 1 || wpw.Count() != 1 || svr.Count() != 1)
            {
                throw new Exception("Unsupported device");
            }

            Name = match.Groups[1].Value.Trim();
            LastDeviceState = match.Groups[2].Value.Trim();
            Firmware = match.Groups[3].Value.Trim();
            GUID = match.Groups[4].Value.Trim();
            MAC = match.Groups[5].Value.Trim();
        }

        public string postFields()
        {
            IEnumerable<KeyValuePair<string, string>> list = Fields.Select((s, i) => new KeyValuePair<string, string>(s.name, s.value));
            HttpContent content = new FormUrlEncodedContent(list);

            HttpClient httpclient = new HttpClient();
            Task<HttpResponseMessage> response = httpclient.PostAsync("http://192.168.4.1", content);
            response.Wait();

            Task<string> response_string = response.Result.Content.ReadAsStringAsync();
            response_string.Wait();

            return response_string.Result;
        }

    }

}
