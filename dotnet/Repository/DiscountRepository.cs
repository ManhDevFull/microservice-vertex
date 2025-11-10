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


        public async Task<Discount?> getDiscountByIdProduct(int id) // lấy giảm giá theo mã sản phẩm
        {
            var result = await (from p in _connect.products
                                where p.id == id
                                join v in _connect.variants on p.id equals v.productid // nối bảng variant

                                join dp in _connect.discountProducts on v.id equals dp.variantid into dpj//nối bảng discoutn product
                                from dp in dpj.DefaultIfEmpty()

                                join d in _connect.discounts on dp.discountid equals d.id into dj //nối bảng discount
                                from d in dj.DefaultIfEmpty()

                                select d
                               ).FirstOrDefaultAsync();
            return result;
        }

        public async Task<Dictionary<int, Discount?>> getDiscountByIdProducts(List<int> ids) // lấy danh sách varaint theo danh sách idproducts
        {
            if (ids == null)
                return new Dictionary<int, Discount?>();
            var result = await (
               from p in _connect.products
               where ids.Contains(p.id)
               join v in _connect.variants on p.id equals v.productid
               join dp in _connect.discountProducts on v.id equals dp.variantid into dpj
               from dp in dpj.DefaultIfEmpty()
               join d in _connect.discounts on dp.discountid equals d.id into dj
               from d in dj.DefaultIfEmpty()
               group d by p.id into g
               select new
               {
                   productId = g.Key,
                   discount = g.FirstOrDefault() 
               }
           ).ToDictionaryAsync(x => x.productId, x => x.discount);
            return result;
        }
    }
}