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

namespace SuplaUpdateTool
{

    class FormField
    {
        public string name;
        public string value;
    }

    class FormFields : IEnumerable
    {
        private ArrayList fields = new ArrayList();

        public FormField this[int index]
        {
            get { return (FormField)fields[index]; }
            set { fields.Insert(index, value); }
        }

        public IEnumerator GetEnumerator()
        {
            return fields.GetEnumerator();
        }

        public int Add(FormField field)
        {
            return fields.Add(field);
        }
    }

    class SuplaDevice
    {
        private FormFields fields { get; }

        public WlanClient.WlanInterface wlanIface;
        public Wlan.WlanAvailableNetwork network;
        public string ssid;

        public string name;
        public string lastState;
        public string firmware;
        public string guid;
        public string mac;

        public Boolean authByEmail;

        SuplaDevice()
        {
            fields = new FormFields();
        }

    }

    class SuplaDevices : IEnumerable
    {
        private ArrayList devices = new ArrayList();

        public SuplaDevice this[int index]
        {
            get { return (SuplaDevice)devices[index]; }
            set { devices.Insert(index, value); }
        }

        public IEnumerator GetEnumerator()
        {
            return devices.GetEnumerator();
        }

        public int Add(SuplaDevice device)
        {
            return devices.Add(device);
        }
    }
}
