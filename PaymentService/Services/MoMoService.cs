using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using PaymentService.Dtos;

namespace PaymentService.Services
{
    public class MoMoService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MoMoService> _logger;
        private readonly HttpClient _httpClient;

        public string PartnerCode { get; }
        public string AccessKey { get; }
        public string SecretKey { get; }
        public string ApiEndpoint { get; }
        public string NotifyUrl { get; }

        public MoMoService(IConfiguration config, ILogger<MoMoService> logger, HttpClient httpClient)
        {
            _config = config;
            _logger = logger;
            _httpClient = httpClient;

            var momoConfig = _config.GetSection("MoMo");
            PartnerCode = momoConfig["PartnerCode"] ?? "";
            AccessKey = momoConfig["AccessKey"] ?? "";
            SecretKey = momoConfig["SecretKey"] ?? "";
            ApiEndpoint = momoConfig["ApiEndpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
            NotifyUrl = momoConfig["NotifyUrl"] ?? "";
        }

        public async Task<MoMoPaymentResult> CreatePaymentAsync(
            string requestId,
            string orderId,
            long amount,
            string orderInfo,
            string returnUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(PartnerCode) || string.IsNullOrEmpty(AccessKey) || string.IsNullOrEmpty(SecretKey))
                {
                    _logger.LogWarning("MoMo credentials not configured");
                    return new MoMoPaymentResult
                    {
                        Success = false,
                        Message = "MoMo payment gateway not configured"
                    };
                }

                // Create request body
                var requestBody = new
                {
                    partnerCode = PartnerCode,
                    partnerName = "Test",
                    storeId = "MomoTestStore",
                    requestId = requestId,
                    amount = amount,
                    orderId = orderId,
                    orderInfo = orderInfo,
                    redirectUrl = returnUrl,
                    ipnUrl = NotifyUrl,
                    requestType = "captureWallet",
                    extraData = "",
                    lang = "vi"
                };

                // Create signature
                var rawHash = $"accessKey={AccessKey}&amount={amount}&extraData=&ipnUrl={NotifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={PartnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
                var signature = ComputeHmacSha256(rawHash, SecretKey);

                // Add signature to request
                var requestData = new
                {
                    partnerCode = PartnerCode,
                    partnerName = "Test",
                    storeId = "MomoTestStore",
                    requestId = requestId,
                    amount = amount,
                    orderId = orderId,
                    orderInfo = orderInfo,
                    redirectUrl = returnUrl,
                    ipnUrl = NotifyUrl,
                    requestType = "captureWallet",
                    extraData = "",
                    lang = "vi",
                    signature = signature
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending payment request to MoMo: {RequestId}, {OrderId}, {Amount}", requestId, orderId, amount);

                var response = await _httpClient.PostAsync(ApiEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("MoMo API error: {StatusCode}, {Response}", response.StatusCode, responseContent);
                    return new MoMoPaymentResult
                    {
                        Success = false,
                        Message = $"MoMo API error: {response.StatusCode}"
                    };
                }

                var result = JsonConvert.DeserializeObject<MoMoApiResponse>(responseContent);

                if (result == null || result.resultCode != 0)
                {
                    _logger.LogError("MoMo payment creation failed: {Message}", result?.message ?? "Unknown error");
                    return new MoMoPaymentResult
                    {
                        Success = false,
                        Message = result?.message ?? "Payment creation failed"
                    };
                }

                _logger.LogInformation("MoMo payment created successfully: {PaymentUrl}", result.payUrl);

                return new MoMoPaymentResult
                {
                    Success = true,
                    PaymentUrl = result.payUrl,
                    QrCode = result.qrCode,
                    Message = result.message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MoMo payment");
                return new MoMoPaymentResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public bool VerifySignature(string rawData, string signature)
        {
            var computedSignature = ComputeHmacSha256(rawData, SecretKey);
            return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }

}

