#!/usr/bin/python
# -*- encoding: utf-8 -*-

import optparse
import socket
import sys
import time
import xmlrpclib

options = None

if __name__ == '__main__':
    socket.setdefaulttimeout(30)

    parser = optparse.OptionParser()
    parser.add_option('-s', '--server', dest = 'opensim', help = 'opensim server (URL)', metavar = 'SERVER')
    parser.add_option('-p', '--password', dest = 'passwd', help = 'opensim server password', metavar = 'PASSWORD')
    parser.add_option('-o', '--oar', dest = 'oar', help = 'OAR file to load', metavar = 'OAR')
    parser.add_option('-r', '--region', dest = 'region', help = 'target region', metavar = 'REGION')

    (options, args) = parser.parse_args()

    if not options.opensim:
        parser.error('option --server|-s is required')
    if not options.passwd:
        parser.error('option --password|-p is required')
    if not options.oar:
        parser.error('option --oar|-o is required')
    if not options.region:
        parser.error('option --region|-r is required')

    try:
        opensim = xmlrpclib.Server(options.opensim)

        for countDown in range(0, 10):
            opensim.admin_broadcast(dict(password = options.passwd,
                                         message = 'This is your captain speaking: region "%s" will be updated in %d sec, hold on tight if you are in that area, earthquakes will occur' % (options.region, 10 - countDown)))
            time.sleep(1)
        
        res = opensim.admin_load_oar(dict(password = options.passwd, region_name = options.region,
                                          filename = options.oar))
        if 'loaded' in res and res['loaded'] == 'true':
            sys.exit(0)
    except xmlrpclib.Error:
        pass
    except socket.error:
        pass

    sys.exit(1)
