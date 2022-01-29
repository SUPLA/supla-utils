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

from abc import (ABCMeta, abstractmethod)
from pathlib import PurePosixPath

from .asset import Asset

class Scanner(metaclass=ABCMeta):

    def __init__(self, base_path: PurePosixPath):
        self.base_path = base_path
        self._assets = list()

    
    @abstractmethod
    def scan(self):
        pass

    def set_assets(self, assets):
        self._assets = assets

    def assets(self):
        return self._assets

    def make_asset(self):
        a = Asset(ios_key=None,
                  android_key=None,
                  ios_translations=dict(), 
                  android_translations=dict())
        self._assets.append(a)
        return a
