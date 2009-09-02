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

    (options, args) = parser.parse_args()

    if not options.opensim:
        parser.error('option --server|-s is required')
    if not options.passwd:
        parser.error('option --password|-p is required')

    try:
        opensim = xmlrpclib.Server(options.opensim)

        for countDown in range(0, 10):
            opensim.admin_broadcast(dict(password = options.passwd,
                                         message = 'Shutting down grid for maintenance in %d secs. Please log off now.' % (10 - countDown)))
            time.sleep(1)
        
        res = opensim.admin_shutdown(dict(password = options.passwd))
        if 'success' in res and res['success'] == 'true':
            sys.exit(0)
    except xmlrpclib.Error:
        pass
    except socket.error:
        pass

    sys.exit(1)
