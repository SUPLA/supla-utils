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

from .scanner import Scanner
from pathlib import PurePath
from xml.etree import ElementTree
import os
import logging

logger = logging.getLogger('android')

class AndroidScanner(Scanner):
    def scan(self):
        logger.info('scan begin')
        for root,dirs,files in os.walk(self.base_path):
            for p in files:
                self._file_walk(PurePath(root, p))

    def _file_walk(self, p):
        if p.name == 'strings.xml':
            self._parse_file(p)

    def _get_lang(self, dirname):
        if dirname.startswith('values'):
            (_, _, sfx) = dirname.partition('-')
            return sfx
        else:
            raise ValueError

    def _add_asset(self, lang: str, name: str, val: str):
        for a in self.assets():
            if a.android_key == name:
                a.android_translations[lang] = val
                return
            elif (lang == "" and ((val == a.ios_key) or
                  (val == a.ios_translations.get('pl', None)))):
                a.android_key = name
                a.android_translations[lang] = val
                return
        a = self.make_asset()
        a.android_key = name
        a.android_translations[lang] = val

    def _parse_file(self, path: PurePath):
        lang = self._get_lang(path.parent.name)
        doc = ElementTree.parse(path)
        root = doc.getroot()
        for res in root.findall('./string'):
            rname = res.attrib['name']
            if res.text != None:
                rval = res.text
                self._add_asset(lang, rname, rval)
