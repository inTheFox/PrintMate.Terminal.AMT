using System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.D3DCompiler;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace PrintMate.Terminal.Rendering
{
    /// <summary>
    /// Чистый DirectX 11 рендерер для 3D визуализации CLI проектов
    /// </summary>
    public class DX11Renderer : IDisposable
    {
        #region Приватные поля

        private Device _device;
        private DeviceContext _context;
        private SwapChain _swapChain;
        private RenderTargetView _renderTargetView;
        private DepthStencilView _depthStencilView;
        private Texture2D _depthBuffer;
        private Texture2D _backBuffer;

        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;

        private RasterizerState _rasterizerState;
        private DepthStencilState _depthStencilState;
        private BlendState _blendState;

        private Buffer _constantBuffer;

        // Shadow mapping ресурсы
        private Texture2D _shadowMap;
        private DepthStencilView _shadowMapDSV;
        private ShaderResourceView _shadowMapSRV;
        private VertexShader _shadowVertexShader;
        private SamplerState _shadowSampler;
        private const int ShadowMapSize = 2048;

        // MSAA ресурсы для антиалиасинга
        private Texture2D _msaaRenderTarget;
        private RenderTargetView _msaaRenderTargetView;
        private Texture2D _msaaDepthBuffer;
        private DepthStencilView _msaaDepthStencilView;
        private const int MsaaSampleCount = 4; // 4x MSAA

        private int _width;
        private int _height;
        private bool _isInitialized;

        // Highlight состояние
        private Vector3 _highlightColor = new Vector3(0.2f, 0.5f, 1.0f); // Синий цвет по умолчанию
        private float _highlightPartId = -1.0f; // -1 = нет выделения

        #endregion

        #region Публичные свойства

        public Device Device => _device;
        public DeviceContext Context => _context;
        public bool IsInitialized => _isInitialized;
        public IntPtr BackBufferPtr { get; private set; }
        public int BackBufferWidth => _width;
        public int BackBufferHeight => _height;

        #endregion

        #region Конструктор

        public DX11Renderer()
        {
            _isInitialized = false;
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализирует DirectX 11 устройство для offscreen рендеринга (без окна)
        /// </summary>
        public void InitializeOffscreen(int width, int height)
        {
            _width = width;
            _height = height;

            // Создаём устройство без swap chain (offscreen)
            CreateDevice();

            // Создаём offscreen render target
            CreateOffscreenRenderTarget();

            // Создаём MSAA render target для антиалиасинга
            CreateMsaaRenderTarget();

            // Создаём depth buffer
            CreateDepthStencilBuffer();

            // Создаём shadow map
            CreateShadowMap();

            // Создаём шейдеры
            CreateShaders();

            // Создаём состояния рендеринга
            CreateRenderStates();

            // Создаём constant buffer для матриц
            CreateConstantBuffer();

            // Настраиваем viewport
            SetupViewport();

            _isInitialized = true;
            
        }

        /// <summary>
        /// Инициализирует DirectX 11 устройство и ресурсы (с окном)
        /// </summary>
        public void Initialize(IntPtr windowHandle, int width, int height)
        {
            _width = width;
            _height = height;

            // Создаём устройство и swap chain
            CreateDeviceAndSwapChain(windowHandle);

            // Создаём render target
            CreateRenderTarget();

            // Создаём depth buffer
            CreateDepthStencilBuffer();

            // Создаём шейдеры
            CreateShaders();

            // Создаём состояния рендеринга
            CreateRenderStates();

            // Создаём constant buffer для матриц
            CreateConstantBuffer();

            // Настраиваем viewport
            SetupViewport();

            _isInitialized = true;
            
        }

        private void CreateDevice()
        {
            _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_0);
            _context = _device.ImmediateContext;
            
        }

        private void CreateOffscreenRenderTarget()
        {
            var textureDesc = new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm, // BGRA для совместимости с D3DImage
                SampleDescription = new SampleDescription(1, 0), // БЕЗ MSAA для shared ресурса!
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.Shared // Важно для D3DImage!
            };

            _backBuffer = new Texture2D(_device, textureDesc);
            _renderTargetView = new RenderTargetView(_device, _backBuffer);

            // Получаем shared handle для D3DImage
            using (var resource = _backBuffer.QueryInterface<SharpDX.DXGI.Resource>())
            {
                BackBufferPtr = resource.SharedHandle;
            }

            
        }

        private void CreateMsaaRenderTarget()
        {
            // Создаём MSAA render target (для рендеринга с антиалиасингом)
            var msaaTextureDesc = new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(MsaaSampleCount, 0), // 4x MSAA
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _msaaRenderTarget = new Texture2D(_device, msaaTextureDesc);
            _msaaRenderTargetView = new RenderTargetView(_device, _msaaRenderTarget);

            // Создаём MSAA depth buffer
            var msaaDepthDesc = new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(MsaaSampleCount, 0), // 4x MSAA
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _msaaDepthBuffer = new Texture2D(_device, msaaDepthDesc);
            _msaaDepthStencilView = new DepthStencilView(_device, _msaaDepthBuffer);

            
        }

        private void CreateDeviceAndSwapChain(IntPtr windowHandle)
        {
            var swapChainDescription = new SwapChainDescription
            {
                BufferCount = 2,
                ModeDescription = new ModeDescription(_width, _height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = windowHandle,
                SampleDescription = new SampleDescription(4, 0), // 4x MSAA
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                Flags = SwapChainFlags.AllowModeSwitch
            };

            Device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new[] { FeatureLevel.Level_11_0 },
                swapChainDescription,
                out _device,
                out _swapChain
            );

            _context = _device.ImmediateContext;
            
        }

        private void CreateRenderTarget()
        {
            _backBuffer = _swapChain.GetBackBuffer<Texture2D>(0);
            _renderTargetView = new RenderTargetView(_device, _backBuffer);

            BackBufferPtr = _backBuffer.NativePointer;
            
        }

        private void CreateDepthStencilBuffer()
        {
            var depthBufferDesc = new Texture2DDescription
            {
                Width = _width,
                Height = _height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = new SampleDescription(1, 0), // БЕЗ MSAA (совпадает с backbuffer)
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _depthBuffer = new Texture2D(_device, depthBufferDesc);
            _depthStencilView = new DepthStencilView(_device, _depthBuffer);

            
        }

        private void CreateShadowMap()
        {
            // Создаём depth texture для shadow map
            var shadowMapDesc = new Texture2DDescription
            {
                Width = ShadowMapSize,
                Height = ShadowMapSize,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32_Typeless, // Typeless для возможности использовать как depth и shader resource
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _shadowMap = new Texture2D(_device, shadowMapDesc);

            // Создаём depth stencil view для shadow map
            var dsvDesc = new DepthStencilViewDescription
            {
                Format = Format.D32_Float,
                Dimension = DepthStencilViewDimension.Texture2D,
                Texture2D = new DepthStencilViewDescription.Texture2DResource { MipSlice = 0 }
            };
            _shadowMapDSV = new DepthStencilView(_device, _shadowMap, dsvDesc);

            // Создаём shader resource view для чтения shadow map в пиксельном шейдере
            var srvDesc = new ShaderResourceViewDescription
            {
                Format = Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource { MipLevels = 1, MostDetailedMip = 0 }
            };
            _shadowMapSRV = new ShaderResourceView(_device, _shadowMap, srvDesc);

            // Создаём sampler для shadow map (обычный linear sampler)
            var samplerDesc = new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                BorderColor = new RawColor4(1, 1, 1, 1), // Белый border = вне тени
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            };
            _shadowSampler = new SamplerState(_device, samplerDesc);

            
        }

        private void CreateShaders()
        {
            // Shadow pass vertex shader (простой - только трансформация в light space)
            var shadowVertexShaderCode = @"
                cbuffer ConstantBuffer : register(b0)
                {
                    matrix World;
                    matrix View;
                    matrix Projection;
                    float3 CameraPosition;
                    float Padding;
                    matrix LightViewProjection;
                    float3 HighlightColor;
                    float HighlightPartId;
                };

                struct VS_INPUT
                {
                    float3 Position : POSITION;
                    float3 Normal : NORMAL;
                    float4 Color : COLOR;
                };

                float4 VS(VS_INPUT input) : SV_POSITION
                {
                    float4 worldPos = mul(float4(input.Position, 1.0f), World);
                    return mul(worldPos, LightViewProjection);
                }
            ";

            // Main pass vertex shader с shadow mapping
            var vertexShaderCode = @"
                cbuffer ConstantBuffer : register(b0)
                {
                    matrix World;
                    matrix View;
                    matrix Projection;
                    float3 CameraPosition;
                    float Padding;
                    matrix LightViewProjection;
                    float3 HighlightColor;
                    float HighlightPartId;
                };

                struct VS_INPUT
                {
                    float3 Position : POSITION;
                    float3 Normal : NORMAL;
                    float4 Color : COLOR;
                };

                struct PS_INPUT
                {
                    float4 Position : SV_POSITION;
                    float3 WorldPos : TEXCOORD0;
                    float4 LightSpacePos : TEXCOORD1;
                    float3 Normal : NORMAL;
                    float4 Color : COLOR;
                };

                PS_INPUT VS(VS_INPUT input)
                {
                    PS_INPUT output;

                    float4 worldPos = mul(float4(input.Position, 1.0f), World);
                    float4 viewPos = mul(worldPos, View);
                    output.Position = mul(viewPos, Projection);

                    output.WorldPos = worldPos.xyz;
                    output.LightSpacePos = mul(worldPos, LightViewProjection);
                    output.Normal = mul(input.Normal, (float3x3)World);
                    output.Color = input.Color;

                    return output;
                }
            ";

            var pixelShaderCode = @"
                cbuffer ConstantBuffer : register(b0)
                {
                    matrix World;
                    matrix View;
                    matrix Projection;
                    float3 CameraPosition;
                    float Padding;
                    matrix LightViewProjection;
                    float3 HighlightColor;
                    float HighlightPartId;
                };

                Texture2D ShadowMap : register(t0);
                SamplerState ShadowSampler : register(s0);

                struct PS_INPUT
                {
                    float4 Position : SV_POSITION;
                    float3 WorldPos : TEXCOORD0;
                    float4 LightSpacePos : TEXCOORD1;
                    float3 Normal : NORMAL;
                    float4 Color : COLOR;
                };

                float CalculateShadow(float4 lightSpacePos)
                {
                    // Perspective divide
                    float3 projCoords = lightSpacePos.xyz / lightSpacePos.w;

                    // Преобразуем в [0,1] диапазон для текстуры
                    projCoords.x = projCoords.x * 0.5 + 0.5;
                    projCoords.y = -projCoords.y * 0.5 + 0.5;

                    // Если вне shadow map - не в тени
                    if (projCoords.x < 0.0 || projCoords.x > 1.0 ||
                        projCoords.y < 0.0 || projCoords.y > 1.0 ||
                        projCoords.z < 0.0 || projCoords.z > 1.0)
                        return 1.0;

                    // PCF (Percentage Closer Filtering) для мягких теней
                    float shadow = 0.0;
                    float bias = 0.005; // Увеличили bias чтобы убрать shadow acne
                    float2 texelSize = 1.0 / float2(2048.0, 2048.0);

                    [unroll]
                    for (int x = -1; x <= 1; ++x)
                    {
                        [unroll]
                        for (int y = -1; y <= 1; ++y)
                        {
                            float2 offset = float2(x, y) * texelSize;
                            float depth = ShadowMap.Sample(ShadowSampler, projCoords.xy + offset).r;
                            shadow += (projCoords.z - bias) > depth ? 0.0 : 1.0;
                        }
                    }
                    shadow /= 9.0;

                    return shadow;
                }

                float4 PS(PS_INPUT input) : SV_TARGET
                {
                    // Flat shading через производные для чётких граней
                    float3 dFdxPos = ddx(input.WorldPos);
                    float3 dFdyPos = ddy(input.WorldPos);
                    float3 flatNormal = normalize(cross(dFdxPos, dFdyPos));

                    // Используем плоскую нормаль вместо интерполированной
                    float3 normal = flatNormal;
                    float3 viewDir = normalize(CameraPosition - input.WorldPos);

                    // Рассчитываем тень (усилен контраст)
                    float shadow = CalculateShadow(input.LightSpacePos);

                    // Key Light (основной направленный свет) - полностью затеняется
                    float3 keyLightDir = normalize(float3(1.0f, 1.2f, 0.8f));
                    float keyNdotL = max(dot(normal, keyLightDir), 0.0f);
                    float3 keyLight = float3(1.0f, 0.95f, 0.9f) * keyNdotL * 0.85f * shadow;

                    // Fill Light (заполняющий свет с противоположной стороны) - частично затеняется
                    float3 fillLightDir = normalize(float3(-0.8f, 0.4f, -0.3f));
                    float fillNdotL = max(dot(normal, fillLightDir), 0.0f);
                    float fillShadow = lerp(0.4f, 1.0f, shadow); // Частичное затенение
                    float3 fillLight = float3(0.5f, 0.55f, 0.7f) * fillNdotL * 0.3f * fillShadow;

                    // Back Light (контровой свет для выделения силуэта) - не затеняется
                    float3 backLightDir = normalize(float3(0.0f, 0.6f, -1.0f));
                    float backNdotL = max(dot(normal, backLightDir), 0.0f);
                    float3 backLight = float3(0.6f, 0.7f, 0.9f) * backNdotL * 0.35f;

                    // Rim lighting для силуэта (усилен)
                    float rimFactor = 1.0f - max(dot(viewDir, normal), 0.0f);
                    rimFactor = pow(rimFactor, 2.0f);
                    float3 rimLight = float3(1.0f, 0.9f, 0.8f) * rimFactor * 0.25f;

                    // Ambient lighting - сильно затемняется в тени для контраста
                    float ambientShadow = lerp(0.3f, 1.0f, shadow); // Тени делают ambient темнее
                    float3 ambient = float3(0.25f, 0.27f, 0.32f) * ambientShadow;

                    // Суммируем все освещение
                    float3 totalLight = ambient + keyLight + fillLight + backLight + rimLight;

                    // Небольшой clamp для избежания пересветов
                    totalLight = min(totalLight, 1.5f);

                    // Декодируем данные из vertex color:
                    // R channel: 1.0 = последний слой (оранжевый), 0.0 = обычный слой (серый)
                    // G channel: для платформы и сетки используется напрямую
                    // B channel: для платформы и сетки используется напрямую
                    // A channel: partId (0-255 закодировано как 0.0-1.0), для платформы = 0
                    float isLastLayer = input.Color.r;
                    float vertexPartId = input.Color.a * 255.0f;

                    // Определяем базовый цвет и финальный цвет
                    float4 finalColor;

                    // ПРОВЕРКА: если partId = 0 И это НЕ последний слой, то это платформа/сетка/граница
                    // Используем vertex color напрямую БЕЗ освещения
                    // НО если это последний слой (isLastLayer = 1.0), то это деталь и нужно освещение!
                    if (vertexPartId < 0.5f && isLastLayer < 0.5f)
                    {
                        finalColor = float4(input.Color.rgb, 1.0f);
                    }
                    else
                    {
                        // Это деталь - единый оранжевый цвет #FF7700 для всех слоёв
                        float3 baseColor = float3(1.0f, 0.467f, 0.0f);

                        // Применяем освещение к базовому цвету деталей
                        finalColor = float4(baseColor * totalLight, 1.0f);
                    }

                    // Проверяем, нужно ли выделить эту деталь (shader-based highlighting)
                    if (HighlightPartId >= 0.0f && abs(vertexPartId - HighlightPartId) < 0.5f)
                    {
                        // Применяем highlight color с сохранением освещения
                        finalColor.rgb = HighlightColor * totalLight;
                    }

                    return finalColor;
                }
            ";

            // Компилируем шейдеры
            var shadowVertexBytecode = ShaderBytecode.Compile(shadowVertexShaderCode, "VS", "vs_5_0");
            var vertexShaderBytecode = ShaderBytecode.Compile(vertexShaderCode, "VS", "vs_5_0");
            var pixelShaderBytecode = ShaderBytecode.Compile(pixelShaderCode, "PS", "ps_5_0");

            _shadowVertexShader = new VertexShader(_device, shadowVertexBytecode);
            _vertexShader = new VertexShader(_device, vertexShaderBytecode);
            _pixelShader = new PixelShader(_device, pixelShaderBytecode);

            // Создаём input layout
            var inputElements = new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 24, 0)
            };

            _inputLayout = new InputLayout(_device, vertexShaderBytecode, inputElements);

            shadowVertexBytecode.Dispose();
            vertexShaderBytecode.Dispose();
            pixelShaderBytecode.Dispose();

            
        }

        private void CreateRenderStates()
        {
            // Rasterizer state (NO culling для полых моделей - видим обе стороны стенок)
            var rasterizerDesc = new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None, // Отключаем culling - рендерим обе стороны граней
                IsFrontCounterClockwise = false,
                DepthBias = 0,
                SlopeScaledDepthBias = 0.0f,
                DepthBiasClamp = 0.0f,
                IsDepthClipEnabled = true,
                IsScissorEnabled = false,
                IsMultisampleEnabled = true,
                IsAntialiasedLineEnabled = true
            };
            _rasterizerState = new RasterizerState(_device, rasterizerDesc);

            // Depth stencil state
            var depthStencilDesc = new DepthStencilStateDescription
            {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,
                IsStencilEnabled = false
            };
            _depthStencilState = new DepthStencilState(_device, depthStencilDesc);

            // Blend state (для прозрачности)
            var blendDesc = new BlendStateDescription
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                BlendOperation = BlendOperation.Add,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            _blendState = new BlendState(_device, blendDesc);

            
        }

        private void CreateConstantBuffer()
        {
            // Constant buffer для матриц и параметров освещения
            var bufferDesc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<ConstantBufferData>(),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            };

            _constantBuffer = new Buffer(_device, bufferDesc);
            
        }

        private void SetupViewport()
        {
            var viewport = new Viewport(0, 0, _width, _height, 0.0f, 1.0f);
            _context.Rasterizer.SetViewport(viewport);
            
        }

        #endregion

        #region Рендеринг

        /// <summary>
        /// Начинает рендер фрейма
        /// </summary>
        public void BeginFrame(Color4 clearColor)
        {
            // Очищаем MSAA render target для рендеринга с антиалиасингом
            _context.ClearRenderTargetView(_msaaRenderTargetView, clearColor);
            _context.ClearDepthStencilView(_msaaDepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            // Устанавливаем MSAA render target для рендеринга
            _context.OutputMerger.SetRenderTargets(_msaaDepthStencilView, _msaaRenderTargetView);
            _context.Rasterizer.State = _rasterizerState;
            _context.OutputMerger.DepthStencilState = _depthStencilState;
            _context.OutputMerger.BlendState = _blendState;
        }

        /// <summary>
        /// Устанавливает матрицы для рендеринга
        /// </summary>
        public void SetMatrices(Matrix world, Matrix view, Matrix projection, Vector3 cameraPosition)
        {
            // Вычисляем матрицу света (directional light с позиции key light)
            Vector3 lightPos = new Vector3(200, 300, 200); // Позиция света
            Vector3 lightTarget = Vector3.Zero; // Смотрит в центр
            Vector3 lightUp = Vector3.UnitY;

            Matrix lightView = Matrix.LookAtLH(lightPos, lightTarget, lightUp);
            Matrix lightProjection = Matrix.OrthoLH(400, 400, 10.0f, 600.0f); // Уменьшили размер для более чёткой тени
            Matrix lightViewProjection = lightView * lightProjection;

            var data = new ConstantBufferData
            {
                World = Matrix.Transpose(world),
                View = Matrix.Transpose(view),
                Projection = Matrix.Transpose(projection),
                CameraPosition = cameraPosition,
                Padding = 0,
                LightViewProjection = Matrix.Transpose(lightViewProjection),
                HighlightColor = _highlightColor,
                HighlightPartId = _highlightPartId
            };

            var dataBox = _context.MapSubresource(_constantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            Utilities.Write(dataBox.DataPointer, ref data);
            _context.UnmapSubresource(_constantBuffer, 0);

            _context.VertexShader.SetConstantBuffer(0, _constantBuffer);
            _context.PixelShader.SetConstantBuffer(0, _constantBuffer);
        }

        /// <summary>
        /// Начинает shadow pass - рендер в shadow map
        /// </summary>
        public void BeginShadowPass()
        {
            // Очищаем shadow map
            _context.ClearDepthStencilView(_shadowMapDSV, DepthStencilClearFlags.Depth, 1.0f, 0);

            // Привязываем shadow map как render target (только depth)
            _context.OutputMerger.SetRenderTargets(_shadowMapDSV, (RenderTargetView)null);

            // Устанавливаем viewport для shadow map
            var shadowViewport = new Viewport(0, 0, ShadowMapSize, ShadowMapSize, 0.0f, 1.0f);
            _context.Rasterizer.SetViewport(shadowViewport);

            // Используем shadow vertex shader (без pixel shader - только depth)
            _context.VertexShader.Set(_shadowVertexShader);
            _context.PixelShader.Set(null);
        }

        /// <summary>
        /// Завершает shadow pass и возвращается к main pass
        /// </summary>
        public void EndShadowPass()
        {
            // Восстанавливаем MSAA render target (для антиалиасинга)
            _context.OutputMerger.SetRenderTargets(_msaaDepthStencilView, _msaaRenderTargetView);

            // Восстанавливаем viewport
            var viewport = new Viewport(0, 0, _width, _height, 0.0f, 1.0f);
            _context.Rasterizer.SetViewport(viewport);

            // Привязываем shadow map как shader resource для чтения
            _context.PixelShader.SetShaderResource(0, _shadowMapSRV);
            _context.PixelShader.SetSampler(0, _shadowSampler);

            // Возвращаем main шейдеры
            _context.VertexShader.Set(_vertexShader);
            _context.PixelShader.Set(_pixelShader);
        }

        /// <summary>
        /// Рендерит меш в shadow map (shadow pass)
        /// </summary>
        public void DrawMeshShadow(CliMesh mesh)
        {
            if (mesh == null || mesh.VertexBuffer == null || mesh.IndexBuffer == null)
                return;

            _context.InputAssembler.InputLayout = _inputLayout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.VertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            _context.InputAssembler.SetIndexBuffer(mesh.IndexBuffer, Format.R32_UInt, 0);

            _context.DrawIndexed(mesh.IndexCount, 0, 0);
        }

        /// <summary>
        /// Рендерит меш (main pass с тенями)
        /// </summary>
        public void DrawMesh(CliMesh mesh)
        {
            if (mesh == null || mesh.VertexBuffer == null || mesh.IndexBuffer == null)
                return;

            _context.InputAssembler.InputLayout = _inputLayout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.VertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            _context.InputAssembler.SetIndexBuffer(mesh.IndexBuffer, Format.R32_UInt, 0);

            _context.DrawIndexed(mesh.IndexCount, 0, 0);
        }

        /// <summary>
        /// Рендерит mesh как wireframe (линии)
        /// </summary>
        public void DrawWireframe(CliMesh mesh)
        {
            if (mesh == null || mesh.VertexBuffer == null || mesh.IndexBuffer == null)
                return;

            _context.InputAssembler.InputLayout = _inputLayout;
            _context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            _context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.VertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            _context.InputAssembler.SetIndexBuffer(mesh.IndexBuffer, Format.R32_UInt, 0);

            _context.DrawIndexed(mesh.IndexCount, 0, 0);
        }

        /// <summary>
        /// Завершает рендер фрейма и представляет результат
        /// </summary>
        public void EndFrame()
        {
            // Resolve MSAA render target в обычный backbuffer для сглаживания
            _context.ResolveSubresource(_msaaRenderTarget, 0, _backBuffer, 0, Format.B8G8R8A8_UNorm);

            // Если есть swap chain - present, если нет - просто flush
            if (_swapChain != null)
            {
                _swapChain.Present(1, PresentFlags.None);
            }
            else
            {
                _context.Flush(); // Для offscreen режима
            }
        }

        /// <summary>
        /// Устанавливает ID детали для выделения
        /// </summary>
        /// <param name="partId">ID детали для выделения, или null для сброса</param>
        public void SetHighlightedPart(int? partId)
        {
            _highlightPartId = partId.HasValue ? (float)partId.Value : -1.0f;
        }

        #endregion

        #region Resize

        /// <summary>
        /// Изменяет размер рендер таргета
        /// </summary>
        public void Resize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            _width = width;
            _height = height;

            // Очищаем старые ресурсы
            _context.OutputMerger.SetRenderTargets((RenderTargetView)null);
            _renderTargetView?.Dispose();
            _depthStencilView?.Dispose();
            _depthBuffer?.Dispose();
            _backBuffer?.Dispose();

            // Пересоздаём ресурсы в зависимости от режима
            if (_swapChain != null)
            {
                // Режим с окном
                _swapChain.ResizeBuffers(2, width, height, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowModeSwitch);
                CreateRenderTarget();
            }
            else
            {
                // Offscreen режим
                CreateOffscreenRenderTarget();
            }

            CreateDepthStencilBuffer();
            SetupViewport();

            
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            // Shadow resources
            _shadowSampler?.Dispose();
            _shadowMapSRV?.Dispose();
            _shadowMapDSV?.Dispose();
            _shadowMap?.Dispose();
            _shadowVertexShader?.Dispose();

            // MSAA resources
            _msaaDepthStencilView?.Dispose();
            _msaaDepthBuffer?.Dispose();
            _msaaRenderTargetView?.Dispose();
            _msaaRenderTarget?.Dispose();

            // Main resources
            _constantBuffer?.Dispose();
            _blendState?.Dispose();
            _depthStencilState?.Dispose();
            _rasterizerState?.Dispose();
            _inputLayout?.Dispose();
            _pixelShader?.Dispose();
            _vertexShader?.Dispose();
            _depthStencilView?.Dispose();
            _depthBuffer?.Dispose();
            _renderTargetView?.Dispose();
            _backBuffer?.Dispose();
            _swapChain?.Dispose();
            _context?.Dispose();
            _device?.Dispose();

            _isInitialized = false;
            
        }

        #endregion
    }

    #region Вспомогательные структуры

    /// <summary>
    /// Структура вершины
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color4 Color;

        public Vertex(Vector3 position, Vector3 normal, Color4 color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }
    }

    /// <summary>
    /// Структура constant buffer
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct ConstantBufferData
    {
        public Matrix World;
        public Matrix View;
        public Matrix Projection;
        public Vector3 CameraPosition;
        public float Padding;
        public Matrix LightViewProjection;
        public Vector3 HighlightColor; // Цвет выделения (синий для выбранной детали)
        public float HighlightPartId;   // ID выделенной детали (-1 = нет выделения)
    }

    /// <summary>
    /// Класс для хранения меша
    /// </summary>
    public class CliMesh : IDisposable
    {
        public Buffer VertexBuffer { get; set; }
        public Buffer IndexBuffer { get; set; }
        public int IndexCount { get; set; }
        public int VertexCount { get; set; }

        public void Dispose()
        {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();
        }
    }

    #endregion
}
