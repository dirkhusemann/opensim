/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Specialized;
using System.Net;
using System.IO;
using System.Text;

namespace OpenSim.Framework.Servers
{
    public class OSHttpRequest
    {
        private string[] _acceptTypes;
        private Encoding _contentEncoding;
        private long _contentLength64;
        private string _contentType;
        private CookieCollection _cookies;
        private NameValueCollection _headers;
        private string _httpMethod;
        private Stream _inputStream;
        private bool _isSecureConnection;
        private bool _isAuthenticated;
        private bool _keepAlive;
        private bool _hasbody;
        private string _rawUrl;
        private Uri _url;
        private NameValueCollection _queryString;
        private string _userAgent;

        public string[] AcceptTypes
        {
            get { return _acceptTypes; }
        }

        public Encoding ContentEncoding
        {
            get { return _contentEncoding; }
        }

        public long ContentLength
        {
            get { return _contentLength64; }
        }

        public long ContentLength64
        {
            get { return _contentLength64; }
        }

        public string ContentType
        {
            get { return _contentType; }
        }

        public CookieCollection Cookies
        {
            get { return _cookies; }
        }

        public NameValueCollection Headers
        {
            get { return _headers; }
        }

        public string HttpMethod
        {
            get { return _httpMethod; }
        }

        public Stream InputStream
        {
            get { return _inputStream; }
        }

        public bool IsSecureConnection
        {
            get { return _isSecureConnection; }
        }

        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
        }

        public bool HasEntityBody
        {
            get { return _hasbody; }
        }

        public bool KeepAlive
        {
            get { return _keepAlive; }
        }

        public string RawUrl
        {
            get { return _rawUrl; }
        }

        public Uri Url
        {
            get { return _url; }
        }

        public string UserAgent
        {
            get { return _userAgent; }
        }

        public NameValueCollection QueryString
        {
            get { return _queryString; }
        }

        public OSHttpRequest()
        {
        }

        public OSHttpRequest(HttpListenerRequest req)
        {
            _acceptTypes = req.AcceptTypes;
            _contentEncoding = req.ContentEncoding;
            _contentLength64 = req.ContentLength64;
            _contentType = req.ContentType;
            _cookies = req.Cookies;
            _headers = req.Headers;
            _httpMethod = req.HttpMethod;
            _hasbody = req.HasEntityBody;
            _inputStream = req.InputStream;
            _isSecureConnection = req.IsSecureConnection;
            _isAuthenticated = req.IsAuthenticated;
            _keepAlive = req.KeepAlive;
            _rawUrl = req.RawUrl;
            _url = req.Url;
            _queryString = req.QueryString;
            _userAgent = req.UserAgent;
        }
    }
}
