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
                dir_ = PurePath(root)
                if dir_.name.startswith('values'):                    
                    self._file_walk(PurePath(dir_.parent, PurePath('values'), p))
                self._file_walk(PurePath(root, p))

    def _file_walk(self, p):
        if p.name == 'strings.xml':
            self._parse_file(p)

    def _get_lang(self, dirname):
        if dirname.startswith('values'):
            (_, _, sfx) = dirname.partition('-')
            if sfx == '':
                sfx = 'en'
            return sfx
        else:
            raise ValueError

    def _add_asset(self, lang: str, name: str, val: str):
        logger.debug("add asset %s[%s] = %s", name, lang, val)
        done = False
        for a in self.assets():
            if a.android_key == name:
                logger.debug("found existing asset with key %s", name)
                a.android_translations[lang] = val
                done = True
            elif (lang == "en" and (val == a.ios_key or val == a.ios_translations.get('Base', None))):
                logger.debug("assigning %s to %s because ios string matches", name, a.android_key)
                a.android_key = name
                a.android_translations[lang] = val
                done = True
        if done:
            return
        a = self.make_asset()
        a.android_key = name
        a.android_translations[lang] = val
        logger.debug("made new asset for %s", name)

    def _parse_file(self, path: PurePath):
        logger.debug("parsing %s", path)
        lang = self._get_lang(path.parent.name)
        doc = ElementTree.parse(path)
        root = doc.getroot()
        for res in root.findall('./string'):
            rname = res.attrib['name']
            if res.text != None:
                rval = res.text
                self._add_asset(lang, rname, rval)
