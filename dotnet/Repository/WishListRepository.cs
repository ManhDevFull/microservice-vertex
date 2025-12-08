using be_dotnet_ecommerce1.Data;
using dotnet.Dtos;
using dotnet.Model;
using dotnet.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace dotnet.Repository;

public class WishListRepository : IWishListRepository
{
    private readonly ConnectData _db;
    public WishListRepository(ConnectData db) { _db = db; }

    public async Task<List<WishListItemDTO>> GetAsync(int accountId)
    {
        var query = from w in _db.wishLists
                    join p in _db.products on w.productid equals p.id
                    where w.accountid == accountId && !p.isdeleted
                    select new WishListItemDTO
                    {
                        id = w.id,
                        productId = p.id,
                        productName = p.nameproduct,
                        imageUrl = (p.imageurls != null && p.imageurls.Length > 0)
                                    ? p.imageurls[0].TrimStart('/')
                                    : null,

                        minPrice = _db.variants
                            .Where(v => v.product_id == p.id && !v.isdeleted)
                            .Select(v => (decimal?)v.price)
                            .OrderBy(x => x)
                            .FirstOrDefault() ?? 0m,
                        totalStock = _db.variants
                            .Where(v => v.product_id == p.id && !v.isdeleted)
                            .Select(v => (int?)v.stock)
                            .Sum() ?? 0
                    };
        return await query.ToListAsync();
    }

    public async Task<List<WishListItemDTO>> AddAsync(int accountId, int productId)
    {
        var exists = await _db.wishLists.AnyAsync(x => x.accountid == accountId && x.productid == productId);
        var productExists = await _db.products.AnyAsync(p => p.id == productId && !p.isdeleted);
        if (!productExists) return await GetAsync(accountId);

        if (!exists)
        {
            await _db.wishLists.AddAsync(new WishList { accountid = accountId, productid = productId });
            await _db.SaveChangesAsync();
        }
        return await GetAsync(accountId);
    }

    public async Task<List<WishListItemDTO>> RemoveAsync(int accountId, int productId)
    {
        var item = await _db.wishLists.FirstOrDefaultAsync(x => x.accountid == accountId && x.productid == productId);
        if (item != null)
        {
            _db.wishLists.Remove(item);
            await _db.SaveChangesAsync();
        }
        return await GetAsync(accountId);
    }
}
