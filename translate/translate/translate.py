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

import click
import logging

from pathlib import PurePosixPath
from .android import AndroidScanner
from .ios import IOSScanner
from .reporter import Reporter

@click.command()
@click.option("--android", help="path of android sources")
@click.option("--ios", help="path for ios sources")
@click.option("--out", help="output")
def main(android: PurePosixPath, ios: PurePosixPath, out: PurePosixPath):
    logging.basicConfig(format='%(asctime)-15s %(name)-10s %(message)s', 
                        level=logging.DEBUG)
    if android and ios:
        logging.error("simultaeous processing of iOS and Android resources is not supported")
        exit(1)
    all_assets = list()
    if android:
        logging.info("using android %s", android)
        scanner = AndroidScanner(android)
        scanner.set_assets(all_assets)
        scanner.scan()
        Reporter(all_assets, 'android').generate(out)
    if ios:
        logging.info("using ios %s", ios)
        scanner = IOSScanner(ios)
        scanner.set_assets(all_assets)
        scanner.scan()
        Reporter(all_assets, 'ios').generate(out)

    
    

if __name__ == "__main__":
    main()
