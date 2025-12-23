using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Opc2Lib
{
    public class LogicControllerUaClient : ILogicControllerProvider
    {
        private string CommandPrefix;

        private readonly string endpointUrl;
        private readonly int _namespaceId;
        private readonly int timeout;
        private readonly SecurityPolicies securityPolicy;
        private readonly UserIdentity userIdentity;
        private readonly ApplicationConfiguration config;

        private Session session;
        private bool _isReconnecting = false;
        private readonly object _reconnectLock = new object();
        private CancellationTokenSource _reconnectCts;

        public bool Connected
        {
            get
            {
                if (session == null) return false;
                return session.Connected;
            }
        }
        private Dictionary<CommandInfo, object> _values = new();
        private ReadValueIdCollection _readValuesCollection;
        private List<CommandInfo> _commands;

        public enum SecurityPolicies
        {
            None,
            Basic128Rsa15,
            Basic256,
            Basic256Sha256,
            Aes128Sha256RsaOaep,
            Aes256Sha256RsaPss
        }

        public LogicControllerUaClient(string commandPrefix,int namespaceId,  string serverAddress, int port, int timeoutMs, SecurityPolicies policy, UserIdentity identity)
        {
            endpointUrl = $"opc.tcp://{serverAddress}:{port}";
            timeout = timeoutMs;
            securityPolicy = policy;
            userIdentity = identity;
            config = BuildApplicationConfiguration();
            CommandPrefix = commandPrefix;
            _namespaceId = namespaceId;
            
        }

        private ApplicationConfiguration BuildApplicationConfiguration()
        {

            var certBasePath = Path.Combine(Directory.GetCurrentDirectory(), "OPC UA Certificates", "pki");

            return new ApplicationConfiguration()
            {
                ApplicationName = "LogicControllerUaClient",
                ApplicationType = ApplicationType.Client,
                ApplicationUri = "urn:localhost:LogicControllerUaClient",
                ProductUri = "urn:LogicControllerUaClient",
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(certBasePath, "own")
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(certBasePath, "trusted")
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(certBasePath, "issuer")
                    },
                    RejectedCertificateStore = new CertificateStoreIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(certBasePath, "rejected")
                    },
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false,
                    MinimumCertificateKeySize = 1024,
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = timeout },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = timeout }
            };
        }

        public async Task WaitBoolValue(CommandInfo info, bool value, int delay = 500, CancellationToken? cancellationToken = null)
        {
            
        }

        public async Task ConnectAsync()
        {
            if (_isReconnecting) return;

            try
            {
                Console.WriteLine("Starting OPC UA Client configuration...");

                // Создание директорий
                Directory.CreateDirectory(config.SecurityConfiguration.ApplicationCertificate.StorePath);
                Directory.CreateDirectory(config.SecurityConfiguration.TrustedPeerCertificates.StorePath);
                Directory.CreateDirectory(config.SecurityConfiguration.TrustedIssuerCertificates.StorePath);
                Directory.CreateDirectory(config.SecurityConfiguration.RejectedCertificateStore.StorePath);

                // Проверка/создание сертификата
                var appCert = config.SecurityConfiguration.ApplicationCertificate;
                var certificate = await appCert.Find(true);
                if (certificate == null)
                {
                    //Console.WriteLine("Application certificate not found. Creating a new one...");
                    var certBuilder = CertificateFactory.CreateCertificate(
                        config.ApplicationUri,
                        config.ApplicationName,
                        "CN=LogicControllerUaClient,O=OPC Foundation",
                        null);
                    certificate = certBuilder.CreateForRSA();
                    using (var store = appCert.OpenStore())
                    {
                        await store.Add(certificate);
                        //Console.WriteLine("New certificate created and saved.");
                    }
                    appCert.Certificate = certificate;
                }
                else
                {
                    //Console.WriteLine("Application certificate found.");
                    appCert.Certificate = certificate;
                }

                await config.Validate(ApplicationType.Client);

                // Выбор конечной точки
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl,
                    securityPolicy != SecurityPolicies.None,
                    timeout);

                var endpointConfiguration = EndpointConfiguration.Create(config);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                // Создание сессии
                session = await Session.Create(config, endpoint, false, "OpcUaClientSession",
                    (uint)timeout, userIdentity, null);

                // Подписка на KeepAlive для отслеживания разрыва
                session.KeepAlive += OnKeepAlive;

                Console.WriteLine("Session created successfully.");

                if (session.Connected)
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения к OPC: {ex.Message}");
                await Task.Delay(3000);
                Task.Run(ConnectAsync);
            }
        }

        public async Task DisconnectAsync()
        {
            Disconnect();
            await Task.CompletedTask;
        }

        private void OnKeepAlive(ISession sender, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsBad(e.Status))
            {
                Console.WriteLine($"KeepAlive error: {e.Status}");
                Disconnect();
                Task.Factory.StartNew(StartReconnectLoop);
            }
        }

        private async Task StartReconnectLoop()
        {
            await ConnectAsync();
        }

        private async Task Observer()
        {
            while (true)
            {
                if (!Connected)
                {
                    await Task.Delay(1000);
                    continue;
                }


                try
                {
                    var readResponse = await session.ReadAsync(null, 0, TimestampsToReturn.Both,
                        _readValuesCollection, CancellationToken.None);

                    for (int i = 0; i < _commands.Count; i++)
                    {
                        var command = _commands[i];
                        if (StatusCode.IsGood(readResponse.Results[i].StatusCode))
                        {
                            _values.TryAdd(command, readResponse.Results[i].Value);
                        }
                        else
                        {
                            switch (command.ValueCommandType)
                            {
                                case ValueCommandType.Bool:
                                    _values.TryAdd(command, false);
                                    break;
                                case ValueCommandType.Real:
                                    _values.TryAdd(command, 0f);
                                    break;
                                case ValueCommandType.Unsigned:
                                    _values.TryAdd(command, (ushort)0);
                                    break;
                                case ValueCommandType.Dint:
                                    _values.TryAdd(command, 0);
                                    break;
                            }
                        }
                    }

                    await Task.Delay(200);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Task.Factory.StartNew(Observer);
                }
            }
        }

        // Массовая запись: принимает словарь CommandInfo -> значение
        public async Task WriteMultipleAsync(Dictionary<CommandInfo, object> values)
        {
            if (!Connected)
                return;

            var writeValues = new WriteValueCollection();

            foreach (var kvp in values)
            {
                writeValues.Add(new WriteValue
                {
                    NodeId = new NodeId($"ns={_namespaceId};s={CommandPrefix}{kvp.Key.Command}"),
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(kvp.Value))
                });
            }

            var response = await session.WriteAsync(
                requestHeader: null,
                nodesToWrite: writeValues,
                ct: CancellationToken.None
            );

            // Проверка результатов
            for (int i = 0; i < response.Results.Count; i++)
            {
                if (!StatusCode.IsGood(response.Results[i]))
                {
                    var key = values.Keys.ElementAt(i);
                    Console.WriteLine($"Write error for {key.Command}: {response.Results[i]}");
                    // Можно выбросить исключение или продолжить — зависит от требований
                }
            }
        }



        public async Task<T> GetAsync<T>(CommandInfo info)
        {
            if (!Connected)
                return default;

            try
            {
                var readValue = new ReadValueId
                {
                    NodeId = new NodeId($"ns={_namespaceId};s={CommandPrefix}{info.Command}"),
                    AttributeId = Attributes.Value
                };

                var readResponse = await session.ReadAsync(null, 0, TimestampsToReturn.Both,
                    new ReadValueIdCollection { readValue }, CancellationToken.None);

                if (StatusCode.IsGood(readResponse.Results[0].StatusCode))
                {
                    return (T)readResponse.Results[0].Value;
                }
                else
                {
                    throw new Exception($"Failed to read valueCommand: {readResponse.Results[0].StatusCode}");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("BadNotConnected"))
                {
                }
                throw;
            }
        }

        public async Task SetAsync(CommandInfo info, object value)
        {
            if (!Connected)
                return;

            try
            {
                var writeValue = new WriteValue
                {
                    NodeId = new NodeId($"ns={_namespaceId};s={CommandPrefix}{info.Command}"),
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(value))
                };

                var writeResponse = await session.WriteAsync(null,
                    new WriteValueCollection { writeValue }, CancellationToken.None);

                if (!StatusCode.IsGood(writeResponse.Results[0]))
                {
                    ///throw new Exception($"Failed to write valueCommand: {writeResponse.Results[0]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing valueCommand: {ex.Message}");
                //throw;
            }
        }

        // --- Ваши типизированные методы остаются без изменений ---
        public async Task<bool> GetBoolAsync(CommandInfo info) => await GetAsync<bool>(info);
        public async Task SetBoolAsync(CommandInfo info, bool value) => await SetAsync(info, value);
        public async Task<float> GetFloatAsync(CommandInfo info) => await GetAsync<float>(info);
        public async Task SetFloatAsync(CommandInfo info, float value) => await SetAsync(info, value);
        public async Task<double> GetDoubleAsync(CommandInfo info) => await GetAsync<double>(info);
        public async Task SetDoubleAsync(CommandInfo info, double value) => await SetAsync(info, value);
        public async Task<int> GetInt32Async(CommandInfo info) => await GetAsync<int>(info);
        public async Task SetInt32Async(CommandInfo info, int value) => await SetAsync(info, value);
        public async Task<short> GetInt16Async(CommandInfo info) => await GetAsync<short>(info);
        public async Task SetInt16Async(CommandInfo info, short value) => await SetAsync(info, value);
        public async Task<uint> GetUInt32Async(CommandInfo info) => await GetAsync<uint>(info);
        public async Task SetUInt32Async(CommandInfo info, uint value) => await SetAsync(info, value);
        public async Task<ushort> GetUInt16Async(CommandInfo info) => await GetAsync<ushort>(info);
        public async Task SetUInt16Async(CommandInfo info, ushort value) => await SetAsync(info, value);

        public void Disconnect()
        {
            //session?.KeepAlive -= OnKeepAlive;
            if (session != null)
            {
                session.Close();
                session?.Dispose();
                session = null;

                // Отмена переподключения при явном Disconnect
                _reconnectCts?.Cancel();
                _isReconnecting = false;
            }
        }
    }
}