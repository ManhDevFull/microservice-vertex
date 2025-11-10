using dotnet.Model;

namespace be_dotnet_ecommerce1.Repository.IRepository
{
    public interface IDiscountRepository
    {
        public Task<Dictionary<int, Discount?>> getDiscountByIdProducts(List<int> ids);
        public Task<Discount?> getAllDiscount();
    }
}