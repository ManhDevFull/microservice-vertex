using be.Service.IService;
using be_dotnet_ecommerce1.Dtos;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options; 

namespace be_dotnet_ecommerce1.Service
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        public PhotoService(IOptions<CloudinarySettings> config)
        {
            // Tạo tài khoản từ config
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(account); // Khởi tạo Cloudinary
        }

        public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();
            if (file.Length > 0)
            {
                // Mở luồng đọc file
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation()
                        .Height(150).Width(150).Crop("fill").Gravity("face")
                };

                // Gọi API Cloudinary
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            return uploadResult;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string publicId)
        {
            // Logic xóa (nếu cần)
            var deleteParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deleteParams);
        }
             public async Task<DeletionResult> DeletePhotoByUrlAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return new DeletionResult { Result = "Url rỗng hoặc null" };
            }

            string publicId;
            try
            {
                // 1. Phân tích chuỗi URL
                var uri = new Uri(imageUrl);

                // 2. Lấy tên tệp tin từ đường dẫn (ví dụ: "abc123xyz.jpg")
                var fileName = Path.GetFileName(uri.AbsolutePath); 

                // 3. Lấy tên tệp tin không bao gồm phần mở rộng (ví dụ: "abc123xyz")
                // Đây chính là PublicId mà Cloudinary đã tạo ngẫu nhiên
                publicId = Path.GetFileNameWithoutExtension(fileName); 
            }
            catch (Exception ex)
            {
                // Lỗi nếu URL không hợp lệ
                return new DeletionResult { Result = $"Lỗi phân tích URL: {ex.Message}" };
            }

            if (string.IsNullOrEmpty(publicId))
            {
                return new DeletionResult { Result = "Không thể trích xuất PublicId từ URL" };
            }

            // 4. Gọi hàm xóa bằng PublicId đã trích xuất
            return await DeletePhotoAsync(publicId);
        }
    }
}