#!/usr/bin/python
# -*- encoding: utf-8 -*-

import optparse
import sys
import urllib2
import xml.etree.ElementTree as ET

options = None

if __name__ == '__main__':
    parser = optparse.OptionParser()
    parser.add_option('-s', '--server', dest = 'opensim', help = 'opensim server (URL)', metavar = 'SERVER')

    (options, args) = parser.parse_args()

    if not options.opensim:
        parser.error('option --server|-s is required')

    if not options.opensim.endswith('/'):
        options.opensim += '/'

    regionsURI = '%sadmin/regioninfo/' % options.opensim
    totalAvatars = 0

    try:
        req = urllib2.Request(regionsURI)
        req.add_header('Accept', 'text/xml')
        riXml = ET.parse(urllib2.urlopen(req)).getroot()

        totalAvatars = 0
        for region in riXml:
            totalAvatars += int(region.attrib['avatars'])

    except urllib2.URLError:
        pass

    print '%d' % totalAvatars

    sys.exit(1)
