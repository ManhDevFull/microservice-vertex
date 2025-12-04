using CloudinaryDotNet.Actions; 

namespace be.Service.IService 
{
    public interface IPhotoService
    {

        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);

        Task<DeletionResult> DeletePhotoAsync(string publicId);
<<<<<<< HEAD
        Task<DeletionResult> DeletePhotoByUrlAsync(string oldAvatarUrl);
=======
>>>>>>> user
    }
}