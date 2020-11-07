using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ClickOnceGet.Server.Services
{
    public class CertificateValidater
    {
        private IHttpClientFactory HttpClientFactory { get; }

        public CertificateValidater(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        public async Task<string> GetSSHPubKeyStrOfGitHubAccountAsync(string gitHubAccountName)
        {
            try
            {
                var client = HttpClientFactory.CreateClient();
                return await client.GetStringAsync($"https://github.com/{gitHubAccountName}.keys");
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public bool EqualsPublicKey(string sshPubKeyString, string pathOfCertFile)
        {
            var module1 = GetModuleFromX509CertificateFile(pathOfCertFile);
            var modules2 = GetModuleFromSSHPublicKeyString(sshPubKeyString);
            return modules2.Any(module2 => Enumerable.SequenceEqual(module1, module2));
        }

        public bool EqualsPublicKey(string sshPubKeyString, X509Certificate pathOfCert)
        {
            var module1 = GetModuleFromX509CertificateFile(pathOfCert);
            var modules2 = GetModuleFromSSHPublicKeyString(sshPubKeyString);
            return modules2.Any(module2 => Enumerable.SequenceEqual(module1, module2));
        }

        // MEMO:
        // The purpose of using"IPAddress.NetworkToHostOrder()" method is changing endian from big endian to little endian.

        public IEnumerable<byte[]> GetModuleFromSSHPublicKeyString(string pubKeySSHFormat)
        {
            if (string.IsNullOrEmpty(pubKeySSHFormat)) yield break;

            // Split each rows
            var pubKeyBodies = pubKeySSHFormat
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(row => row.Trim().Split(' ', '\t').Last());

            foreach (var pubKeyBody in pubKeyBodies)
            {
                var pubKeyBin = Convert.FromBase64String(pubKeyBody);
                using var ms = new MemoryStream(pubKeyBin);
                using var binReader = new BinaryReader(ms);

                // Get byte size of algorithm name.
                var sizeOfAlgorithmName = IPAddress.NetworkToHostOrder(binReader.ReadInt32());

                // Read and drop algorithm name (generally, this is "ssh-rsa" 7 bytes).
                binReader.Read(new byte[sizeOfAlgorithmName], 0, sizeOfAlgorithmName);

                // Get byte size of exponent.
                var sizeOfExponent = IPAddress.NetworkToHostOrder(binReader.ReadInt32());

                // Read and drop exponent.
                binReader.Read(new byte[sizeOfExponent], 0, sizeOfExponent);

                // Get byte size of module.
                var sizeOfModule = IPAddress.NetworkToHostOrder(binReader.ReadInt32());
                // Read module and return it.
                var module = new byte[sizeOfModule];
                binReader.Read(module, 0, sizeOfModule);

                yield return module;
            }
        }

        public byte[] GetModuleFromX509CertificateFile(string pathOfCert)
        {
            var cert = X509Certificate.CreateFromCertFile(pathOfCert);
            return GetModuleFromX509CertificateFile(cert);
        }

        public byte[] GetModuleFromX509CertificateFile(X509Certificate cert)
        {
            var pubKeyBin = cert.GetPublicKey();

            using var ms = new MemoryStream(pubKeyBin);
            using var binReader = new BinaryReader(ms);
            for (; ; )
            {
                var signature = binReader.ReadByte();
                if (signature == 0x00) continue;

                var sizeOfBlock = (Int32)binReader.ReadByte();
                if ((sizeOfBlock & 0x80) != 0)
                {
                    var sizeOfSizeInfo = (sizeOfBlock & 0x7f);
                    var sizeInfoBuff = new byte[sizeof(Int64)];
                    binReader.Read(sizeInfoBuff, sizeInfoBuff.Length - sizeOfSizeInfo, sizeOfSizeInfo);
                    sizeOfBlock = (Int32)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(sizeInfoBuff, 0));
                }

                // sigunature is 0x02, it is module part.
                if (signature == 0x02)
                {
                    var module = new byte[sizeOfBlock];
                    binReader.Read(module, 0, sizeOfBlock);
                    return module;
                }
            }
        }
    }
}