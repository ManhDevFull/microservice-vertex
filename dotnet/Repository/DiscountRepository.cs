using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Repository.IRepository;
using dotnet.Model;
using Microsoft.EntityFrameworkCore;

namespace be_dotnet_ecommerce1.Repository
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly ConnectData _connect;
        public DiscountRepository(ConnectData connect)
        {
            _connect = connect;
        }

        public Task<Discount?> getAllDiscount()
        {
            throw new NotImplementedException();
        }
    }
}