using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Woopsa
{
    public class TlsProcessor : PreRouteProcessor
    {
        public TlsProcessor(string certificateLocation, string certificatePassword)
        {
            _certificate = new X509Certificate2(certificateLocation, certificatePassword);
        }

        public override Stream StartProcessStream(Stream input)
        {
            SslStream secureStream = new SslStream(input, true);
            try
            {
                secureStream.AuthenticateAsServer(_certificate, false, System.Security.Authentication.SslProtocols.Tls, true);
            }
            catch (IOException) 
            {
                return null;
            }
            return secureStream;
        }

        public override void EndProcessStream(Stream input)
        {
            (input as SslStream).Close();
        }

        private X509Certificate2 _certificate;
    }
}
