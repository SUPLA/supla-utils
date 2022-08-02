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
import os
import re
import logging

logger = logging.getLogger('ios')

class IOSScanner(Scanner):
    def scan(self):
        logger.info('scan begin')
        for root,dirs,files in os.walk(self.base_path):
            for p in files:
                self._source_walk(PurePath(root, p))
        for root,dirs,files in os.walk(self.base_path):
            for p in files:
                self._resource_walk(PurePath(root, p))

    def _source_walk(self, p):
        if p.suffix == '.m' or p.suffix == '.swift':
            self._parse_source(p)

    def _resource_walk(self, p):
        if p.suffix == '.strings':
            self._parse_resource(p)

    def _parse_source(self, p):
        f = open(p, 'r')
        pattern = re.compile('NSLocalizedString\(@?"(([^"\\\]|\\\.)*)"')
        for l in f:
            match = pattern.search(l)
            if match:
                self._add_nslocalized_string(self.dequote(match.group(1)));
        f.close()
        
    def dequote(self, s):
        return s.replace('\"', '"')

    def _parse_resource(self, p):
        ppd = p.parent
        ln = ppd.stem
#        pat1 = re.compile('"(([^"\\\]|\\\.)*)" = "(([^"\\\]|\\\.)*)"; ObjectID = "([^"]*)";')
        pat1 = re.compile('"(([^"\\\]|\\\.)*)"; ObjectID = "([^"]*)";')
        pattern = re.compile('"(([^"\\\]|\\\.)*)" = "(([^"\\\]|\\\.)*)"')
        f = open(p, 'r')
        last_id = ''
        last_txt = ''
        for l in f:
            m = pat1.search(l)
            if m:
                last_id = m.group(2)
                last_txt = m.group(1)
            match = pattern.match(l)
            if match:
                self._add_nslocalized_string(self.dequote(match.group(1)), 'Base', last_txt)
                self._add_nslocalized_string(self.dequote(match.group(1)), ln, 
                                             self.dequote(match.group(3)))
        f.close()

    def _add_nslocalized_string(self, key, lang = None, value = None):
        for a in self.assets():
            if a.ios_key == key:
                if lang:
                    a.ios_translations[lang] = value
                return
            if a.android_translations.get("", None) == key:
                a.ios_key == key
                if lang:
                    a.ios_translations[lang] = value
                return
        
        a = self.make_asset()
        a.ios_key = key
        if lang:
            a.ios_translations[lang] = value
