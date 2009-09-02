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

    regionsURI = '%sadmin/regions/' % options.opensim

    try:
        req = urllib2.Request(regionsURI)
        req.add_header('Accept', 'text/xml')
        regionsXml = ET.parse(urllib2.urlopen(req)).getroot()

        regions = regionsXml.getchildren()
        if len(regions) > 0:
            sys.exit(0)

    except urllib2.URLError:
        pass
    
    sys.exit(1)
