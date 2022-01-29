# Copyright (C) AC SOFTWARE SP. Z O.O.
#
# This program is free software; you can redistribute it and/or
# modify it under the terms of the GNU General Public License
# as published by the Free Software Foundation; either version 2
# of the License, or (at your option) any later version.

# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.

# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

from pathlib import PurePath
from openpyxl import Workbook

class Reporter:
    def __init__(self, assets, mode):
        self._assets = assets
        self._mode = mode
        if mode == 'ios':
            self._reference_lang = 'pl'
        else:
            self._reference_lang = 'pl'

    def generate(self, out):
        wb = Workbook()
        langs = set()
        for a in self._assets:
            langs |= set(a.ios_translations.keys())
            langs |= set(a.android_translations.keys())
        for l in sorted(langs):
            if l == self._reference_lang:
                continue
            if l:
                title = l
            else:
                title = 'en'
                
            ws = wb.create_sheet(title)
            ws.append(["Key", "English", "Translation"])
            for a in self._assets:
                if self._mode == 'ios':
                    if(a.ios_translations.get(self._reference_lang, None) and
                       a.ios_translations.get(l, None) == None):
                        ws.append([a.ios_key, a.ios_translations.get('Base', ''),  ''])
                elif self._mode == 'android':
                    if(a.android_translations.get(self._reference_lang, None) and 
                       a.android_translations.get(l, None) == None):
                        ws.append([a.android_key, a.android_translations.get('', ''), ''])

        wb.save(out)
