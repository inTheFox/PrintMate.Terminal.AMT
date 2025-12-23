//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Opc.Ua;
//using System;
//using System.IO;
//using System.Security.Cryptography.X509Certificates;


//namespace Opc2Lib
//{

//    public class CertificateGenerator
//    {
//        public static X509Certificate2 CreateSelfSignedCertificate(
//            string applicationUri,
//            string applicationName,
//            string commonName,
//            string organization,
//            string organizationalUnit,
//            string locality,
//            string state,
//            string country,
//            string[] dnsNames,
//            string[] ipAddresses)
//        {
//            // Создаем сертификат с помощью CertificateFactory
//            var certificateBuilder = CertificateFactory.CreateCertificate(
//                applicationUri,
//                applicationName,
//                commonName, // Common Name (CN)
//                null); // Subject Alternative Name будет добавлен далее

//            // Настраиваем дополнительные поля сертификата (если поддерживается вашей версией библиотеки)
//            certificateBuilder.Organization = organization;
//            certificateBuilder.OrganizationalUnit = organizationalUnit;
//            certificateBuilder.Locality = locality;
//            certificateBuilder.State = state;
//            certificateBuilder.Country = country;

//            // Добавляем DNS-имена и IP-адреса как Subject Alternative Names (SAN)
//            if (dnsNames != null && dnsNames.Length > 0)
//            {
//                foreach (var dnsName in dnsNames)
//                {
//                    certificateBuilder.AddDomainName(dnsName);
//                }
//            }

//            if (ipAddresses != null && ipAddresses.Length > 0)
//            {
//                foreach (var ipAddress in ipAddresses)
//                {
//                    certificateBuilder.AddIPAddress(ipAddress);
//                }
//            }

//            // Генерируем сертификат с использованием RSA
//            X509Certificate2 certificate = certificateBuilder.CreateForRSA();

//            return certificate;
//        }

//        public static void SaveCertificateToStore(
//            X509Certificate2 certificate,
//            string storePath)
//        {
//            // Создаем хранилище сертификатов
//            var storeIdentifier = new CertificateIdentifier();
//            storeIdentifier.StoreType = CertificateStoreType.Directory;
//            storeIdentifier.StorePath = storePath;

//            // Сохраняем сертификат в хранилище
//            using (var store = storeIdentifier.OpenStore())
//            {
//                store.Add(certificate).Wait();
//                Console.WriteLine("Certificate saved to store.");
//            }
//        }
//    }
//}
