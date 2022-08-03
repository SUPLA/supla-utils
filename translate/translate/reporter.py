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
import logging

logger = logging.getLogger('reporter')

class Reporter:
    def __init__(self, assets, mode):
        self._assets = assets
        self._mode = mode
        self._with_ios_keys = True ## TODO: parametrize
        if mode == 'ios':
            self._reference_lang = 'pl'
        else:
            self._reference_lang = 'pl'

    def generate(self, out):
        logger.info('generate begin')
        wb = Workbook()
        langs = set()
        for a in self._assets:
            langs |= set(a.ios_translations.keys())
            langs |= set(a.android_translations.keys())

        for l in sorted(langs):
            base_texts = list()
            if l == self._reference_lang or l == 'Base':
                continue
            if l:
                title = l
            else:
                continue;
                
            ws = wb.create_sheet(title)
            logger.info("Start processing language %s", title)
            cols = list()
            if self._with_ios_keys:
                cols.append("iOS key")
            cols.append("Base")
            cols.append("Translation")
            ws.append(cols)
            for a in self._assets:
                
                android_trans = a.android_translations.get(l, None)
                ios_trans = a.ios_translations.get(l, None)
                ios_ref = a.ios_translations.get(self._reference_lang, None)
                and_ref = a.android_translations.get(self._reference_lang, None)
                ios_en = a.ios_translations.get('Base', a.ios_translations.get('en', ''))
                if ios_en == '':
                    ios_en = a.ios_key
                and_en = a.android_translations.get('en', None)
                
                logger.debug("asset %s/%s has reference translation: %s/%s",
                             a.ios_key, a.android_key, ios_ref, and_ref)
                logger.debug("asset %s/%s has en translation: %s/%s",
                             a.ios_key, a.android_key, ios_en, and_en)
                logger.debug("asset %s/%s has %s translation: %s/%s",
                             a.ios_key, a.android_key, l, ios_trans, android_trans)
                if(not ios_ref and not and_ref):
                    logger.info("skipping [%s/%s] because it's not translated to reference language", a.ios_key, a.android_key)
                    continue

                if(ios_trans or android_trans):
                    logger.info("skipping [%s/%s] because it's already translated", a.ios_key, a.android_key)
                    continue

                if ((a.ios_translations.get(self._reference_lang, "") ==
                    ios_en) or
                    (a.android_translations.get(self._reference_lang, "") ==
                    and_en)):
                    logger.info("skipping [%s/%s] because translation looks same as original")
                    continue

                logger.debug("working on %s/%s", a.ios_key, a.android_key)
                if(not android_trans or not ios_trans):
                    base_text = ios_ref
                    if (not base_text) or (len(base_text) == 0):
                        base_text = and_ref

                    if not (base_text in base_texts):
                        data = list()
                        if self._with_ios_keys:
                            data.append(a.ios_key)
                        data.append(base_text)
                        data.append('')
                        ws.append(data)
                        base_texts.append(base_text)

        wb.save(out)
